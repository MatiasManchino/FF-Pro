using FreightForwarder.Managers;
using FreightForwarder.Models;
using static FreightForwarder.Models.Constants;
using UnityEngine;

namespace FreightForwarder.Map
{
    /// <summary>
    /// Renders a cargo route on the 3D globe as a multi-segment LineRenderer
    /// and animates a dot along it. Waypoints (lat/lon) define the path;
    /// maritime/land routes hug the surface, air routes arc on each leg.
    /// Created/destroyed by RouteManager.
    /// </summary>
    public class CargoRoute : MonoBehaviour
    {
        public string CargoId { get; private set; }

        private const int SEGMENTS = 120;

        private Vector3[]    _dirs;
        private float[]      _radii;
        private Vector3[]    _worldPos;
        private LineRenderer _line;
        private Transform    _dot;
        private bool         _ready;
        private Material     _lineMat;
        private Material     _dotMat;
        private Cargo        _cargo;
        private float        _earthDenominator;
        private Transform    _earthTransform;

        // ── Init ─────────────────────────────────────────────────────────────────
        public void Initialize(string cargoId, Vector2[] waypoints, TransportMode mode, Cargo cargo)
        {
            CargoId = cargoId;
            _cargo  = cargo;
            if (WorldMap.Instance != null)
            {
                _earthDenominator = WorldMap.Instance.earthRadius * 2f;
                _earthTransform   = WorldMap.Instance.transform;
            }
            else
            {
                _earthDenominator = 2000f;
            }
            BuildArc(waypoints, mode);
            CreateVisuals(mode);
            _ready = true;
        }

        // ── Arc construction ──────────────────────────────────────────────────────
        private void BuildArc(Vector2[] waypoints, TransportMode mode)
        {
            _dirs     = new Vector3[SEGMENTS + 1];
            _radii    = new float  [SEGMENTS + 1];
            _worldPos = new Vector3[SEGMENTS + 1];

            // 3 units above surface avoids z-fighting with the opaque globe geometry.
            float earthR  = _earthDenominator * 0.5f;
            float groundR = earthR + 3f;
            float peakR   = groundR + PeakArcOffset(mode);

            // Air/Multimodal: each leg has its own parabolic arc (takeoff → cruise → landing).
            // Maritime/Land: flat path at groundR, hugging the surface.
            bool arcPerLeg = mode == TransportMode.Air || mode == TransportMode.Multimodal;

            if (waypoints == null || waypoints.Length < 2)
            {
                for (int i = 0; i <= SEGMENTS; i++) { _dirs[i] = Vector3.up; _radii[i] = groundR; }
                return;
            }

            int n = waypoints.Length;
            var dirs = new Vector3[n];
            for (int k = 0; k < n; k++)
                dirs[k] = LatLonToDir(waypoints[k].x, waypoints[k].y);

            // Distribute SEGMENTS proportionally by angular span of each leg.
            float[] angles = new float[n - 1];
            float totalAngle = 0f;
            for (int k = 0; k < n - 1; k++)
            {
                float dot = Mathf.Clamp(Vector3.Dot(dirs[k], dirs[k + 1]), -1f, 1f);
                angles[k]   = Mathf.Acos(dot);
                totalAngle += angles[k];
            }
            if (totalAngle < 1e-5f) totalAngle = 1e-5f;

            int si = 0;
            for (int k = 0; k < n - 1; k++)
            {
                int legSegs = (k < n - 2)
                    ? Mathf.Max(1, Mathf.RoundToInt((angles[k] / totalAngle) * SEGMENTS))
                    : SEGMENTS - si; // last leg gets remaining slots
                legSegs = Mathf.Max(1, legSegs);

                for (int i = 0; i < legSegs && si <= SEGMENTS; i++, si++)
                {
                    float t    = (float)i / legSegs;
                    _dirs[si]  = Vector3.Slerp(dirs[k], dirs[k + 1], t);
                    _radii[si] = arcPerLeg
                        ? groundR + (peakR - groundR) * Mathf.Sin(t * Mathf.PI)
                        : groundR;
                }
            }

            _dirs[SEGMENTS]  = dirs[n - 1];
            _radii[SEGMENTS] = groundR;
        }

        // Returns how many units ABOVE groundR the route peaks.
        // groundR is already 3u above the surface, so these values are the visible arc height.
        private static float PeakArcOffset(TransportMode mode) => mode switch
        {
            TransportMode.Air        => 28f,
            TransportMode.Maritime   => 0f,
            TransportMode.Land       => 0f,
            TransportMode.Rail       => 0f,
            TransportMode.Multimodal => 15f,
            _                        => 0f,
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

        // ── Visuals ───────────────────────────────────────────────────────────────
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
            _lineMat                   = new Material(Shader.Find("Unlit/Color"));
            _lineMat.color             = color;
            _line.sharedMaterial       = _lineMat;

            var dotGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotGO.name = "RouteDot";
            dotGO.transform.localScale = Vector3.one * DotSize(mode);
            Destroy(dotGO.GetComponent<SphereCollider>());
            var dotRend = dotGO.GetComponent<MeshRenderer>();
            _dotMat                   = new Material(Shader.Find("Unlit/Color"));
            _dotMat.color             = color;
            dotRend.sharedMaterial    = _dotMat;
            dotRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dotRend.receiveShadows    = false;
            _dot = dotGO.transform;
        }

        // ── Update ────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_ready || _earthTransform == null) return;

            for (int i = 0; i <= SEGMENTS; i++)
                _worldPos[i] = DirToWorld(_dirs[i], _radii[i]);
            _line.SetPositions(_worldPos);

            float progress = GetProgress();
            float indexF   = progress * SEGMENTS;
            int   ia       = Mathf.FloorToInt(indexF);
            int   ib       = Mathf.Min(ia + 1, SEGMENTS);
            float frac     = indexF - ia;

            Vector3 dir = Vector3.Slerp(_dirs[ia], _dirs[ib], frac);
            float   r   = Mathf.Lerp(_radii[ia], _radii[ib], frac);
            _dot.position = DirToWorld(dir, r);
        }

        private float GetProgress()
        {
            if (_cargo == null || _cargo.TotalTransitDays <= 0) return 0f;
            int   currentDay = FFTimeManager.Instance?.CurrentDay ?? _cargo.StartDay;
            float dayFrac    = FFTimeManager.Instance?.DayProgress ?? 0f;
            float elapsed    = (currentDay - _cargo.StartDay) + dayFrac;
            return Mathf.Clamp01(elapsed / _cargo.TotalTransitDays);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static Vector3 LatLonToDir(float lat, float lon)
        {
            float latR = lat * Mathf.Deg2Rad;
            float lonR = lon * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(latR) * Mathf.Cos(lonR),
                Mathf.Sin(latR),
                Mathf.Cos(latR) * Mathf.Sin(lonR));
        }

        private Vector3 DirToWorld(Vector3 dir, float radius)
        {
            return _earthTransform.TransformPoint(dir * (radius / _earthDenominator));
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────
        private void OnDestroy()
        {
            if (_line    != null) Destroy(_line.gameObject);
            if (_dot     != null) Destroy(_dot.gameObject);
            if (_lineMat != null) Destroy(_lineMat);
            if (_dotMat  != null) Destroy(_dotMat);
        }
    }
}
