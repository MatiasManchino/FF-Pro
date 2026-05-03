using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Representa un agente de transporte en el juego.
/// Los agentes tienen personalidades únicas que afectan su comportamiento y confiabilidad.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "NewAgent", menuName = "FreightForwarder/Agent")]
public class Agent : ScriptableObject
{
    [Header("Información Básica")]
    [SerializeField] private string agentName;
    [SerializeField] private string companyName;
    [SerializeField] private Constants.TransportMode specialization;

    [Header("Personalidad y Comportamiento")]
    [SerializeField] private AgentPersonality personality;
    [SerializeField] private Constants.AgentRating rating;

    [Header("Estadísticas")]
    [SerializeField] private float reliability; // 0-1, cuánto se puede confiar
    [SerializeField] private float speedBonus; // Multiplicador de velocidad
    [SerializeField] private float costMultiplier; // Multiplicador de costo
    [SerializeField] private int experienceLevel;

    [Header("Estado")]
    [SerializeField] private bool isAvailable = true;
    [SerializeField] private int lastUsedDay;
    [SerializeField] private List<string> completedRoutes;

    // Propiedades públicas
    public string Name => agentName;
    public string CompanyName { get => companyName; set => companyName = value; }
    public Constants.TransportMode Specialization => specialization;
    public AgentPersonality Personality { get => personality; set => personality = value; }
    public Constants.AgentRating Rating => rating;
    public float Reliability => reliability;
    public float SpeedBonus => speedBonus;
    public float CostMultiplier => costMultiplier;
    public int ExperienceLevel => experienceLevel;
    public bool IsAvailable => isAvailable;
    public int LastUsedDay => lastUsedDay;
    public List<string> CompletedRoutes => completedRoutes;

    // Propiedades calculadas
    public float EffectiveReliability => Mathf.Clamp(reliability + (experienceLevel * 0.05f), 0f, 1f);
    public float EffectiveSpeed => speedBonus * (1f + (experienceLevel * 0.1f));
    public float EffectiveCost => costMultiplier * (1f - (experienceLevel * 0.02f));

    /// <summary>
    /// Inicializa el agente con valores predeterminados.
    /// </summary>
    private void OnEnable()
    {
        if (completedRoutes == null)
        {
            completedRoutes = new List<string>();
        }
    }

    /// <summary>
    /// Asigna una carga al agente y lo marca como ocupado.
    /// </summary>
    /// <param name="cargo">La carga a asignar</param>
    public void AssignCargo(Cargo cargo)
    {
        if (!isAvailable) return;

        isAvailable = false;
        lastUsedDay = TimeManager.Instance != null ? TimeManager.Instance.GetTotalDays() : 0;

        Debug.Log($"Agente {agentName} asignado a carga: {cargo.Name}");
    }

    /// <summary>
    /// Libera al agente después de completar una carga.
    /// </summary>
    /// <param name="cargo">La carga completada</param>
    /// <param name="success">Si la carga fue exitosa</param>
    public void ReleaseFromCargo(Cargo cargo, bool success)
    {
        isAvailable = true;

        if (success)
        {
            // Registrar ruta completada
            string routeKey = $"{cargo.OriginCity.Name}-{cargo.DestinationCity.Name}";
            if (!completedRoutes.Contains(routeKey))
            {
                completedRoutes.Add(routeKey);
            }

            // Ganar experiencia
            experienceLevel++;
            Debug.Log($"Agente {agentName} completó carga exitosamente. Experiencia: {experienceLevel}");
        }
        else
        {
            // Pérdida de confiabilidad por fallo
            reliability = Mathf.Max(0.1f, reliability - 0.1f);
            Debug.Log($"Agente {agentName} falló en carga. Confiabilidad reducida: {reliability}");
        }
    }

    /// <summary>
    /// Calcula la probabilidad de éxito para una carga específica.
    /// </summary>
    /// <param name="cargo">La carga a evaluar</param>
    /// <returns>Probabilidad de éxito (0-1)</returns>
    public float CalculateSuccessProbability(Cargo cargo)
    {
        if (cargo == null) return 0f;

        float baseProbability = EffectiveReliability;

        // Modificadores por tipo de carga
        float cargoModifier = cargo.CargoType switch
        {
            Constants.CargoType.Urgent => -0.1f, // Más riesgoso
            Constants.CargoType.Dangerous => -0.15f, // Muy riesgoso
            Constants.CargoType.Refrigerated => -0.05f, // Algo riesgoso
            _ => 0f
        };

        // Modificadores por distancia
        float distanceModifier = cargo.Distance > 10000 ? -0.1f : 0f; // Rutas muy largas

        // Modificadores por especialización
        float specializationModifier = cargo.TransportMode == specialization ? 0.1f : 0f;

        // Modificadores por experiencia en ruta
        string routeKey = $"{cargo.OriginCity.Name}-{cargo.DestinationCity.Name}";
        float experienceModifier = completedRoutes.Contains(routeKey) ? 0.05f : 0f;

        float finalProbability = baseProbability + cargoModifier + distanceModifier +
                                specializationModifier + experienceModifier;

        return Mathf.Clamp(finalProbability, 0.05f, 0.95f);
    }

    /// <summary>
    /// Simula el comportamiento del agente durante el transporte.
    /// </summary>
    /// <param name="cargo">La carga en tránsito</param>
    /// <returns>True si el transporte fue exitoso</returns>
    public bool SimulateTransport(Cargo cargo)
    {
        float successProbability = CalculateSuccessProbability(cargo);

        // Aplicar modificadores de personalidad
        if (personality != null)
        {
            successProbability = personality.ModifySuccessProbability(successProbability, cargo);
        }

        float roll = Random.value;
        bool success = roll <= successProbability;

        Debug.Log($"Agente {agentName} - Probabilidad: {successProbability:0.##}, Resultado: {(success ? "ÉXITO" : "FALLO")}");

        return success;
    }

    /// <summary>
    /// Calcula el costo de usar este agente para una carga.
    /// </summary>
    /// <param name="cargo">La carga</param>
    /// <returns>Costo calculado</returns>
    public float CalculateCost(Cargo cargo)
    {
        if (cargo == null) return 0f;

        float baseCost = cargo.TransportCost;
        float agentCost = baseCost * EffectiveCost;

        // Costo adicional por especialización
        if (cargo.TransportMode != specialization)
        {
            agentCost *= 1.2f; // 20% extra por no especialización
        }

        return agentCost;
    }

    /// <summary>
    /// Obtiene una descripción detallada del agente.
    /// </summary>
    /// <returns>Descripción formateada</returns>
    public string GetDescription()
    {
        string desc = $"{agentName}\n";
        desc += $"Compañía: {companyName}\n";
        desc += $"Especialización: {specialization}\n";
        desc += $"Calificación: {rating}\n";
        desc += $"Confiabilidad: {EffectiveReliability:0.##}\n";
        desc += $"Bonus de velocidad: {EffectiveSpeed:0.##}x\n";
        desc += $"Multiplicador de costo: {EffectiveCost:0.##}x\n";
        desc += $"Nivel de experiencia: {experienceLevel}\n";

        if (personality != null)
        {
            desc += $"Personalidad: {personality.Name}\n";
        }

        desc += $"Disponible: {(isAvailable ? "Sí" : "No")}\n";

        return desc;
    }

    /// <summary>
    /// Crea un agente de prueba con valores predeterminados.
    /// </summary>
    public static Agent CreateTestAgent(string name, Constants.TransportMode spec, Constants.AgentRating rating)
    {
        Agent agent = CreateInstance<Agent>();
        agent.agentName = name;
        agent.companyName = "Test Company";
        agent.specialization = spec;
        agent.rating = rating;
        agent.reliability = rating switch
        {
            Constants.AgentRating.Poor => 0.6f,
            Constants.AgentRating.Regular => 0.75f,
            Constants.AgentRating.Good => 0.85f,
            Constants.AgentRating.VeryGood => 0.92f,
            Constants.AgentRating.Excellent => 0.98f,
            _ => 0.75f
        };
        agent.speedBonus = 1f;
        agent.costMultiplier = Constants.AGENT_COST_MULTIPLIER;
        agent.experienceLevel = 0;
        agent.isAvailable = true;
        agent.lastUsedDay = 0;
        agent.completedRoutes = new List<string>();

        return agent;
    }
}

/// <summary>
/// Define la personalidad de un agente, que afecta su comportamiento.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "NewAgentPersonality", menuName = "FreightForwarder/AgentPersonality")]
public class AgentPersonality : ScriptableObject
{
    [SerializeField] private string personalityName;
    [SerializeField] private string description;

    [Header("Modificadores de Comportamiento")]
    [SerializeField] private float reliabilityModifier;
    [SerializeField] private float speedModifier;
    [SerializeField] private float costModifier;

    [Header("Comportamientos Especiales")]
    [SerializeField] private bool mayAbandonCargo;
    [SerializeField] private bool mayOvercharge;
    [SerializeField] private bool mayDelayDelivery;

    public string Name => personalityName;
    public string Description { get => description; set => description = value; }
    public float ReliabilityModifier { get => reliabilityModifier; set => reliabilityModifier = value; }
    public float SpeedModifier { get => speedModifier; set => speedModifier = value; }
    public float CostModifier { get => costModifier; set => costModifier = value; }
    public bool MayAbandonCargo { get => mayAbandonCargo; set => mayAbandonCargo = value; }
    public bool MayOvercharge { get => mayOvercharge; set => mayOvercharge = value; }
    public bool MayDelayDelivery { get => mayDelayDelivery; set => mayDelayDelivery = value; }

    /// <summary>
    /// Modifica la probabilidad de éxito basada en la personalidad.
    /// </summary>
    public float ModifySuccessProbability(float baseProbability, Cargo cargo)
    {
        float modified = baseProbability + reliabilityModifier;

        // Comportamientos especiales aleatorios
        if (mayAbandonCargo && Random.value < 0.05f) // 5% chance
        {
            modified -= 0.2f;
            Debug.Log($"Agente {personalityName} consideró abandonar la carga");
        }

        return Mathf.Clamp(modified, 0.05f, 0.95f);
    }

    /// <summary>
    /// Crea una personalidad de prueba.
    /// </summary>
    public static AgentPersonality CreateTestPersonality(string name, float reliabilityMod)
    {
        AgentPersonality personality = CreateInstance<AgentPersonality>();
        personality.personalityName = name;
        personality.description = "Personalidad de prueba";
        personality.reliabilityModifier = reliabilityMod;
        personality.speedModifier = 0f;
        personality.costModifier = 0f;
        personality.mayAbandonCargo = false;
        personality.mayOvercharge = false;
        personality.mayDelayDelivery = false;

        return personality;
    }
}