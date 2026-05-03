using UnityEngine;
using System;

/// <summary>
/// Representa una carga/mercancía en el juego de freight forwarding.
/// Contiene toda la información necesaria sobre el envío, rutas, costos y estado.
/// </summary>
[Serializable]
public class Cargo
{
    [Header("Identificación")]
    [SerializeField] private string id;
    [SerializeField] private string name;
    [SerializeField] private Constants.CargoType cargoType;

    [Header("Ubicaciones")]
    [SerializeField] private WorldCity originCity;
    [SerializeField] private WorldCity destinationCity;

    [Header("Transporte")]
    [SerializeField] private Constants.TransportMode transportMode;
    [SerializeField] private Agent assignedAgent;

    [Header("Valores Económicos")]
    [SerializeField] private float cargoValue;
    [SerializeField] private float transportCost;
    [SerializeField] private float quotedPrice;
    [SerializeField] private float finalPrice;

    [Header("Tiempo")]
    [SerializeField] private int creationDay;
    [SerializeField] private int expiryDay;
    [SerializeField] private int estimatedDeliveryDay;
    [SerializeField] private int actualDeliveryDay;

    [Header("Estado")]
    [SerializeField] private Constants.CargoStatus status;
    [SerializeField] private Client client;

    [Header("Eventos")]
    [SerializeField] private GameEvent activeEvent;

    // Propiedades públicas
    public string Id => id;
    public string Name => name;
    public Constants.CargoType CargoType => cargoType;
    public WorldCity OriginCity => originCity;
    public WorldCity DestinationCity => destinationCity;
    public Constants.TransportMode TransportMode => transportMode;
    public Agent AssignedAgent => assignedAgent;
    public float CargoValue => cargoValue;
    public float TransportCost => transportCost;
    public float QuotedPrice => quotedPrice;
    public float FinalPrice => finalPrice;
    public int CreationDay => creationDay;
    public int ExpiryDay => expiryDay;
    public int EstimatedDeliveryDay => estimatedDeliveryDay;
    public int ActualDeliveryDay => actualDeliveryDay;
    public Constants.CargoStatus Status => status;
    public Client Client => client;
    public GameEvent ActiveEvent => activeEvent;

    // Propiedades calculadas
    public float Distance => CalculateDistance();
    public int DaysInTransit => TimeManager.Instance != null ? TimeManager.Instance.GetTotalDays() - creationDay : 0;
    public bool IsExpired => TimeManager.Instance != null && TimeManager.Instance.GetTotalDays() > expiryDay;
    public bool IsLate => actualDeliveryDay > estimatedDeliveryDay;
    public float Profit => finalPrice - transportCost;

    /// <summary>
    /// Constructor para crear una nueva carga.
    /// </summary>
    public Cargo(string name, Constants.CargoType cargoType, WorldCity origin, WorldCity destination, Client client)
    {
        this.id = GenerateId();
        this.name = name;
        this.cargoType = cargoType;
        this.originCity = origin;
        this.destinationCity = destination;
        this.client = client;
        this.status = Constants.CargoStatus.Available;

        // Calcular valores económicos
        CalculateCargoValue();
        CalculateExpiryDate();

        Debug.Log($"Carga creada: {name} ({cargoType}) de {origin.Name} a {destination.Name}");
    }

    /// <summary>
    /// Genera un ID único para la carga.
    /// </summary>
    private string GenerateId()
    {
        return $"CARGO_{DateTime.Now.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";
    }

    /// <summary>
    /// Calcula el valor base de la carga basado en tipo, distancia y otros factores.
    /// </summary>
    private void CalculateCargoValue()
    {
        float distance = CalculateDistance();
        float baseValue = distance * 10f; // Valor base por km
        float typeMultiplier = Constants.CARGO_VALUE_MULTIPLIERS[cargoType];

        cargoValue = baseValue * typeMultiplier;
    }

    /// <summary>
    /// Calcula la fecha de expiración de la carga.
    /// </summary>
    private void CalculateExpiryDate()
    {
        if (TimeManager.Instance != null)
        {
            creationDay = TimeManager.Instance.GetTotalDays();
            expiryDay = creationDay + Constants.CARGO_EXPIRY_DAYS;
        }
    }

    /// <summary>
    /// Calcula la distancia entre origen y destino usando coordenadas.
    /// </summary>
    private float CalculateDistance()
    {
        if (originCity == null || destinationCity == null) return 0f;

        // Usar fórmula de Haversine para distancia en km
        float lat1 = originCity.Latitude * Mathf.Deg2Rad;
        float lon1 = originCity.Longitude * Mathf.Deg2Rad;
        float lat2 = destinationCity.Latitude * Mathf.Deg2Rad;
        float lon2 = destinationCity.Longitude * Mathf.Deg2Rad;

        float dLat = lat2 - lat1;
        float dLon = lon2 - lon1;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(lat1) * Mathf.Cos(lat2) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return 6371 * c; // Radio de la Tierra en km
    }

    /// <summary>
    /// Asigna un agente de transporte a la carga.
    /// </summary>
    public void AssignAgent(Agent agent)
    {
        assignedAgent = agent;
        Debug.Log($"Agente {agent.Name} asignado a carga {name}");
    }

    /// <summary>
    /// Establece el modo de transporte para la carga.
    /// </summary>
    public void SetTransportMode(Constants.TransportMode mode)
    {
        transportMode = mode;

        // Recalcular costo de transporte
        if (EconomyManager.Instance != null)
        {
            transportCost = EconomyManager.Instance.CalculateTransportCost(Distance, cargoValue, transportMode);
        }

        Debug.Log($"Modo de transporte establecido: {mode} para carga {name}");
    }

    /// <summary>
    /// Establece el precio cotizado por el jugador.
    /// </summary>
    public void SetQuotedPrice(float price)
    {
        quotedPrice = price;
        status = Constants.CargoStatus.Quoting;
        Debug.Log($"Precio cotizado: ${price} para carga {name}");
    }

    /// <summary>
    /// Acepta la cotización y establece el precio final.
    /// </summary>
    public void AcceptQuote(float finalPrice)
    {
        this.finalPrice = finalPrice;
        status = Constants.CargoStatus.Active;

        // Calcular tiempo estimado de entrega
        CalculateEstimatedDelivery();

        Debug.Log($"Cotización aceptada: ${finalPrice} para carga {name}");
    }

    /// <summary>
    /// Calcula el tiempo estimado de entrega basado en distancia y modo de transporte.
    /// </summary>
    private void CalculateEstimatedDelivery()
    {
        if (TimeManager.Instance == null) return;

        int currentDay = TimeManager.Instance.GetTotalDays();
        float distance = CalculateDistance();

        // Velocidades aproximadas en km/día
        float speed = transportMode switch
        {
            Constants.TransportMode.Maritime => 600f, // 20 nudos
            Constants.TransportMode.Air => 19200f,    // 800 km/h * 24h
            Constants.TransportMode.Land => 800f,     // 800 km/día
            Constants.TransportMode.Rail => 1000f,    // 1000 km/día
            Constants.TransportMode.Multimodal => 1200f, // Combinado
            _ => 800f
        };

        int days = Mathf.Max(1, Mathf.CeilToInt(distance / speed));
        estimatedDeliveryDay = currentDay + days;
    }

    /// <summary>
    /// Marca la carga como completada.
    /// </summary>
    public void Complete()
    {
        status = Constants.CargoStatus.Completed;
        actualDeliveryDay = TimeManager.Instance != null ? TimeManager.Instance.GetTotalDays() : 0;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.RegisterCompletedCargo(finalPrice);
        }

        Debug.Log($"Carga completada: {name}");
    }

    /// <summary>
    /// Marca la carga como fallida.
    /// </summary>
    public void Fail()
    {
        status = Constants.CargoStatus.Failed;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.RegisterFailedCargo(transportCost * 0.5f); // Penalización del 50%
        }

        Debug.Log($"Carga fallida: {name}");
    }

    /// <summary>
    /// Asigna un evento activo a la carga.
    /// </summary>
    public void SetActiveEvent(GameEvent gameEvent)
    {
        activeEvent = gameEvent;
        Debug.Log($"Evento {gameEvent.Name} asignado a carga {name}");
    }

    /// <summary>
    /// Verifica si la carga puede usar un modo de transporte específico.
    /// </summary>
    public bool CanUseTransportMode(Constants.TransportMode mode)
    {
        if (originCity == null || destinationCity == null) return false;

        return mode switch
        {
            Constants.TransportMode.Maritime => originCity.HasPort && destinationCity.HasPort,
            Constants.TransportMode.Air => originCity.HasAirport && destinationCity.HasAirport,
            Constants.TransportMode.Land => originCity.IsLandHub && destinationCity.IsLandHub,
            Constants.TransportMode.Rail => originCity.HasRail && destinationCity.HasRail,
            Constants.TransportMode.Multimodal => true, // Siempre disponible
            _ => false
        };
    }

    /// <summary>
    /// Obtiene una descripción detallada de la carga.
    /// </summary>
    public string GetDescription()
    {
        return $"{name} ({cargoType})\n" +
               $"De: {originCity?.Name ?? "Desconocido"} → A: {destinationCity?.Name ?? "Desconocido"}\n" +
               $"Distancia: {Distance:0} km\n" +
               $"Valor: ${cargoValue:0}\n" +
               $"Estado: {status}";
    }
}