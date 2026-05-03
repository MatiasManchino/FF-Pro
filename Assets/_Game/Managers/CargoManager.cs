using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CargoManager gestiona todas las cargas del juego: creación, asignación, seguimiento y resolución.
/// Es el punto central para el ciclo de vida de las cargas.
/// </summary>
public class CargoManager : Singleton<CargoManager>
{
    [Header("Configuración del Mercado")]
    [SerializeField] private int maxMarketCargos = 10;
    [SerializeField] private float marketRefreshIntervalMinutes = 5f;

    [Header("Listas de Cargas")]
    [SerializeField] private List<Cargo> marketCargos = new List<Cargo>();
    [SerializeField] private List<Cargo> activeCargos = new List<Cargo>();
    [SerializeField] private List<Cargo> completedCargos = new List<Cargo>();
    [SerializeField] private List<Cargo> failedCargos = new List<Cargo>();

    [Header("Referencias")]
    [SerializeField] private List<WorldCity> availableCities;
    [SerializeField] private List<Client> availableClients;
    [SerializeField] private List<Agent> availableAgents;

    private float lastMarketRefreshTime;

    // Eventos
    public System.Action<Cargo> OnCargoAddedToMarket;
    public System.Action<Cargo> OnCargoAccepted;
    public System.Action<Cargo> OnCargoCompleted;
    public System.Action<Cargo> OnCargoFailed;

    // Propiedades públicas
    public List<Cargo> MarketCargos => marketCargos;
    public List<Cargo> ActiveCargos => activeCargos;
    public List<Cargo> CompletedCargos => completedCargos;
    public List<Cargo> FailedCargos => failedCargos;
    public int TotalCargos => marketCargos.Count + activeCargos.Count + completedCargos.Count + failedCargos.Count;

    /// <summary>
    /// Inicializa el CargoManager.
    /// </summary>
    public void Initialize()
    {
        marketCargos = new List<Cargo>();
        activeCargos = new List<Cargo>();
        completedCargos = new List<Cargo>();
        failedCargos = new List<Cargo>();

        lastMarketRefreshTime = 0f;

        // Inicializar listas si no están asignadas
        if (availableCities == null) availableCities = new List<WorldCity>();
        if (availableClients == null) availableClients = new List<Client>();
        if (availableAgents == null) availableAgents = new List<Agent>();

        Debug.Log("CargoManager inicializado");

        // Generar cargas iniciales para el mercado
        RefreshMarket();
    }

    /// <summary>
    /// Actualización por frame del CargoManager.
    /// </summary>
    private void Update()
    {
        // Refrescar mercado periódicamente
        if (Time.time - lastMarketRefreshTime >= marketRefreshIntervalMinutes * 60f)
        {
            RefreshMarket();
        }

        // Verificar expiración de cargas en mercado
        CheckExpiredCargos();

        // Procesar cargas activas
        ProcessActiveCargos();
    }

    /// <summary>
    /// Refresca el mercado con nuevas cargas.
    /// </summary>
    public void RefreshMarket()
    {
        lastMarketRefreshTime = Time.time;

        // Remover cargas expiradas
        marketCargos.RemoveAll(cargo => cargo.IsExpired);

        // Generar nuevas cargas hasta el máximo
        int cargosToGenerate = maxMarketCargos - marketCargos.Count;
        for (int i = 0; i < cargosToGenerate; i++)
        {
            Cargo newCargo = GenerateRandomCargo();
            if (newCargo != null)
            {
                marketCargos.Add(newCargo);
                OnCargoAddedToMarket?.Invoke(newCargo);
                Debug.Log($"Nueva carga agregada al mercado: {newCargo.Name}");
            }
        }

        Debug.Log($"Mercado refrescado. Cargas disponibles: {marketCargos.Count}");
    }

    /// <summary>
    /// Genera una carga aleatoria para el mercado.
    /// </summary>
    /// <returns>Carga generada o null si falla</returns>
    private Cargo GenerateRandomCargo()
    {
        if (availableCities == null || availableCities.Count < 2 ||
            availableClients == null || availableClients.Count == 0)
        {
            Debug.LogWarning("No hay suficientes ciudades o clientes para generar cargas");
            return null;
        }

        // Seleccionar ciudades aleatorias
        WorldCity origin = availableCities[Random.Range(0, availableCities.Count)];
        WorldCity destination = availableCities[Random.Range(0, availableCities.Count)];

        // Asegurar que origen y destino sean diferentes
        while (destination == origin)
        {
            destination = availableCities[Random.Range(0, availableCities.Count)];
        }

        // Seleccionar cliente aleatorio
        Client client = availableClients[Random.Range(0, availableClients.Count)];

        // Seleccionar tipo de carga basado en probabilidades
        Constants.CargoType cargoType = SelectRandomCargoType();

        // Generar nombre descriptivo
        string cargoName = GenerateCargoName(cargoType);

        // Crear la carga
        Cargo newCargo = new Cargo(cargoName, cargoType, origin, destination, client);

        return newCargo;
    }

    /// <summary>
    /// Selecciona un tipo de carga aleatorio basado en probabilidades.
    /// </summary>
    private Constants.CargoType SelectRandomCargoType()
    {
        float roll = Random.value;
        float cumulative = 0f;

        // Probabilidades aproximadas (ajustables)
        if ((cumulative += 0.4f) >= roll) return Constants.CargoType.General;
        if ((cumulative += 0.2f) >= roll) return Constants.CargoType.Refrigerated;
        if ((cumulative += 0.15f) >= roll) return Constants.CargoType.Dangerous;
        if ((cumulative += 0.15f) >= roll) return Constants.CargoType.Urgent;
        return Constants.CargoType.Valuable;
    }

    /// <summary>
    /// Genera un nombre descriptivo para la carga.
    /// </summary>
    private string GenerateCargoName(Constants.CargoType type)
    {
        string[] prefixes = type switch
        {
            Constants.CargoType.Refrigerated => new[] { "Alimentos", "Medicamentos", "Productos Frescos", "Vacunas" },
            Constants.CargoType.Dangerous => new[] { "Químicos", "Explosivos", "Materiales Tóxicos", "Sustancias Peligrosas" },
            Constants.CargoType.Urgent => new[] { "Documentos Urgentes", "Equipos Médicos", "Partes Críticas", "Envío Express" },
            Constants.CargoType.Valuable => new[] { "Obras de Arte", "Joyería", "Electrónicos Premium", "Antigüedades" },
            _ => new[] { "Mercancía General", "Productos Industriales", "Bienes de Consumo", "Materiales de Construcción" }
        };

        return prefixes[Random.Range(0, prefixes.Length)];
    }

    /// <summary>
    /// Verifica y remueve cargas expiradas del mercado.
    /// </summary>
    private void CheckExpiredCargos()
    {
        List<Cargo> expiredCargos = marketCargos.FindAll(cargo => cargo.IsExpired);

        foreach (Cargo expiredCargo in expiredCargos)
        {
            marketCargos.Remove(expiredCargo);
            failedCargos.Add(expiredCargo);
            expiredCargo.Fail();
            Debug.Log($"Carga expirada removida del mercado: {expiredCargo.Name}");
        }
    }

    /// <summary>
    /// Procesa las cargas activas: verifica llegada, eventos, etc.
    /// </summary>
    private void ProcessActiveCargos()
    {
        foreach (Cargo cargo in activeCargos)
        {
            // Verificar si llegó a destino
            if (TimeManager.Instance != null &&
                TimeManager.Instance.GetTotalDays() >= cargo.EstimatedDeliveryDay)
            {
                ResolveCargo(cargo);
            }
        }
    }

    /// <summary>
    /// Acepta una cotización y mueve la carga a activa.
    /// </summary>
    /// <param name="cargo">La carga a aceptar</param>
    /// <param name="quotedPrice">Precio final acordado</param>
    /// <param name="selectedAgent">Agente asignado</param>
    /// <param name="transportMode">Modo de transporte seleccionado</param>
    public bool AcceptQuote(Cargo cargo, float quotedPrice, Agent selectedAgent, Constants.TransportMode transportMode)
    {
        if (cargo == null || !marketCargos.Contains(cargo) || selectedAgent == null)
            return false;

        // Verificar que el agente esté disponible
        if (!selectedAgent.IsAvailable)
            return false;

        // Verificar que el modo de transporte sea válido
        if (!cargo.CanUseTransportMode(transportMode))
            return false;

        // Mover carga del mercado a activa
        marketCargos.Remove(cargo);
        activeCargos.Add(cargo);

        // Configurar carga
        cargo.SetQuotedPrice(quotedPrice);
        cargo.SetTransportMode(transportMode);
        cargo.AssignAgent(selectedAgent);
        cargo.AcceptQuote(quotedPrice);

        // Marcar agente como ocupado
        selectedAgent.AssignCargo(cargo);

        OnCargoAccepted?.Invoke(cargo);
        Debug.Log($"Cotización aceptada: {cargo.Name} por ${quotedPrice} con {selectedAgent.Name}");

        return true;
    }

    /// <summary>
    /// Resuelve una carga que ha llegado a su destino.
    /// </summary>
    /// <param name="cargo">La carga a resolver</param>
    private void ResolveCargo(Cargo cargo)
    {
        if (cargo == null || !activeCargos.Contains(cargo)) return;

        activeCargos.Remove(cargo);

        // Simular resultado con el agente
        bool success = cargo.AssignedAgent.SimulateTransport(cargo);

        if (success)
        {
            cargo.Complete();
            completedCargos.Add(cargo);
            OnCargoCompleted?.Invoke(cargo);
            Debug.Log($"Carga completada exitosamente: {cargo.Name}");
        }
        else
        {
            cargo.Fail();
            failedCargos.Add(cargo);
            OnCargoFailed?.Invoke(cargo);
            Debug.Log($"Carga fallida: {cargo.Name}");
        }

        // Liberar agente
        cargo.AssignedAgent.ReleaseFromCargo(cargo, success);
    }

    /// <summary>
    /// Obtiene estadísticas del sistema de cargas.
    /// </summary>
    public Dictionary<string, int> GetCargoStats()
    {
        return new Dictionary<string, int>
        {
            { "MarketCargos", marketCargos.Count },
            { "ActiveCargos", activeCargos.Count },
            { "CompletedCargos", completedCargos.Count },
            { "FailedCargos", failedCargos.Count },
            { "TotalCargos", TotalCargos }
        };
    }

    /// <summary>
    /// Busca una carga por ID.
    /// </summary>
    public Cargo FindCargoById(string id)
    {
        Cargo cargo = marketCargos.Find(c => c.Id == id);
        if (cargo != null) return cargo;

        cargo = activeCargos.Find(c => c.Id == id);
        if (cargo != null) return cargo;

        cargo = completedCargos.Find(c => c.Id == id);
        if (cargo != null) return cargo;

        return failedCargos.Find(c => c.Id == id);
    }

    /// <summary>
    /// Limpia todas las cargas (para reinicio del juego).
    /// </summary>
    public void ClearAllCargos()
    {
        marketCargos.Clear();
        activeCargos.Clear();
        completedCargos.Clear();
        failedCargos.Clear();

        Debug.Log("Todas las cargas limpiadas");
    }

    /// <summary>
    /// Agrega una ciudad a la lista de disponibles.
    /// </summary>
    public void AddAvailableCity(WorldCity city)
    {
        if (city != null && !availableCities.Contains(city))
        {
            availableCities.Add(city);
        }
    }

    /// <summary>
    /// Agrega un cliente a la lista de disponibles.
    /// </summary>
    public void AddAvailableClient(Client client)
    {
        if (client != null && !availableClients.Contains(client))
        {
            availableClients.Add(client);
        }
    }

    /// <summary>
    /// Agrega un agente a la lista de disponibles.
    /// </summary>
    public void AddAvailableAgent(Agent agent)
    {
        if (agent != null && !availableAgents.Contains(agent))
        {
            availableAgents.Add(agent);
        }
    }
}