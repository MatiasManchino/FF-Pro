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
        // rutas demo removidas — las rutas dinámicas las maneja RouteManager
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
        RenderSettings.skybox      = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.03f, 0.06f);

        var cam = mapCameraController?.GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.farClipPlane    = 20000f;
        }
    }

    private void CreateSunVisual()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SunVisual";
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
        Color yellow = new Color(1f, 0.9f, 0f)
        ;
        
        // América del Sur
        SpawnCity("Buenos Aires", -38.46f, -58.38f, yellow);
        SpawnCity("São Paulo", -24.90f, -46.63f, yellow);
        SpawnCity("Valparaíso", -36.64f, -71.63f, yellow);
        SpawnCity("Santiago", -37.15f, -70.67f, yellow);
        SpawnCity("Lima", -12.99f, -77.04f, yellow);
        SpawnCity("Bogotá", 5.94f, -74.07f, yellow);
        SpawnCity("Cartagena", 10.36f, -74.51f, yellow);
        
        // América del Norte
        SpawnCity("Miami", 29.71f, -79.98f, yellow);
        SpawnCity("New York", 46.57f, -74.00f, yellow);
        SpawnCity("Los Ángeles", 39.09f, -118.24f, yellow);
        SpawnCity("Houston", 34.22f, -95.37f, yellow);
        SpawnCity("Vancouver", 56.25f, -123.12f, yellow);
        SpawnCity("Ciudad de México", 22.56f, -99.13f, yellow);
        
        // América Central
        SpawnCity("Panamá", 10.78f, -79.50f, yellow);
        
        // Europa Occidental
        SpawnCity("Roma", 48.19f, 12.66f, yellow);
        SpawnCity("Rotterdam", 59.23f, 4.48f, yellow);
        SpawnCity("London", 58.77f, -0.13f, yellow);
        SpawnCity("Amberes", 58.44f, 4.40f, yellow);
        SpawnCity("Barcelona", 47.34f, 2.16f, yellow);
        SpawnCity("Marsella", 49.50f, 5.37f, yellow);
        SpawnCity("Madrid", 46.24f, -3.70f, yellow);
        SpawnCity("París", 55.78f, 2.35f, yellow);
        SpawnCity("Frankfurt", 57.19f, 8.68f, yellow);
        SpawnCity("Hamburgo", 61.07f, 9.99f, yellow);
        SpawnCity("Casablanca", 38.55f, -7.59f, yellow);
        
        // Europa del Este y Mediterráneo
        SpawnCity("Atenas", 43.49f, 23.73f, yellow);
        SpawnCity("Estambul", 46.91f, 28.98f, yellow);
        
        // África
        SpawnCity("Johannesburgo", -28.95f, 28.05f, yellow);
        SpawnCity("Cape Town", -37.00f, 19.50f, yellow);
        SpawnCity("Port Said", 35.91f, 32.28f, yellow);
        SpawnCity("Mombasa", -3.95f, 39.67f, yellow);
        
        // Medio Oriente
        SpawnCity("Dubái", 29.17f, 55.30f, yellow);
        SpawnCity("Jeddah", 24.93f, 39.17f, yellow);
        
        // Asia del Sur
        SpawnCity("Mumbai", 22.17f, 72.88f, yellow);
        SpawnCity("Karachi", 28.70f, 67.00f, yellow);
        SpawnCity("Colombo", 8.44f, 79.84f, yellow);
        
        // Sudeste Asiático
        SpawnCity("Singapur", 2.14f, 103.82f, yellow);
        SpawnCity("Bangkok", 16.14f, 100.52f, yellow);
        SpawnCity("Ho Chi Minh", 12.84f, 106.63f, yellow);
        SpawnCity("Manila", 17.10f, 120.98f, yellow);
        
        // Asia del Este
        SpawnCity("Hong Kong", 25.82f, 114.17f, yellow);
        SpawnCity("Shanghái", 35.88f, 121.47f, yellow);
        SpawnCity("Taipéi", 28.88f, 121.57f, yellow);
        SpawnCity("Tokio", 40.90f, 139.69f, yellow);
        SpawnCity("Busan", 40.24f, 129.04f, yellow);
        SpawnCity("Vladivostok", 49.28f, 131.89f, yellow);
        
        // Oceanía
        SpawnCity("Sídney", -37.62f, 151.21f, yellow);
        SpawnCity("Auckland", -40.97f, 174.76f, yellow);
    }


    private void SpawnCity(string cityName, float lat, float lon, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = cityName;
        go.transform.localScale = new Vector3(5f, 0.8f, 5f);

        Destroy(go.GetComponent<Collider>());                    // Eliminar collider del cilindro
        var col = go.AddComponent<SphereCollider>();            // Agregar SphereCollider
        col.radius = 11f;                                        // Radio grande para el mouse

        var rend = go.GetComponent<MeshRenderer>();
        var mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        rend.sharedMaterial   = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        var marker = go.AddComponent<CityMarker>();
        marker.cityName      = cityName;
        marker.latitude      = lat;
        marker.longitude     = lon;
        marker.surfaceOffset = -4f;
    }
}