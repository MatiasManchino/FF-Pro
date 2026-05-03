using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AgentManager gestiona todos los agentes de transporte del juego: creación, asignación,
/// seguimiento de rendimiento y evolución de confiabilidad.
/// </summary>
public class AgentManager : Singleton<AgentManager>
{
    [Header("Configuración de Agentes")]
    [SerializeField] private int totalAgents = Constants.TOTAL_AGENTS;
    [SerializeField] private float agentPerformanceReviewIntervalDays = 30f;

    [Header("Listas de Agentes")]
    [SerializeField] private List<Agent> allAgents = new List<Agent>();
    [SerializeField] private List<Agent> availableAgents = new List<Agent>();
    [SerializeField] private List<Agent> busyAgents = new List<Agent>();

    [Header("Personalidades Disponibles")]
    [SerializeField] private List<AgentPersonality> availablePersonalities;

    private float lastPerformanceReviewTime;

    // Eventos
    public System.Action<Agent> OnAgentAdded;
    public System.Action<Agent> OnAgentBecameAvailable;
    public System.Action<Agent> OnAgentAssigned;
    public System.Action<Agent, float> OnAgentReliabilityChanged;

    // Propiedades públicas
    public List<Agent> AllAgents => allAgents;
    public List<Agent> AvailableAgents => availableAgents;
    public List<Agent> BusyAgents => busyAgents;
    public int TotalAgents => allAgents.Count;
    public int AvailableAgentCount => availableAgents.Count;
    public int BusyAgentCount => busyAgents.Count;

    /// <summary>
    /// Inicializa el AgentManager.
    /// </summary>
    public void Initialize()
    {
        allAgents = new List<Agent>();
        availableAgents = new List<Agent>();
        busyAgents = new List<Agent>();

        lastPerformanceReviewTime = 0f;

        // Inicializar personalidades si no están asignadas
        if (availablePersonalities == null || availablePersonalities.Count == 0)
        {
            availablePersonalities = new List<AgentPersonality>();
            GenerateDefaultPersonalities();
        }

        Debug.Log("AgentManager inicializado");

        // Generar agentes iniciales
        GenerateInitialAgents();
    }

    /// <summary>
    /// Actualización por frame del AgentManager.
    /// </summary>
    private void Update()
    {
        // Revisar rendimiento de agentes periódicamente
        if (TimeManager.Instance != null &&
            TimeManager.Instance.GetTotalDays() - lastPerformanceReviewTime >= agentPerformanceReviewIntervalDays)
        {
            PerformPerformanceReview();
        }
    }

    /// <summary>
    /// Genera personalidades predeterminadas para agentes.
    /// </summary>
    private void GenerateDefaultPersonalities()
    {
        // Personalidad Confiable
        AgentPersonality reliable = AgentPersonality.CreateTestPersonality("Confiable", 0.1f);
        reliable.Description = "Agente confiable que siempre cumple pero cobra un poco más.";
        reliable.CostModifier = 0.1f;
        availablePersonalities.Add(reliable);

        // Personalidad Rápida
        AgentPersonality fast = AgentPersonality.CreateTestPersonality("Rápido", 0f);
        fast.Description = "Agente rápido pero con riesgo moderado de accidentes.";
        fast.SpeedModifier = 0.2f;
        fast.ReliabilityModifier = -0.05f;
        availablePersonalities.Add(fast);

        // Personalidad Barata
        AgentPersonality cheap = AgentPersonality.CreateTestPersonality("Económico", -0.05f);
        cheap.Description = "Agente barato pero lento y con baja confiabilidad.";
        cheap.CostModifier = -0.15f;
        cheap.SpeedModifier = -0.1f;
        availablePersonalities.Add(cheap);

        // Personalidad Estafador
        AgentPersonality shady = AgentPersonality.CreateTestPersonality("Estafador", -0.2f);
        shady.Description = "Agente barato pero muy propenso a abandonar cargas o cobrar extra.";
        shady.CostModifier = -0.2f;
        shady.MayAbandonCargo = true;
        shady.MayOvercharge = true;
        availablePersonalities.Add(shady);

        // Personalidad Perezosa
        AgentPersonality lazy = AgentPersonality.CreateTestPersonality("Perezoso", -0.1f);
        lazy.Description = "Agente lento que a veces se retrasa en las entregas.";
        lazy.SpeedModifier = -0.15f;
        lazy.MayDelayDelivery = true;
        availablePersonalities.Add(lazy);
    }

    /// <summary>
    /// Genera el pool inicial de agentes.
    /// </summary>
    private void GenerateInitialAgents()
    {
        string[] firstNames = { "Carlos", "María", "José", "Ana", "Luis", "Carmen", "Antonio", "Isabel", "Francisco", "Dolores" };
        string[] lastNames = { "Transportes", "Logistics", "Shipping", "Cargo", "Express", "Global", "Fast", "Reliable", "Speed", "Safe" };
        string[] companySuffixes = { "S.A.", "Ltd.", "Corp.", "Inc.", "LLC", "Co.", "Group", "Services" };

        Constants.TransportMode[] transportModes = {
            Constants.TransportMode.Maritime,
            Constants.TransportMode.Air,
            Constants.TransportMode.Land,
            Constants.TransportMode.Rail,
            Constants.TransportMode.Multimodal
        };

        Constants.AgentRating[] ratings = {
            Constants.AgentRating.Poor,
            Constants.AgentRating.Regular,
            Constants.AgentRating.Good,
            Constants.AgentRating.VeryGood,
            Constants.AgentRating.Excellent
        };

        for (int i = 0; i < totalAgents; i++)
        {
            // Generar nombre de compañía
            string firstName = firstNames[Random.Range(0, firstNames.Length)];
            string lastName = lastNames[Random.Range(0, lastNames.Length)];
            string suffix = companySuffixes[Random.Range(0, companySuffixes.Length)];
            string companyName = $"{firstName} {lastName} {suffix}";

            // Seleccionar especialización y rating
            Constants.TransportMode specialization = transportModes[Random.Range(0, transportModes.Length)];
            Constants.AgentRating rating = SelectWeightedRating();

            // Crear agente
            Agent newAgent = Agent.CreateTestAgent($"{firstName} {lastName}", specialization, rating);
            newAgent.CompanyName = companyName;

            // Asignar personalidad aleatoria
            if (availablePersonalities.Count > 0)
            {
                AgentPersonality personality = availablePersonalities[Random.Range(0, availablePersonalities.Count)];
                // Nota: En implementación real, clonar la personalidad
                newAgent.Personality = personality;
            }

            // Agregar a listas
            allAgents.Add(newAgent);
            availableAgents.Add(newAgent);

            OnAgentAdded?.Invoke(newAgent);
        }

        Debug.Log($"Generados {allAgents.Count} agentes iniciales");
    }

    /// <summary>
    /// Selecciona un rating de agente con pesos (más agentes regulares).
    /// </summary>
    private Constants.AgentRating SelectWeightedRating()
    {
        float roll = Random.value;
        if (roll < 0.1f) return Constants.AgentRating.Poor;
        if (roll < 0.3f) return Constants.AgentRating.Regular;
        if (roll < 0.6f) return Constants.AgentRating.Good;
        if (roll < 0.8f) return Constants.AgentRating.VeryGood;
        return Constants.AgentRating.Excellent;
    }

    /// <summary>
    /// Realiza una revisión de rendimiento de todos los agentes.
    /// </summary>
    private void PerformPerformanceReview()
    {
        lastPerformanceReviewTime = TimeManager.Instance.GetTotalDays();

        foreach (Agent agent in allAgents)
        {
            // Pequeños ajustes aleatorios a la confiabilidad basados en rendimiento
            float performanceAdjustment = Random.Range(-0.02f, 0.02f);
            float oldReliability = agent.Reliability;

            // Los agentes con buena calificación tienden a mantener/mejorar
            if (agent.Rating >= Constants.AgentRating.Good)
            {
                performanceAdjustment += 0.01f;
            }

            // Aplicar ajuste (con clamping)
            // Nota: En implementación real, modificar la propiedad privada
            // agent.reliability = Mathf.Clamp(agent.Reliability + performanceAdjustment, 0.1f, 0.95f);

            // Notificar cambio si fue significativo
            // if (Mathf.Abs(agent.Reliability - oldReliability) > 0.05f)
            // {
            //     OnAgentReliabilityChanged?.Invoke(agent, agent.Reliability);
            // }
        }

        Debug.Log("Revisión de rendimiento de agentes completada");
    }

    /// <summary>
    /// Asigna un agente a una carga.
    /// </summary>
    /// <param name="agent">Agente a asignar</param>
    /// <param name="cargo">Carga a asignar</param>
    /// <returns>True si la asignación fue exitosa</returns>
    public bool AssignAgentToCargo(Agent agent, Cargo cargo)
    {
        if (agent == null || cargo == null || !availableAgents.Contains(agent))
            return false;

        availableAgents.Remove(agent);
        busyAgents.Add(agent);

        agent.AssignCargo(cargo);

        OnAgentAssigned?.Invoke(agent);
        Debug.Log($"Agente {agent.Name} asignado a carga: {cargo.Name}");

        return true;
    }

    /// <summary>
    /// Libera un agente de una carga.
    /// </summary>
    /// <param name="agent">Agente a liberar</param>
    /// <param name="cargo">Carga completada</param>
    /// <param name="success">Si la carga fue exitosa</param>
    public void ReleaseAgentFromCargo(Agent agent, Cargo cargo, bool success)
    {
        if (agent == null || !busyAgents.Contains(agent)) return;

        busyAgents.Remove(agent);
        availableAgents.Add(agent);

        agent.ReleaseFromCargo(cargo, success);

        OnAgentBecameAvailable?.Invoke(agent);
        Debug.Log($"Agente {agent.Name} liberado de carga: {cargo.Name}");
    }

    /// <summary>
    /// Obtiene agentes disponibles para un modo de transporte específico.
    /// </summary>
    /// <param name="transportMode">Modo de transporte requerido</param>
    /// <returns>Lista de agentes disponibles</returns>
    public List<Agent> GetAvailableAgentsForTransportMode(Constants.TransportMode transportMode)
    {
        return availableAgents.FindAll(agent => agent.Specialization == transportMode);
    }

    /// <summary>
    /// Obtiene agentes ordenados por confiabilidad (más confiables primero).
    /// </summary>
    /// <returns>Lista ordenada de agentes</returns>
    public List<Agent> GetAgentsByReliability()
    {
        List<Agent> sortedAgents = new List<Agent>(availableAgents);
        sortedAgents.Sort((a, b) => b.EffectiveReliability.CompareTo(a.EffectiveReliability));
        return sortedAgents;
    }

    /// <summary>
    /// Obtiene agentes ordenados por costo (más baratos primero).
    /// </summary>
    /// <returns>Lista ordenada de agentes</returns>
    public List<Agent> GetAgentsByCost()
    {
        List<Agent> sortedAgents = new List<Agent>(availableAgents);
        sortedAgents.Sort((a, b) => a.EffectiveCost.CompareTo(b.EffectiveCost));
        return sortedAgents;
    }

    /// <summary>
    /// Busca un agente por nombre.
    /// </summary>
    /// <param name="agentName">Nombre del agente</param>
    /// <returns>Agente encontrado o null</returns>
    public Agent FindAgentByName(string agentName)
    {
        return allAgents.Find(a => a.Name == agentName);
    }

    /// <summary>
    /// Obtiene estadísticas de los agentes.
    /// </summary>
    public Dictionary<string, float> GetAgentStats()
    {
        if (allAgents.Count == 0) return new Dictionary<string, float>();

        float avgReliability = 0f;
        float avgExperience = 0f;
        Dictionary<Constants.AgentRating, int> ratingCounts = new Dictionary<Constants.AgentRating, int>();

        foreach (Agent agent in allAgents)
        {
            avgReliability += agent.EffectiveReliability;
            avgExperience += agent.ExperienceLevel;

            if (!ratingCounts.ContainsKey(agent.Rating))
                ratingCounts[agent.Rating] = 0;
            ratingCounts[agent.Rating]++;
        }

        Dictionary<string, float> stats = new Dictionary<string, float>
        {
            { "AverageReliability", avgReliability / allAgents.Count },
            { "AverageExperience", avgExperience / allAgents.Count },
            { "AvailableAgents", availableAgents.Count },
            { "BusyAgents", busyAgents.Count },
            { "TotalAgents", allAgents.Count }
        };

        // Agregar conteos por rating
        foreach (var kvp in ratingCounts)
        {
            stats.Add($"Rating{kvp.Key}", kvp.Value);
        }

        return stats;
    }

    /// <summary>
    /// Limpia todos los agentes (para reinicio del juego).
    /// </summary>
    public void ClearAllAgents()
    {
        availableAgents.Clear();
        busyAgents.Clear();
        allAgents.Clear();
        Debug.Log("Todos los agentes limpiados");
    }

    /// <summary>
    /// Agrega un agente personalizado al sistema.
    /// </summary>
    /// <param name="agent">Agente a agregar</param>
    public void AddCustomAgent(Agent agent)
    {
        if (agent != null && !allAgents.Contains(agent))
        {
            allAgents.Add(agent);
            if (agent.IsAvailable && !availableAgents.Contains(agent))
            {
                availableAgents.Add(agent);
            }
            OnAgentAdded?.Invoke(agent);
            Debug.Log($"Agente personalizado agregado: {agent.Name}");
        }
    }
}