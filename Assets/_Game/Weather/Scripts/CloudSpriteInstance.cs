using UnityEngine;

namespace FreightForwarder.Weather
{

    // Sprite de nube posicionado sobre el globo.
    // Tangent-aligned (plano sobre la superficie), visible desde el exterior como imagen satelital.
    // La deriva sigue la circulación atmosférica real: Hadley, Ferrel y células polares.

    public class CloudSpriteInstance : MonoBehaviour
    {
        public float Lat;
        public float Lon;
        public float TargetAlpha;
// Ejecuta local
        public float DriftLon;   // perturbación de turbulencia local (no la corriente principal)
        public float DriftLat;
        public bool  IsStorm;

        private Material       _mat;
        private Transform      _earthTransform;
        private float          _localR;
        private SunController  _sun;
        private float     _currentAlpha;
        private float     _age;
        private float     _lifetime;
        private bool      _despawning;

        private float _baseScale;
        private float _sizeFreq;
        private float _sizePhase;
        private float _alphaFreq;
        private float _alphaPhase;
        private float _stretchX = 1f;
        private float _stretchY = 1f;

        private static readonly int PropAlpha       = Shader.PropertyToID("_Alpha");
        private static readonly int PropNightFactor = Shader.PropertyToID("_NightFactor");
        private static readonly int PropStretchDir  = Shader.PropertyToID("_StretchDir");
        private static readonly int PropSphereR     = Shader.PropertyToID("_SphereR");

        private const float CLOUD_HEIGHT = 60f;

// Devuelve el despawning
        public bool Despawning => _despawning;
// Dead
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
            _sizeFreq   = Random.Range(0.04f, 0.10f);
            _sizePhase  = Random.Range(0f, Mathf.PI * 2f);
            _alphaFreq  = Random.Range(0.06f, 0.14f);
            _alphaPhase = Random.Range(0f, Mathf.PI * 2f);
            _stretchX   = 1f;
            _stretchY   = 1f;

            // Caché earth transform and localR once — they never change at runtime.
            if (WorldMap.Instance != null)
            {
                float earthR    = WorldMap.Instance.earthRadius;
                _localR         = (earthR + CLOUD_HEIGHT) / (earthR * 2f);
                _earthTransform = WorldMap.Instance.transform;
            }
            else
            {
                _localR         = (1000f + CLOUD_HEIGHT) / 2000f;
                _earthTransform = null;
            }
            _sun = SunController.Instance;

            // Pool: reutilizar el material si ya existe en lugar de crear uno nuevo cada vez.
            if (_mat == null)
            {
                _mat = new Material(shader);
                _mat.renderQueue = 3002;
                float sphereR = WorldMap.Instance != null
                    ? WorldMap.Instance.earthRadius + CLOUD_HEIGHT : 1060f;
                _mat.SetFloat(PropSphereR, sphereR);
                var rend = GetComponent<MeshRenderer>();
                rend.sharedMaterial    = _mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows    = false;
            }
            _mat.mainTexture = tex;
            _mat.SetFloat(PropAlpha, _currentAlpha);
        }

        // Ejecuta las comprobaciones necesarias en cada fotograma del juego.

        private void Update()
        {
            _age += Time.deltaTime;

            // Velocidad de juego: pausa detiene las nubes, x10/x100/x1000 las acelera (cap visual a 100x)
            float speedMult = TimeManager.Instance != null
                ? Mathf.Clamp(TimeManager.Instance.CurrentSpeedMultiplier, 0f, 100f)
                : 1f;

            // Rebotar cerca de los polos para que los sprites no se acumulen en el límite.
            // 82° permite que los sprites lleguen y moren en los polos (alta cobertura polar real).
            if (Lat >  82f) DriftLat = -Mathf.Abs(DriftLat);
            if (Lat < -82f) DriftLat =  Mathf.Abs(DriftLat);

            // Circulación atmosférica + turbulencia local escaladas al tiempo de juego
            GetAtmosphericDrift(Lat, out float baseLon, out float baseLat);
            float dt = Time.deltaTime * speedMult;
            Lon += (baseLon + DriftLon) * dt;
            if (Lon >  180f) Lon -= 360f;
            if (Lon < -180f) Lon += 360f;
            Lat = Mathf.Clamp(Lat + (baseLat + DriftLat) * dt, -88f, 88f);

            // Deformación lenta hacia el sentido de traslado
            float dLon = baseLon + DriftLon;
            float dLat = baseLat + DriftLat;
            float dMag = Mathf.Max(0.001f, Mathf.Sqrt(dLon * dLon + dLat * dLat));
            float nX   = dLon / dMag;
            float nY   = dLat / dMag;
            float targetStretchX = 1f + 0.48f * (nX * nX) - 0.20f * (nY * nY);
            float targetStretchY = 1f + 0.68f * (nY * nY) - 0.20f * (nX * nX);
            _stretchX = Mathf.Lerp(_stretchX, targetStretchX, Time.deltaTime * 0.4f);
            _stretchY = Mathf.Lerp(_stretchY, targetStretchY, Time.deltaTime * 0.4f);
            _mat?.SetVector(PropStretchDir, new Vector4(nX, nY, 0f, 0f));

            Vector3 dir = LatLonToDir(Lat, Lon);
            UpdateWorldPosition(dir);

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

            // Día / noche
            _mat?.SetFloat(PropNightFactor, ComputeDaylightFactor());

            // Respiración de tamaño: ±9 % de la escala base, período 63–157 s por sprite
            float sizeBreath = 1f + Mathf.Sin(_age * _sizeFreq + _sizePhase) * 0.29f;
            transform.localScale = new Vector3(
                _baseScale * sizeBreath * _stretchX,
                _baseScale * sizeBreath * _stretchY,
                1f);

            // Al morir, devolver al pool en lugar de destruir el GameObject
            if (Dead) { CloudRenderer.Instance?.Release(this); return; }
        }

// Gestiona begin despawn.
        public void BeginDespawn() => _despawning = true;

// Calcula daylight factor
        private float ComputeDaylightFactor()
        {
            if (_sun == null || _earthTransform == null) return 1f;
            float angle = _sun.GetSunAngleAtPosition(transform.position, _earthTransform.position);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-20f, 20f, angle));
        }

        // ── Posición + orientación tangente ───────────────────────────────────

        private void UpdateWorldPosition(Vector3 dir)
        {
            if (_earthTransform == null) return;

            transform.position = _earthTransform.TransformPoint(dir * _localR);

            Vector3 worldOutward = (transform.position - _earthTransform.position).normalized;
            Vector3 northRef     = _earthTransform.up;
            Vector3 northTangent = Vector3.ProjectOnPlane(northRef, worldOutward).normalized;
            if (northTangent.sqrMagnitude < 0.01f)
                northTangent = Vector3.ProjectOnPlane(_earthTransform.right, worldOutward).normalized;
            transform.rotation = Quaternion.LookRotation(worldOutward, northTangent);
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        // ── Circulación atmosférica ───────────────────────────────────────────


        // Calcula la velocidad de deriva en grados/segundo según la latitud.
        // Célula de Hadley (0-30°): vientos alisios → movimiento hacia el OESTE.
        // Célula de Ferrel (30-60°): vientos del oeste → movimiento hacia el ESTE.
        // Célula Polar (60-90°): alisios polares del este → movimiento hacia el OESTE.
        // Transiciones suavizadas con SmoothStep para evitar saltos bruscos.

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

        // Latitud longitud to dir.

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