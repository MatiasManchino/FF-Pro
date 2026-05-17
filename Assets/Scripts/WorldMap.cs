using System;
using UnityEngine;
using System.Collections.Generic;

public class WorldMap : MonoBehaviour
{
    public static WorldMap Instance { get; private set; }

    [Header("Tierra")]
    public float        earthRadius = 1000f;
    public MeshRenderer earthMeshRenderer;

    [Header("Texturas mensuales")]
    [Tooltip("Ruta relativa a cualquier carpeta Resources/ del proyecto.")]
    public string texturesPath = "Map/Textures/";

    [Header("Textura nocturna")]
    [Range(0f, 1f)]
    public float nightBrightness = 0.3f;

    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private Material _mat;

    // Cached shader property IDs
    private static readonly int PropMainTex  = Shader.PropertyToID("_MainTex");
    private static readonly int PropBlendTex = Shader.PropertyToID("_BlendTex");
    private static readonly int PropBlend    = Shader.PropertyToID("_Blend");
    private static readonly int PropNightTex        = Shader.PropertyToID("_NightTex");
    private static readonly int PropSunDir          = Shader.PropertyToID("_SunDir");
    private static readonly int PropNightBrightness = Shader.PropertyToID("_NightBrightness");

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Scale here so CityMarkers can parent correctly during their Start().
        transform.localScale = Vector3.one * (earthRadius * 2f);
    }

    void Start()
    {
        if (earthMeshRenderer == null)
            earthMeshRenderer = GetComponent<MeshRenderer>();

        if (earthMeshRenderer == null)
        {
            Debug.LogError("[WorldMap] MeshRenderer no encontrado.");
            enabled = false;
            return;
        }

        _mat = earthMeshRenderer.material;
        LoadTextures();
    }

    void Update()
    {
        if (TimeManager.Instance == null || _textures.Count == 0) return;
        UpdateTexture(TimeManager.Instance.CurrentUtcTime);

        if (SunController.Instance != null)
            _mat.SetVector(PropSunDir, SunController.Instance.GetSunDirection());
    }

    private void LoadTextures()
    {
        _textures.Clear();
        for (int i = 1; i <= 12; i++)
        {
            var tex = Resources.Load<Texture2D>(texturesPath + i.ToString("00"));
            if (tex != null)
                _textures.Add(tex);
            else
                Debug.LogWarning($"[WorldMap] No se encontró: {texturesPath}{i:00}");
        }

        if (_textures.Count == 0)
        {
            Debug.LogError("[WorldMap] Sin texturas mensuales — el globo no se verá correctamente.");
            enabled = false;
            return;
        }

        var nightTex = Resources.Load<Texture2D>(texturesPath + "BlackMarble");
        if (nightTex != null && _mat.HasProperty(PropNightTex))
        {
            _mat.SetTexture(PropNightTex, nightTex);
            _mat.SetFloat(PropNightBrightness, nightBrightness);
        }
        else if (nightTex == null)
            Debug.LogWarning("[WorldMap] No se encontró BlackMarble en " + texturesPath);
    }

    private void UpdateTexture(DateTime utcTime)
    {
        if (_textures.Count < 12) return;

        int   cur   = utcTime.Month - 1;
        int   next  = (cur + 1) % 12;
        float blend = (float)utcTime.Day / DateTime.DaysInMonth(utcTime.Year, utcTime.Month);

        if (_mat.HasProperty(PropBlendTex))
        {
            _mat.SetTexture(PropMainTex,  _textures[cur]);
            _mat.SetTexture(PropBlendTex, _textures[next]);
            _mat.SetFloat(PropBlend, blend);
        }
        else
        {
            _mat.mainTexture = _textures[cur];
        }
    }

    // ── Coordinate conversion ─────────────────────────────────────────────────
    // Convention: lon=0 → +X, lon=90°E → +Z, North Pole → +Y.
    // This aligns with SunController: the sun is always in the +X half-space at UTC noon.
    
    /// <summary>
    /// Convierte coordenadas de latitud/longitud a posición 3D en la esfera.
    /// </summary>
    /// <param name="lat">Latitud en grados (-90 a 90).</param>
    /// <param name="lon">Longitud en grados (-180 a 180).</param>
    /// <param name="radius">Radio de la esfera.</param>
    /// <returns>Posición 3D en coordenadas locales.</returns>
    public Vector3 LatLonToPosition(float lat, float lon, float radius)
    {
        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;
        return new Vector3(
            radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad),
            radius * Mathf.Sin(latRad),
            radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad));
    }

    /// <summary>
    /// Convierte coordenadas de latitud/longitud a posición mundial.
    /// </summary>
    /// <param name="lat">Latitud en grados.</param>
    /// <param name="lon">Longitud en grados.</param>
    /// <returns>Posición 3D en coordenadas mundiales.</returns>
    public Vector3 LatLonToWorldPosition(float lat, float lon)
    {
        Vector3 localPos = LatLonToPosition(lat, lon, earthRadius);
        return transform.TransformPoint(localPos);
    }

    /// <summary>
    /// Convierte una posición mundial a coordenadas de latitud/longitud.
    /// </summary>
    /// <param name="worldPos">Posición mundial.</param>
    /// <returns>Vector2 con (latitud, longitud) en grados.</returns>
    public Vector2 WorldPositionToLatLon(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        return LocalPositionToLatLon(localPos);
    }

    /// <summary>
    /// Convierte una posición local de la esfera a coordenadas de latitud/longitud.
    /// </summary>
    /// <param name="localPos">Posición local relativa al centro de la Tierra.</param>
    /// <returns>Vector2 con (latitud, longitud) en grados.</returns>
    public Vector2 LocalPositionToLatLon(Vector3 localPos)
    {
        Vector3 dir = localPos.normalized;
        
        float lat = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float lon = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        
        return new Vector2(lat, lon);
    }

    /// <summary>
    /// Obtiene la normal de la superficie en una posición mundial.
    /// </summary>
    /// <param name="worldPos">Posición mundial.</param>
    /// <returns>Vector normal hacia afuera de la esfera.</returns>
    public Vector3 GetSurfaceNormal(Vector3 worldPos) => (worldPos - transform.position).normalized;

    /// <summary>
    /// Obtiene la normal de la superficie en una latitud/longitud dada.
    /// </summary>
    /// <param name="lat">Latitud en grados.</param>
    /// <param name="lon">Longitud en grados.</param>
    /// <returns>Vector normal hacia afuera en la posición especificada.</returns>
    public Vector3 GetSurfaceNormalAtLatLon(float lat, float lon)
    {
        Vector3 localPos = LatLonToPosition(lat, lon, 1.0f);
        return transform.TransformDirection(localPos).normalized;
    }

    /// <summary>
    /// Calcula la distancia en línea recta entre dos puntos sobre la superficie.
    /// </summary>
    /// <param name="lat1">Latitud del primer punto.</param>
    /// <param name="lon1">Longitud del primer punto.</param>
    /// <param name="lat2">Latitud del segundo punto.</param>
    /// <param name="lon2">Longitud del segundo punto.</param>
    /// <returns>Distancia en unidades Unity.</returns>
    public float DistanceBetweenPoints(float lat1, float lon1, float lat2, float lon2)
    {
        Vector3 pos1 = LatLonToPosition(lat1, lon1, earthRadius);
        Vector3 pos2 = LatLonToPosition(lat2, lon2, earthRadius);
        return Vector3.Distance(pos1, pos2);
    }

    /// <summary>
    /// Calcula la distancia del arco de círculo máximo entre dos puntos (más precisa que la distancia lineal).
    /// </summary>
    /// <param name="lat1">Latitud del primer punto.</param>
    /// <param name="lon1">Longitud del primer punto.</param>
    /// <param name="lat2">Latitud del segundo punto.</param>
    /// <param name="lon2">Longitud del segundo punto.</param>
    /// <returns>Distancia del arco en unidades Unity.</returns>
    public float GreatCircleDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float lat1Rad = lat1 * Mathf.Deg2Rad;
        float lon1Rad = lon1 * Mathf.Deg2Rad;
        float lat2Rad = lat2 * Mathf.Deg2Rad;
        float lon2Rad = lon2 * Mathf.Deg2Rad;

        float deltaLon = lon2Rad - lon1Rad;
        
        float a = Mathf.Sin((lat2Rad - lat1Rad) * 0.5f);
        float b = Mathf.Sin(deltaLon * 0.5f);
        
        float h = a * a + Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) * b * b;
        float angle = 2f * Mathf.Atan2(Mathf.Sqrt(h), Mathf.Sqrt(1f - h));
        
        return earthRadius * angle;
    }

    /// <summary>
    /// Verifica si un punto está en el lado iluminado de la Tierra.
    /// Requiere SunController.Instance para funcionar.
    /// </summary>
    /// <param name="worldPos">Posición mundial a verificar.</param>
    /// <returns>True si el punto está iluminado por el sol.</returns>
    public bool IsPointInDaylight(Vector3 worldPos)
    {
        if (SunController.Instance == null) return false;
        return SunController.Instance.IsDaylightAtPosition(worldPos, transform.position);
    }

    /// <summary>
    /// Verifica si una coordenada geográfica está en el lado iluminado de la Tierra.
    /// </summary>
    /// <param name="lat">Latitud en grados.</param>
    /// <param name="lon">Longitud en grados.</param>
    /// <returns>True si está iluminado por el sol.</returns>
    public bool IsLatLonInDaylight(float lat, float lon)
    {
        Vector3 worldPos = LatLonToWorldPosition(lat, lon);
        return IsPointInDaylight(worldPos);
    }

    /// <summary>
    /// Obtiene el ángulo del sol sobre el horizonte en una posición dada.
    /// </summary>
    /// <param name="worldPos">Posición mundial.</param>
    /// <returns>Ángulo en grados (negativo = noche, 0 = horizonte, 90 = cenit).</returns>
    public float GetSunAngleAtPoint(Vector3 worldPos)
    {
        if (SunController.Instance == null) return -90f;
        return SunController.Instance.GetSunAngleAtPosition(worldPos, transform.position);
    }

    /// <summary>
    /// Obtiene el mesh renderer de la Tierra.
    /// </summary>
    public MeshRenderer GetMeshRenderer()
    {
        return earthMeshRenderer;
    }

    /// <summary>
    /// Obtiene la textura actual de la Tierra.
    /// </summary>
    public Texture GetCurrentTexture()
    {
        if (_mat != null)
            return _mat.mainTexture;
        return null;
    }

    /// <summary>
    /// Obtiene el nombre del mes actual basado en la textura cargada.
    /// </summary>
    public string GetCurrentMonthName()
    {
        if (TimeManager.Instance == null) return "Desconocido";
        return TimeManager.Instance.CurrentUtcTime.ToString("MMMM");
    }

    /// <summary>
    /// Calcula el punto subsolar (donde el sol está en el cenit).
    /// </summary>
    /// <returns>Vector2 con (latitud, longitud) del punto subsolar.</returns>
    public Vector2 GetSubsolarPoint()
    {
        if (SunController.Instance == null || TimeManager.Instance == null)
            return Vector2.zero;

        // La declinación solar es la latitud del punto subsolar
        float subsolarLat = SunController.Instance.GetCurrentDeclination();
        
        // La longitud del punto subsolar cambia con la hora del día
        DateTime utc = TimeManager.Instance.CurrentUtcTime;
        double hoursSinceNoon = utc.TimeOfDay.TotalHours - 12.0;
        float subsolarLon = (float)(hoursSinceNoon * 15.0);
        
        // Normalizar longitud a -180/180
        subsolarLon = ((subsolarLon + 180f) % 360f) - 180f;
        
        return new Vector2(subsolarLat, subsolarLon);
    }

    // ── Diagnóstico en editor ─────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Dibujar ecuador
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        DrawCircle(Vector3.zero, Vector3.up, earthRadius, 360);
        
        // Dibujar meridiano de Greenwich
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
        DrawCircle(Vector3.zero, Vector3.forward, earthRadius, 360);
        
        // Dibujar punto subsolar
        if (SunController.Instance != null && TimeManager.Instance != null)
        {
            Vector2 subsolar = GetSubsolarPoint();
            Vector3 subsolarWorld = LatLonToWorldPosition(subsolar.x, subsolar.y);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(subsolarWorld, 30f);
        }
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments)
    {
        Vector3 from = Vector3.Cross(normal, Vector3.up).normalized;
        if (from.sqrMagnitude < 0.001f)
            from = Vector3.Cross(normal, Vector3.forward).normalized;
        
        Vector3 prevPoint = center + from * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 dir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, normal) * from;
            Vector3 nextPoint = center + dir * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
#endif
}