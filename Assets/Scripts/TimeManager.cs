using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    // 1 día de juego = 20 minutos reales = 1200 segundos reales.
    // A velocidad 1x, 1 segundo real avanza 72 segundos de juego (86400/1200).
    private const double GAME_SECONDS_PER_REAL_SECOND = 86400.0 / 1200.0; // 72

    [Header("Fecha/Hora Inicial (UTC)")]
    [SerializeField] private int _startYear   = 2023;
    [SerializeField] private int _startMonth  = 3;
    [SerializeField] private int _startDay    = 20;
    [SerializeField] private int _startHour   = 11; // 08:00 Buenos Aires = 11:00 UTC
    [SerializeField] private int _startMinute = 0;

    // Velocidades: 0=Pausa, 1=1x, 2=10x, 3=100x, 4=1000x
    public readonly float[] TimeSpeeds = { 0f, 1f, 10f, 100f, 1000f };
    [SerializeField] private int _speedIndex = 1; // arranca en 1x

    public const float BA_UTC_OFFSET = -3f;

    public float    CurrentSpeedMultiplier => TimeSpeeds[_speedIndex];
    public int      CurrentSpeedIndex      => _speedIndex;
    public float    DayProgress            { get; private set; }
    public DateTime CurrentUtcTime         { get; private set; }
    public DateTime CurrentLocalTime       => CurrentUtcTime.AddHours(BA_UTC_OFFSET);

    private DateTime _startUtc;
    private double   _elapsedGameSeconds;
    
    // Variables para detectar cambios de día/mes/año
    private int _lastDayOfYear = -1;
    private int _lastMonth = -1;
    private int _lastYear = -1;

    // ── Eventos ────────────────────────────────────────────────────────────
    /// <summary>Se dispara al comenzar un nuevo día UTC.</summary>
    public event Action<DateTime> OnNewDay;
    
    /// <summary>Se dispara al comenzar un nuevo mes.</summary>
    public event Action<DateTime> OnNewMonth;
    
    /// <summary>Se dispara al comenzar un nuevo año.</summary>
    public event Action<DateTime> OnNewYear;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _startUtc            = new DateTime(_startYear, _startMonth, _startDay,
                                            _startHour, _startMinute, 0, DateTimeKind.Utc);
        CurrentUtcTime       = _startUtc;
        _elapsedGameSeconds  = 0.0;
        
        // Inicializar tracking de fecha
        _lastDayOfYear = CurrentUtcTime.DayOfYear;
        _lastMonth = CurrentUtcTime.Month;
        _lastYear = CurrentUtcTime.Year;
        
        UpdateDayProgress();
    }

    void Update()
    {
        if (CurrentSpeedMultiplier > 0f)
        {
            _elapsedGameSeconds += Time.deltaTime
                                   * GAME_SECONDS_PER_REAL_SECOND
                                   * (double)CurrentSpeedMultiplier;
            CurrentUtcTime = _startUtc.AddSeconds(_elapsedGameSeconds);
        }
        
        CheckDateChanges();
        UpdateDayProgress();
    }

    private void UpdateDayProgress() =>
        DayProgress = (float)(CurrentUtcTime.TimeOfDay.TotalSeconds / 86400.0);

    /// <summary>
    /// Verifica si hubo cambio de día, mes o año y dispara los eventos correspondientes.
    /// </summary>
    private void CheckDateChanges()
    {
        int currentDayOfYear = CurrentUtcTime.DayOfYear;
        int currentMonth = CurrentUtcTime.Month;
        int currentYear = CurrentUtcTime.Year;

        // Detectar cambio de año
        if (currentYear != _lastYear)
        {
            _lastYear = currentYear;
            OnNewYear?.Invoke(CurrentUtcTime);
            
            // Un nuevo año también implica nuevo mes y nuevo día
            _lastMonth = currentMonth;
            OnNewMonth?.Invoke(CurrentUtcTime);
            
            _lastDayOfYear = currentDayOfYear;
            OnNewDay?.Invoke(CurrentUtcTime);
        }
        // Detectar cambio de mes
        else if (currentMonth != _lastMonth)
        {
            _lastMonth = currentMonth;
            OnNewMonth?.Invoke(CurrentUtcTime);
            
            // Un nuevo mes también implica nuevo día
            _lastDayOfYear = currentDayOfYear;
            OnNewDay?.Invoke(CurrentUtcTime);
        }
        // Detectar cambio de día
        else if (currentDayOfYear != _lastDayOfYear)
        {
            _lastDayOfYear = currentDayOfYear;
            OnNewDay?.Invoke(CurrentUtcTime);
        }
    }

    /// <summary>
    /// Cambia la velocidad de simulación.
    /// </summary>
    /// <param name="i">Índice de velocidad (0=Pausa, 1=1x, 2=10x, 3=100x, 4=1000x).</param>
    public void SetSpeedIndex(int i)
    {
        if (i >= 0 && i < TimeSpeeds.Length) _speedIndex = i;
    }

    /// <summary>
    /// Obtiene el nombre de la velocidad actual.
    /// </summary>
    public string GetCurrentSpeedLabel()
    {
        return _speedIndex switch
        {
            0 => "PAUSA",
            1 => "x1",
            2 => "x10",
            3 => "x100",
            4 => "x1000",
            _ => "?"
        };
    }

    /// <summary>
    /// Obtiene el progreso del día como porcentaje (0-100%).
    /// </summary>
    public float GetDayProgressPercent()
    {
        return DayProgress * 100f;
    }

    /// <summary>
    /// Calcula los días transcurridos desde el inicio de la simulación.
    /// </summary>
    public int GetElapsedDays()
    {
        return (int)(_elapsedGameSeconds / 86400.0);
    }

    /// <summary>
    /// Obtiene la estación del año para el hemisferio especificado.
    /// </summary>
    /// <param name="northernHemisphere">True para hemisferio norte, false para sur.</param>
    /// <returns>Nombre de la estación.</returns>
    public string GetCurrentSeason(bool northernHemisphere = true)
    {
        int month = CurrentUtcTime.Month;
        
        if (northernHemisphere)
        {
            return month switch
            {
                12 or 1 or 2 => "Invierno",
                3 or 4 or 5 => "Primavera",
                6 or 7 or 8 => "Verano",
                9 or 10 or 11 => "Otoño",
                _ => "?"
            };
        }
        else
        {
            return month switch
            {
                12 or 1 or 2 => "Verano",
                3 or 4 or 5 => "Otoño",
                6 or 7 or 8 => "Invierno",
                9 or 10 or 11 => "Primavera",
                _ => "?"
            };
        }
    }

    /// <summary>
    /// Reinicia la simulación a la fecha/hora inicial.
    /// </summary>
    public void ResetToStart()
    {
        _elapsedGameSeconds = 0.0;
        CurrentUtcTime = _startUtc;
        _lastDayOfYear = CurrentUtcTime.DayOfYear;
        _lastMonth = CurrentUtcTime.Month;
        _lastYear = CurrentUtcTime.Year;
        UpdateDayProgress();
    }
}