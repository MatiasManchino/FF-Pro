using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Representa un cliente en el juego de freight forwarding.
/// Los clientes tienen personalidades que afectan cómo negocian y pagan.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "NewClient", menuName = "FreightForwarder/Client")]
public class Client : ScriptableObject
{
    [Header("Información Básica")]
    [SerializeField] private string clientName;
    [SerializeField] private string companyName;
    [SerializeField] private Constants.ClientType clientType;

    [Header("Preferencias y Tolerancia")]
    [SerializeField] private float tolerance; // 0-1, cuánto tolera precios altos
    [SerializeField] private float negotiationSkill; // 0-1, qué tan bueno negociando
    [SerializeField] private List<Constants.CargoType> preferredCargoTypes;

    [Header("Historial")]
    [SerializeField] private int totalContracts;
    [SerializeField] private int successfulContracts;
    [SerializeField] private int failedContracts;
    [SerializeField] private float totalPaid;
    [SerializeField] private float averagePaymentDelay;

    [Header("Estado Actual")]
    [SerializeField] private float currentSatisfaction; // 0-1
    [SerializeField] private bool isActive;
    [SerializeField] private int lastInteractionDay;

    // Propiedades públicas
    public string Name => clientName;
    public string CompanyName { get => companyName; set => companyName = value; }
    public Constants.ClientType ClientType => clientType;
    public float Tolerance => tolerance;
    public float NegotiationSkill => negotiationSkill;
    public List<Constants.CargoType> PreferredCargoTypes => preferredCargoTypes;
    public int TotalContracts => totalContracts;
    public int SuccessfulContracts => successfulContracts;
    public int FailedContracts => failedContracts;
    public float TotalPaid => totalPaid;
    public float AveragePaymentDelay => averagePaymentDelay;
    public float CurrentSatisfaction => currentSatisfaction;
    public int LastInteractionDay => lastInteractionDay;

    // Propiedades calculadas
    public float SuccessRate => totalContracts > 0 ? (float)successfulContracts / totalContracts : 0f;
    public float AverageContractValue => totalContracts > 0 ? totalPaid / totalContracts : 0f;
    public bool IsSatisfied => currentSatisfaction >= 0.7f;
    public bool IsDissatisfied => currentSatisfaction <= 0.3f;
    public bool IsActive { get => isActive; set => isActive = value; }

    /// <summary>
    /// Inicializa el cliente con valores predeterminados.
    /// </summary>
    private void OnEnable()
    {
        if (preferredCargoTypes == null)
        {
            preferredCargoTypes = new List<Constants.CargoType>();
        }
    }

    /// <summary>
    /// Evalúa una cotización y decide si aceptarla.
    /// </summary>
    /// <param name="cargo">La carga cotizada</param>
    /// <param name="quotedPrice">Precio cotizado</param>
    /// <returns>True si acepta la cotización</returns>
    public bool EvaluateQuote(Cargo cargo, float quotedPrice)
    {
        if (cargo == null) return false;

        // Calcular precio "justo" basado en valor de carga y distancia
        float fairPrice = CalculateFairPrice(cargo);

        // Modificador por tipo de cliente
        float typeModifier = Constants.CLIENT_TOLERANCE_MODIFIERS[clientType];

        // Modificador por satisfacción actual
        float satisfactionModifier = (currentSatisfaction - 0.5f) * 0.2f;

        // Tolerancia efectiva
        float effectiveTolerance = tolerance + typeModifier + satisfactionModifier;

        // Probabilidad de aceptación
        float priceRatio = quotedPrice / fairPrice;
        float acceptanceProbability = Mathf.Clamp(1f - (priceRatio - 1f) + effectiveTolerance, 0f, 1f);

        // Aplicar habilidad de negociación del cliente
        acceptanceProbability *= (1f + negotiationSkill * 0.5f);

        float roll = Random.value;
        bool accepts = roll <= acceptanceProbability;

        Debug.Log($"Cliente {clientName} evalúa cotización: ${quotedPrice} vs justo ${fairPrice:0}. Prob: {acceptanceProbability:0.##}, Resultado: {(accepts ? "ACEPTA" : "RECHAZA")}");

        // Actualizar última interacción
        lastInteractionDay = TimeManager.Instance != null ? TimeManager.Instance.GetTotalDays() : 0;

        return accepts;
    }

    /// <summary>
    /// Calcula un precio "justo" para una carga.
    /// </summary>
    /// <param name="cargo">La carga</param>
    /// <returns>Precio justo estimado</returns>
    private float CalculateFairPrice(Cargo cargo)
    {
        if (cargo == null) return 0f;

        float baseCost = cargo.TransportCost;
        float profitMargin = 0.3f; // 30% margen típico
        float typeMultiplier = Constants.CARGO_VALUE_MULTIPLIERS[cargo.CargoType];

        return baseCost * (1f + profitMargin) * typeMultiplier;
    }

    /// <summary>
    /// Inicia una negociación con contraoferta del cliente.
    /// </summary>
    /// <param name="cargo">La carga</param>
    /// <param name="originalQuote">Cotización original del jugador</param>
    /// <returns>Contraoferta del cliente (o 0 si no negocia)</returns>
    public float MakeCounterOffer(Cargo cargo, float originalQuote)
    {
        if (cargo == null) return 0f;

        // Probabilidad de hacer contraoferta
        float counterOfferChance = 0.4f + (negotiationSkill * 0.3f) - (tolerance * 0.2f);
        counterOfferChance = Mathf.Clamp(counterOfferChance, 0.1f, 0.8f);

        if (Random.value > counterOfferChance)
        {
            return 0f; // No hace contraoferta
        }

        // Calcular contraoferta
        float fairPrice = CalculateFairPrice(cargo);
        float maxDiscount = negotiationSkill * 0.15f; // Hasta 15% descuento máximo
        float discount = Random.Range(0.05f, maxDiscount);

        float counterOffer = originalQuote * (1f - discount);

        // No ofrecer menos del costo + pequeño margen
        float minimumOffer = cargo.TransportCost * 1.05f;
        counterOffer = Mathf.Max(counterOffer, minimumOffer);

        Debug.Log($"Cliente {clientName} hace contraoferta: ${counterOffer:0} (original: ${originalQuote:0})");

        return counterOffer;
    }

    /// <summary>
    /// Registra el resultado de un contrato.
    /// </summary>
    /// <param name="success">Si el contrato fue exitoso</param>
    /// <param name="paymentAmount">Monto pagado</param>
    /// <param name="paymentDelay">Días de retraso en pago</param>
    public void RegisterContractResult(bool success, float paymentAmount, int paymentDelay = 0)
    {
        totalContracts++;

        if (success)
        {
            successfulContracts++;
            currentSatisfaction = Mathf.Min(1f, currentSatisfaction + 0.1f);
        }
        else
        {
            failedContracts++;
            currentSatisfaction = Mathf.Max(0f, currentSatisfaction - 0.2f);
        }

        totalPaid += paymentAmount;
        averagePaymentDelay = ((averagePaymentDelay * (totalContracts - 1)) + paymentDelay) / totalContracts;

        Debug.Log($"Cliente {clientName} - Contrato registrado. Satisfacción: {currentSatisfaction:0.##}");
    }

    /// <summary>
    /// Actualiza la satisfacción del cliente con el tiempo.
    /// </summary>
    public void UpdateSatisfaction(float decayRate)
    {
        if (TimeManager.Instance == null) return;

        int daysSinceLastInteraction = TimeManager.Instance.GetTotalDays() - lastInteractionDay;

        // Degradación gradual de satisfacción con el tiempo
        if (daysSinceLastInteraction > 30)
        {
            currentSatisfaction = Mathf.Max(0f, currentSatisfaction - decayRate);
        }
    }

    /// <summary>
    /// Verifica si el cliente está interesado en un tipo de carga específico.
    /// </summary>
    /// <param name="cargoType">Tipo de carga</param>
    /// <returns>True si está interesado</returns>
    public bool IsInterestedInCargoType(Constants.CargoType cargoType)
    {
        return preferredCargoTypes.Contains(cargoType) || preferredCargoTypes.Count == 0;
    }

    /// <summary>
    /// Obtiene una descripción detallada del cliente.
    /// </summary>
    /// <returns>Descripción formateada</returns>
    public string GetDescription()
    {
        string desc = $"{clientName}\n";
        desc += $"Compañía: {companyName}\n";
        desc += $"Tipo: {clientType}\n";
        desc += $"Tolerancia: {tolerance:0.##}\n";
        desc += $"Habilidad negociación: {negotiationSkill:0.##}\n";
        desc += $"Satisfacción actual: {currentSatisfaction:0.##}\n";
        desc += $"Contratos totales: {totalContracts}\n";
        desc += $"Tasa de éxito: {SuccessRate:0.##}\n";

        if (preferredCargoTypes.Count > 0)
        {
            desc += "Cargas preferidas: ";
            foreach (var type in preferredCargoTypes)
            {
                desc += $"{type}, ";
            }
            desc = desc.TrimEnd(',', ' ') + "\n";
        }

        desc += $"Activo: {(isActive ? "Sí" : "No")}\n";

        return desc;
    }

    /// <summary>
    /// Crea un cliente de prueba con valores predeterminados.
    /// </summary>
    public static Client CreateTestClient(string name, Constants.ClientType type)
    {
        Client client = CreateInstance<Client>();
        client.clientName = name;
        client.companyName = "Test Company";
        client.clientType = type;
        client.tolerance = type switch
        {
            Constants.ClientType.GoodPayer => 0.8f,
            Constants.ClientType.BadPayer => 0.3f,
            Constants.ClientType.UrgentClient => 0.9f,
            Constants.ClientType.CreditClient => 0.6f,
            Constants.ClientType.VeryBadClient => 0.2f,
            Constants.ClientType.ContractClient => 0.7f,
            _ => 0.5f
        };
        client.negotiationSkill = Random.Range(0.3f, 0.8f);
        client.preferredCargoTypes = new List<Constants.CargoType>();
        client.totalContracts = 0;
        client.successfulContracts = 0;
        client.failedContracts = 0;
        client.totalPaid = 0f;
        client.averagePaymentDelay = 0f;
        client.currentSatisfaction = 0.5f;
        client.isActive = true;
        client.lastInteractionDay = 0;

        return client;
    }
}