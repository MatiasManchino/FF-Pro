using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// EconomyManager gestiona el sistema económico del juego, incluyendo dinero, reputación y finanzas.
/// Maneja transacciones, ganancias, pérdidas y el estado financiero general de la empresa.
/// </summary>
public class EconomyManager : Singleton<EconomyManager>
{
    [Header("Estado Financiero")]
    [SerializeField] private float currentMoney = Constants.INITIAL_MONEY;
    [SerializeField] private float currentReputation = Constants.INITIAL_REPUTATION;

    [Header("Historial Financiero")]
    [SerializeField] private float totalEarned = 0f;
    [SerializeField] private float totalSpent = 0f;
    [SerializeField] private int completedCargos = 0;
    [SerializeField] private int failedCargos = 0;

    // Eventos
    public UnityEvent<float> OnMoneyChanged;
    public UnityEvent<float> OnReputationChanged;
    public UnityEvent OnBankruptcy;

    // Propiedades públicas
    public float CurrentMoney => currentMoney;
    public float CurrentReputation => currentReputation;
    public float TotalEarned => totalEarned;
    public float TotalSpent => totalSpent;
    public int CompletedCargos => completedCargos;
    public int FailedCargos => failedCargos;
    public bool IsBankrupt => currentMoney <= Constants.BANKRUPTCY_THRESHOLD;
    public bool HasLowReputation => currentReputation <= Constants.MIN_REPUTATION;

    /// <summary>
    /// Inicializa el EconomyManager con valores iniciales.
    /// </summary>
    public void Initialize()
    {
        currentMoney = Constants.INITIAL_MONEY;
        currentReputation = Constants.INITIAL_REPUTATION;
        totalEarned = 0f;
        totalSpent = 0f;
        completedCargos = 0;
        failedCargos = 0;

        Debug.Log($"EconomyManager inicializado. Dinero: ${currentMoney}, Reputación: {currentReputation}");
    }

    /// <summary>
    /// Agrega dinero a la cuenta del jugador.
    /// </summary>
    /// <param name="amount">Cantidad de dinero a agregar</param>
    /// <param name="reason">Razón de la transacción (para logging)</param>
    public void AddMoney(float amount, string reason = "")
    {
        if (amount <= 0) return;

        float oldMoney = currentMoney;
        currentMoney += amount;
        totalEarned += amount;

        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"Dinero agregado: +${amount} ({reason}). Total: ${currentMoney}");

        CheckBankruptcy();
    }

    /// <summary>
    /// Resta dinero de la cuenta del jugador.
    /// </summary>
    /// <param name="amount">Cantidad de dinero a restar</param>
    /// <param name="reason">Razón de la transacción (para logging)</param>
    /// <returns>True si la transacción fue exitosa</returns>
    public bool SpendMoney(float amount, string reason = "")
    {
        if (amount <= 0) return true;
        if (amount > currentMoney) return false; // No hay suficiente dinero

        float oldMoney = currentMoney;
        currentMoney -= amount;
        totalSpent += amount;

        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"Dinero gastado: -${amount} ({reason}). Total: ${currentMoney}");

        CheckBankruptcy();
        return true;
    }

    /// <summary>
    /// Modifica la reputación del jugador.
    /// </summary>
    /// <param name="amount">Cantidad de reputación a agregar (puede ser negativa)</param>
    /// <param name="reason">Razón del cambio (para logging)</param>
    public void ModifyReputation(float amount, string reason = "")
    {
        float oldReputation = currentReputation;
        currentReputation = Mathf.Clamp(currentReputation + amount, Constants.MIN_REPUTATION, Constants.MAX_REPUTATION);

        if (currentReputation != oldReputation)
        {
            OnReputationChanged?.Invoke(currentReputation);
            Debug.Log($"Reputación modificada: {amount:+0.##;-0.##} ({reason}). Total: {currentReputation}");
        }
    }

    /// <summary>
    /// Registra una carga completada exitosamente.
    /// </summary>
    /// <param name="revenue">Ingresos generados por la carga</param>
    public void RegisterCompletedCargo(float revenue)
    {
        completedCargos++;
        AddMoney(revenue, "Carga completada");
        ModifyReputation(1f, "Carga completada exitosamente");
    }

    /// <summary>
    /// Registra una carga fallida.
    /// </summary>
    /// <param name="penalty">Pérdida financiera por el fallo</param>
    public void RegisterFailedCargo(float penalty = 0f)
    {
        failedCargos++;
        if (penalty > 0)
        {
            SpendMoney(penalty, "Penalización por carga fallida");
        }
        ModifyReputation(-2f, "Carga fallida");
    }

    /// <summary>
    /// Calcula el costo de transporte basado en distancia, tipo de carga y modo de transporte.
    /// </summary>
    /// <param name="distance">Distancia en km</param>
    /// <param name="cargoValue">Valor base de la carga</param>
    /// <param name="transportMode">Modo de transporte</param>
    /// <returns>Costo calculado</returns>
    public float CalculateTransportCost(float distance, float cargoValue, Constants.TransportMode transportMode)
    {
        float baseCost = distance * 0.01f * cargoValue; // Costo base por km
        float multiplier = Constants.TRANSPORT_MULTIPLIERS[transportMode];
        return baseCost * multiplier;
    }

    /// <summary>
    /// Calcula los ingresos por una carga completada.
    /// </summary>
    /// <param name="cargoValue">Valor base de la carga</param>
    /// <param name="transportCost">Costo de transporte</param>
    /// <param name="profitMargin">Margen de ganancia (0.1 = 10%)</param>
    /// <returns>Ingresos calculados</returns>
    public float CalculateRevenue(float cargoValue, float transportCost, float profitMargin = 0.2f)
    {
        return transportCost * (1f + profitMargin);
    }

    /// <summary>
    /// Obtiene estadísticas financieras resumidas.
    /// </summary>
    /// <returns>Diccionario con estadísticas</returns>
    public System.Collections.Generic.Dictionary<string, float> GetFinancialStats()
    {
        return new System.Collections.Generic.Dictionary<string, float>
        {
            { "CurrentMoney", currentMoney },
            { "CurrentReputation", currentReputation },
            { "TotalEarned", totalEarned },
            { "TotalSpent", totalSpent },
            { "NetProfit", totalEarned - totalSpent },
            { "CompletedCargos", completedCargos },
            { "FailedCargos", failedCargos },
            { "SuccessRate", completedCargos > 0 ? (float)completedCargos / (completedCargos + failedCargos) : 0f }
        };
    }

    /// <summary>
    /// Verifica si el jugador está en bancarrota y dispara eventos correspondientes.
    /// </summary>
    private void CheckBankruptcy()
    {
        if (IsBankrupt)
        {
            OnBankruptcy?.Invoke();
            Debug.LogWarning("¡BANCARROTA! El juego debería terminar.");
        }
    }

    /// <summary>
    /// Reinicia el estado económico (para nuevo juego).
    /// </summary>
    public void ResetEconomy()
    {
        Initialize();
        Debug.Log("Economía reiniciada.");
    }
}