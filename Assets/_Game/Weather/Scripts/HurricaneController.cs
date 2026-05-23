using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Weather
{
    /// <summary>
    /// Controla el sprite del huracán: rotación del ojo, animación de formación
    /// desde los bordes hacia el centro, y drift suave sobre el mapa.
    /// </summary>
    public class HurricaneController : Singleton<HurricaneController>
    {
        [SerializeField] private float rotationSpeed   = 0.35f;  // rad/s (sentido horario)
        [SerializeField] private float buildSpeed      = 0.018f; // progreso por segundo
        [SerializeField] private float driftLonPerSec  = 0.04f;
        [SerializeField] private float driftLatPerSec  = 0.015f;
        [SerializeField] private float spriteScale     = 320f;   // world units

        private GameObject _go;
        private Material   _mat;
        private Shader     _shader;

        private float _rotation;      // ángulo acumulado
        private float _buildProgress; // 0 → 1
        private float _alpha;

        private float _lat;
        private float _lon;
        private bool  _active;

        private Transform _earthTransform;
        private float     _localR;

        private float _lastAlpha = -1f;
        private float _lastBuild = -1f;
        private float _lastRot   = float.NaN;

        private static readonly int PropAlpha   = Shader.PropertyToID("_Alpha");
        private static readonly int PropBuild   = Shader.PropertyToID("_BuildProgress");
        private static readonly int PropRot     = Shader.PropertyToID("_Rotation");

        // ── Init ─────────────────────────────────────────────────────────────

        public void Initialize(Texture2D hurricaneTex)
        {
            _shader = Shader.Find("FF/HurricaneSprite");
            if (_shader == null)
            {
                Debug.LogWarning("[Hurricane] Shader FF/HurricaneSprite no encontrado.");
                return;
            }

            _go   = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _go.name = "HurricaneSprite";
            Destroy(_go.GetComponent<MeshCollider>());

            _mat = new Material(_shader) { mainTexture = hurricaneTex };
            _mat.renderQueue = 3003;
            _mat.SetFloat(PropAlpha, 0f);
            _mat.SetFloat(PropBuild, 0f);

            var rend = _go.GetComponent<MeshRenderer>();
            rend.sharedMaterial    = _mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            _go.transform.localScale = Vector3.one * spriteScale;
            _go.SetActive(false);

            if (WorldMap.Instance != null)
            {
                float earthR    = WorldMap.Instance.earthRadius;
                _localR         = (earthR + 70f) / (earthR * 2f);
                _earthTransform = WorldMap.Instance.transform;
            }
        }

        // ── Activar / desactivar ──────────────────────────────────────────────

        public void Activate(float lat, float lon)
        {
            if (_go == null) return;
            _lat  = lat;
            _lon  = lon;
            _active = true;
            _go.SetActive(true);
        }

        public void Deactivate()
        {
            _active = false;
        }

        public bool IsActive => _active;

        // ── Update ───────────────────────────────────────────────────────────

        private void Update()
        {
            if (_go == null || _mat == null) return;

            float speedMult = TimeManager.Instance != null
                ? Mathf.Clamp(TimeManager.Instance.CurrentSpeedMultiplier, 0f, 100f)
                : 1f;
            float dt = Time.deltaTime * speedMult;

            if (_active)
            {
                // Construir desde los bordes hacia el centro (animación visual, no escala con velocidad)
                _buildProgress = Mathf.MoveTowards(_buildProgress, 1f, buildSpeed * Time.deltaTime);
                _alpha         = Mathf.MoveTowards(_alpha,         0.85f, 0.015f * Time.deltaTime);

                // Rotar (animación visual, tiempo real)
                _rotation -= rotationSpeed * Time.deltaTime;

                // Derivar en el mapa según velocidad de juego
                _lon += driftLonPerSec * dt;
                _lat += driftLatPerSec * dt;
                if (_lon >  180f) _lon -= 360f;
                _lat = Mathf.Clamp(_lat, -70f, 70f);
            }
            else
            {
                // Desvanecerse y resetear (tiempo real, no escala)
                _buildProgress = Mathf.MoveTowards(_buildProgress, 0f, buildSpeed * 0.5f * Time.deltaTime);
                _alpha         = Mathf.MoveTowards(_alpha,         0f,  0.01f * Time.deltaTime);

                if (_alpha <= 0.001f)
                {
                    _go.SetActive(false);
                    _buildProgress = 0f;
                    _rotation      = 0f;
                    return;
                }
            }

            if (_alpha         != _lastAlpha) { _mat.SetFloat(PropAlpha, _alpha);         _lastAlpha = _alpha; }
            if (_buildProgress != _lastBuild) { _mat.SetFloat(PropBuild, _buildProgress); _lastBuild = _buildProgress; }
            if (_rotation      != _lastRot)   { _mat.SetFloat(PropRot,   _rotation);      _lastRot   = _rotation; }

            UpdateWorldPosition();
        }

        // ── Posición en la esfera ─────────────────────────────────────────────

        private void UpdateWorldPosition()
        {
            if (_earthTransform == null) return;

            Vector3 dir      = CloudSpriteInstance.LatLonToDir(_lat, _lon);
            Vector3 worldPos = _earthTransform.TransformPoint(dir * _localR);
            _go.transform.position = worldPos;

            // Tangent-aligned: el huracán yace plano sobre el océano (vista satelital)
            Vector3 worldOutward = (worldPos - _earthTransform.position).normalized;
            Vector3 northRef     = _earthTransform.up;
            Vector3 northTangent = Vector3.ProjectOnPlane(northRef, worldOutward).normalized;
            if (northTangent.sqrMagnitude < 0.01f)
                northTangent = Vector3.ProjectOnPlane(_earthTransform.right, worldOutward).normalized;
            _go.transform.rotation = Quaternion.LookRotation(worldOutward, northTangent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_go  != null) Destroy(_go);
            if (_mat != null) Destroy(_mat);
        }
    }
}
