using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-200)]
public class SceneInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeScene()
    {
        if (FindAnyObjectByType<MapManager>() != null)
            return;

        Debug.Log("Inicializando escena del juego...");

        // ── Ambiente oscuro para que el lado nocturno de la Tierra sea negro ──
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.08f);

        // ── Raíz de managers ──────────────────────────────────────────────────
        GameObject managersObject = new GameObject("_Managers");

        // ── Globo terráqueo ───────────────────────────────────────────────────
        GameObject mapObject = new GameObject("Map");
        mapObject.transform.SetParent(managersObject.transform);

        GameObject mapSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mapSphere.name = "MapSphere";
        mapSphere.transform.SetParent(mapObject.transform);
        mapSphere.transform.localPosition = Vector3.zero;
        mapSphere.transform.localScale    = new Vector3(10f, 10f, 10f);

        Collider col = mapSphere.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        Renderer mapRenderer = mapSphere.GetComponent<Renderer>();
        if (mapRenderer != null)
        {
            // Standard para que responda a la luz solar
            Shader stdShader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            mapRenderer.material = new Material(stdShader);
            // Sin reflexión especular para que se vea como mapa
            mapRenderer.material.SetFloat("_Metallic",   0f);
            mapRenderer.material.SetFloat("_Glossiness", 0f);
        }

        // ── Sol (luz direccional) ─────────────────────────────────────────────
        GameObject sunObject = new GameObject("Sun");
        Light sunLight = sunObject.AddComponent<Light>();
        sunLight.type      = LightType.Directional;
        sunLight.intensity = 1.3f;
        sunLight.color     = new Color(1f, 0.95f, 0.85f);
        sunObject.transform.rotation = Quaternion.Euler(23.5f, 0f, 0f);

        SunController sunController = sunObject.AddComponent<SunController>();
        sunController.SetLight(sunLight);

        // ── Cámara ────────────────────────────────────────────────────────────
        GameObject cameraObject = new GameObject("GameCamera");
        cameraObject.transform.SetParent(managersObject.transform);
        cameraObject.transform.position = new Vector3(0f, 4f, -18f);
        cameraObject.transform.LookAt(mapObject.transform);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags       = CameraClearFlags.SolidColor;
        camera.backgroundColor  = new Color(0.02f, 0.02f, 0.06f); // espacio

        MapCameraController camCtrl = cameraObject.AddComponent<MapCameraController>();
        camCtrl.SetCamera(camera);
        camCtrl.SetFocusPoint(mapObject.transform);

        // ── Canvas UI (legacy — GameUIPanel usa OnGUI, esto es por si acaso) ─
        GameObject canvasObject = new GameObject("GameUI");
        canvasObject.transform.SetParent(managersObject.transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // ── GameBootstrapper ──────────────────────────────────────────────────
        GameObject bootstrapperObject = new GameObject("GameBootstrapper");
        bootstrapperObject.transform.SetParent(managersObject.transform);
        bootstrapperObject.AddComponent<GameBootstrapper>();

        // ── MapManager ────────────────────────────────────────────────────────
        GameObject mapManagerObject = new GameObject("MapManager");
        mapManagerObject.transform.SetParent(managersObject.transform);
        MapManager mapManager = mapManagerObject.AddComponent<MapManager>();
        mapManager.SetMapRenderer(mapRenderer);
        mapManager.SetMapRoot(mapObject.transform);

        // ── SunController al Bootstrapper para que Initialize() lo alcance ───
        // (ya se inicializa solo en su propio Awake/Start; lo guardamos también aquí)

        Debug.Log("Escena inicializada correctamente.");
    }
}
