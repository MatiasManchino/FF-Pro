#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;


// Verifica que las horas de luz simuladas por cada ciudad coincidan con
// los valores astronómicos calculados desde la latitud de la ciudad.
//
// El valor esperado se calcula con la fórmula estándar de duración del día:
// D = (2/15) * arccos(-tan(φ) * tan(δ))
// donde δ es la declinación solar del día (Spencer 1971).
//
// Esto reemplaza la base de datos de promedios mensuales anterior, que estaba
// incorrecta para latitudes altas (ej. Hamburg: 20.2h en junio vs real ~17h).

public class DaylightVerifier : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tolerancia base en horas para ciudades < 30° lat. Aumenta con latitud.")]
    public float toleranceHours = 0.5f;

    [Tooltip("Mostrar mensajes de depuración en consola.")]
    public bool showDebugInfo = true;

    [Tooltip("Mostrar también los días que pasan la verificación (no solo las violaciones).")]
    public bool showAllChecks = false;

    [Tooltip("Tolerancia dinámica: 0-30°=0.5h · 30-60°=1.0h · 60°+=1.5h")]
    public bool useLatitudinalTolerance = true;

    [Header("Estado (solo lectura)")]
    [SerializeField] private float complianceRate = 100f;
    [SerializeField] private int   totalChecks;
    [SerializeField] private int   totalViolations;

    private List<CityMarker> registeredCities = new List<CityMarker>();
    private bool             firstDayEventReceived;

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

// Gestiona to string.
        public override string ToString()
        {
            return $"{cityName} (Mes {month}, Día {day}): Esperado {expectedHours:F2}h, " +
                   $"Real {actualHours:F1}h, Diff {difference:F2}h";
        }
    }

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
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

        Debug.Log($"[DaylightVerifier] Iniciado. Referencia: fórmula astronómica. Tol base ±{toleranceHours}h — " +
                  $"esperando registro de ciudades y primer rollover UTC...");
        // Nota: cities register in their own Start() — count se be >0 once scene indica si live
    }

    private int  _dayCount;
    private bool _pendingVerification;

    // HandleNewDay se ejecuta from TimeManager.Actualiza() — antes de CityMarker.Actualiza() determina si tiene
    // calculado actualDaylightHours for the día. LateUpdate() guarantees all
    // CityMarker.Actualiza() calls have already run antes de we read their values.
    void LateUpdate()
    {
        if (!_pendingVerification) return;
        _pendingVerification = false;

// Foreach
        foreach (var city in registeredCities)
        {
            if (!city.HasCompletedFirstDay()) continue;
            VerifyCityDaylight(city);
        }

        _dayCount++;
        if (showDebugInfo && (_dayCount <= 3 || _dayCount % 7 == 0))
            Debug.Log($"[DaylightVerifier] Día {_dayCount} — {GetStatistics()}");
    }

// Gestiona nuevo día.
    private void HandleNewDay(DateTime utcDate)
    {
        if (!firstDayEventReceived)
        {
            firstDayEventReceived = true;
            if (showDebugInfo)
                Debug.Log($"[DaylightVerifier] Primer rollover UTC ({utcDate:yyyy-MM-dd}). " +
                          "Verificación inicia desde el próximo día UTC completo.");
            return;
        }

        _pendingVerification = true;
    }

// Registra ciudad
    public void RegisterCity(CityMarker city)
    {
        if (!registeredCities.Contains(city))
        {
            registeredCities.Add(city);
            if (showDebugInfo)
                Debug.Log($"[DaylightVerifier] Ciudad registrada: {city.cityName}");
        }
    }

// Desregistra ciudad
    public void UnregisterCity(CityMarker city)
    {
        registeredCities.Remove(city);
        if (showDebugInfo)
            Debug.Log($"[DaylightVerifier] Ciudad eliminada: {city.cityName}");
    }

// Obtiene tolerance for latitude
    private float GetToleranceForLatitude(float lat)
    {
        float absLat = Mathf.Abs(lat);
        if (!useLatitudinalTolerance) return toleranceHours;
        if (absLat >= 60f) return 1.5f;
        if (absLat >= 30f) return 1.0f;
        return toleranceHours;
    }

// Verifica ciudad daylight.
    private void VerifyCityDaylight(CityMarker city)
    {
        if (string.IsNullOrEmpty(city.cityName)) return;
        if (TimeManager.Instance == null) return;

        DateTime closedDay   = TimeManager.Instance.CurrentUtcTime.Date.AddDays(-1);
        int      reportMonth = closedDay.Month;
        int      reportDay   = closedDay.Day;

        float expected   = ComputeAstronomicalDaylight(city.latitude, reportMonth, reportDay);
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
        // Realiza if
        else if (showAllChecks && showDebugInfo)
        {
            Debug.Log($"[DaylightVerifier] OK: {city.cityName} ({reportMonth}/{reportDay}): " +
                      $"Esp {expected:F2}h, Real {actual:F1}h, Diff {difference:F2}h (tol ±{tolerance}h)");
        }

        if (totalChecks > 0)
            complianceRate = ((totalChecks - totalViolations) / (float)totalChecks) * 100f;
    }

    // ── Fórmula astronómica ───────────────────────────────────────────────────

    // Estándar día-length fórmula (Spencer 1971 declination aproximación).
    // Matches real-world sunrise-to-sunset within ~0.1-0.2h for any date and latitude.
    // Calcula astronomical daylight.
    public static float ComputeAstronomicalDaylight(float latitude, int month, int day)
    {
        int   doy  = GetDayOfYear(month, day);
        float decl = 23.45f * Mathf.Sin(Mathf.Deg2Rad * (360f / 365f * (doy - 81)));
        float cosHA = -Mathf.Tan(Mathf.Deg2Rad * latitude) * Mathf.Tan(Mathf.Deg2Rad * decl);
        cosHA = Mathf.Clamp(cosHA, -1f, 1f);
        return 2f * Mathf.Acos(cosHA) * Mathf.Rad2Deg / 15f;
    }

// Obtiene día of año
    private static int GetDayOfYear(int month, int day)
    {
        int[] cumDays = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
        return cumDays[month - 1] + day;
    }

    // ── API pública ───────────────────────────────────────────────────────────

    // Gestiona get expected daylight.
    public float GetExpectedDaylight(float latitude, int month)
        => ComputeAstronomicalDaylight(latitude, month, 15);

// Obtiene compliance rate
    public float GetComplianceRate() => complianceRate;

// Obtiene statistics
    public string GetStatistics()
    {
        return $"Verificaciones: {totalChecks} | Violaciones: {totalViolations} | " +
               $"Cumplimiento: {complianceRate:F1}% | Ciudades: {registeredCities.Count}";
    }

// Obtiene violations for ciudad
    public List<DaylightViolation> GetViolationsForCity(string cityName)
        => violations.FindAll(v => v.cityName == cityName);

// Obtiene violations for mes
    public List<DaylightViolation> GetViolationsForMonth(int month)
        => violations.FindAll(v => v.month == month);

// Borra violations.
    public void ClearViolations()
    {
        violations.Clear();
        totalViolations = 0;
        totalChecks     = 0;
        complianceRate  = 100f;
        Debug.Log("[DaylightVerifier] Historial de violaciones limpiado.");
    }

// Exporta violations informe.
    public string ExportViolationsReport()
    {
        if (violations.Count == 0) return "No hay violaciones registradas.";

        var report = "=== REPORTE DE VIOLACIONES DE LUZ SOLAR ===\n";
        report += $"Total: {violations.Count} | Cumplimiento: {complianceRate:F1}%\n";
        report += "Referencia: fórmula astronómica exacta por latitud\n\n";

        var sorted = new List<DaylightViolation>(violations);
        sorted.Sort((a, b) => b.difference.CompareTo(a.difference));
// Foreach
        foreach (var v in sorted)
            report += $"• {v}\n";

        return report;
    }

// Restablece verifier
    public void ResetVerifier()
    {
        firstDayEventReceived = false;
        _pendingVerification  = false;
        _dayCount             = 0;
        ClearViolations();
        if (showDebugInfo)
            Debug.Log("[DaylightVerifier] Estado reiniciado.");
    }

    [ContextMenu("Imprimir Reporte Completo")]
// Imprime informe.
    public void PrintReport()
    {
        Debug.Log("[DaylightVerifier] " + GetStatistics());
        Debug.Log(ExportViolationsReport());
    }

    [ContextMenu("Imprimir Estado Actual de Ciudades")]
// Imprime actual ciudad estado.
    public void PrintCurrentCityState()
    {
        if (registeredCities.Count == 0) { Debug.Log("[DaylightVerifier] No hay ciudades registradas."); return; }
        int month = TimeManager.Instance != null ? TimeManager.Instance.CurrentUtcTime.Month : 1;
        int day   = TimeManager.Instance != null ? TimeManager.Instance.CurrentUtcTime.Day   : 15;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[DaylightVerifier] Estado actual — {registeredCities.Count} ciudades:");
// Foreach
        foreach (var city in registeredCities)
        {
            float expected = ComputeAstronomicalDaylight(city.latitude, month, day);
            sb.AppendLine($"  {city.cityName,-20} | {(city.isInDaylight ? "DIA " : "NOCHE")} | " +
                          $"Acum: {city.actualDaylightHours:F1}h | Esp ({city.latitude:F1}°): {expected:F2}h");
        }
        Debug.Log(sb.ToString());
    }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnNewDay -= HandleNewDay;
    }
}
#endif