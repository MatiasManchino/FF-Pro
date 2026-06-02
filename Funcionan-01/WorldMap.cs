using System;
using UnityEngine;
using System.Collections.Generic;

public class WorldMap : MonoBehaviour
{
// Gestiona instance.
    public static WorldMap Instance { get; private set; }

    [Header("Tierra")]
    public float        earthRadius = 10f;
    public MeshRenderer earthMeshRenderer;

    [Header("Texturas mensuales")]
    [Tooltip("Ruta relativa a cualquier carpeta Resources/ del proyecto.")]
    public string texturesPath = "Map/Textures/";

    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private Material _mat;

    // Almacenado en caché sombreado property IDs
    private static readonly int PropMainTex  = Shader.PropertyToID("_MainTex");
    private static readonly int PropBlendTex = Shader.PropertyToID("_BlendTex");
    private static readonly int PropBlend    = Shader.PropertyToID("_Blend");

// Configura referencias tempranas antes de Start.
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Escala here para que CityMarkers puede padre correctamente during their Start().
        transform.localScale = Vector3.one * (earthRadius * 2f);
    }

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
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

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (TimeManager.Instance == null || _textures.Count == 0) return;
        UpdateTexture(TimeManager.Instance.CurrentUtcTime);
    }

// Carga texturas
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
        }
    }

// Actualiza textura
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
    public Vector3 LatLonToPosition(float lat, float lon, float radius)
    {
        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;
        return new Vector3(
            radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad),
            radius * Mathf.Sin(latRad),
            radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad));
    }

// Obtiene superficie normal
    public Vector3 GetSurfaceNormal(Vector3 worldPos) => (worldPos - transform.position).normalized;
}