using UnityEngine;

namespace FreightForwarder.Weather
{
    /// <summary>
    /// Sprite de nube posicionado sobre el globo.
    /// Tangent-aligned (plano sobre la superficie), visible desde el exterior como imagen satelital.
    /// La deriva sigue la circulación atmosférica real: Hadley, Ferrel y células polares.
    /// </summary>
    public class CloudSpriteInstance : MonoBehaviour
    {
        public float Lat;
        public float Lon;
        public float TargetAlpha;
        public float DriftLon;   // perturbación de turbulencia local (no la corriente principal)
        public float DriftLat;
        public bool  IsStorm;

        private Material _mat;
        private float    _currentAlpha;
        private float    _age;
        private float    _lifetime;
        private bool     _despawning;

        private float _baseScale;
        private float _sizeFreq;
        private float _sizePhase;
        private float _alphaFreq;
        private float _alphaPhase;

        private static readonly int PropAlpha = Shader.PropertyToID("_Alpha");

        private const float CLOUD_HEIGHT = 60f;

        public bool Despawning => _despawning;
        public bool Dead       => _currentAlpha <= 0.001f && (_despawning || _age >= _lifetime);

        // ── Init ─────────────────────────────────────────────────────────────

        public void Init(Texture2D tex, float lat, float lon,
                         float targetAlpha, float driftLon, float driftLat,
                         float lifetime, bool isStorm, Shader shader)
        {
            Lat         = lat;
            Lon         = lon;
            TargetAlpha = targetAlpha;
            DriftLon    = driftLon;
            DriftLat    = driftLat;
            IsStorm     = isStorm;

            _lifetime   = lifetime;
            _despawning = false;

            // Offset de edad aleatorio: evita que sprites creados juntos expiren juntos.
            // Hasta 40% de su lifetime ya "vivido" al nacer → muertes escalonadas.
            _age          = Random.Range(0f, lifetime * 0.4f);
            _currentAlpha = TargetAlpha * 0.2f;

            // Guardar escala base y generar fases aleatorias para que cada nube
            // tenga su propio ritmo de respiración, sin sincronizarse con las demás
            _baseScale  = transform.localScale.x;
            _sizeFreq   = Random.Range(0.04f, 0.10f);  // período 63–157 s → muy lento
            _sizePhase  = Random.Range(0f, Mathf.PI * 2f);
            _alphaFreq  = Random.Range(0.06f, 0.14f);  // período 45–105 s
            _alphaPhase = Random.Range(0f, Mathf.PI * 2f);

            _mat = new Material(shader) { mainTexture = tex };
            _mat.renderQueue = 3002;
            _mat.SetFloat(PropAlpha, _currentAlpha);

            var rend = GetComponent<MeshRenderer>();
            rend.sharedMaterial    = _mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
        }

        // ── Update ───────────────────────────────────────────────────────────

        private void Update()
        {
            _age += Time.deltaTime;

            // Velocidad de juego: pausa detiene las nubes, x10/x100/x1000 las acelera (cap visual a 100x)
            float speedMult = TimeManager.Instance != null
                ? Mathf.Clamp(TimeManager.Instance.CurrentSpeedMultiplier, 0f, 100f)
                : 1f;

            // Rebotar en los límites polares antes de aplicar deriva:
            // sin esto el DriftLat fijo acumula todas las nubes en ±80° para siempre
            if (Lat >  70f) DriftLat = -Mathf.Abs(DriftLat);
            if (Lat < -70f) DriftLat =  Mathf.Abs(DriftLat);

            // Circulación atmosférica + turbulencia local escaladas al tiempo de juego
            GetAtmosphericDrift(Lat, out float baseLon, out float baseLat);
            float dt = Time.deltaTime * speedMult;
            Lon += (baseLon + DriftLon) * dt;
            if (Lon >  180f) Lon -= 360f;
            if (Lon < -180f) Lon += 360f;
            Lat = Mathf.Clamp(Lat + (baseLat + DriftLat) * dt, -80f, 80f);

            // Alpha con fade in/out
            const float FADE_IN  = 2f;
            const float FADE_OUT = 3f;

            float desired = _despawning ? 0f
                : _age < FADE_IN                   ? Mathf.Lerp(TargetAlpha * 0.2f, TargetAlpha, _age / FADE_IN)
                : _age < _lifetime - FADE_OUT      ? TargetAlpha
                : Mathf.Lerp(TargetAlpha, 0f, (_age - (_lifetime - FADE_OUT)) / FADE_OUT);

            _currentAlpha = Mathf.Lerp(_currentAlpha, desired, Time.deltaTime * 2.5f);

            // Respiración de alpha: ±12 % del valor actual, período 45–105 s por sprite
            float alphaBreath = 1f + Mathf.Sin(_age * _alphaFreq + _alphaPhase) * 0.12f;
            _mat?.SetFloat(PropAlpha, Mathf.Clamp01(_currentAlpha * alphaBreath));

            // Respiración de tamaño: ±9 % de la escala base, período 63–157 s por sprite
            float sizeBreath = 1f + Mathf.Sin(_age * _sizeFreq + _sizePhase) * 0.09f;
            transform.localScale = Vector3.one * (_baseScale * sizeBreath);

            UpdateWorldPosition();

            // Auto-destrucción inmediata al morir: libera el slot sin esperar el Refresh
            if (Dead) Destroy(gameObject);
        }

        public void BeginDespawn() => _despawning = true;

        // ── Posición + orientación tangente ───────────────────────────────────

        private void UpdateWorldPosition()
        {
            if (WorldMap.Instance == null) return;

            Vector3 dir    = LatLonToDir(Lat, Lon);
            float   earthR = WorldMap.Instance.earthRadius;
            float   localR = (earthR + CLOUD_HEIGHT) / (earthR * 2f);

            transform.position = WorldMap.Instance.transform.TransformPoint(dir * localR);

            // Tangent-aligned: el quad yace plano sobre la superficie del globo.
            // forward(+Z) apunta hacia el espacio → frente del quad visible desde fuera.
            // El depth buffer oculta automáticamente las nubes del lado trasero.
            Vector3 worldOutward = (transform.position - WorldMap.Instance.transform.position).normalized;
            Vector3 northRef     = WorldMap.Instance.transform.up;
            Vector3 northTangent = Vector3.ProjectOnPlane(northRef, worldOutward).normalized;
            if (northTangent.sqrMagnitude < 0.01f)
                northTangent = Vector3.ProjectOnPlane(WorldMap.Instance.transform.right, worldOutward).normalized;
            transform.rotation = Quaternion.LookRotation(worldOutward, northTangent);
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        // ── Circulación atmosférica ───────────────────────────────────────────

        /// <summary>
        /// Calcula la velocidad de deriva en grados/segundo según la latitud.
        /// Célula de Hadley (0-30°): vientos alisios → movimiento hacia el OESTE.
        /// Célula de Ferrel (30-60°): vientos del oeste → movimiento hacia el ESTE.
        /// Célula Polar (60-90°): alisios polares del este → movimiento hacia el OESTE.
        /// Transiciones suavizadas con SmoothStep para evitar saltos bruscos.
        /// </summary>
        private static void GetAtmosphericDrift(float lat, out float driftLon, out float driftLat)
        {
            float absLat = Mathf.Abs(lat);
            float sign   = lat >= 0f ? 1f : -1f;

            // Pesos de transición entre células (suavizados)
            float wHF = Mathf.SmoothStep(0f, 1f, (absLat - 25f) / 10f); // Hadley→Ferrel en 25-35°
            float wFP = Mathf.SmoothStep(0f, 1f, (absLat - 55f) / 10f); // Ferrel→Polar en 55-65°

            // Velocidad zonal (longitud/s) — única corriente sistemática visible
            float lonHadley = -0.55f; // alisios: hacia el oeste
            float lonFerrel = +0.55f; // westerlies: hacia el este
            float lonPolar  = -0.25f; // polares del este: hacia el oeste, más lentos
            float lonHF = Mathf.Lerp(lonHadley, lonFerrel, wHF);
            driftLon = Mathf.Lerp(lonHF, lonPolar, wFP);

            // Sin deriva meridional sistemática: evita que todo converja a 3 bandas.
            // La variación en latitud viene solo de la turbulencia local (DriftLat por sprite).
            driftLat = 0f;
        }

        // ── Utilidad compartida ───────────────────────────────────────────────

        public static Vector3 LatLonToDir(float lat, float lon)
        {
            float latR = lat * Mathf.Deg2Rad;
            float lonR = lon * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(latR) * Mathf.Cos(lonR),
                               Mathf.Sin(latR),
                               Mathf.Cos(latR) * Mathf.Sin(lonR));
        }
    }
}
