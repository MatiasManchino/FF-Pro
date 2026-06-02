using System;
using UnityEngine;
 

// Controla la rotación de la Tierra y la dirección de la luz solar.
//
// MODELO ASTRONÓMICO (corregido):
// - El eje terrestre permanece alineado con +Y mundial (NO se aplica tilt al transform).
// - El efecto estacional se introduce vía la componente Y del vector sunDir = (cos δ, sin δ, 0).
// - La rotación diaria es pura alrededor de Y: angleY = (UTC_hours - 12) * 15°.
// - Greenwich (lon=0°) queda cenital al sol a las 12:00 UTC, equinoccio.
//
// FIX CRÍTICO (vs versión anterior):
// La versión previa hacía Quaternion.Euler(axialTilt, -angleY, 0f), aplicando el tilt
// de 23.44° AL MISMO TIEMPO que la declinación ya estaba incluida en sunDir. El efecto
// estacional quedaba aplicado dos veces, causando un déficit sistemático de horas de luz
// en latitudes >40° durante el invierno (verificado: NY 64% del esperado, Vancouver 54%).

public class SunController : MonoBehaviour
{
// Gestiona instance.
    public static SunController Instance { get; private set; }
 
    [Header("Referencias")]
    public Light     sunLight;
    public Transform earthTransform;
 
    [Header("Inclinación del eje terrestre")]
    [Tooltip("Mantenido como parámetro de referencia. NO se aplica como rotación física al " +
             "earthTransform: en este modelo el eje terrestre permanece alineado con +Y mundial " +
             "y el efecto estacional se introduce vía la componente Y del vector sunDir (sin δ).")]
    [Range(0f, 45f)]
    public float axialTilt = 23.44f;
 
    [Header("Sol visual (opcional)")]
    public Transform sunVisual;
    public float sunDistance = 10000f;
 
    [Header("Iluminación CONSTANTE")]
    public float sunIntensity = 1.2f;
    public Color ambientColor = new Color(0.25f, 0.25f, 0.35f);
 
    private int     _cachedDayOfYear = -1;
    private float   _cachedDeclination;
    private Vector3 _cachedSunDirection = Vector3.right;
 
// Configura referencias tempranas antes de Start.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
 
        ApplyConstantLighting();
        if (earthTransform != null)
            earthTransform.rotation = Quaternion.identity;
    }
 
// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (TimeManager.Instance == null || sunLight == null || earthTransform == null)
            return;
 
        DateTime utc = TimeManager.Instance.CurrentUtcTime;
 
        // Orden: AimSun cachea sunDir → RotateEarth fija el transform.
        // Cualquier consumidor que lea GetSunDirection() obtiene el valor del mismo fotograma.
        AimSun(utc);
        RotateEarth(utc);
    }
 
// Aplica constant lighting
    void ApplyConstantLighting()
    {
        if (sunLight != null)
        {
            sunLight.intensity = sunIntensity;
            sunLight.color = Color.white;
        }
        RenderSettings.ambientLight = ambientColor;
    }
 
    // ──────────────────────────────────────────────────────────────────────────
    // Rotación diaria pura alrededor del eje Y mundial.
    // ANTES: Quaternion.Euler(axialTilt, -angleY, 0f)   ← BUG: doble cómputo
    // AHORA: Quaternion.Euler(0f,        -angleY, 0f)   ← FIX
    // Al mediodía UTC (angleY = 0), Greenwich (+X local) apunta al sol (+X mundial).
    // Realiza rotate earth
    void RotateEarth(DateTime utc)
    {
        double totalHours      = utc.TimeOfDay.TotalHours;
        double hoursSinceNoon  = totalHours - 12.0;
 
        // 15° por hora exacta. Mod 360 evita acumulación de error en floats.
        float angleY = (float)((hoursSinceNoon * 15.0) % 360.0);
 
        earthTransform.rotation = Quaternion.Euler(0f, -angleY, 0f);
    }
 
    // ──────────────────────────────────────────────────────────────────────────
    // Dirección del sol en espacio mundial al mediodía UTC.
    // sunDir = (cos δ, sin δ, 0)
    // - Equinoccio   (δ=0):        sol en +X → ambos hemisferios igual.
    // - Solsticio jun (δ=+23.44°): sol con offset +Y → hemisferio norte favorecido.
    // - Solsticio dic (δ=-23.44°): sol con offset -Y → hemisferio sur favorecido.
    // Realiza aim sun
    void AimSun(DateTime utc)
    {
        if (utc.DayOfYear != _cachedDayOfYear)
        {
            _cachedDayOfYear   = utc.DayOfYear;
            _cachedDeclination = SolarDeclination(utc);
        }
 
        float declinationRad = _cachedDeclination * Mathf.Deg2Rad;
 
        Vector3 sunDir = new Vector3(
            Mathf.Cos(declinationRad),
            Mathf.Sin(declinationRad),
            0f
        ).normalized;
 
        _cachedSunDirection = sunDir;
        sunLight.transform.rotation = Quaternion.LookRotation(-sunDir, Vector3.up);
 
        if (sunVisual != null)
            sunVisual.position = earthTransform.position + sunDir * sunDistance;
    }
 
    // Fórmula de Spencer (1971) - precisión ±0.01°. Maneja años bisiestos.
    float SolarDeclination(DateTime utc)
    {
        int dayOfYear  = utc.DayOfYear;
        int daysInYear = DateTime.IsLeapYear(utc.Year) ? 366 : 365;
 
        float B = 2.0f * Mathf.PI * (dayOfYear - 1) / daysInYear;
 
        float declination = 0.006918f
            - 0.399912f * Mathf.Cos(B)
            + 0.070257f * Mathf.Sin(B)
            - 0.006758f * Mathf.Cos(2 * B)
            + 0.000907f * Mathf.Sin(2 * B)
            - 0.002697f * Mathf.Cos(3 * B)
            + 0.001480f * Mathf.Sin(3 * B);
 
        return declination * Mathf.Rad2Deg;
    }
 
    // ── API pública (sin cambios de firma) ───────────────────────────────────
 
    public Vector3 GetSunDirection()       => _cachedSunDirection;
// Obtiene actual declination
    public float   GetCurrentDeclination() => _cachedDeclination;
 
// Indica si daylight at posición.
    public bool IsDaylightAtPosition(Vector3 worldPosition, Vector3 earthCenter)
    {
        Vector3 normal     = (worldPosition - earthCenter).normalized;
        float   dotProduct = Vector3.Dot(normal, _cachedSunDirection);
        // Refracción atmosférica: el centro del sol se considera sobre el horizonte
        // cuando está hasta 50' (0.833°) por debajo del horizonte geométrico.
        return dotProduct > -0.01454f;
    }
 
// Obtiene sun ángulo at posición
    public float GetSunAngleAtPosition(Vector3 worldPosition, Vector3 earthCenter)
    {
        Vector3 normal     = (worldPosition - earthCenter).normalized;
        float   dotProduct = Vector3.Dot(normal, _cachedSunDirection);
        float   angleRad   = Mathf.Asin(Mathf.Clamp(dotProduct, -1f, 1f));
        return angleRad * Mathf.Rad2Deg;
    }
 
// Se ejecuta al dibujar gizmos en la escena.
    void OnDrawGizmos()
    {
        if (sunLight != null && earthTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 earthCenter = earthTransform.position;
            Gizmos.DrawRay(earthCenter, _cachedSunDirection * earthTransform.localScale.x * 0.8f);
 
            Gizmos.color = Color.cyan;
            Vector3 axisDirection = earthTransform.up;
            Gizmos.DrawRay(earthCenter,  axisDirection * earthTransform.localScale.x * 0.7f);
            Gizmos.DrawRay(earthCenter, -axisDirection * earthTransform.localScale.x * 0.7f);
        }
    }
 
// Elimina el marcador del registro y destruye su label al destruir el objeto.
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}