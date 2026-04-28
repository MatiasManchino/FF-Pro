using UnityEngine;
using FreightForwarder.Managers;

namespace FreightForwarder
{
    /// <summary>
    /// SunController — Controla la rotación del sol basada en la hora del día
    /// </summary>
    public class SunController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Material _skyboxMaterial;
        
        [Header("Configuración")]
        [SerializeField] private float _rotationSpeed = 1f;
        [SerializeField] private bool _autoRotate = true;
        
        [Header("Colores del cielo según hora")]
        [SerializeField] private Color _midnightColor = new Color(0.05f, 0.05f, 0.1f);
        [SerializeField] private Color _sunriseColor = new Color(0.3f, 0.2f, 0.1f);
        [SerializeField] private Color _noonColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color _sunsetColor = new Color(0.5f, 0.2f, 0.1f);
        
        [Header("Intensidades")]
        [SerializeField] private float _minIntensity = 0.05f;
        [SerializeField] private float _maxIntensity = 1.2f;
        
        private float _currentTimeOfDay = 0.5f; // 0 = medianoche, 0.5 = mediodía
        private int _currentDay = -1;
        
        private void Start()
        {
            if (_sunLight == null)
                _sunLight = GetComponent<Light>();
            
            if (_sunLight == null)
            {
                Debug.LogWarning("[SunController] No se encontró Light. Creando una nueva.");
                _sunLight = gameObject.AddComponent<Light>();
                _sunLight.type = LightType.Directional;
            }
            
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += UpdateSunPosition;
            }
            
            UpdateSunPosition();
        }
        
        private void Update()
        {
            if (_autoRotate && TimeManager.Instance != null && !TimeManager.Instance.IsPaused)
            {
                // El sol rota según el progreso del día
                float dayProgress = TimeManager.Instance.DayProgress;
                _currentTimeOfDay = dayProgress;
                
                ApplySunRotation();
                ApplyLighting();
            }
        }
        
        private void UpdateSunPosition()
        {
            if (TimeManager.Instance == null) return;
            
            // Al empezar un nuevo día, resetear el sol
            _currentTimeOfDay = 0f;
            ApplySunRotation();
        }
        
        private void ApplySunRotation()
        {
            // Rotación: 0° = medianoche (sol abajo), 180° = mediodía (sol arriba)
            float sunAngle = _currentTimeOfDay * 180f - 90f;
            transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
        }
        
        private void ApplyLighting()
        {
            if (_sunLight == null) return;
            
            // Calcular intensidad según la posición del sol
            float intensityFactor = Mathf.Sin(_currentTimeOfDay * Mathf.PI);
            intensityFactor = Mathf.Max(0f, intensityFactor);
            float intensity = Mathf.Lerp(_minIntensity, _maxIntensity, intensityFactor);
            _sunLight.intensity = intensity;
            
            // Cambiar color de la luz según la hora
            if (_currentTimeOfDay < 0.25f) // Medianoche a Amanecer
            {
                float t = _currentTimeOfDay / 0.25f;
                _sunLight.color = Color.Lerp(_midnightColor, _sunriseColor, t);
                RenderSettings.ambientLight = Color.Lerp(Color.black, new Color(0.1f, 0.1f, 0.15f), t);
            }
            else if (_currentTimeOfDay < 0.5f) // Amanecer a Mediodía
            {
                float t = (_currentTimeOfDay - 0.25f) / 0.25f;
                _sunLight.color = Color.Lerp(_sunriseColor, _noonColor, t);
                RenderSettings.ambientLight = Color.Lerp(new Color(0.1f, 0.1f, 0.15f), Color.white * 0.3f, t);
            }
            else if (_currentTimeOfDay < 0.75f) // Mediodía a Atardecer
            {
                float t = (_currentTimeOfDay - 0.5f) / 0.25f;
                _sunLight.color = Color.Lerp(_noonColor, _sunsetColor, t);
                RenderSettings.ambientLight = Color.Lerp(Color.white * 0.3f, new Color(0.15f, 0.1f, 0.05f), t);
            }
            else // Atardecer a Medianoche
            {
                float t = (_currentTimeOfDay - 0.75f) / 0.25f;
                _sunLight.color = Color.Lerp(_sunsetColor, _midnightColor, t);
                RenderSettings.ambientLight = Color.Lerp(new Color(0.15f, 0.1f, 0.05f), Color.black, t);
            }
            
            // Actualizar Skybox si existe
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetColor("_Tint", _sunLight.color * 0.5f);
            }
        }
        
        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= UpdateSunPosition;
            }
        }
    }
}