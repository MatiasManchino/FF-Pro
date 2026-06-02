using FreightForwarder.Models;
using FreightForwarder.Systems.Maritime;
using UnityEngine;
using UnityEngine.Rendering;

namespace FreightForwarder.Map
{
    // Renders a moving vehicle on the globe for Air, Land, and Rail cargos.
    public class TransportMarker : MonoBehaviour
    {
// Cargamento.
        public Cargo Cargo { get; private set; }

        private Transform _earth;
        private float     _denom;
        private Vector3   _originDir;
        private Vector3   _destDir;
        private float     _arcHeight;            // altura del arco aéreo (sube en el medio)
        private GameObject _routeArcGO;          // línea naranja de la ruta aérea
        private static Material _airRouteMat;
        private System.DateTime _startUtc;       // momento de aceptación (progreso = 0 en el origen)
        private System.DateTime _endUtc;         // medianoche objetivo de entrega (progreso = 1 en destino)
        private bool      _timing;

// Crea
        public static TransportMarker Create(Cargo cargo)
        {
            if (WorldMap.Instance == null) return null;

            GameObject prefab = GetPrefabForMode(cargo.TransportMode);
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<BoxCollider>());
                var rend = go.GetComponent<MeshRenderer>();
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows    = false;
            }
            go.name = $"{cargo.TransportMode}_{cargo.Id[..6]}";

            var m = go.AddComponent<TransportMarker>();
            m.Cargo = cargo;
            return m;
        }

// Obtiene prefab for mode
        private static GameObject GetPrefabForMode(Constants.TransportMode mode)
        {
            if (WorldMap.Instance == null) return null;
            return mode switch
            {
                Constants.TransportMode.Air  => WorldMap.Instance.planePrefab,
                Constants.TransportMode.Land => WorldMap.Instance.truckPrefab,
                Constants.TransportMode.Rail => WorldMap.Instance.trainPrefab,
                _                            => null
            };
        }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (WorldMap.Instance == null || Cargo == null) { Destroy(gameObject); return; }

            if (CityDatabase.AllCities == null
                || !CityDatabase.AllCities.TryGetValue(Cargo.OriginCityId, out WorldCity origin)
                || !CityDatabase.AllCities.TryGetValue(Cargo.DestinationCityId, out WorldCity dest))
            {
                Debug.LogWarning($"[TransportMarker] Ciudades no encontradas: {Cargo.OriginCityId} → {Cargo.DestinationCityId}");
                Destroy(gameObject);
                return;
            }

            _denom      = WorldMap.Instance.earthRadius * 2f;
            _earth      = WorldMap.Instance.transform;
            _originDir  = MaritimeRouteDatabase.LatLonToLocalDir(origin.Latitude, origin.Longitude);
            _destDir    = MaritimeRouteDatabase.LatLonToLocalDir(dest.Latitude, dest.Longitude);

            // Arco aéreo: sube en el medio, proporcional a la distancia angular del vuelo.
            _arcHeight  = WorldMap.Instance.earthRadius * 0.20f
                        * Mathf.Clamp01(Vector3.Angle(_originDir, _destDir) / 180f);

            // Anclar el progreso al momento real de aceptación (no a la hora del día):
            // arranca en 0 (aeropuerto de origen) y llega a 1 al entregarse (medianoche objetivo).
            if (TimeManager.Instance != null)
            {
                _startUtc = TimeManager.Instance.CurrentUtcTime;
                _endUtc   = _startUtc.Date.AddDays(Mathf.Max(1, Cargo.TotalTransitDays));
                _timing   = true;
            }

            transform.SetParent(_earth, false);
            if (Cargo.TransportMode == Constants.TransportMode.Air) BuildAirRouteArc();
            UpdateVisual();
        }

        private void OnDestroy()
        {
            if (_routeArcGO != null) Destroy(_routeArcGO);
        }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
        private void Update()
        {
            if (Cargo == null || Cargo.IsCompleted()) { Destroy(gameObject); return; }
            UpdateVisual();
        }

// Actualiza visual
        private void UpdateVisual()
        {
            // Progreso anclado al momento de aceptación → arranca en el origen (0), fluido, y llega a 1 en destino.
            float progress;
            if (_timing && _endUtc > _startUtc && TimeManager.Instance != null)
            {
                double total   = (_endUtc - _startUtc).TotalSeconds;
                double elapsed = (TimeManager.Instance.CurrentUtcTime - _startUtc).TotalSeconds;
                progress = total > 0.0 ? Mathf.Clamp01((float)(elapsed / total)) : 0f;
            }
            else
            {
                float dayFrac     = TimeManager.Instance != null ? TimeManager.Instance.DayProgress : 0f;
                float elapsedDays = (Cargo.TotalTransitDays - Cargo.DaysRemaining) + dayFrac;
                progress = Cargo.TotalTransitDays > 0 ? Mathf.Clamp01(elapsedDays / Cargo.TotalTransitDays) : 0f;
            }

            // Días operando detenido en terminal de origen/destino: el avión no se mueve durante esos días.
            float dwellFrac = Cargo.TotalTransitDays > 0
                ? (float)Constants.TERMINAL_OPERATION_DAYS / Cargo.TotalTransitDays
                : 0f;
            progress = DwellRemap(progress, dwellFrac, dwellFrac);

            Vector3 dir = Vector3.Slerp(_originDir, _destDir, progress);

            // El avión: arranca/termina al ras del aeropuerto (≈ punto de la ciudad) y sube en el arco.
            float elevation = Cargo.TransportMode == Constants.TransportMode.Air
                ? 2f + _arcHeight * Mathf.Sin(Mathf.PI * progress)
                : 5f;
            float r = WorldMap.Instance.earthRadius + elevation;
            if (Cargo.TransportMode == Constants.TransportMode.Air)
                r *= 1.01f;   // el avión vuela 1% por encima de su línea de ruta
            transform.localPosition = dir * (r / _denom);

            float scale = Cargo.TransportMode switch
            {
                Constants.TransportMode.Air  => WorldMap.Instance.planeScale,
                Constants.TransportMode.Land => WorldMap.Instance.truckScale,
                Constants.TransportMode.Rail => WorldMap.Instance.trainScale,
                _                            => 0.00005f
            };
            transform.localScale = Vector3.one * scale;

            // Bow points in direction of travel
            const float eps = 0.001f;
            bool  atEnd   = progress >= 1f - eps;
            float pSample = atEnd ? progress - eps : progress + eps;
            Vector3 dirSample = Vector3.Slerp(_originDir, _destDir, pSample);
            Vector3 travelDir = Vector3.ProjectOnPlane(dirSample - dir, dir).normalized;
            if (atEnd) travelDir = -travelDir;

            Vector3 bowAxis = Cargo.TransportMode switch
            {
                Constants.TransportMode.Air  => WorldMap.Instance.planeBowAxis,
                Constants.TransportMode.Land => WorldMap.Instance.truckBowAxis,
                Constants.TransportMode.Rail => WorldMap.Instance.trainBowAxis,
                _                            => Vector3.forward
            };
            // Para el avión usamos el eje "arriba" del modelo (panza hacia la Tierra); el resto, auto.
            Vector3 deckAxis = Cargo.TransportMode == Constants.TransportMode.Air
                ? WorldMap.Instance.planeDeckAxis
                : default;
            Quaternion baseRot = ShipMarker.OrientToRoute(dir, travelDir, bowAxis, deckAxis);
            if (Cargo.TransportMode == Constants.TransportMode.Air)
            {
                // Nariz arriba al subir, abajo al bajar (pitch ∝ tasa de ascenso del arco).
                float maxPitch = Mathf.Clamp(_arcHeight * 0.25f, 0f, 22f);
                // En tierra (operando) va nivelado; solo cabecea mientras vuela.
                float pitchDeg = (progress > 0.0001f && progress < 0.9999f)
                    ? maxPitch * Mathf.Cos(Mathf.PI * progress)
                    : 0f;
                Vector3 wingAxis = Vector3.Cross(travelDir, dir).normalized;
                baseRot = Quaternion.AngleAxis(pitchDeg, wingAxis) * baseRot;
            }
            transform.localRotation = baseRot;
        }

        // Remapea el progreso temporal (0→1) para que el vehículo quede DETENIDO (progreso fijo) durante
        // los días de operación en origen (dO) y destino (dD), y se mueva normalmente en el medio.
        private static float DwellRemap(float p, float dO, float dD)
        {
            float travel = 1f - dO - dD;
            if (travel <= 0.0001f) return Mathf.Clamp01(p);
            if (p <= dO)        return 0f;
            if (p >= 1f - dD)   return 1f;
            return (p - dO) / travel;
        }

        // Dibuja la ruta aérea como un arco NARANJA de gran círculo que sube en el medio (semicírculo).
        private void BuildAirRouteArc()
        {
            _routeArcGO = new GameObject($"AirRoute_{Cargo.Id[..6]}");
            _routeArcGO.transform.SetParent(_earth, false);

            var lr = _routeArcGO.AddComponent<LineRenderer>();
            lr.useWorldSpace     = false;
            lr.loop              = false;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            const int segs = 48;
            lr.positionCount = segs + 1;
            float baseR = WorldMap.Instance.earthRadius + 2f;   // arranca/termina al ras (punto de la ciudad)
            for (int i = 0; i <= segs; i++)
            {
                float t  = (float)i / segs;
                Vector3 dir = Vector3.Slerp(_originDir, _destDir, t).normalized;
                float r  = (baseR + _arcHeight * Mathf.Sin(Mathf.PI * t)) / _denom;
                lr.SetPosition(i, dir * r);
            }

            lr.startWidth = lr.endWidth = Constants.ROUTE_LINE_WIDTH;
            if (_airRouteMat == null)
                _airRouteMat = new Material(Shader.Find("Unlit/Color"))
                    { color = new Color(1f, 0.55f, 0.1f, 1f) };   // naranja
            lr.material       = _airRouteMat;
            lr.numCapVertices = 2;
        }
    }
}