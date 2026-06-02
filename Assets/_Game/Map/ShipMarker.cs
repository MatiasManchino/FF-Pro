using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Systems.Maritime;
using UnityEngine;
using UnityEngine.Rendering;

namespace FreightForwarder.Map
{
    public class ShipMarker : MonoBehaviour
    {
// Gestiona shipment.
        public MaritimeShipment Shipment { get; private set; }

        private Transform  _earth;
        private float      _denom;
        private float      _r;
        private GameObject _routeLineGO;
        private Vector3    _bowAxis;
        private Vector3    _deckAxis;
        private Quaternion _lastValidRotation = Quaternion.identity;
        private bool       _hasValidRotation;
        private bool       _rotationInitialized;

        // Higher = snappier turns. Fotograma-rate independent (used as 1 - exp(-k·dt)).
        private const float RotationSmoothing = 6f;

        private static Material _matLine;
        private static readonly Dictionary<string, ShipMarker> _registry = new();

        // Devuelve el marcador (barco) de una carga, si existe en el mapa.
        public static bool TryGetMarker(string cargoId, out ShipMarker marker)
            => _registry.TryGetValue(cargoId, out marker);

// Crea
        public static ShipMarker Create(MaritimeShipment shipment)
        {
            if (shipment == null)
            {
                Debug.LogWarning("[ShipMarker] Create called with null shipment — skipping.");
                return null;
            }

            GameObject go;
            if (WorldMap.Instance?.shipPrefab != null)
            {
                go = Instantiate(WorldMap.Instance.shipPrefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(go.GetComponent<SphereCollider>());
                var rend = go.GetComponent<MeshRenderer>();
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows    = false;
            }
            go.name = $"Ship_{shipment.CargoId}";

            var m = go.AddComponent<ShipMarker>();
            m.Shipment = shipment;
            _registry[shipment.CargoId] = m;
            return m;
        }

        private void OnDestroy()
        {
            if (Shipment != null && _registry.TryGetValue(Shipment.CargoId, out var existing) && existing == this)
                _registry.Remove(Shipment.CargoId);
        }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (WorldMap.Instance == null) { Destroy(gameObject); return; }

            _denom = WorldMap.Instance.earthRadius * 2f;
            _r     = WorldMap.Instance.earthRadius + 5f;
            _earth = WorldMap.Instance.transform;
            transform.SetParent(_earth, false);

            DetectOrientationAxes();
            BuildRouteLine();
            UpdateVisual();
        }

        // Auto-derives bow and deck axes in model local space. Bow comes from the longest bounds axis;
        // deck (up) comes from the hull-bottom mesh (Object_19) position relative to the ship centre.
        // The Object_NN transforms all sit at the model origin, so only their MESH geometry is useful.
        private void DetectOrientationAxes()
        {
            Transform bow   = FindChildNamed(transform, "Object_30");
            Transform stern = FindChildNamed(transform, "Object_29");

            PrintReferenceObjectInfo("Object_30 (bow)",   bow);
            PrintReferenceObjectInfo("Object_29 (stern)", stern);

            if (bow != null && stern != null)
            {
                Vector3 bowLocal   = transform.InverseTransformPoint(bow.position);
                Vector3 sternLocal = transform.InverseTransformPoint(stern.position);
                Vector3 axis       = bowLocal - sternLocal;

                Debug.LogWarning($"[ShipMarker] bowLocal={bowLocal:F4}  sternLocal={sternLocal:F4}  delta={axis:F4}  sqrMag={axis.sqrMagnitude:F6}");

                if (axis.sqrMagnitude > 0.0001f)
                {
                    _bowAxis = axis.normalized;
                    Debug.LogWarning($"[ShipMarker] OK bow axis from reference objects: {_bowAxis}");
                }
                else
                {
                    Debug.LogWarning("[ShipMarker] Object_29 y Object_30 estan en la misma posicion — ambos en el origen del modelo en Blender. Usando bounds.");
                    _bowAxis = DetectBowAxisFromBounds();
                }
            }
            else
            {
                Debug.LogWarning("[ShipMarker] Object_30 / Object_29 no encontrados en la jerarquia. Usando bounds.");
                _bowAxis = DetectBowAxisFromBounds();
            }

            // Deck (up) axis. Auto-detect from the hull-bottom mesh (Object_19) FIRST: that piece must
            // face the Earth's centre, so (shipCentre − hullCentre) is the model's true up. This now
            // runs regardless of the inspector value, so a stale shipDeckAxis (e.g. the old (0,1,0)
            // default = the model's SIDE) can no longer force the ship onto its side. The inspector
            // is only a fallback for when Object_19 can't be measured.
            _deckAxis = DetectDeckAxisFromHull("Object_19");
            if (_deckAxis.sqrMagnitude > 0.01f)
            {
                Debug.LogWarning($"[ShipMarker] Deck axis AUTO desde casco (Object_19): {_deckAxis}");
            }
            else
            {
                Vector3 inspectorDeck = WorldMap.Instance.shipDeckAxis;
                _deckAxis = inspectorDeck.sqrMagnitude > 0.01f ? inspectorDeck.normalized : Vector3.up;
                Debug.LogWarning($"[ShipMarker] Object_19 no medible — deck axis FALLBACK: {_deckAxis} " +
                                 "(mirá el log 'Hijos reales' de arriba).");
            }

            Debug.LogWarning($"[ShipMarker] EJES FINALES — bow={_bowAxis}  deck={_deckAxis}  shipBowAxis inspector={WorldMap.Instance.shipBowAxis}");
        }

// Imprime reference object info.
        private void PrintReferenceObjectInfo(string label, Transform t)
        {
            if (t == null)
            {
                Debug.LogWarning($"[ShipMarker] {label}: NO ENCONTRADO en la jerarquia");
                return;
            }
            Vector3 local = transform.InverseTransformPoint(t.position);
            Debug.LogWarning($"[ShipMarker] {label}: world={t.position:F3}  localInShip={local:F4}");
        }

        // Finds the longest dimension of the combined mesh bounds in local space.
        // For a ship model that is longer than it is wide/tall, this gives the bow/stern axis.
        private Vector3 DetectBowAxisFromBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[ShipMarker] No renderers found — cannot auto-detect bow axis. " +
                                 "Set GameBootstrapper.shipBowAxis to a normalized direction like (1,0,0) or (0,0,1).");
                return WorldMap.Instance.shipBowAxis.sqrMagnitude > 0.01f
                    ? WorldMap.Instance.shipBowAxis.normalized
                    : Vector3.forward;
            }

            // Accumulate all 8 corners of each renderer's world AABB into local space.
            Vector3 localMin =  Vector3.one * float.MaxValue;
            Vector3 localMax = -Vector3.one * float.MaxValue;

// Foreach
            foreach (var r in renderers)
            {
                Bounds wb = r.bounds;
                // Iterate the 8 corners of the world-space AABB.
                for (int ix = 0; ix <= 1; ix++)
                for (int iy = 0; iy <= 1; iy++)
                for (int iz = 0; iz <= 1; iz++)
                {
                    Vector3 corner = new Vector3(
                        ix == 0 ? wb.min.x : wb.max.x,
                        iy == 0 ? wb.min.y : wb.max.y,
                        iz == 0 ? wb.min.z : wb.max.z);
                    Vector3 local = transform.InverseTransformPoint(corner);
                    localMin = Vector3.Min(localMin, local);
                    localMax = Vector3.Max(localMax, local);
                }
            }

            Vector3 size = localMax - localMin;
            float ax = Mathf.Abs(size.x);
            float ay = Mathf.Abs(size.y);
            float az = Mathf.Abs(size.z);

            Vector3 axis;
            if (ax >= ay && ax >= az) axis = Vector3.right;
            else if (az >= ay)        axis = Vector3.forward;
            else                      axis = Vector3.up;

            Debug.LogWarning($"[ShipMarker] BOUNDS local: size={size:F3}  bow axis detectado={axis}  (X={ax:F2} Y={ay:F2} Z={az:F2})");
            return axis;
        }

        // Derives the deck (up) axis from the hull-bottom mesh. The named object (Object_19) is the
        // part that rests on the water; its mesh centre sits below the ship's centre, so the vector
        // ship-centre − hull-centre points up out of the deck. The model is axis-aligned, so the
        // raw resultado indica si snapped to the nearest cardinal axis. Devuelve model-local up, or zero if
        // Object_19 is missing (the real child names are logged) / perfectly centred.
        private Vector3 DetectDeckAxisFromHull(string hullObjectName)
        {
            Transform hull = FindChildNamed(transform, hullObjectName);
            if (hull == null)
            {
                var names = new System.Text.StringBuilder();
// Foreach
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    names.Append(t.name).Append("  ");
                Debug.LogWarning($"[ShipMarker] No encontré '{hullObjectName}'. Hijos reales: {names}");
                return Vector3.zero;
            }

            var hullRenderers = hull.GetComponentsInChildren<Renderer>(true);
            var allRenderers  = GetComponentsInChildren<Renderer>(true);
            if (hullRenderers.Length == 0 || allRenderers.Length == 0)
            {
                Debug.LogWarning($"[ShipMarker] '{hullObjectName}' sin Renderers (hull={hullRenderers.Length}, all={allRenderers.Length}).");
                return Vector3.zero;
            }

            Bounds shipBounds = CombinedBoundsWorld(allRenderers);
            Bounds hullBounds = CombinedBoundsWorld(hullRenderers);

            // Keel sits below the centre, so (shipCentre − hullCentre) points up out of the deck.
            // Convert both centres to model-local first, then take the difference.
            Vector3 localUp = transform.InverseTransformPoint(shipBounds.center)
                            - transform.InverseTransformPoint(hullBounds.center);

            Debug.LogWarning($"[ShipMarker] Hull '{hullObjectName}' localUpRaw={localUp:F4}");
            if (localUp.sqrMagnitude < 1e-10f) return Vector3.zero;

            Vector3 cardinal = SnapToCardinal(localUp);
            Debug.LogWarning($"[ShipMarker] Deck/up auto = {cardinal} (quilla Object_19 → centro de la Tierra).");
            return cardinal;
        }

        // The model's bounds are axis-aligned, so the true up is one of ±X/±Y/±Z. Pick the dominant.
        private static Vector3 SnapToCardinal(Vector3 v)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            if (ax >= ay && ax >= az) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (ay >= az)             return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

// Gestiona combined bounds mundo.
        private static Bounds CombinedBoundsWorld(Renderer[] renderers)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

// Busca hijo named
        private static Transform FindChildNamed(Transform root, string childName)
        {
// Foreach
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
        private void Update()
        {
            if (Shipment == null || Shipment.IsCompleted)
            {
                if (_routeLineGO != null) Destroy(_routeLineGO);
                Destroy(gameObject);
                return;
            }
            UpdateVisual();
        }

// Construye ruta line.
        private void BuildRouteLine()
        {
            if (Shipment.Waypoints == null || Shipment.Waypoints.Length < 2) return;

            _routeLineGO = new GameObject($"Route_{Shipment.CargoId}");
            _routeLineGO.transform.SetParent(_earth, false);

            var lr = _routeLineGO.AddComponent<LineRenderer>();
            lr.useWorldSpace     = false;
            lr.loop              = false;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            float rLine = (WorldMap.Instance.earthRadius + 1.5f) / _denom;
            lr.positionCount = Shipment.Waypoints.Length;
            for (int i = 0; i < Shipment.Waypoints.Length; i++)
            {
                Vector3 dir = MaritimeRouteDatabase.LatLonToLocalDir(
                    Shipment.Waypoints[i].x, Shipment.Waypoints[i].y);
                lr.SetPosition(i, dir * rLine);
            }

            lr.startWidth = Constants.ROUTE_LINE_WIDTH;
            lr.endWidth   = Constants.ROUTE_LINE_WIDTH;

            if (_matLine == null)
                _matLine = new Material(Shader.Find("Unlit/Color"))
                    { color = new Color(0.15f, 0.65f, 1f, 1f) };
            lr.material       = _matLine;
            lr.numCapVertices = 2;
        }

        // Maps the model fotograma (bow, deck) onto the mundo fotograma (travel tangent, surface normal):
        // resultado · bowAxis  → travelDir   (bow points exactly along the ruta)
        // resultado · deckAxis → dir         (deck rests flat on the curved surface)
        // bowAxis / deckAxis are in model local space; dir / travelDir are in Earth local space.
        // travelDir   == 0 (stopped)  → upright pose facing local north.
        // deckAxis    == 0 (unknown)  → assume the model's vertical is world-up.
        // rollDegrees != 0            → extra roll around the travel axis (cosmetic tuning).
        public static Quaternion OrientToRoute(Vector3 dir, Vector3 travelDir, Vector3 bowAxis, Vector3 deckAxis = default, float rollDegrees = 0f)
        {
            // The outward surface normal is the one direction we can always trust.
            Vector3 up = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.up;

            // The bow lives in the tangent plane, so strip any radial component from travelDir.
            Vector3 forward = Vector3.ProjectOnPlane(travelDir, up);
            if (forward.sqrMagnitude < 1e-8f)
            {
                // Stopped: face local north along the surface (north pole = +Y in this convention).
                forward = Vector3.ProjectOnPlane(Vector3.up, up);
                if (forward.sqrMagnitude < 1e-8f)        // sitting exactly on a pole
                    forward = Vector3.ProjectOnPlane(Vector3.forward, up);
            }
            forward.Normalize();

            // Objetivo world basis: forward = travel tangent, up = surface normal (already orthogonal,
            // so LookRotation honours both exactly).
            Quaternion worldRot = Quaternion.LookRotation(forward, up);

            // Model basis: bow plays the role of "forward", deck the role of "up".
            bowAxis = bowAxis.sqrMagnitude > 1e-8f ? bowAxis.normalized : Vector3.forward;
            Vector3 deck = deckAxis.sqrMagnitude > 1e-4f ? deckAxis.normalized : DefaultDeckAxis(bowAxis);
            // Keep deck perpendicular to bow so LookRotation can't go degenerate (deck ∥ bow).
            deck = Vector3.ProjectOnPlane(deck, bowAxis);
            if (deck.sqrMagnitude < 1e-4f) deck = DefaultDeckAxis(bowAxis);
            deck.Normalize();
            Quaternion modelRot = Quaternion.LookRotation(bowAxis, deck);

            // worldRot · modelRot⁻¹ rotates the model fotograma onto the mundo fotograma.
            Quaternion result = worldRot * Quaternion.Inverse(modelRot);

            // Optional manual roll around the travel/length axis (fixes models that sit on their side).
            if (Mathf.Abs(rollDegrees) > 0.001f)
                result = Quaternion.AngleAxis(rollDegrees, forward) * result;

            return result;
        }

        // Fallback when the deck (vertical) axis is unknown: take world-up, made perpendicular
        // to the bow. Si a ship ever looks rolled onto its side, feed a real deck axis instead.
        private static Vector3 DefaultDeckAxis(Vector3 bowAxis)
        {
            Vector3 deck = Vector3.ProjectOnPlane(Vector3.up, bowAxis);
            if (deck.sqrMagnitude < 1e-4f)               // bow is (nearly) vertical
                deck = Vector3.ProjectOnPlane(Vector3.forward, bowAxis);
            return deck.normalized;
        }

        // Remapea el progreso temporal (0→1) para que el barco quede DETENIDO (progreso fijo) durante
        // los días de operación en origen (dO) y destino (dD), y se mueva normalmente en el medio.
        private static float DwellRemap(float p, float dO, float dD)
        {
            float travel = 1f - dO - dD;
            if (travel <= 0.0001f) return Mathf.Clamp01(p);
            if (p <= dO)        return 0f;
            if (p >= 1f - dD)   return 1f;
            return (p - dO) / travel;
        }

// Actualiza visual
        private void UpdateVisual()
        {
            // Posición progress. DaysElapsed steps a whole day at a time, which teleports the ship
            // once per game-day. Añade the fraction of the current day (DayProgress, continuous) so it
            // glides smoothly along the ruta. DayProgress indica si read straight from TimeManager because
            // it is updated in the same Actualiza that bumps DaysElapsed, so the pair stays consistent
            // and progress never overshoots at a día boundary. Falls regreso to whole-día stepping if the
            // clock isn't present.
            float dayFraction = TimeManager.Instance != null ? TimeManager.Instance.DayProgress : 0f;
            float progress = Shipment.TotalTTDays > 0
                ? Mathf.Clamp01((Shipment.DaysElapsed + dayFraction) / Shipment.TotalTTDays)
                : 0f;
            // Días operando detenido en puerto de origen/destino: el barco no se mueve durante esos días.
            float dwellFrac = Shipment.TotalTTDays > 0
                ? (float)Constants.PORT_OPERATION_DAYS / Shipment.TotalTTDays
                : 0f;
            progress = DwellRemap(progress, dwellFrac, dwellFrac);
            Vector2 pos2d  = Shipment.GetCurrentPosition(progress);
            Vector3 dir    = MaritimeRouteDatabase.LatLonToLocalDir(pos2d.x, pos2d.y);

            transform.localPosition = dir * (_r / _denom);
            transform.localScale    = Vector3.one * (WorldMap.Instance?.shipScale ?? 0.00005f);

            // Tangent of travel: sample a hair further along the ruta and project onto the surface.
            const float eps = 0.001f;
            bool  atEnd   = progress >= 1f - eps;
            float pSample = atEnd ? progress - eps : progress + eps;
            Vector2 samplePos = Shipment.GetCurrentPosition(pSample);
            Vector3 dirSample = MaritimeRouteDatabase.LatLonToLocalDir(samplePos.x, samplePos.y);

            Vector3 travelDir = Vector3.ProjectOnPlane(dirSample - dir, dir);
            if (atEnd) travelDir = -travelDir;

            float roll = WorldMap.Instance != null ? WorldMap.Instance.shipRollOffset : 0f;

            Quaternion target;
            if (travelDir.sqrMagnitude > 1e-7f)
            {
                target = OrientToRoute(dir, travelDir, _bowAxis, _deckAxis, roll);
                _lastValidRotation = target;
                _hasValidRotation  = true;
            }
            else
            {
                // Stopped or coincident waypoints: hold the last good heading,
                // or fall regreso to an upright, north-facing pose on first genera.
                target = _hasValidRotation
                    ? _lastValidRotation
                    : OrientToRoute(dir, Vector3.zero, _bowAxis, _deckAxis, roll);
            }

            // Snap on the first fotograma (avoid swinging in from identity), then ease afterwards.
            if (_rotationInitialized)
            {
                float t = 1f - Mathf.Exp(-RotationSmoothing * Time.deltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target, t);
            }
            else
            {
                transform.localRotation = target;
                _rotationInitialized = true;
            }
        }
    }
}