using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
// Gestiona instance.
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

// Devuelve el velocidad multiplier actual
    public float    CurrentSpeedMultiplier => TimeSpeeds[_speedIndex];
// Devuelve el velocidad índice actual
    public int      CurrentSpeedIndex      => _speedIndex;
// Día progress.
    public float    DayProgress            { get; private set; }
// Actual utc tiempo.
    public DateTime CurrentUtcTime         { get; private set; }
// Actual local tiempo
    public DateTime CurrentLocalTime       => CurrentUtcTime.AddHours(BA_UTC_OFFSET);

    private DateTime _startUtc;
    private double   _elapsedGameSeconds;

// Configura referencias tempranas antes de Start.
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        _startUtc            = new DateTime(_startYear, _startMonth, _startDay,
                                            _startHour, _startMinute, 0, DateTimeKind.Utc);
        CurrentUtcTime       = _startUtc;
        _elapsedGameSeconds  = 0.0;
        UpdateDayProgress();
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (CurrentSpeedMultiplier > 0f)
        {
            _elapsedGameSeconds += Time.deltaTime
                                   * GAME_SECONDS_PER_REAL_SECOND
                                   * (double)CurrentSpeedMultiplier;
            CurrentUtcTime = _startUtc.AddSeconds(_elapsedGameSeconds);
        }
        UpdateDayProgress();
    }

// Actualiza día progress
    private void UpdateDayProgress() =>
        DayProgress = (float)(CurrentUtcTime.TimeOfDay.TotalSeconds / 86400.0);

// Establece velocidad index.
    public void SetSpeedIndex(int i)
    {
        if (i >= 0 && i < TimeSpeeds.Length) _speedIndex = i;
    }
}