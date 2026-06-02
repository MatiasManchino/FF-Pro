using UnityEngine;
using UnityEngine.Rendering;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Referencias (se auto-resuelven si están vacías)")]
    public TimeManager         timeManager;
    public SunController       sunController;
    public WorldMap            worldMap;
    public MapCameraController mapCameraController;
    public UIManager           uiManager;

// Configura referencias tempranas antes de Start.
    void Awake()
    {
        EnsureTimeManager();
        EnsureWorldMap();
        EnsureSunController();
        EnsureCamera();
        EnsureUIManager();
        SetupBackground();
        CreateSunVisual();
        SpawnCities();
    }

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        // El MapCameraController se inicializa solo (espera un fotograma y apunta a Buenos Aires)
        uiManager?.CenterUIHubPanel();
    }

// Gestiona ensure tiempo gestor.
    private void EnsureTimeManager()
    {
        if (timeManager != null) return;
        timeManager = Object.FindAnyObjectByType<TimeManager>();
        if (timeManager == null)
            timeManager = new GameObject("TimeManager").AddComponent<TimeManager>();
    }

// Gestiona ensure mundo mapa.
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
            // Realiza material
            ? new Material(shader)
            : new Material(Shader.Find("Standard"));

        if (shader == null)
            Debug.LogWarning("[Bootstrap] Shader 'Custom/EarthBlend' no encontrado — usando Standard.");
    }

// Gestiona ensure sol controlador.
    private void EnsureSunController()
    {
        if (sunController != null) return;
        sunController = Object.FindAnyObjectByType<SunController>();
        if (sunController != null) return;

        var go = new GameObject("SunController");
        sunController = go.AddComponent<SunController>();

        Light dirLight = null;
// Foreach
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

// Gestiona ensure cámara.
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

// Gestiona ensure UI gestor.
    private void EnsureUIManager()
    {
        if (uiManager != null) return;
        uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null)
            uiManager = new GameObject("UIManager").AddComponent<UIManager>();
    }

// Establece up background.
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
            cam.farClipPlane    = 500f;
        }
    }

// Crea sun visual
    private void CreateSunVisual()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SunVisual";
        go.transform.localScale = Vector3.one * 9f;
        Destroy(go.GetComponent<SphereCollider>());

        var rend = go.GetComponent<MeshRenderer>();
        var mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0.95f, 0.65f);
        rend.sharedMaterial   = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        sunController.sunVisual = go.transform;
    }

// Genera cities.
    private void SpawnCities()
    {
        SpawnCity("Buenos Aires", -38.45f, -58.38f, new Color(1.0f, 0.25f, 0.1f));
        SpawnCity("Roma",          48.19f,  12.66f, new Color(0.2f, 0.55f, 1.0f));
        SpawnCity("Miami",         26.89f, -79.98f, new Color(0.2f, 1.00f, 0.4f));
    }

// Genera ciudad.
    private void SpawnCity(string cityName, float lat, float lon, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = cityName;
        go.transform.localScale = Vector3.one * 0.35f;

        var rend = go.GetComponent<MeshRenderer>();
        var mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        rend.sharedMaterial   = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        var marker = go.AddComponent<CityMarker>();
        marker.latitude      = lat;
        marker.longitude     = lon;
        marker.surfaceOffset = 0.15f;
    }
}