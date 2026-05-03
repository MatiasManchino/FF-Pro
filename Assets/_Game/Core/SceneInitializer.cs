using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SceneInitializer configura la escena del juego automáticamente si no existe.
/// Crea el mapa, cámara, Canvas UI y GameObjects de managers.
/// </summary>
[DefaultExecutionOrder(-200)]
public class SceneInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeScene()
    {
        // Si la escena ya está configurada, no hacer nada
        if (FindAnyObjectByType<MapManager>() != null)
            return;

        Debug.Log("Inicializando escena del juego...");

        // Crear GameObject raíz para managers
        GameObject managersObject = new GameObject("_Managers");
        
        // Crear GameObject para el mapa
        GameObject mapObject = new GameObject("Map");
        mapObject.transform.SetParent(managersObject.transform);
        
        // Crear Quad para visualizar el mapa
        GameObject mapQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mapQuad.name = "MapQuad";
        mapQuad.transform.SetParent(mapObject.transform);
        mapQuad.transform.localPosition = Vector3.zero;
        mapQuad.transform.localScale = new Vector3(10f, 10f, 1f);
        
        // Remover el collider del quad
        Collider collider = mapQuad.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);
        
        // Obtener el renderer y configurar material
        Renderer mapRenderer = mapQuad.GetComponent<Renderer>();
        if (mapRenderer != null)
        {
            Shader textureShader = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            mapRenderer.material = new Material(textureShader);
        }

        // Crear cámara
        GameObject cameraObject = new GameObject("GameCamera");
        cameraObject.transform.SetParent(managersObject.transform);
        cameraObject.transform.position = new Vector3(0f, 3f, -8f);
        cameraObject.transform.LookAt(mapObject.transform);
        
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        // Crear Canvas para UI
        GameObject canvasObject = new GameObject("GameUI");
        canvasObject.transform.SetParent(managersObject.transform);
        
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Crear GameBootstrapper
        GameObject bootstrapperObject = new GameObject("GameBootstrapper");
        bootstrapperObject.transform.SetParent(managersObject.transform);
        
        GameBootstrapper bootstrapper = bootstrapperObject.AddComponent<GameBootstrapper>();

        // Crear MapManager
        GameObject mapManagerObject = new GameObject("MapManager");
        mapManagerObject.transform.SetParent(managersObject.transform);
        
        MapManager mapManager = mapManagerObject.AddComponent<MapManager>();
        mapManager.SetMapRenderer(mapRenderer);
        mapManager.SetMapRoot(mapObject.transform);

        Debug.Log("Escena inicializada correctamente.");
    }
}
