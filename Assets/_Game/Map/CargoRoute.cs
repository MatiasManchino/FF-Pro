using FreightForwarder.Managers;
using FreightForwarder.Models;
using static FreightForwarder.Models.Constants;
using UnityEngine;

namespace FreightForwarder.Map
{
    /// <summary>
    /// Dibuja en el globo el arco de una carga activa y anima un punto a lo largo de él.
    /// Creado/destruido por RouteManager.
    /// </summary>
    public class CargoRoute : MonoBehaviour
    {
        public string CargoId { get; private set; }

        private const int SEGMENTS = 80;

        private Vector3[]    _dirs;
        private float[]      _radii;
        private Vector3[]    _worldPos;
        private LineRenderer _line;
        private Transform    _dot;
        private bool         _ready;

        // ── Init ─────────────────────────────────────────────────────────────
        public void Initialize(string cargoId, WorldCity origin, WorldCity dest, TransportMode mode)
        {
            CargoId = cargoId;
            BuildArc(origin, dest, mode);
            CreateVisuals(mode);
            _ready = true;
        }

        // ── Arc construction ─────────────────────────────────────────────────
        private void BuildArc(WorldCity origin, WorldCity dest, TransportMode mode)
        {
            _dirs    = new Vector3[SEGMENTS + 1];
            _radii   = new float  [SEGMENTS + 1];
            _worldPos = new Vector3[SEGMENTS + 1];

            Vector3 a = LatLonToDir(origin.Latitude, origin.Longitude);
            Vector3 b = LatLonToDir(dest.Latitude,   dest.Longitude);

            float groundR = 1000f;
            float peakR   = PeakRadius(mode);
            bool  hasArc  = mode == TransportMode.Air || mode == TransportMode.Multimodal;

            for (int i = 0; i <= SEGMENTS; i++)
            {
                float t  = (float)i / SEGMENTS;
                _dirs[i] = Vector3.Slerp(a, b, t).normalized;
                _radii[i] = hasArc
                    ? groundR + (peakR - groundR) * Mathf.Sin(t * Mathf.PI)
                    : peakR;
            }
        }

        private static float PeakRadius(TransportMode mode) => mode switch
        {
            TransportMode.Air        => 1060f,
            TransportMode.Maritime   => 998.5f,
            TransportMode.Land       => 999.5f,
            TransportMode.Rail       => 999.5f,
            TransportMode.Multimodal => 1035f,
            _                        => 999f,
        };

        private static Color ModeColor(TransportMode mode) => mode switch
        {
            TransportMode.Air        => new Color(0.2f,  0.5f,  1.0f),
            TransportMode.Maritime   => new Color(1.0f,  0.5f,  0.05f),
            TransportMode.Land       => new Color(0.2f,  0.85f, 0.3f),
            TransportMode.Rail       => new Color(0.9f,  0.8f,  0.1f),
            TransportMode.Multimodal => new Color(0.7f,  0.3f,  1.0f),
            _                        => Color.white,
        };

        private static float LineWidth(TransportMode mode) => mode switch
        {
            TransportMode.Air      => 5f,
            TransportMode.Maritime => 4f,
            _                      => 3f,
        };

        private static float DotSize(TransportMode mode) => mode switch
        {
            TransportMode.Air      => 25f,
            TransportMode.Maritime => 22f,
            _                      => 18f,
        };

        // ── Visuals ──────────────────────────────────────────────────────────
        private void CreateVisuals(TransportMode mode)
        {
            var color = ModeColor(mode);

            var lineGO = new GameObject("RouteLine");
            _line = lineGO.AddComponent<LineRenderer>();
            _line.useWorldSpace        = true;
            _line.positionCount        = SEGMENTS + 1;
            _line.startWidth           = LineWidth(mode);
            _line.endWidth             = LineWidth(mode);
            _line.generateLightingData = false;
            _line.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows       = false;
            var lineMat  = new Material(Shader.Find("Unlit/Color"));
            lineMat.color = color;
            _line.sharedMaterial = lineMat;

            var dotGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotGO.name = "RouteDot";
            dotGO.transform.localScale = Vector3.one * DotSize(mode);
            Destroy(dotGO.GetComponent<SphereCollider>());
            var dotRend = dotGO.GetComponent<MeshRenderer>();
            var dotMat  = new Material(Shader.Find("Unlit/Color"));
            dotMat.color = color;
            dotRend.sharedMaterial    = dotMat;
            dotRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dotRend.receiveShadows    = false;
            _dot = dotGO.transform;
        }

        // ── Update ───────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_ready || WorldMap.Instance == null) return;

            for (int i = 0; i <= SEGMENTS; i++)
                _worldPos[i] = DirToWorld(_dirs[i], _radii[i]);
            _line.SetPositions(_worldPos);

            float progress = GetProgress();
            float indexF   = progress * SEGMENTS;
            int   ia       = Mathf.FloorToInt(indexF);
            int   ib       = Mathf.Min(ia + 1, SEGMENTS);
            float frac     = indexF - ia;

            Vector3 dir  = Vector3.Slerp(_dirs[ia], _dirs[ib], frac).normalized;
            float   r    = Mathf.Lerp(_radii[ia], _radii[ib], frac);
            _dot.position = DirToWorld(dir, r);
        }

        private float GetProgress()
        {
            var cargos = CargoManager.Instance?.ActiveCargos;
            if (cargos == null) return 0f;
            var cargo = cargos.Find(c => c.Id == CargoId);
            if (cargo == null || cargo.TotalTransitDays <= 0) return 0f;
            int   currentDay = FFTimeManager.Instance?.CurrentDay ?? cargo.StartDay;
            float dayFrac    = FFTimeManager.Instance?.DayProgress ?? 0f;
            float elapsed    = (currentDay - cargo.StartDay) + dayFrac;
            return Mathf.Clamp01(elapsed / cargo.TotalTransitDays);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static Vector3 LatLonToDir(float lat, float lon)
        {
            float latR = lat * Mathf.Deg2Rad;
            float lonR = lon * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(latR) * Mathf.Cos(lonR),
                Mathf.Sin(latR),
                Mathf.Cos(latR) * Mathf.Sin(lonR));
        }

        private static Vector3 DirToWorld(Vector3 dir, float radius)
        {
            float localR = radius / (WorldMap.Instance.earthRadius * 2f);
            return WorldMap.Instance.transform.TransformPoint(dir * localR);
        }

        // ── Cleanup ──────────────────────────────────────────────────────────
        private void OnDestroy()
        {
            if (_line != null) Destroy(_line.gameObject);
            if (_dot  != null) Destroy(_dot.gameObject);
        }
    }
}
