using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Utils;

namespace FreightForwarder.Map
{
    /// <summary>
    /// WorldMap.cs — Controlador del globo terrestre 3D con texturas estacionales
    /// </summary>
    public class WorldMap : Singleton<WorldMap>
    {
        [Header("Referencias")]
        [SerializeField] private Camera _mapCamera;
        [SerializeField] private Transform _earthTransform;
        [SerializeField] private MeshRenderer _earthRenderer;
        
        [Header("Configuración del Globo")]
        [SerializeField] private float _earthRadius = 10f;
        [SerializeField] private Material _earthMaterial;
        [SerializeField] private Texture2D _defaultTexture;
        
        [Header("Texturas Estacionales")]
        [SerializeField] private Texture2D[] _monthlyTextures;
        [SerializeField] private bool _enableSeasonalTextures = true;
        [SerializeField] private float _textureBlendSpeed = 0.5f;
        
        [Header("Efectos Climáticos")]
        [SerializeField] private ParticleSystem _rainEffect;
        [SerializeField] private ParticleSystem _snowEffect;
        [SerializeField] private ParticleSystem _cloudsEffect;
        
        [Header("Configuración de Marcadores")]
        [SerializeField] private GameObject _cityMarkerPrefab;
        [SerializeField] private float _markerScale = 0.2f;
        [SerializeField] private Color _unlockedCityColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color _lockedCityColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _officeCityColor = new Color(1f, 0.8f, 0.2f);
        
        // =========================================================================
        // PROPIEDADES
        // =========================================================================
        
        public float EarthRadius => _earthRadius;
        public Transform EarthTransform => _earthTransform;
        public Camera MapCamera => _mapCamera;
        
        // =========================================================================
        // VARIABLES PRIVADAS
        // =========================================================================
        
        private Dictionary<string, CityMarker> _cityMarkers;
        private List<RouteRenderer> _activeRoutes;
        private CameraController _cameraController;
        private Material _activeEarthMaterial;
        private int _currentMonth = -1;
        private Coroutine _blendCoroutine;
        
        // =========================================================================
        // EVENTOS
        // =========================================================================
        
        public event Action<WorldCity> OnCityClicked;
        public event Action<WorldCity> OnCityHovered;
        
        // =========================================================================
        // INICIALIZACIÓN
        // =========================================================================
        
        protected override void OnAwake()
        {
            _cityMarkers = new Dictionary<string, CityMarker>();
            _activeRoutes = new List<RouteRenderer>();
            
            // Crear el globo si no existe
            if (_earthTransform == null)
            {
                CreateEarth();
            }
            
            // Configurar material
            if (_earthRenderer != null)
            {
                _activeEarthMaterial = _earthRenderer.material;
                if (_defaultTexture != null && _activeEarthMaterial != null)
                {
                    _activeEarthMaterial.mainTexture = _defaultTexture;
                }
            }
            else if (_earthMaterial != null)
            {
                _activeEarthMaterial = _earthMaterial;
            }
            
            // Configurar cámara
            if (_mapCamera == null)
            {
                _mapCamera = Camera.main;
            }
            
            // Configurar controlador de cámara
            _cameraController = GetComponent<CameraController>();
            if (_cameraController == null)
            {
                _cameraController = gameObject.AddComponent<CameraController>();
            }
            
            if (_mapCamera != null && _earthTransform != null)
            {
                _cameraController.Initialize(_mapCamera, _earthTransform);
            }
            
            // Cargar texturas estacionales automáticamente desde Resources
            if (_enableSeasonalTextures)
            {
                LoadMonthlyTexturesFromResources();
            }
            
            // Crear marcadores de ciudades (después de que el globo exista)
            CreateCityMarkers();
            
            // Suscribirse al cambio de fecha
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateChanged += OnDateChanged;
                
                // Aplicar textura del mes actual
                if (_enableSeasonalTextures && _monthlyTextures != null)
                {
                    int currentMonth = TimeManager.Instance.CurrentDate.Month;
                    SetMonthTexture(currentMonth);
                    _currentMonth = currentMonth;
                }
            }
            
            Debug.Log("[WorldMap] Inicializado");
        }
        
        /// <summary>
        /// Crea la esfera del globo terrestre
        /// </summary>
        private void CreateEarth()
        {
            GameObject earthObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthObj.name = "Earth";
            earthObj.transform.SetParent(transform);
            earthObj.transform.localPosition = Vector3.zero;
            earthObj.transform.localScale = Vector3.one * _earthRadius * 2f;
            
            _earthTransform = earthObj.transform;
            _earthRenderer = earthObj.GetComponent<MeshRenderer>();
            
            // Asignar material por defecto si existe
            if (_earthMaterial != null && _earthRenderer != null)
            {
                _earthRenderer.material = _earthMaterial;
            }
            
            Debug.Log("[WorldMap] Globo terrestre creado");
        }
        
        protected override void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateChanged -= OnDateChanged;
            }
            base.OnDestroy();
        }
        
        // =========================================================================
        // CARGA AUTOMÁTICA DE TEXTURAS DESDE RESOURCES
        // =========================================================================
        
        public void LoadMonthlyTexturesFromResources()
        {
            _monthlyTextures = new Texture2D[12];
            int loadedCount = 0;
            
            for (int i = 1; i <= 12; i++)
            {
                string path = $"Map/Textures/{i:00}";
                Texture2D tex = Resources.Load<Texture2D>(path);
                
                if (tex != null)
                {
                    _monthlyTextures[i - 1] = tex;
                    loadedCount++;
                    Debug.Log($"[WorldMap] Textura cargada: {path}");
                }
                else
                {
                    Debug.LogWarning($"[WorldMap] No se encontró la textura: {path}");
                }
            }
            
            Debug.Log($"[WorldMap] Texturas estacionales cargadas: {loadedCount}/12");
        }
        
        // =========================================================================
        // TEXTURAS ESTACIONALES
        // =========================================================================
        
        private void OnDateChanged(DateTime newDate)
        {
            if (!_enableSeasonalTextures)
                return;
            
            int month = newDate.Month;
            
            if (month != _currentMonth)
            {
                _currentMonth = month;
                
                if (_blendCoroutine != null)
                    StopCoroutine(_blendCoroutine);
                _blendCoroutine = StartCoroutine(BlendToMonthTexture(month));
                
                UpdateWeatherEffects();
            }
        }
        
        public void SetMonthTexture(int month)
        {
            if (_monthlyTextures == null || month - 1 >= _monthlyTextures.Length)
            {
                Debug.LogWarning($"[WorldMap] No hay textura para el mes {month}");
                return;
            }
            
            Texture2D newTexture = _monthlyTextures[month - 1];
            if (newTexture != null && _activeEarthMaterial != null)
            {
                _activeEarthMaterial.mainTexture = newTexture;
                _currentMonth = month;
                Debug.Log($"[WorldMap] Textura cambiada al mes {month}");
            }
        }
        
        private IEnumerator BlendToMonthTexture(int month)
        {
            if (_monthlyTextures == null || month - 1 >= _monthlyTextures.Length)
            {
                Debug.LogWarning($"[WorldMap] No hay textura para el mes {month}");
                yield break;
            }
            
            Texture2D newTexture = _monthlyTextures[month - 1];
            if (newTexture == null || _activeEarthMaterial == null)
                yield break;
            
            _activeEarthMaterial.mainTexture = newTexture;
            Debug.Log($"[WorldMap] Textura cambiada al mes {month}");
            yield return null;
        }
        
        private void UpdateWeatherEffects()
        {
            bool isRainySeason = (_currentMonth >= 4 && _currentMonth <= 6);
            
            if (_rainEffect != null)
            {
                if (isRainySeason && !_rainEffect.isPlaying)
                    _rainEffect.Play();
                else if (!isRainySeason && _rainEffect.isPlaying)
                    _rainEffect.Stop();
            }
            
            bool isWinter = (_currentMonth == 12 || _currentMonth == 1 || _currentMonth == 2);
            
            if (_snowEffect != null)
            {
                if (isWinter && !_snowEffect.isPlaying)
                    _snowEffect.Play();
                else if (!isWinter && _snowEffect.isPlaying)
                    _snowEffect.Stop();
            }
        }
        
        public void SetMonthlyTextures(Texture2D[] textures)
        {
            if (textures.Length != 12)
            {
                Debug.LogWarning("[WorldMap] Se necesitan exactamente 12 texturas (1 por mes)");
                return;
            }
            
            _monthlyTextures = textures;
            
            if (TimeManager.Instance != null && _enableSeasonalTextures)
            {
                int currentMonth = TimeManager.Instance.CurrentDate.Month;
                SetMonthTexture(currentMonth);
                _currentMonth = currentMonth;
            }
        }
        
        // =========================================================================
        // MÉTODOS DE CIUDADES
        // =========================================================================
        
        private void CreateCityMarkers()
        {
            // Verificar que el globo exista
            if (_earthTransform == null)
            {
                Debug.LogWarning("[WorldMap] No se puede crear marcadores: el globo no existe");
                return;
            }
            
            // Verificar que CityDatabase tenga ciudades
            if (CityDatabase.AllCities == null || CityDatabase.AllCities.Count == 0)
            {
                Debug.LogWarning("[WorldMap] No hay ciudades en CityDatabase");
                return;
            }
            
            if (_cityMarkerPrefab != null)
            {
                foreach (var city in CityDatabase.AllCities.Values)
                {
                    Vector3 position = LatLonToVector3(city.Latitude, city.Longitude, _earthRadius);
                    
                    GameObject markerObj = Instantiate(_cityMarkerPrefab, _earthTransform);
                    markerObj.transform.localPosition = position;
                    markerObj.transform.localScale = Vector3.one * _markerScale;
                    
                    CityMarker marker = markerObj.GetComponent<CityMarker>();
                    if (marker == null)
                        marker = markerObj.AddComponent<CityMarker>();
                    
                    marker.Initialize(city, this);
                    _cityMarkers[city.Id] = marker;
                }
            }
            else
            {
                foreach (var city in CityDatabase.AllCities.Values)
                {
                    Vector3 position = LatLonToVector3(city.Latitude, city.Longitude, _earthRadius);
                    
                    GameObject markerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    markerObj.transform.SetParent(_earthTransform);
                    markerObj.transform.localPosition = position;
                    markerObj.transform.localScale = Vector3.one * 0.1f;
                    
                    Renderer renderer = markerObj.GetComponent<Renderer>();
                    bool isUnlocked = city.IsUnlocked;
                    bool hasOffice = false;
                    
                    if (hasOffice)
                        renderer.material.color = _officeCityColor;
                    else if (isUnlocked)
                        renderer.material.color = _unlockedCityColor;
                    else
                        renderer.material.color = _lockedCityColor;
                    
                    CityMarker marker = markerObj.AddComponent<CityMarker>();
                    marker.Initialize(city, this);
                    _cityMarkers[city.Id] = marker;
                }
            }
            
            Debug.Log($"[WorldMap] Creados {_cityMarkers.Count} marcadores de ciudades");
        }
        
        public void UpdateMarkerColor(string cityId, bool isUnlocked, bool hasOffice)
        {
            if (_cityMarkers.TryGetValue(cityId, out CityMarker marker))
            {
                Color color;
                if (hasOffice)
                    color = _officeCityColor;
                else if (isUnlocked)
                    color = _unlockedCityColor;
                else
                    color = _lockedCityColor;
                
                marker.SetColor(color);
            }
        }
        
        public CityMarker GetCityMarker(string cityId)
        {
            _cityMarkers.TryGetValue(cityId, out CityMarker marker);
            return marker;
        }
        
        // =========================================================================
        // MÉTODOS DE UTILIDAD
        // =========================================================================
        
        public Vector3 LatLonToVector3(float latitude, float longitude, float radius)
        {
            float latRad = latitude * Mathf.Deg2Rad;
            float lonRad = longitude * Mathf.Deg2Rad;
            
            float x = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);
            float y = radius * Mathf.Sin(latRad);
            float z = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
            
            return new Vector3(x, y, z);
        }
        
        public void FocusOnCity(string cityId)
        {
            if (_cityMarkers.TryGetValue(cityId, out CityMarker marker) && _cameraController != null)
            {
                _cameraController.FocusOnPoint(marker.transform.position);
                Debug.Log($"[WorldMap] Enfocando en ciudad: {cityId}");
            }
        }
        
        public void CreateRoute(string originId, string destinationId, Constants.TransportMode mode, Color color)
        {
            if (!_cityMarkers.TryGetValue(originId, out CityMarker origin) ||
                !_cityMarkers.TryGetValue(destinationId, out CityMarker destination))
            {
                Debug.LogWarning($"[WorldMap] No se encontraron ciudades para ruta: {originId} → {destinationId}");
                return;
            }
            
            GameObject routeObj = new GameObject($"Route_{originId}_{destinationId}");
            routeObj.transform.SetParent(_earthTransform);
            
            RouteRenderer renderer = routeObj.AddComponent<RouteRenderer>();
            renderer.Initialize(origin.transform.position, destination.transform.position, _earthRadius, mode, color);
            
            _activeRoutes.Add(renderer);
        }
        
        public void ClearAllRoutes()
        {
            foreach (var route in _activeRoutes)
            {
                if (route != null)
                    Destroy(route.gameObject);
            }
            _activeRoutes.Clear();
        }
        
        // =========================================================================
        // CALLBACKS INTERNOS
        // =========================================================================
        
        internal void OnCityClickedInternal(WorldCity city)
        {
            OnCityClicked?.Invoke(city);
        }
        
        internal void OnCityHoveredInternal(WorldCity city)
        {
            OnCityHovered?.Invoke(city);
        }
    }
}