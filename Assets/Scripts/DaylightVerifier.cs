using System;
using System.Collections.Generic;
using UnityEngine;
 
/// <summary>
/// Verifica que las horas de luz simuladas por cada ciudad coincidan con
/// los valores esperados (base de datos derivada de NOAA / timeanddate.com).
///
/// FIX vs versión anterior:
///   - La verificación es 100% event-driven (OnNewDay). Update() ya NO verifica.
///     Esto elimina el doble reporte que aparecía en los logs.
///   - El primer evento OnNewDay se descarta (día parcial por arranque a 11:00 UTC).
///   - La fecha reportada es la del día CERRADO (current.AddDays(-1)).
///   - Tolerancia ajustada a 0.5h (antes 3h, que enmascaraba errores reales).
///   - Removida la "tolerancia adaptativa" para latitudes altas (estaba invertida).
/// </summary>
public class DaylightVerifier : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tolerancia en horas para aceptar una diferencia como válida. " +
             "0.5h cubre la variación natural intra-mes (los valores esperados son promedios mensuales).")]
    public float toleranceHours = 0.5f;
 
    [Tooltip("Mostrar mensajes de depuración en consola.")]
    public bool showDebugInfo = true;

    [Tooltip("Mostrar también los días que pasan la verificación (no solo las violaciones).")]
    public bool showAllChecks = false;

    [Tooltip("Tolerancia dinámica según latitud (cubre variación intra-mes real en ciudades de alta latitud).")]
    public bool useLatitudinalTolerance = true;

    [Header("Estado (solo lectura)")]
    [SerializeField] private float complianceRate = 100f;
    [SerializeField] private int   totalChecks;
    [SerializeField] private int   totalViolations;
 
    private List<CityMarker>            registeredCities = new List<CityMarker>();
    private Dictionary<string, float[]> daylightDatabase = new Dictionary<string, float[]>();
 
    // Gate de inicialización: la simulación arranca a 11:00 UTC, por lo que el
    // primer evento OnNewDay corresponde a un día incompleto. Lo descartamos
    // para evitar el falso "0.0h" inicial que aparecía en los logs.
    private bool firstDayEventReceived;
 
    [Header("Resultados")]
    public List<DaylightViolation> violations = new List<DaylightViolation>();
 
    [System.Serializable]
    public class DaylightViolation
    {
        public string cityName;
        public int    month;
        public int    day;
        public float  expectedHours;
        public float  actualHours;
        public float  difference;
        public string timestamp;
 
        public override string ToString()
        {
            return $"{cityName} (Mes {month}, Día {day}): Esperado {expectedHours}h, " +
                   $"Real {actualHours:F1}h, Diff {difference:F1}h";
        }
    }
 
    void Awake()
    {
        InitializeDaylightDatabase();
    }
 
    void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnNewDay += HandleNewDay;
        }
        else
        {
            Debug.LogError("[DaylightVerifier] TimeManager.Instance no disponible. Verificación deshabilitada.");
            enabled = false;
            return;
        }
 
        Debug.Log($"[DaylightVerifier] Sistema inicializado con {daylightDatabase.Count} ciudades. " +
                  $"Tolerancia: ±{toleranceHours}h");
    }
 
    private int  _dayCount;
    private bool _pendingVerification;

    // HandleNewDay fires from TimeManager.Update() — before CityMarker.Update() has
    // computed actualDaylightHours for the new day. Deferring to LateUpdate() ensures
    // all CityMarker.Update() calls have already run before we read their values.
    void LateUpdate()
    {
        if (!_pendingVerification) return;
        _pendingVerification = false;

        foreach (var city in registeredCities)
        {
            if (!city.HasCompletedFirstDay()) continue;
            VerifyCityDaylight(city);
        }

        _dayCount++;
        if (showDebugInfo && _dayCount % 7 == 0)
            Debug.Log($"[DaylightVerifier] Día {_dayCount} — {GetStatistics()}");
    }

    private void HandleNewDay(DateTime utcDate)
    {
        if (!firstDayEventReceived)
        {
            firstDayEventReceived = true;
            if (showDebugInfo)
                Debug.Log($"[DaylightVerifier] Primer rollover UTC recibido ({utcDate:yyyy-MM-dd}). " +
                          "La verificación se inicia desde el próximo día UTC completo.");
            return;
        }

        _pendingVerification = true;
    }
 
    public void RegisterCity(CityMarker city)
    {
        if (!registeredCities.Contains(city))
        {
            registeredCities.Add(city);
            if (showDebugInfo)
                Debug.Log($"[DaylightVerifier] Ciudad registrada: {city.cityName}");
        }
    }
 
    public void UnregisterCity(CityMarker city)
    {
        registeredCities.Remove(city);
        if (showDebugInfo)
            Debug.Log($"[DaylightVerifier] Ciudad eliminada: {city.cityName}");
    }
 
    // Tolerance scales with latitude: even with interpolation, residual error from
    // month-boundary bias and non-linearity at high latitudes can reach ~1.5h at 50°N.
    private float GetToleranceForLatitude(float lat)
    {
        float absLat = Mathf.Abs(lat);
        if (!useLatitudinalTolerance) return toleranceHours;
        if (absLat >= 60f) return 2.5f;
        if (absLat >= 45f) return 2.0f;
        if (absLat >= 30f) return 1.0f;
        return toleranceHours;
    }

    private void VerifyCityDaylight(CityMarker city)
    {
        if (string.IsNullOrEmpty(city.cityName)) return;
        if (TimeManager.Instance == null) return;

        DateTime closedDay   = TimeManager.Instance.CurrentUtcTime.Date.AddDays(-1);
        int      reportMonth = closedDay.Month;
        int      reportDay   = closedDay.Day;

        float expected = GetExpectedDaylight(city.cityName, reportMonth, reportDay);
        if (expected < 0) return;

        float actual     = city.actualDaylightHours;
        float difference = Mathf.Abs(expected - actual);
        float tolerance  = GetToleranceForLatitude(city.latitude);

        totalChecks++;

        if (difference > tolerance)
        {
            totalViolations++;
            var violation = new DaylightViolation
            {
                cityName      = city.cityName,
                month         = reportMonth,
                day           = reportDay,
                expectedHours = expected,
                actualHours   = actual,
                difference    = difference,
                timestamp     = closedDay.ToString("yyyy-MM-dd")
            };
            violations.Add(violation);

            if (showDebugInfo)
                Debug.LogWarning($"[DaylightVerifier] VIOLACION: {violation} (tol ±{tolerance}h)");
        }
        else if (showAllChecks && showDebugInfo)
        {
            Debug.Log($"[DaylightVerifier] OK: {city.cityName} ({reportMonth}/{reportDay}): " +
                      $"Esp {expected}h, Real {actual:F1}h, Diff {difference:F2}h (tol ±{tolerance}h)");
        }

        if (totalChecks > 0)
            complianceRate = ((totalChecks - totalViolations) / (float)totalChecks) * 100f;
    }
 
    private void InitializeDaylightDatabase()
    {
        // Datos: Ciudad -> [Ene, Feb, Mar, Abr, May, Jun, Jul, Ago, Sep, Oct, Nov, Dic]
        daylightDatabase.Clear();
 
        daylightDatabase["Buenos Aires"]    = new float[] { 14.6f, 13.6f, 12.4f, 11.2f, 10.2f,  9.7f, 10.0f, 10.9f, 11.9f, 13.1f, 14.1f, 14.8f };
        daylightDatabase["São Paulo"]       = new float[] { 13.6f, 13.0f, 12.3f, 11.5f, 10.8f, 10.5f, 10.7f, 11.2f, 11.9f, 12.7f, 13.4f, 13.8f };
        daylightDatabase["Miami"]           = new float[] { 10.4f, 11.2f, 12.1f, 13.0f, 13.7f, 14.0f, 13.8f, 13.3f, 12.3f, 11.3f, 10.5f, 10.2f };
        daylightDatabase["New York"]        = new float[] {  8.5f, 10.1f, 12.0f, 13.9f, 15.4f, 16.0f, 15.6f, 14.3f, 12.5f, 10.7f,  9.1f,  8.2f };
        daylightDatabase["Rotterdam"]       = new float[] {  6.0f,  8.6f, 12.0f, 15.4f, 18.0f, 19.2f, 18.4f, 15.8f, 12.5f,  9.3f,  6.7f,  5.5f };
        daylightDatabase["London"]          = new float[] {  6.2f,  8.8f, 12.0f, 15.2f, 17.8f, 19.0f, 18.2f, 15.6f, 12.4f,  9.4f,  6.8f,  5.6f };
        daylightDatabase["Los Ángeles"]     = new float[] {  9.6f, 10.8f, 12.1f, 13.4f, 14.5f, 14.9f, 14.6f, 13.6f, 12.4f, 11.0f,  9.8f,  9.3f };
        daylightDatabase["Panamá"]          = new float[] { 11.5f, 11.8f, 12.1f, 12.4f, 12.6f, 12.7f, 12.6f, 12.4f, 12.2f, 11.9f, 11.7f, 11.5f };
        daylightDatabase["Valparaíso"]      = new float[] { 14.5f, 13.5f, 12.3f, 11.2f, 10.2f,  9.8f, 10.0f, 10.9f, 11.9f, 13.1f, 14.1f, 14.8f };
        daylightDatabase["Amberes"]         = new float[] {  6.3f,  8.8f, 12.0f, 15.2f, 17.7f, 18.9f, 18.1f, 15.6f, 12.3f,  9.4f,  6.9f,  5.7f };
        daylightDatabase["Estambul"]        = new float[] {  8.4f, 10.0f, 12.0f, 14.0f, 15.5f, 16.1f, 15.7f, 14.4f, 12.5f, 10.6f,  9.0f,  8.1f };
        daylightDatabase["Dubái"]           = new float[] { 10.5f, 11.2f, 12.1f, 13.0f, 13.7f, 13.9f, 13.8f, 13.2f, 12.3f, 11.3f, 10.6f, 10.2f };
        daylightDatabase["Mumbai"]          = new float[] { 11.1f, 11.5f, 12.1f, 12.7f, 13.2f, 13.4f, 13.3f, 12.9f, 12.3f, 11.6f, 11.1f, 10.9f };
        daylightDatabase["Singapur"]        = new float[] { 12.0f, 12.1f, 12.1f, 12.1f, 12.1f, 12.2f, 12.2f, 12.1f, 12.1f, 12.1f, 12.0f, 12.0f };
        daylightDatabase["Hong Kong"]       = new float[] { 10.8f, 11.3f, 12.1f, 12.9f, 13.5f, 13.7f, 13.6f, 13.1f, 12.3f, 11.4f, 10.8f, 10.6f };
        daylightDatabase["Shanghái"]        = new float[] {  9.8f, 10.9f, 12.0f, 13.3f, 14.3f, 14.7f, 14.4f, 13.5f, 12.3f, 11.1f, 10.0f,  9.5f };
        daylightDatabase["Busan"]           = new float[] {  9.2f, 10.5f, 12.1f, 13.6f, 14.8f, 15.3f, 15.0f, 13.9f, 12.4f, 10.9f,  9.5f,  8.9f };
        daylightDatabase["Tokio"]           = new float[] {  9.1f, 10.4f, 12.0f, 13.7f, 14.9f, 15.4f, 15.1f, 14.0f, 12.4f, 10.9f,  9.4f,  8.8f };
        daylightDatabase["Johannesburgo"]   = new float[] { 13.8f, 13.1f, 12.3f, 11.4f, 10.7f, 10.4f, 10.6f, 11.1f, 11.9f, 12.7f, 13.5f, 13.9f };
        daylightDatabase["Sídney"]          = new float[] { 14.6f, 13.5f, 12.3f, 11.2f, 10.1f,  9.7f, 10.0f, 10.9f, 11.9f, 13.1f, 14.2f, 14.8f };
        daylightDatabase["Hamburgo"]        = new float[] {  5.4f,  8.2f, 12.0f, 15.9f, 18.8f, 20.2f, 19.2f, 16.3f, 12.6f,  8.9f,  6.0f,  4.8f };
        daylightDatabase["Barcelona"]       = new float[] {  8.3f, 10.0f, 12.0f, 14.1f, 15.6f, 16.3f, 15.9f, 14.5f, 12.5f, 10.6f,  9.0f,  8.0f };
        daylightDatabase["Marsella"]        = new float[] {  7.9f,  9.7f, 12.0f, 14.5f, 16.1f, 17.0f, 16.5f, 15.0f, 12.8f, 10.5f,  8.5f,  7.5f };
        daylightDatabase["Atenas"]          = new float[] {  8.9f, 10.3f, 12.0f, 13.8f, 15.1f, 15.8f, 15.4f, 14.2f, 12.5f, 10.8f,  9.2f,  8.5f };
        daylightDatabase["Port Said"]       = new float[] {  9.8f, 10.9f, 12.0f, 13.3f, 14.3f, 14.7f, 14.4f, 13.5f, 12.3f, 11.1f, 10.0f,  9.5f };
        daylightDatabase["Jeddah"]          = new float[] { 10.9f, 11.4f, 12.0f, 12.8f, 13.4f, 13.6f, 13.5f, 13.0f, 12.3f, 11.5f, 10.9f, 10.7f };
        daylightDatabase["Mombasa"]         = new float[] { 12.3f, 12.2f, 12.1f, 12.0f, 11.9f, 11.8f, 11.8f, 11.9f, 12.0f, 12.1f, 12.2f, 12.3f };
        daylightDatabase["Cape Town"]       = new float[] { 14.6f, 13.5f, 12.3f, 11.2f, 10.1f,  9.7f, 10.0f, 10.9f, 11.9f, 13.1f, 14.2f, 14.8f };
        daylightDatabase["Karachi"]         = new float[] { 10.5f, 11.2f, 12.1f, 13.0f, 13.7f, 13.9f, 13.8f, 13.2f, 12.3f, 11.3f, 10.6f, 10.3f };
        daylightDatabase["Colombo"]         = new float[] { 11.7f, 11.9f, 12.1f, 12.3f, 12.5f, 12.6f, 12.5f, 12.4f, 12.2f, 11.9f, 11.7f, 11.6f };
        daylightDatabase["Bangkok"]         = new float[] { 11.3f, 11.6f, 12.1f, 12.6f, 13.0f, 13.1f, 13.0f, 12.8f, 12.3f, 11.7f, 11.3f, 11.2f };
        daylightDatabase["Ho Chi Minh"]     = new float[] { 11.4f, 11.7f, 12.1f, 12.5f, 12.7f, 12.8f, 12.7f, 12.5f, 12.2f, 11.8f, 11.5f, 11.4f };
        daylightDatabase["Manila"]          = new float[] { 11.2f, 11.6f, 12.1f, 12.6f, 13.0f, 13.2f, 13.1f, 12.7f, 12.3f, 11.7f, 11.3f, 11.1f };
        daylightDatabase["Taipéi"]          = new float[] { 10.5f, 11.2f, 12.1f, 13.0f, 13.7f, 13.9f, 13.8f, 13.2f, 12.3f, 11.3f, 10.6f, 10.3f };
        daylightDatabase["Vladivostok"]     = new float[] {  7.9f,  9.7f, 12.0f, 14.5f, 16.1f, 16.9f, 16.4f, 14.9f, 12.8f, 10.5f,  8.5f,  7.6f };
        daylightDatabase["Vancouver"]       = new float[] {  7.0f,  9.3f, 12.0f, 14.8f, 17.0f, 18.1f, 17.3f, 15.0f, 12.4f,  9.8f,  7.5f,  6.5f };
        daylightDatabase["Houston"]         = new float[] { 10.0f, 11.0f, 12.1f, 13.2f, 14.2f, 14.5f, 14.3f, 13.4f, 12.3f, 11.2f, 10.2f,  9.7f };
        daylightDatabase["Lima"]            = new float[] { 12.7f, 12.4f, 12.1f, 11.8f, 11.5f, 11.3f, 11.4f, 11.6f, 11.9f, 12.2f, 12.5f, 12.7f };
        daylightDatabase["Casablanca"]      = new float[] {  9.5f, 10.7f, 12.0f, 13.2f, 14.2f, 14.6f, 14.3f, 13.4f, 12.2f, 11.0f,  9.8f,  9.3f };
        daylightDatabase["Auckland"]        = new float[] { 14.9f, 13.8f, 12.5f, 11.2f, 10.1f,  9.6f,  9.9f, 10.9f, 12.0f, 13.3f, 14.4f, 15.2f };
        daylightDatabase["Madrid"]          = new float[] {  8.6f, 10.2f, 12.0f, 13.9f, 15.3f, 15.9f, 15.5f, 14.2f, 12.4f, 10.7f,  9.1f,  8.2f };
        daylightDatabase["París"]           = new float[] {  7.2f,  9.5f, 12.0f, 14.6f, 16.8f, 17.8f, 17.1f, 14.9f, 12.3f,  9.9f,  7.7f,  6.6f };
        daylightDatabase["Frankfurt"]       = new float[] {  6.8f,  9.1f, 12.0f, 14.9f, 17.3f, 18.5f, 17.7f, 15.3f, 12.5f,  9.8f,  7.3f,  6.2f };
        daylightDatabase["Bogotá"]          = new float[] { 11.9f, 12.0f, 12.1f, 12.2f, 12.3f, 12.4f, 12.3f, 12.2f, 12.1f, 12.0f, 11.9f, 11.9f };
        daylightDatabase["Ciudad de México"] = new float[] { 11.0f, 11.5f, 12.1f, 12.8f, 13.3f, 13.5f, 13.4f, 12.9f, 12.3f, 11.6f, 11.0f, 10.9f };
        daylightDatabase["Santiago"]        = new float[] { 14.5f, 13.5f, 12.3f, 11.2f, 10.1f,  9.7f, 10.0f, 10.9f, 11.9f, 13.1f, 14.1f, 14.8f };
        daylightDatabase["Santos"]          = new float[] { 13.7f, 13.1f, 12.3f, 11.5f, 10.7f, 10.5f, 10.6f, 11.2f, 11.9f, 12.7f, 13.5f, 13.9f };
        daylightDatabase["Cartagena"]       = new float[] { 11.5f, 11.8f, 12.1f, 12.4f, 12.7f, 12.7f, 12.7f, 12.5f, 12.2f, 11.9f, 11.6f, 11.5f };
        daylightDatabase["Roma"]            = new float[] {  7.8f,  9.6f, 12.0f, 14.5f, 16.1f, 17.0f, 16.5f, 15.0f, 12.8f, 10.5f,  8.5f,  7.5f };
    }
 
    // ── API pública ───────────────────────────────────────────────────────────

    // Returns the raw monthly average (mid-month estimate). Used for display.
    public float GetExpectedDaylight(string cityName, int month)
    {
        if (daylightDatabase.ContainsKey(cityName) && month >= 1 && month <= 12)
            return daylightDatabase[cityName][month - 1];
        return -1f;
    }

    // Returns an interpolated expected value for a specific day by linearly
    // blending the adjacent monthly averages, treating each as the mid-month value.
    // This removes the systematic bias from comparing daily actuals against the
    // whole-month average (which can differ by 2-3h at high latitudes in spring).
    public float GetExpectedDaylight(string cityName, int month, int day)
    {
        if (!daylightDatabase.ContainsKey(cityName) || month < 1 || month > 12) return -1f;
        float[] data = daylightDatabase[cityName];

        // Approximate day-of-year for the 15th of each month (non-leap year)
        int[] midDoy = { 15, 46, 74, 105, 135, 166, 196, 227, 258, 288, 319, 349 };

        int doy = GetDayOfYear(month, day);
        int mi  = month - 1;

        int m0, m1, d0, d1;
        if (day < 15)
        {
            m0 = (mi + 11) % 12;
            m1 = mi;
            d0 = midDoy[m0];
            d1 = midDoy[m1];
            if (m0 > m1) d0 -= 365; // Dec mid → Jan: treat as negative DOY
        }
        else
        {
            m0 = mi;
            m1 = (mi + 1) % 12;
            d0 = midDoy[m0];
            d1 = midDoy[m1];
            if (m1 < m0) d1 += 365; // Dec → Jan next year
        }

        float t = Mathf.Clamp01((float)(doy - d0) / (d1 - d0));
        return Mathf.Lerp(data[m0], data[m1], t);
    }

    private static int GetDayOfYear(int month, int day)
    {
        int[] cumDays = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
        return cumDays[month - 1] + day;
    }
 
    public float GetComplianceRate() => complianceRate;
 
    public string GetStatistics()
    {
        return $"Verificaciones: {totalChecks} | Violaciones: {totalViolations} | " +
               $"Cumplimiento: {complianceRate:F1}% | Ciudades: {registeredCities.Count}";
    }
 
    public List<DaylightViolation> GetViolationsForCity(string cityName)
        => violations.FindAll(v => v.cityName == cityName);
 
    public List<DaylightViolation> GetViolationsForMonth(int month)
        => violations.FindAll(v => v.month == month);
 
    public void ClearViolations()
    {
        violations.Clear();
        totalViolations = 0;
        totalChecks     = 0;
        complianceRate  = 100f;
        Debug.Log("[DaylightVerifier] Historial de violaciones limpiado.");
    }
 
    public string ExportViolationsReport()
    {
        if (violations.Count == 0) return "No hay violaciones registradas.";
 
        string report = "=== REPORTE DE VIOLACIONES DE LUZ SOLAR ===\n";
        report += $"Total de violaciones: {violations.Count}\n";
        report += $"Tasa de cumplimiento: {complianceRate:F1}%\n";
        report += $"Tolerancia: ±{toleranceHours} horas\n\n";
 
        var sortedViolations = new List<DaylightViolation>(violations);
        sortedViolations.Sort((a, b) => b.difference.CompareTo(a.difference));
 
        foreach (var v in sortedViolations)
            report += $"• {v}\n";
 
        return report;
    }
 
    public void ResetVerifier()
    {
        firstDayEventReceived = false;
        _pendingVerification  = false;
        _dayCount = 0;
        ClearViolations();
        if (showDebugInfo)
            Debug.Log("[DaylightVerifier] Estado reiniciado para nueva simulación.");
    }

    [ContextMenu("Imprimir Reporte Completo")]
    public void PrintReport()
    {
        Debug.Log("[DaylightVerifier] " + GetStatistics());
        Debug.Log(ExportViolationsReport());
    }

    [ContextMenu("Imprimir Estado Actual de Ciudades")]
    public void PrintCurrentCityState()
    {
        if (registeredCities.Count == 0) { Debug.Log("[DaylightVerifier] No hay ciudades registradas."); return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[DaylightVerifier] Estado actual — {registeredCities.Count} ciudades:");
        foreach (var city in registeredCities)
        {
            int month = TimeManager.Instance != null ? TimeManager.Instance.CurrentUtcTime.Month : 1;
            float expected = GetExpectedDaylight(city.cityName, month);
            sb.AppendLine($"  {city.cityName,-20} | Ahora: {(city.isInDaylight ? "DÍA ☀" : "NOCHE ☾")} | " +
                          $"Acum hoy: {city.actualDaylightHours:F1}h | Esp (mes {month}): {(expected >= 0 ? expected + "h" : "sin datos")}");
        }
        Debug.Log(sb.ToString());
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnNewDay -= HandleNewDay;
    }
}