using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Weather
{
    /// <summary>
    /// Gestiona todos los sprites de nubes reales sobre el globo.
    /// - Carga las texturas PNG desde Resources
    /// - Divide el mundo en 16×8 regiones de render
    /// - Spawna nubes individuales (alpha 0.2–0.7) y clusters para tormentas
    /// - Delega el huracán a HurricaneController
    /// Mantiene la misma interfaz que antes (Initialize + Refresh) para que
    /// WeatherSystem no necesite cambios.
    /// </summary>
    public class CloudRenderer : Singleton<CloudRenderer>
    {
        // ── Config ────────────────────────────────────────────────────────────

        private const int   REGION_W        = 16;
        private const int   REGION_H        = 8;
        private const int   MIN_SPRITES     = 90;
        private const int   MAX_SPRITES     = 250;
        private const float CLOUD_THRESHOLD = 0.28f;
        private const float STORM_THRESHOLD = 0.52f;
        private const float SPRITE_MIN_SIZE = 60f;      // variedad grande: 60–260 world units
        private const float SPRITE_MAX_SIZE = 260f;
        private const float STORM_SIZE_MULT = 1.5f;
        private const float LIFETIME_MIN    = 100f;
        private const float LIFETIME_MAX    = 260f;

        // ── State ─────────────────────────────────────────────────────────────

        private WeatherGrid   _grid;
        private WeatherConfig _config;
        private Texture2D[]   _cloudTextures;
        private Texture2D     _hurricaneTex;
        private Shader        _cloudShader;

        // Sprites activos
        private readonly List<CloudSpriteInstance> _sprites = new List<CloudSpriteInstance>();

        // Una marca por región: cuántos sprites tiene actualmente
        private readonly int[] _regionSpriteCount = new int[REGION_W * REGION_H];

        // ── Init ──────────────────────────────────────────────────────────────

        public void Initialize(WeatherGrid grid, WeatherConfig config)
        {
            _grid   = grid;
            _config = config;

            LoadTextures();
            ResolveShader();

            HurricaneController.Instance?.Initialize(_hurricaneTex);

            // Relleno inicial: repetir Refresh hasta alcanzar MIN_SPRITES.
            // Con clusters de 3-5 y 128 regiones, 3 pasadas alcanzan los 250 máximos.
            if (_cloudTextures != null && _cloudTextures.Length > 0)
            {
                int passes = 0;
                while (_sprites.Count < MIN_SPRITES && passes < 5)
                {
                    Refresh(grid);
                    passes++;
                }
            }
        }

        private void ResolveShader()
        {
            _cloudShader = Shader.Find("FF/CloudSprite");
            if (_cloudShader != null)
            {
                Debug.Log("[CloudRenderer] Shader FF/CloudSprite OK.");
                return;
            }

            // Fallback: Sprites/Default soporta alpha y es universal en Built-in
            _cloudShader = Shader.Find("Sprites/Default");
            if (_cloudShader != null)
            {
                Debug.LogWarning("[CloudRenderer] FF/CloudSprite no encontrado → usando Sprites/Default. " +
                                 "Las nubes aparecerán sin degradado radial.");
                return;
            }

            Debug.LogError("[CloudRenderer] No se encontró ningún shader válido. Las nubes no se renderizarán.");
        }

        private void LoadTextures()
        {
            // Carga todas las PNGs numeradas de la carpeta Cloud
            var list = new List<Texture2D>();
            string[] names = {
                "01","02","04","05","07","08","09","10","11","13","14","15","16","17","18",
                "20","21","22","23","24","25","26","28","30","31","32","33","34","35","36",
                "37","38","39","40","41","42","43","44","45","46","47","48","49","51","52",
                "53","54","55","56","57","58","59","61","62","63","64","65","66","67","68",
                "69","70","71","72","73","74","75","76","77","78","79","80","81","82","83",
                "84","85","86","87","88","89","90","91","92","96","97","98","100"
            };

            foreach (var n in names)
            {
                var tex = Resources.Load<Texture2D>($"Map/Textures/Cloud/{n}");
                if (tex != null) list.Add(tex);
            }

            _cloudTextures = list.ToArray();
            _hurricaneTex  = Resources.Load<Texture2D>("Map/Textures/Cloud/hurricane1");

            Debug.Log($"[CloudRenderer] Texturas: {_cloudTextures.Length} nubes, " +
                      $"huracán: {(_hurricaneTex != null ? "OK" : "FALTA")}. " +
                      $"Path base: 'Map/Textures/Cloud/XX'");
        }

        // ── Ciclo continuo de spawn ───────────────────────────────────────────

        private float _spawnTimer;

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < 0.5f) return;
            _spawnTimer = 0f;

            if (_cloudTextures == null || _cloudTextures.Length == 0 || _cloudShader == null) return;

            // Limpiar referencias nulas (sprites que se auto-destruyeron)
            for (int i = _sprites.Count - 1; i >= 0; i--)
                if (_sprites[i] == null) _sprites.RemoveAt(i);

            // Rellenar hasta el mínimo con distribución uniforme por latitud
            if (_sprites.Count < MIN_SPRITES)
            {
                int fill = MIN_SPRITES - _sprites.Count;
                for (int i = 0; i < fill && _sprites.Count < MAX_SPRITES; i++)
                {
                    float lat = Mathf.Lerp(-65f, 65f, (i + Random.value) / fill);
                    float lon = Random.Range(-180f, 180f);
                    SpawnSprite(lat, lon, Random.Range(0.35f, 0.65f), false);
                }
            }
        }

        // ── Refresh (llamado por WeatherSystem cada tick) ──────────────────────

        private int _refreshCount;

        public void Refresh(WeatherGrid grid)
        {
            // Auto-inicializar si esta instancia nunca recibió Initialize() (nuevo Singleton creado en runtime)
            if (_cloudTextures == null || _cloudTextures.Length == 0 || _cloudShader == null)
            {
                Debug.LogWarning("[CloudRenderer] Refresh: instancia sin inicializar, recargando recursos...");
                LoadTextures();
                ResolveShader();
                if (_hurricaneTex != null) HurricaneController.Instance?.Initialize(_hurricaneTex);
            }

            if (_cloudTextures == null || _cloudTextures.Length == 0 || _cloudShader == null)
            {
                Debug.LogError("[CloudRenderer] Refresh abortado: no se pudieron cargar los recursos.");
                return;
            }

            _grid = grid;

            // Limpiar sprites muertos
            for (int i = _sprites.Count - 1; i >= 0; i--)
            {
                if (_sprites[i] == null || _sprites[i].Dead)
                {
                    if (_sprites[i] != null) Destroy(_sprites[i].gameObject);
                    _sprites.RemoveAt(i);
                }
            }

            // Recalcular conteo por región
            System.Array.Clear(_regionSpriteCount, 0, _regionSpriteCount.Length);
            foreach (var s in _sprites)
            {
                if (s == null) continue;
                int ri = LatLonToRegion(s.Lat, s.Lon);
                if (ri >= 0) _regionSpriteCount[ri]++;
            }

            // Evaluar el grid y spawnar / despawnar
            // Orden aleatorio para que los sprites no se concentren siempre en las mismas regiones
            var regionOrder = new int[REGION_W * REGION_H];
            for (int i = 0; i < regionOrder.Length; i++) regionOrder[i] = i;
            for (int i = regionOrder.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int t = regionOrder[i]; regionOrder[i] = regionOrder[j]; regionOrder[j] = t;
            }

            bool cycloneFound  = false;
            float cycloneLat   = 0f, cycloneLon = 0f;
            float cycloneStr   = 0f;
            int   regionsAbove = 0;

            foreach (int ri in regionOrder)
            {
                int rx = ri % REGION_W;
                int ry = ri / REGION_W;

                float avgCloud, avgStorm, avgCyclone;
                SampleRegion(rx, ry, out avgCloud, out avgStorm, out avgCyclone);

                float centerLat = Mathf.Lerp(-90f, 90f, (ry + 0.5f) / REGION_H);
                float centerLon = Mathf.Lerp(-180f, 180f, (rx + 0.5f) / REGION_W);

                // Ciclón más fuerte → al huracán
                if (avgCyclone > cycloneStr)
                {
                    cycloneStr   = avgCyclone;
                    cycloneLat   = centerLat;
                    cycloneLon   = centerLon;
                    cycloneFound = avgCyclone > 0.15f;
                }

                // Nubes y ciclones coexisten: el ciclón agrega el sprite de huracán ENCIMA
                // de las nubes de tormenta, nunca las reemplaza.
                int desired;
                if (avgCyclone > 0.4f)
                    desired = Random.Range(5, 9);    // ciclón = cluster denso de tormentas
                else if (avgStorm > STORM_THRESHOLD)
                    desired = Random.Range(6, 10);
                else if (avgCloud > CLOUD_THRESHOLD)
                    desired = Random.Range(3, 5);
                else
                    desired = 0;                     // zona despejada

                if (desired > 0) regionsAbove++;

                int current = _regionSpriteCount[ri];

                // Spawnar el cluster completo de una vez (no de a 1 por Refresh)
                bool isStormSpawn = avgStorm > STORM_THRESHOLD || avgCyclone > 0.4f;
                // Evitar spawns en latitudes polares donde el clamp ±80 atrapa sprites
                float spawnLat = Mathf.Clamp(centerLat, -62f, 62f);
                int need = Mathf.Min(desired - current, MAX_SPRITES - _sprites.Count);
                for (int s = 0; s < need; s++)
                    SpawnSprite(spawnLat, centerLon, avgCloud, isStormSpawn);

                if (current > desired && desired == 0)
                    DespawnOneIn(spawnLat, centerLon);
            }

            // Garantía de mínimo: si el grid cayó bajo o los sprites se concentraron
            // en los polos, rellenar con nubes distribuidas uniformemente en el planeta.
            // Se distribuyen en bandas de latitud para evitar clustering accidental.
            if (_sprites.Count < MIN_SPRITES)
            {
                int fill = MIN_SPRITES - _sprites.Count;
                for (int i = 0; i < fill && _sprites.Count < MAX_SPRITES; i++)
                {
                    // Distribución uniforme por latitud usando estratificación
                    float lat = Mathf.Lerp(-65f, 65f, (i + Random.value) / fill);
                    float lon = Random.Range(-180f, 180f);
                    SpawnSprite(lat, lon, Random.Range(0.35f, 0.65f), false);
                }
            }

            _refreshCount++;
            if (_refreshCount <= 3)
                Debug.Log($"[CloudRenderer] Refresh #{_refreshCount}: {regionsAbove}/{REGION_W * REGION_H} regiones " +
                          $"nubladas, {_sprites.Count} sprites activos.");

            // Gestión del huracán
            if (cycloneFound)
            {
                var hurricane = HurricaneController.Instance;
                if (hurricane != null && !hurricane.IsActive)
                    hurricane.Activate(cycloneLat, cycloneLon);
            }
            else
            {
                HurricaneController.Instance?.Deactivate();
            }
        }

        // ── Spawn / Despawn ───────────────────────────────────────────────────

        private void SpawnSprite(float centerLat, float centerLon, float cloudVal, bool isStorm)
        {
            if (_cloudShader == null || _cloudTextures.Length == 0)
            {
                Debug.LogError($"[CloudRenderer] SpawnSprite bloqueado: shader={_cloudShader != null}, tex={_cloudTextures.Length}");
                return;
            }
            if (_refreshCount <= 2)
                Debug.Log($"[CloudRenderer] Spawneando sprite en ({centerLat:F0},{centerLon:F0}), storm={isStorm}");

            // Nube aleatoria del pool
            var tex = _cloudTextures[Random.Range(0, _cloudTextures.Length)];

            // Jitter ajustado para que las 3-5 nubes de un cluster queden visiblemente agrupadas
            float jitterLat = Random.Range(-4f, 4f);
            float jitterLon = Random.Range(-6f, 6f);
            float lat = Mathf.Clamp(centerLat + jitterLat, -80f, 80f);
            float lon = centerLon + jitterLon;

            float alpha    = Random.Range(0.2f, 0.7f);
            float lifetime = Random.Range(LIFETIME_MIN, LIFETIME_MAX);

            // Tamaño
            float baseSize = Random.Range(SPRITE_MIN_SIZE, SPRITE_MAX_SIZE);
            if (isStorm) baseSize *= STORM_SIZE_MULT;

            // Turbulencia local: lon pequeña (la corriente principal ya es fuerte),
            // lat simétrica grande → cada sprite deriva diferente en latitud, sin bandas
            float driftLon = Random.Range(-0.04f, 0.04f);
            float driftLat = Random.Range(-0.08f, 0.08f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = isStorm ? "StormSprite" : "CloudSprite";
            Destroy(go.GetComponent<MeshCollider>());
            go.transform.localScale = Vector3.one * baseSize;

            var rend = go.GetComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            var sprite = go.AddComponent<CloudSpriteInstance>();
            sprite.Init(tex, lat, lon, alpha, driftLon, driftLat, lifetime, isStorm, _cloudShader);

            _sprites.Add(sprite);

            int ri = LatLonToRegion(lat, lon);
            if (ri >= 0) _regionSpriteCount[ri]++;
        }

        private void DespawnOneIn(float centerLat, float centerLon)
        {
            float bestDist = float.MaxValue;
            CloudSpriteInstance best = null;
            foreach (var s in _sprites)
            {
                if (s == null || s.Despawning) continue;
                float d = Mathf.Abs(s.Lat - centerLat) + Mathf.Abs(s.Lon - centerLon);
                if (d < bestDist) { bestDist = d; best = s; }
            }
            best?.BeginDespawn();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SampleRegion(int rx, int ry,
                                   out float cloud, out float storm, out float cyclone)
        {
            cloud = storm = cyclone = 0f;
            if (_grid == null) return;

            int gxStart = rx * _grid.Width  / REGION_W;
            int gyStart = ry * _grid.Height / REGION_H;
            int gxEnd   = (rx + 1) * _grid.Width  / REGION_W;
            int gyEnd   = (ry + 1) * _grid.Height / REGION_H;
            int count   = 0;

            for (int gy = gyStart; gy < gyEnd; gy++)
            {
                for (int gx = gxStart; gx < gxEnd; gx++)
                {
                    var c = _grid.GetCell(gx, gy);
                    cloud   += c.cloud;
                    storm   += c.storm;
                    cyclone += c.cyclone;
                    count++;
                }
            }

            if (count > 0) { cloud /= count; storm /= count; cyclone /= count; }
        }

        private int LatLonToRegion(float lat, float lon)
        {
            int rx = Mathf.FloorToInt((lon + 180f) / 360f * REGION_W);
            int ry = Mathf.FloorToInt((lat +  90f) / 180f * REGION_H);
            rx = Mathf.Clamp(rx, 0, REGION_W - 1);
            ry = Mathf.Clamp(ry, 0, REGION_H - 1);
            return ry * REGION_W + rx;
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var s in _sprites)
                if (s != null) Destroy(s.gameObject);
            _sprites.Clear();
        }
    }
}
