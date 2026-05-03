using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MapManager carga y administra las texturas del mapa ubicadas en Assets/Art/Map/Resources/Map/Textures.
/// También gestiona marcadores de ciudad y rutas en el mapa.
/// </summary>
public class MapManager : Singleton<MapManager>
{
    [Header("Referencias de visualización")]
    [SerializeField] private Renderer mapRenderer;
    [SerializeField] private Transform mapRoot;

    [Header("Configuración de mapa")]
    [SerializeField] private string resourcesFolder = "Map/Textures";
    [SerializeField] private string defaultMapTextureName = "05";

    [Header("Ciudades")]
    [SerializeField] private List<WorldCity> worldCities = new List<WorldCity>();
    [SerializeField] private GameObject cityMarkerPrefab;
    [SerializeField] private Transform markersParent;

    [Header("Rutas")]
    [SerializeField] private GameObject routePrefab;
    [SerializeField] private Transform routesParent;

    private Dictionary<string, Texture2D> loadedMapTextures = new Dictionary<string, Texture2D>();
    private List<CityMarker> cityMarkers = new List<CityMarker>();
    private List<MapRouteRenderer> routeRenderers = new List<MapRouteRenderer>();
    private string currentMapTextureName;
    private string pendingMapTextureName;

    public string CurrentMapTextureName => currentMapTextureName;
    public IReadOnlyCollection<string> AvailableMapTextures => loadedMapTextures.Keys;
    public IReadOnlyList<CityMarker> CityMarkers => cityMarkers;
    public IReadOnlyList<MapRouteRenderer> RouteRenderers => routeRenderers;
    public IReadOnlyList<WorldCity> WorldCities => worldCities;

    protected override void Awake()
    {
        base.Awake();
        LoadMapTextures();
    }

    private void Start()
    {
        SpawnCityMarkers();
    }

    /// <summary>
    /// Carga todas las texturas disponibles dentro de Resources/Map/Textures.
    /// </summary>
    private void LoadMapTextures()
    {
        loadedMapTextures.Clear();

        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcesFolder);
        foreach (Texture2D texture in textures)
        {
            if (texture != null && !loadedMapTextures.ContainsKey(texture.name))
            {
                loadedMapTextures.Add(texture.name, texture);
            }
        }

        if (loadedMapTextures.Count == 0)
        {
            Debug.LogWarning($"No se encontraron texturas de mapa en Resources/{resourcesFolder}");
            return;
        }

        Debug.Log($"MapManager cargó {loadedMapTextures.Count} texturas de mapa");
        pendingMapTextureName = defaultMapTextureName;
        if (mapRenderer != null)
        {
            LoadMap(defaultMapTextureName);
            pendingMapTextureName = null;
        }
    }

    /// <summary>
    /// Carga una textura de mapa por nombre.
    /// </summary>
    /// <param name="textureName">Nombre del archivo de textura sin extensión</param>
    public void LoadMap(string textureName)
    {
        if (string.IsNullOrEmpty(textureName))
        {
            Debug.LogWarning("Nombre de textura de mapa inválido");
            return;
        }

        if (!loadedMapTextures.TryGetValue(textureName, out Texture2D texture))
        {
            Debug.LogWarning($"Textura de mapa no encontrada: {textureName}. Usando predeterminada.");
            if (!loadedMapTextures.TryGetValue(defaultMapTextureName, out texture))
                return;

            textureName = defaultMapTextureName;
        }

        if (mapRenderer != null)
        {
            mapRenderer.material.mainTexture = texture;
        }

        currentMapTextureName = textureName;
        Debug.Log($"Mapa cargado: {textureName}");
    }

    /// <summary>
    /// Genera marcadores para todas las ciudades definidas en el mapa.
    /// </summary>
    public void SpawnCityMarkers()
    {
        ClearCityMarkers();

        if (worldCities == null || worldCities.Count == 0)
        {
            Debug.LogWarning("No hay ciudades configuradas para el mapa.");
            return;
        }

        foreach (WorldCity city in worldCities)
        {
            if (city == null) continue;

            Vector3 position = GetCityWorldPosition(city);
            GameObject markerObject;
            Transform markerParent = markersParent != null ? markersParent : mapRoot;

            if (cityMarkerPrefab != null)
            {
                markerObject = Instantiate(cityMarkerPrefab, position, Quaternion.identity, markerParent);
            }
            else
            {
                markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerObject.transform.SetParent(markerParent, true);
                markerObject.transform.position = position;
                markerObject.transform.localScale = Vector3.one * 0.2f;
                Destroy(markerObject.GetComponent<Collider>());
            }

            markerObject.name = $"CityMarker_{city.Name}";
            CityMarker cityMarker = markerObject.GetComponent<CityMarker>() ?? markerObject.AddComponent<CityMarker>();
            cityMarker.Initialize(city, position, Color.red);
            cityMarkers.Add(cityMarker);
        }

        Debug.Log($"Spawned {cityMarkers.Count} city markers on the map.");
    }

    /// <summary>
    /// Remueve todos los marcadores de ciudad del mapa.
    /// </summary>
    public void ClearCityMarkers()
    {
        foreach (CityMarker marker in cityMarkers)
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }

        cityMarkers.Clear();
    }

    /// <summary>
    /// Dibuja una ruta entre dos ciudades.
    /// </summary>
    /// <param name="origin">Ciudad de origen</param>
    /// <param name="destination">Ciudad de destino</param>
    /// <param name="color">Color de la ruta</param>
    /// <param name="width">Ancho de la línea</param>
    public MapRouteRenderer DrawRoute(WorldCity origin, WorldCity destination, Color color, float width = 0.1f)
    {
        if (origin == null || destination == null)
        {
            Debug.LogWarning("Origen o destino inválido para dibujar ruta.");
            return null;
        }

        Vector3 originPosition = GetCityWorldPosition(origin);
        Vector3 destinationPosition = GetCityWorldPosition(destination);

        GameObject routeObject;
        if (routePrefab != null)
        {
            routeObject = Instantiate(routePrefab, Vector3.zero, Quaternion.identity, routesParent);
        }
        else
        {
            routeObject = new GameObject($"Route_{origin.Name}_{destination.Name}");
            routeObject.transform.SetParent(routesParent, true);
            routeObject.AddComponent<LineRenderer>();
        }

        MapRouteRenderer routeRenderer = routeObject.GetComponent<MapRouteRenderer>() ?? routeObject.AddComponent<MapRouteRenderer>();
        routeRenderer.Initialize(originPosition, destinationPosition, color, width);
        routeRenderers.Add(routeRenderer);

        return routeRenderer;
    }

    /// <summary>
    /// Elimina todas las rutas dibujadas en el mapa.
    /// </summary>
    public void ClearRoutes()
    {
        foreach (MapRouteRenderer renderer in routeRenderers)
        {
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }

        routeRenderers.Clear();
    }

    /// <summary>
    /// Obtiene la posición en mundo de una ciudad sobre el mapa.
    /// </summary>
    /// <param name="city">Ciudad a convertir</param>
    /// <returns>Posición en el espacio del mapa</returns>
    public Vector3 GetCityWorldPosition(WorldCity city)
    {
        if (city == null)
            return Vector3.zero;

        if (mapRenderer == null)
            return Vector3.zero;

        if (!TryGetMapSurfaceAxes(out Vector3 lonAxis, out Vector3 latAxis, out float width, out float height))
            return mapRenderer.bounds.center;

        float normalizedLon = (city.Longitude + 180f) / 360f - 0.5f;
        float normalizedLat = (city.Latitude + 90f) / 180f - 0.5f;

        Vector3 worldPos = mapRenderer.bounds.center + lonAxis * (normalizedLon * width) + latAxis * (normalizedLat * height);
        return worldPos;
    }

    private bool TryGetMapSurfaceAxes(out Vector3 lonAxis, out Vector3 latAxis, out float width, out float height)
    {
        lonAxis = Vector3.right;
        latAxis = Vector3.up;
        width = 1f;
        height = 1f;

        if (mapRenderer == null)
            return false;

        Bounds bounds = mapRenderer.bounds;
        Vector3 size = bounds.size;

        List<(float size, Vector3 axis)> axes = new List<(float, Vector3)>
        {
            (size.x, mapRenderer.transform.right),
            (size.y, mapRenderer.transform.up),
            (size.z, mapRenderer.transform.forward)
        };

        axes.Sort((a, b) => b.size.CompareTo(a.size));

        width = axes[0].size;
        height = axes[1].size;
        lonAxis = axes[0].axis.normalized;
        latAxis = axes[1].axis.normalized;

        return true;
    }

    /// <summary>
    /// Cambia el tamaño del mapa a través del transform de la raíz.
    /// </summary>
    /// <param name="scale">Escala uniforme</param>
    public void SetMapScale(float scale)
    {
        if (mapRoot != null)
        {
            mapRoot.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        }
    }

    /// <summary>
    /// Activa o desactiva la visualización del mapa.
    /// </summary>
    public void SetMapVisible(bool visible)
    {
        if (mapRoot != null)
        {
            mapRoot.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Devuelve la lista de nombres de texturas cargadas.
    /// </summary>
    public List<string> GetMapTextureNames()
    {
        return new List<string>(loadedMapTextures.Keys);
    }

    /// <summary>
    /// Establece el renderer del mapa.
    /// </summary>
    public void SetMapRenderer(Renderer renderer)
    {
        mapRenderer = renderer;
        if (!string.IsNullOrEmpty(pendingMapTextureName) && loadedMapTextures.Count > 0)
        {
            LoadMap(pendingMapTextureName);
            pendingMapTextureName = null;
        }
    }

    /// <summary>
    /// Establece la raíz del mapa.
    /// </summary>
    public void SetMapRoot(Transform root)
    {
        mapRoot = root;
    }
}
