using UnityEngine;
using UnityEngine.Rendering;
using FreightForwarder.Models;
using FreightForwarder.Managers;
using FreightForwarder.Map;
using FreightForwarder.Weather;
using FreightForwarder.Systems.World;
using FreightForwarder.Systems.Progression;
using FreightForwarder.Systems.Logistics;
using FreightForwarder.Utils;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Referencias (se auto-resuelven si están vacías)")]
    public TimeManager         timeManager;
    public SunController       sunController;
    public WorldMap            worldMap;
    public MapCameraController mapCameraController;
    public UIManager           uiManager;
    public DaylightVerifier    daylightVerifier;


    void Awake()
    {
        EnsureTimeManager();
        EnsureWorldMap();
        EnsureSunController();
        EnsureCamera();
        EnsureUIManager();
        EnsureDaylightVerifier();
        SetupBackground();
        CreateSunVisual();
        SpawnCities();
    }

    void Start()
    {
        // El MapCameraController se inicializa solo (espera un frame y apunta a Buenos Aires)
        uiManager?.CenterUIHubPanel();
        CreateCloudLayer();
        CreateCleanMapCloudLayer();
        CreateAtmosphericHalo();
        // rutas demo removidas — las rutas dinámicas las maneja RouteManager
    }

    private void CreateCleanMapCloudLayer()
    {
        if (GameObject.Find("CloudLayer_CleanMap") != null)
            return;

        var cloudGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cloudGO.name = "CloudLayer_CleanMap";
        SphereMeshUtility.Apply(cloudGO, 64);

        cloudGO.transform.localScale = worldMap.transform.localScale * 1.025f;
        cloudGO.transform.SetParent(worldMap.transform);

        var renderer = cloudGO.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var mat = new Material(Shader.Find("Standard"));

        var tex = Resources.Load<Texture2D>("Map/Textures/Cloud/mapcompleteclean");
        if (tex != null)
        {
            mat.mainTexture = tex;
        }
        else
        {
            Debug.LogError("❌ No se encontró mapcompleteclean en Resources/Map/Textures/Cloud/");
        }

        mat.color = new Color(1f, 1f, 1f, 0.9f);

        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3001;

        renderer.material = mat;

        Destroy(cloudGO.GetComponent<Collider>());

        var controller = cloudGO.AddComponent<CloudLayerController>();
        controller.rotationSpeed      = 0.2f;   // 4x más lenta que antes (era 0.8)
        controller.opacity            = 0.06f;  // 70% más difusa (era 0.2 → 30 % del original)
        controller.randomizeDirection = true;
    }

    private void CreateAtmosphericHalo()
    {
        if (GameObject.Find("AtmosphericHalo") != null)
            return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "AtmosphericHalo";
        SphereMeshUtility.Apply(go, 64);

        // Ligeramente más grande que la Tierra pero dentro de la capa de nubes
        go.transform.localScale = worldMap.transform.localScale * 1.012f;
        go.transform.SetParent(worldMap.transform);

        Destroy(go.GetComponent<Collider>());

        var rend = go.GetComponent<MeshRenderer>();
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        var shader = Shader.Find("Custom/AtmosphericHalo");
        if (shader == null)
        {
            Debug.LogError("❌ Shader Custom/AtmosphericHalo no encontrado");
            Destroy(go);
            return;
        }

        var mat = new Material(shader);
        mat.SetColor("_HaloColor",  new Color(0.75f, 0.95f, 1f, 1f));
        mat.SetVector("_SunDir",    Vector3.right);
        mat.SetFloat("_FresnelPow", 2.0f);
        mat.SetFloat("_Intensity",  0.8f);
        mat.SetFloat("_SunWrap",    0.5f);
        mat.renderQueue = 3001;

        rend.material = mat;

        go.AddComponent<AtmosphericHaloController>();
    }

    private void CreateCloudLayer()
    {
        // Inicializar todos los sistemas de Freight Forwarder
        CityDatabase.Initialize();

        // Managers del juego
        _ = GameManager.Instance;
        _ = FFTimeManager.Instance;
        _ = EconomyManager.Instance;
        _ = AgentManager.Instance;
        _ = ClientManager.Instance;
        _ = CargoManager.Instance;
        _ = EventManager.Instance;
        _ = RouteManager.Instance;

        // Sistema climático
        _ = WeatherManager.Instance;
        _ = CloudRenderer.Instance;
        _ = WeatherImpact.Instance;
        _ = HurricaneController.Instance;

        // Sistemas V2 (FASE 4-9) — activados por FeatureFlags
        if (FeatureFlags.USE_WORLD_STATE)
        {
            _ = WorldStateManager.Instance;
            _ = NewsManager.Instance;
        }
        if (FeatureFlags.USE_PROGRESSION)
        {
            _ = ProgressionManager.Instance;
        }
        if (FeatureFlags.USE_ROUTE_GRAPH)
        {
            RouteGraph.Instance.Build(CityDatabase.AllCities);
        }

        // Inicializar el sistema de clima explícitamente
        var weatherSys = WeatherSystem.Instance;
        weatherSys.Activate();

        GameManager.Instance.StartNewGame();
        Debug.Log($"[Bootstrap] Sistemas FF inicializados. " +
                  $"RouteGraph={FeatureFlags.USE_ROUTE_GRAPH} " +
                  $"WorldState={FeatureFlags.USE_WORLD_STATE} " +
                  $"Progression={FeatureFlags.USE_PROGRESSION}");
    }

    private void EnsureTimeManager()
    {
        if (timeManager != null) return;
        timeManager = Object.FindAnyObjectByType<TimeManager>();
        if (timeManager == null)
            timeManager = new GameObject("TimeManager").AddComponent<TimeManager>();
    }

    private void EnsureWorldMap()
    {
        if (worldMap != null) return;
        worldMap = Object.FindAnyObjectByType<WorldMap>();
        if (worldMap != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Earth";
        SphereMeshUtility.Apply(go, 128);
        worldMap = go.AddComponent<WorldMap>();
        var rend = go.GetComponent<MeshRenderer>();
        worldMap.earthMeshRenderer = rend;

        var shader = Shader.Find("Custom/EarthBlend");
        rend.sharedMaterial = shader != null
            ? new Material(shader)
            : new Material(Shader.Find("Standard"));

        if (shader == null)
            Debug.LogWarning("[Bootstrap] Shader 'Custom/EarthBlend' no encontrado — usando Standard.");
    }

    private void EnsureSunController()
    {
        if (sunController != null) return;
        sunController = Object.FindAnyObjectByType<SunController>();
        if (sunController != null) return;

        var go = new GameObject("SunController");
        sunController = go.AddComponent<SunController>();

        Light dirLight = null;
        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l.type == LightType.Directional) { dirLight = l; break; }
        }
        if (dirLight == null)
        {
            var sunGO = new GameObject("Directional Light");
            dirLight = sunGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.shadows = LightShadows.Soft;
        }

        sunController.sunLight       = dirLight;
        sunController.earthTransform = worldMap.transform;
    }

    private void EnsureCamera()
    {
        if (mapCameraController != null) return;
        mapCameraController = Object.FindAnyObjectByType<MapCameraController>();
        if (mapCameraController != null) return;

        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        mapCameraController = go.AddComponent<MapCameraController>();
        mapCameraController.earthTransform = worldMap.transform;
    }

    private void EnsureUIManager()
    {
        if (uiManager != null) return;
        uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null)
            uiManager = new GameObject("UIManager").AddComponent<UIManager>();
    }

    private void EnsureDaylightVerifier()
    {
        if (daylightVerifier != null) return;
        daylightVerifier = Object.FindAnyObjectByType<DaylightVerifier>();
        if (daylightVerifier == null)
            daylightVerifier = new GameObject("DaylightVerifier").AddComponent<DaylightVerifier>();
    }

    private void SetupBackground()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.03f, 0.06f);

        var starsTex = Resources.Load<Texture2D>("Map/Textures/Space/stars1");
        if (starsTex != null)
        {
            var skyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
            skyboxMat.SetTexture("_MainTex", starsTex);
            skyboxMat.SetFloat("_Exposure", 0.6f);
            RenderSettings.skybox = skyboxMat;
        }
        else
        {
            RenderSettings.skybox = null;
            Debug.LogWarning("[Bootstrap] No se encontró stars1 en Resources/Map/Textures/Space/");
        }

        var cam = mapCameraController?.GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags   = starsTex != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.farClipPlane = 20000f;
        }
    }

    private void CreateSunVisual()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SunVisual";
        SphereMeshUtility.Apply(go, 32);
        go.transform.localScale = Vector3.one * 600f;
        Destroy(go.GetComponent<SphereCollider>());

        var rend = go.GetComponent<MeshRenderer>();
        var mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0.95f, 0.65f);
        rend.sharedMaterial   = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        sunController.sunVisual = go.transform;
    }

    private void SpawnCities()
    {
        Color yellow = new Color(1f, 0.9f, 0f);

        // Coordenadas: lat real, lon = (lon_real + 180) wrapeado a [-180, 180]
        // porque el mesh tiene lon=0 en el Pacífico central en lugar de Greenwich.

        // América del Sur
        SpawnCity("Buenos Aires",   -34.61f,  121.62f, yellow);
        SpawnCity("São Paulo",      -23.55f,  133.37f, yellow);
        SpawnCity("Valparaíso",     -33.05f,  108.37f, yellow);
        SpawnCity("Santiago",       -33.45f,  109.35f, yellow);
        SpawnCity("Lima",           -12.05f,  102.96f, yellow);
        SpawnCity("Bogotá",           4.71f,  105.93f, yellow);
        SpawnCity("Cartagena",       10.39f,  104.49f, yellow);

        // América del Norte
        SpawnCity("Miami",           25.77f,   99.81f, yellow);
        SpawnCity("New York",        40.71f,  105.99f, yellow);
        SpawnCity("Los Ángeles",     34.05f,   61.76f, yellow);
        SpawnCity("Houston",         29.76f,   84.63f, yellow);
        SpawnCity("Vancouver",       49.25f,   56.90f, yellow);
        SpawnCity("Ciudad de México",19.43f,   80.87f, yellow);

        // América Central
        SpawnCity("Panamá",           8.99f,  100.48f, yellow);

        // Europa Occidental
        SpawnCity("Roma",            41.90f, -167.51f, yellow);
        SpawnCity("Rotterdam",       51.92f, -175.52f, yellow);
        SpawnCity("London",          51.51f,  179.87f, yellow);
        SpawnCity("Amberes",         51.22f, -175.60f, yellow);
        SpawnCity("Barcelona",       41.39f, -177.84f, yellow);
        SpawnCity("Marsella",        43.30f, -174.63f, yellow);
        SpawnCity("Madrid",          40.42f,  176.30f, yellow);
        SpawnCity("París",           48.86f, -177.65f, yellow);
        SpawnCity("Frankfurt",       50.11f, -171.32f, yellow);
        SpawnCity("Hamburgo",        53.55f, -170.01f, yellow);
        SpawnCity("Casablanca",      33.59f,  172.38f, yellow);

        // Europa del Este y Mediterráneo
        SpawnCity("Atenas",          37.98f, -156.27f, yellow);
        SpawnCity("Estambul",        41.01f, -151.03f, yellow);

        // África
        SpawnCity("Johannesburgo",  -26.20f, -151.95f, yellow);
        SpawnCity("Cape Town",      -33.93f, -161.58f, yellow);
        SpawnCity("Port Said",       31.26f, -147.72f, yellow);
        SpawnCity("Mombasa",         -4.05f, -140.33f, yellow);

        // Medio Oriente
        SpawnCity("Dubái",           25.20f, -124.73f, yellow);
        SpawnCity("Jeddah",          21.49f, -140.83f, yellow);

        // Asia del Sur
        SpawnCity("Mumbai",          19.08f, -107.12f, yellow);
        SpawnCity("Karachi",         24.86f, -112.99f, yellow);
        SpawnCity("Colombo",          6.93f, -100.16f, yellow);

        // Sudeste Asiático
        SpawnCity("Singapur",         1.35f,  -76.18f, yellow);
        SpawnCity("Bangkok",         13.75f,  -79.50f, yellow);
        SpawnCity("Ho Chi Minh",     10.82f,  -73.34f, yellow);
        SpawnCity("Manila",          14.60f,  -59.02f, yellow);

        // Asia del Este
        SpawnCity("Hong Kong",       22.32f,  -65.83f, yellow);
        SpawnCity("Shanghái",        31.23f,  -58.53f, yellow);
        SpawnCity("Taipéi",          25.03f,  -58.43f, yellow);
        SpawnCity("Tokio",           35.68f,  -40.31f, yellow);
        SpawnCity("Busan",           35.10f,  -50.96f, yellow);
        SpawnCity("Vladivostok",     43.12f,  -48.11f, yellow);

        // Oceanía
        SpawnCity("Sídney",         -33.87f,  -28.79f, yellow);
        SpawnCity("Auckland",       -36.86f,   -5.24f, yellow);
    }


    private void SpawnCity(string cityName, float lat, float lon, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = cityName;
        go.transform.localScale = new Vector3(85f, 85f, 1f);

        Destroy(go.GetComponent<Collider>());
        var col = go.AddComponent<SphereCollider>();
        col.radius = 1f;

        var rend   = go.GetComponent<MeshRenderer>();
        var shader = Shader.Find("FF/CityMarker");
        var mat    = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
        mat.color  = color;
        rend.sharedMaterial    = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        if (shader == null)
            Debug.LogWarning($"[Bootstrap] Shader 'FF/CityMarker' no encontrado para {cityName}.");

        var marker = go.AddComponent<CityMarker>();
        marker.cityName      = cityName;
        marker.latitude      = lat;
        marker.longitude     = lon;
        marker.surfaceOffset = 2f;
    }
}

// 👇 FUERA de GameBootstrapper
public class CloudLayerController : MonoBehaviour
{
    public float rotationSpeed      = 1.5f;
    public float opacity            = 0.15f;
    public bool  randomizeDirection = false; // activar para la capa de nubes clean

    // Tasas de aceleración (grados/s por segundo)
    private const float ACCEL_RATE = 0.25f;
    private const float DECEL_RATE = 0.6f;

    private Material _mat;
    private Vector3  _axis;
    private float    _currentSpeed;
    private float    _targetSpeed;
    private bool     _decelerating;
    private float    _timer;
    private float    _nextChangeSec;

    void Start()
    {
        var rend = GetComponent<MeshRenderer>();
        if (rend != null)
        {
            _mat = rend.material;
            if (_mat.HasProperty("_Color"))
            {
                var c = _mat.color;
                c.a = opacity;
                _mat.color = c;
            }
            SetupMaterialForTransparency(_mat);
        }

        _axis         = Vector3.up;
        _currentSpeed = 0f;                 // arranca desde cero → ease-in natural
        _targetSpeed  = rotationSpeed;
        ScheduleNextChange();
    }

    void Update()
    {
        float speedMult = TimeManager.Instance != null
            ? TimeManager.Instance.CurrentSpeedMultiplier : 1f;

        // Acelerar o desacelerar según si nos acercamos o alejamos de 0
        float rate = Mathf.Abs(_currentSpeed) > Mathf.Abs(_targetSpeed) ? DECEL_RATE : ACCEL_RATE;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, rate * Time.deltaTime);

        transform.Rotate(_axis, _currentSpeed * speedMult * Time.deltaTime);

        if (!randomizeDirection) return;

        _timer += Time.deltaTime;

        if (!_decelerating && _timer >= _nextChangeSec)
        {
            _targetSpeed  = 0f;   // empezar a frenar
            _decelerating = true;
        }

        if (_decelerating && Mathf.Abs(_currentSpeed) < 0.05f)
        {
            // Llegó a ~0: elegir nueva dirección y acelerar de a poco
            _currentSpeed = 0f;
            _decelerating = false;
            _timer        = 0f;
            PickNewDirection();
            ScheduleNextChange();
        }
    }

    private void PickNewDirection()
    {
        // Eje inclinado aleatoriamente entre 5° y 35° del eje Y
        float   tilt    = Random.Range(5f, 35f);
        Vector3 tiltDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        _axis = Quaternion.AngleAxis(tilt, tiltDir) * Vector3.up;

        // 30% de probabilidad de girar en sentido contrario
        float sign = Random.value < 0.3f ? -1f : 1f;
        _targetSpeed = rotationSpeed * sign;
    }

    private void ScheduleNextChange()
    {
        _nextChangeSec = Random.Range(20f, 60f);
    }

    private void SetupMaterialForTransparency(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
    }
}

public class AtmosphericHaloController : MonoBehaviour
{
    private Material _mat;
    private Camera   _cam;

    void Start()
    {
        _mat = GetComponent<MeshRenderer>()?.material;
        _cam = Camera.main;
    }

    void Update()
    {
        if (_mat == null || SunController.Instance == null) return;

        Vector3 sunDir = SunController.Instance.GetSunDirection();
        _mat.SetVector("_SunDir", sunDir);

        if (_cam != null)
        {
            Vector3 camToEarth = (transform.position - _cam.transform.position).normalized;
            float   backlit    = Mathf.Clamp01(Vector3.Dot(camToEarth, sunDir) * 2f);
            _mat.SetFloat("_BacklitFactor", backlit);
        }
    }
}