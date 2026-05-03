using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TimeManager gestiona el tiempo del juego, incluyendo días, meses, años y eventos temporales.
/// Proporciona un sistema de tiempo acelerado para simular el paso del tiempo en el juego.
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
    [Header("Configuración de Tiempo")]
    [SerializeField] private float timeScale = 1f; // Multiplicador de velocidad del tiempo
    [SerializeField] private float secondsPerGameDay = 10f; // Segundos reales por día de juego

    [Header("Estado del Tiempo")]
    [SerializeField] private int currentYear = Constants.GAME_START_YEAR;
    [SerializeField] private int currentMonth = Constants.GAME_START_MONTH;
    [SerializeField] private int currentDay = Constants.GAME_START_DAY;
    [SerializeField] private int currentHour = 0;

    private float gameTimeAccumulator = 0f;
    private bool isGameTimeRunning = false;

    // Eventos
    public UnityEvent OnDayChanged;
    public UnityEvent OnMonthChanged;
    public UnityEvent OnYearChanged;
    public UnityEvent OnHourChanged;

    // Propiedades públicas
    public int CurrentYear => currentYear;
    public int CurrentMonth => currentMonth;
    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public bool IsGameTimeRunning => isGameTimeRunning;
    public float TimeScale => timeScale;

    /// <summary>
    /// Inicializa el TimeManager con la fecha de inicio del juego.
    /// </summary>
    public void Initialize()
    {
        currentYear = Constants.GAME_START_YEAR;
        currentMonth = Constants.GAME_START_MONTH;
        currentDay = Constants.GAME_START_DAY;
        currentHour = 0;

        gameTimeAccumulator = 0f;
        isGameTimeRunning = false;

        Debug.Log($"TimeManager inicializado. Fecha: {GetCurrentDateString()}");
    }

    /// <summary>
    /// Inicia el flujo del tiempo del juego.
    /// </summary>
    public void StartGameTime()
    {
        isGameTimeRunning = true;
        Debug.Log("Tiempo del juego iniciado.");
    }

    /// <summary>
    /// Pausa el tiempo del juego.
    /// </summary>
    public void PauseGameTime()
    {
        isGameTimeRunning = false;
        Debug.Log("Tiempo del juego pausado.");
    }

    /// <summary>
    /// Reanuda el tiempo del juego.
    /// </summary>
    public void ResumeGameTime()
    {
        isGameTimeRunning = true;
        Debug.Log("Tiempo del juego reanudado.");
    }

    /// <summary>
    /// Establece la velocidad del tiempo del juego.
    /// </summary>
    /// <param name="newTimeScale">Nuevo multiplicador de velocidad (1 = normal)</param>
    public void SetTimeScale(float newTimeScale)
    {
        timeScale = Mathf.Max(0.1f, newTimeScale);
        Debug.Log($"Velocidad del tiempo cambiada a: {timeScale}x");
    }

    private void Update()
    {
        if (!isGameTimeRunning) return;

        // Acumular tiempo basado en el timeScale
        gameTimeAccumulator += Time.deltaTime * timeScale;

        // Avanzar horas cuando se complete un "día de juego"
        while (gameTimeAccumulator >= secondsPerGameDay)
        {
            gameTimeAccumulator -= secondsPerGameDay;
            AdvanceHour();
        }
    }

    /// <summary>
    /// Avanza una hora en el tiempo del juego.
    /// </summary>
    private void AdvanceHour()
    {
        currentHour++;

        if (currentHour >= Constants.HOURS_PER_DAY)
        {
            currentHour = 0;
            AdvanceDay();
        }

        OnHourChanged?.Invoke();
    }

    /// <summary>
    /// Avanza un día en el tiempo del juego.
    /// </summary>
    private void AdvanceDay()
    {
        currentDay++;

        if (currentDay > Constants.DAYS_PER_MONTH)
        {
            currentDay = 1;
            AdvanceMonth();
        }

        OnDayChanged?.Invoke();
        Debug.Log($"Nuevo día: {GetCurrentDateString()}");
    }

    /// <summary>
    /// Avanza un mes en el tiempo del juego.
    /// </summary>
    private void AdvanceMonth()
    {
        currentMonth++;

        if (currentMonth > 12)
        {
            currentMonth = 1;
            AdvanceYear();
        }

        OnMonthChanged?.Invoke();
        Debug.Log($"Nuevo mes: {GetCurrentDateString()}");
    }

    /// <summary>
    /// Avanza un año en el tiempo del juego.
    /// </summary>
    private void AdvanceYear()
    {
        currentYear++;
        OnYearChanged?.Invoke();
        Debug.Log($"Nuevo año: {GetCurrentDateString()}");
    }

    /// <summary>
    /// Avanza múltiples días en el tiempo del juego.
    /// </summary>
    /// <param name="days">Número de días a avanzar</param>
    public void AdvanceDays(int days)
    {
        for (int i = 0; i < days; i++)
        {
            AdvanceDay();
        }
    }

    /// <summary>
    /// Obtiene la fecha actual como string formateado.
    /// </summary>
    /// <returns>Fecha en formato "DD/MM/YYYY HH:00"</returns>
    public string GetCurrentDateString()
    {
        return $"{currentDay:00}/{currentMonth:00}/{currentYear} {currentHour:00}:00";
    }

    /// <summary>
    /// Obtiene el número total de días transcurridos desde el inicio del juego.
    /// </summary>
    /// <returns>Días totales</returns>
    public int GetTotalDays()
    {
        int yearsInDays = (currentYear - Constants.GAME_START_YEAR) * 12 * Constants.DAYS_PER_MONTH;
        int monthsInDays = (currentMonth - Constants.GAME_START_MONTH) * Constants.DAYS_PER_MONTH;
        int days = currentDay - Constants.GAME_START_DAY;

        return yearsInDays + monthsInDays + days;
    }

    /// <summary>
    /// Verifica si es un día laborable (lunes a viernes).
    /// </summary>
    /// <returns>True si es día laborable</returns>
    public bool IsWeekday()
    {
        // Asumiendo que el día 1 es lunes
        int dayOfWeek = (GetTotalDays() % 7) + 1;
        return dayOfWeek >= 1 && dayOfWeek <= 5; // Lunes a viernes
    }

    /// <summary>
    /// Verifica si es fin de semana.
    /// </summary>
    /// <returns>True si es sábado o domingo</returns>
    public bool IsWeekend()
    {
        return !IsWeekday();
    }
}