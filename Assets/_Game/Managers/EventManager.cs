using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EventManager gestiona los eventos aleatorios que afectan las cargas en tránsito.
/// Controla la generación, aplicación y resolución de eventos contextuales.
/// </summary>
public class EventManager : Singleton<EventManager>
{
    [Header("Configuración de Eventos")]
    [SerializeField] private float baseEventProbability = Constants.EVENT_PROBABILITY_BASE;
    [SerializeField] private int maxEventsPerDay = Constants.EVENTS_PER_DAY_MAX;
    [SerializeField] private float eventImpactDuration = Constants.EVENT_IMPACT_DURATION_DAYS;

    [Header("Eventos Disponibles")]
    [SerializeField] private List<GameEvent> availableEvents = new List<GameEvent>();

    [Header("Estado de Eventos")]
    [SerializeField] private List<GameEvent> activeEvents = new List<GameEvent>();
    [SerializeField] private Dictionary<Cargo, List<GameEvent>> cargoActiveEvents = new Dictionary<Cargo, List<GameEvent>>();

    private int lastEventCheckDay = 0;

    // Eventos
    public System.Action<GameEvent, Cargo> OnEventTriggered;
    public System.Action<GameEvent> OnEventExpired;

    // Propiedades públicas
    public List<GameEvent> AvailableEvents => availableEvents;
    public List<GameEvent> ActiveEvents => activeEvents;
    public int ActiveEventCount => activeEvents.Count;
    public float EventImpactDuration => eventImpactDuration;

    /// <summary>
    /// Inicializa el EventManager.
    /// </summary>
    public void Initialize()
    {
        activeEvents = new List<GameEvent>();
        cargoActiveEvents = new Dictionary<Cargo, List<GameEvent>>();

        // Generar eventos predeterminados si no están asignados
        if (availableEvents == null || availableEvents.Count == 0)
        {
            GenerateDefaultEvents();
        }

        // Ajustar duración de evento por configuración global si no se especifica
        foreach (GameEvent gameEvent in availableEvents)
        {
            if (gameEvent.DurationDays <= 0)
            {
                gameEvent.DurationDays = Mathf.RoundToInt(eventImpactDuration);
            }
        }

        Debug.Log("EventManager inicializado");
    }

    /// <summary>
    /// Actualización por frame del EventManager.
    /// </summary>
    private void Update()
    {
        // Verificar generación de nuevos eventos diariamente
        if (TimeManager.Instance != null &&
            TimeManager.Instance.GetTotalDays() > lastEventCheckDay)
        {
            CheckForNewEvents();
            lastEventCheckDay = TimeManager.Instance.GetTotalDays();
        }

        // Actualizar eventos activos
        UpdateActiveEvents();
    }

    /// <summary>
    /// Genera los eventos predeterminados del juego.
    /// </summary>
    private void GenerateDefaultEvents()
    {
        availableEvents = new List<GameEvent>();

        // Tormenta
        GameEvent storm = GameEvent.CreateTestEvent("Tormenta", Constants.EventType.Storm, 0.05f);
        storm.DelayDays = 2f;
        storm.DamageChance = 0.1f;
        storm.AffectedTransportModes.Add(Constants.TransportMode.Maritime);
        storm.AffectedTransportModes.Add(Constants.TransportMode.Air);
        storm.Description = "Tormenta severa retrasa el transporte y puede causar daños.";
        availableEvents.Add(storm);

        // Huelga
        GameEvent strike = GameEvent.CreateTestEvent("Huelga", Constants.EventType.Strike, 0.03f);
        strike.DelayDays = 3f;
        strike.AffectedTransportModes.Add(Constants.TransportMode.Land);
        strike.AffectedTransportModes.Add(Constants.TransportMode.Rail);
        strike.Description = "Huelga laboral paraliza el transporte terrestre.";
        availableEvents.Add(strike);

        // Inspección Aduanera
        GameEvent customsDelay = GameEvent.CreateTestEvent("Inspección Aduanera", Constants.EventType.CustomsDelay, 0.04f);
        customsDelay.DelayDays = 1f;
        customsDelay.CostMultiplier = 1.5f;
        customsDelay.AffectedCargoTypes.Add(Constants.CargoType.Dangerous);
        customsDelay.Description = "Inspección aduanera retrasa la carga y aumenta costos.";
        availableEvents.Add(customsDelay);

        // Falla de Equipo
        GameEvent equipmentFailure = GameEvent.CreateTestEvent("Falla de Equipo", Constants.EventType.EquipmentFailure, 0.02f);
        equipmentFailure.DelayDays = 1f;
        equipmentFailure.DamageChance = 0.15f;
        equipmentFailure.Description = "Falla mecánica en el medio de transporte.";
        availableEvents.Add(equipmentFailure);

        // Robo
        GameEvent theft = GameEvent.CreateTestEvent("Robo", Constants.EventType.Theft, 0.01f);
        theft.LossChance = 0.05f;
        theft.AffectedCargoTypes.Add(Constants.CargoType.Valuable);
        theft.Description = "Intento de robo durante el transporte.";
        availableEvents.Add(theft);

        // Accidente
        GameEvent accident = GameEvent.CreateTestEvent("Accidente", Constants.EventType.Accident, 0.025f);
        accident.DelayDays = 2f;
        accident.DamageChance = 0.2f;
        accident.AffectedTransportModes.Add(Constants.TransportMode.Land);
        accident.Description = "Accidente de tránsito retrasa la entrega.";
        availableEvents.Add(accident);

        // Escasez de Combustible
        GameEvent fuelShortage = GameEvent.CreateTestEvent("Escasez de Combustible", Constants.EventType.FuelShortage, 0.02f);
        fuelShortage.DelayDays = 1f;
        fuelShortage.CostMultiplier = 1.3f;
        fuelShortage.AffectedTransportModes.Add(Constants.TransportMode.Air);
        fuelShortage.AffectedTransportModes.Add(Constants.TransportMode.Land);
        fuelShortage.Description = "Escasez de combustible aumenta costos y retrasa.";
        availableEvents.Add(fuelShortage);

        // Inestabilidad Política
        GameEvent politicalInstability = GameEvent.CreateTestEvent("Inestabilidad Política", Constants.EventType.PoliticalInstability, 0.015f);
        politicalInstability.DelayDays = 3f;
        politicalInstability.AffectedRegions.Add("Middle East");
        politicalInstability.AffectedRegions.Add("Africa");
        politicalInstability.Description = "Conflictos políticos afectan rutas específicas.";
        availableEvents.Add(politicalInstability);

        // Desastre Natural
        GameEvent naturalDisaster = GameEvent.CreateTestEvent("Desastre Natural", Constants.EventType.NaturalDisaster, 0.005f);
        naturalDisaster.DelayDays = 5f;
        naturalDisaster.DamageChance = 0.3f;
        naturalDisaster.LossChance = 0.1f;
        naturalDisaster.Description = "Terremoto, inundación u otro desastre natural.";
        availableEvents.Add(naturalDisaster);

        // Caída del Mercado
        GameEvent marketCrash = GameEvent.CreateTestEvent("Caída del Mercado", Constants.EventType.MarketCrash, 0.01f);
        marketCrash.CostMultiplier = 1.4f;
        marketCrash.Description = "Volatilidad económica aumenta costos de transporte.";
        availableEvents.Add(marketCrash);

        Debug.Log($"Generados {availableEvents.Count} eventos predeterminados");
    }

    /// <summary>
    /// Verifica si deben generarse nuevos eventos aleatorios.
    /// </summary>
    private void CheckForNewEvents()
    {
        if (CargoManager.Instance == null) return;

        // Obtener cargas activas
        List<Cargo> activeCargos = CargoManager.Instance.ActiveCargos;
        if (activeCargos.Count == 0) return;

        // Número de eventos a generar hoy
        int eventsToday = Mathf.Min(maxEventsPerDay, Mathf.CeilToInt(activeCargos.Count * baseEventProbability));

        for (int i = 0; i < eventsToday; i++)
        {
            // Seleccionar carga aleatoria
            Cargo randomCargo = activeCargos[Random.Range(0, activeCargos.Count)];

            // Intentar asignar evento aleatorio
            TryAssignRandomEvent(randomCargo);
        }
    }

    /// <summary>
    /// Intenta asignar un evento aleatorio a una carga.
    /// </summary>
    /// <param name="cargo">Carga objetivo</param>
    private void TryAssignRandomEvent(Cargo cargo)
    {
        if (cargo == null) return;

        // Filtrar eventos que pueden afectar esta carga
        List<GameEvent> applicableEvents = availableEvents.FindAll(e => e.CanAffectCargo(cargo));

        if (applicableEvents.Count == 0) return;

        // Seleccionar evento basado en probabilidad
        List<GameEvent> weightedEvents = new List<GameEvent>();
        foreach (GameEvent gameEvent in applicableEvents)
        {
            float probability = gameEvent.GetEffectiveProbability(cargo);
            int weight = Mathf.CeilToInt(probability * 100f); // Convertir a peso entero

            for (int j = 0; j < weight; j++)
            {
                weightedEvents.Add(gameEvent);
            }
        }

        if (weightedEvents.Count == 0) return;

        // Seleccionar evento
        GameEvent selectedEvent = weightedEvents[Random.Range(0, weightedEvents.Count)];

        // Activar evento
        AssignEventToCargo(selectedEvent, cargo);
    }

    /// <summary>
    /// Asigna un evento específico a una carga.
    /// </summary>
    /// <param name="gameEvent">Evento a asignar</param>
    /// <param name="cargo">Carga objetivo</param>
    public void AssignEventToCargo(GameEvent gameEvent, Cargo cargo)
    {
        if (gameEvent == null || cargo == null) return;

        // Activar evento
        gameEvent.Activate(cargo);

        // Agregar a listas de seguimiento
        activeEvents.Add(gameEvent);

        if (!cargoActiveEvents.ContainsKey(cargo))
        {
            cargoActiveEvents[cargo] = new List<GameEvent>();
        }
        cargoActiveEvents[cargo].Add(gameEvent);

        // Asignar evento a la carga
        cargo.SetActiveEvent(gameEvent);

        OnEventTriggered?.Invoke(gameEvent, cargo);
        Debug.Log($"Evento '{gameEvent.Name}' asignado a carga: {cargo.Name}");
    }

    /// <summary>
    /// Actualiza el estado de todos los eventos activos.
    /// </summary>
    private void UpdateActiveEvents()
    {
        List<GameEvent> expiredEvents = new List<GameEvent>();

        foreach (GameEvent gameEvent in activeEvents)
        {
            gameEvent.UpdateEvent();

            if (gameEvent.IsExpired)
            {
                expiredEvents.Add(gameEvent);
            }
        }

        // Remover eventos expirados
        foreach (GameEvent expiredEvent in expiredEvents)
        {
            activeEvents.Remove(expiredEvent);
            OnEventExpired?.Invoke(expiredEvent);
            Debug.Log($"Evento expirado: {expiredEvent.Name}");
        }
    }

    /// <summary>
    /// Obtiene todos los eventos activos de una carga específica.
    /// </summary>
    /// <param name="cargo">Carga a consultar</param>
    /// <returns>Lista de eventos activos</returns>
    public List<GameEvent> GetActiveEventsForCargo(Cargo cargo)
    {
        if (cargoActiveEvents.ContainsKey(cargo))
        {
            return new List<GameEvent>(cargoActiveEvents[cargo]);
        }
        return new List<GameEvent>();
    }

    /// <summary>
    /// Fuerza la resolución de un evento (para testing o administración).
    /// </summary>
    /// <param name="gameEvent">Evento a resolver</param>
    public void ForceResolveEvent(GameEvent gameEvent)
    {
        if (gameEvent != null && activeEvents.Contains(gameEvent))
        {
            gameEvent.Deactivate();
            activeEvents.Remove(gameEvent);
            OnEventExpired?.Invoke(gameEvent);
            Debug.Log($"Evento forzado a expirar: {gameEvent.Name}");
        }
    }

    /// <summary>
    /// Obtiene estadísticas de eventos.
    /// </summary>
    public Dictionary<string, int> GetEventStats()
    {
        Dictionary<Constants.EventType, int> eventTypeCounts = new Dictionary<Constants.EventType, int>();

        foreach (GameEvent gameEvent in availableEvents)
        {
            if (!eventTypeCounts.ContainsKey(gameEvent.EventType))
                eventTypeCounts[gameEvent.EventType] = 0;
            eventTypeCounts[gameEvent.EventType]++;
        }

        Dictionary<string, int> stats = new Dictionary<string, int>
        {
            { "TotalAvailableEvents", availableEvents.Count },
            { "ActiveEvents", activeEvents.Count }
        };

        // Agregar conteos por tipo
        foreach (var kvp in eventTypeCounts)
        {
            stats.Add($"Type{kvp.Key}", kvp.Value);
        }

        return stats;
    }

    /// <summary>
    /// Busca un evento por nombre.
    /// </summary>
    /// <param name="eventName">Nombre del evento</param>
    /// <returns>Evento encontrado o null</returns>
    public GameEvent FindEventByName(string eventName)
    {
        return availableEvents.Find(e => e.Name == eventName);
    }

    /// <summary>
    /// Limpia todos los eventos activos (para reinicio del juego).
    /// </summary>
    public void ClearActiveEvents()
    {
        foreach (GameEvent gameEvent in activeEvents)
        {
            gameEvent.Deactivate();
        }

        activeEvents.Clear();
        cargoActiveEvents.Clear();
        Debug.Log("Eventos activos limpiados");
    }

    /// <summary>
    /// Agrega un evento personalizado al sistema.
    /// </summary>
    /// <param name="gameEvent">Evento a agregar</param>
    public void AddCustomEvent(GameEvent gameEvent)
    {
        if (gameEvent != null && !availableEvents.Contains(gameEvent))
        {
            availableEvents.Add(gameEvent);
            Debug.Log($"Evento personalizado agregado: {gameEvent.Name}");
        }
    }
}