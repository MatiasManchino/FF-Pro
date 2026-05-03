using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Representa un evento aleatorio que puede afectar cargas en tránsito.
/// Los eventos tienen diferentes tipos, impactos y duraciones.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "NewGameEvent", menuName = "FreightForwarder/GameEvent")]
public class GameEvent : ScriptableObject
{
    [Header("Información Básica")]
    [SerializeField] private string eventName;
    [SerializeField] private string description;
    [SerializeField] private Constants.EventType eventType;

    [Header("Impacto del Evento")]
    [SerializeField] private float delayDays; // Días de retraso causados
    [SerializeField] private float damageChance; // Probabilidad de daño (0-1)
    [SerializeField] private float lossChance; // Probabilidad de pérdida total (0-1)
    [SerializeField] private float costMultiplier; // Multiplicador de costo adicional

    [Header("Condiciones")]
    [SerializeField] private List<Constants.TransportMode> affectedTransportModes;
    [SerializeField] private List<Constants.CargoType> affectedCargoTypes;
    [SerializeField] private List<string> affectedRegions;

    [Header("Duración y Probabilidad")]
    [SerializeField] private int durationDays;
    [SerializeField] private float baseProbability;

    [Header("Estado")]
    [SerializeField] private bool isActive;
    [SerializeField] private int startDay;
    [SerializeField] private int endDay;

    // Propiedades públicas
    public string Name => eventName;
    public string Description { get => description; set => description = value; }
    public Constants.EventType EventType => eventType;
    public float DelayDays { get => delayDays; set => delayDays = value; }
    public float DamageChance { get => damageChance; set => damageChance = value; }
    public float LossChance { get => lossChance; set => lossChance = value; }
    public float CostMultiplier { get => costMultiplier; set => costMultiplier = value; }
    public List<Constants.TransportMode> AffectedTransportModes => affectedTransportModes;
    public List<Constants.CargoType> AffectedCargoTypes => affectedCargoTypes;
    public List<string> AffectedRegions => affectedRegions;
    public int DurationDays { get => durationDays; set => durationDays = value; }
    public float BaseProbability => baseProbability;
    public bool IsActive => isActive;
    public int StartDay => startDay;
    public int EndDay => endDay;

    // Propiedades calculadas
    public int DaysRemaining => isActive && TimeManager.Instance != null ?
        Mathf.Max(0, endDay - TimeManager.Instance.GetTotalDays()) : 0;
    public bool IsExpired => !isActive || (TimeManager.Instance != null && TimeManager.Instance.GetTotalDays() > endDay);

    /// <summary>
    /// Inicializa el evento con valores predeterminados.
    /// </summary>
    private void OnEnable()
    {
        if (affectedTransportModes == null)
            affectedTransportModes = new List<Constants.TransportMode>();
        if (affectedCargoTypes == null)
            affectedCargoTypes = new List<Constants.CargoType>();
        if (affectedRegions == null)
            affectedRegions = new List<string>();
    }

    /// <summary>
    /// Activa el evento en una carga específica.
    /// </summary>
    /// <param name="cargo">La carga afectada</param>
    public void Activate(Cargo cargo)
    {
        if (cargo == null || isActive) return;

        isActive = true;
        startDay = TimeManager.Instance != null ? TimeManager.Instance.GetTotalDays() : 0;
        endDay = startDay + durationDays;

        Debug.Log($"Evento '{eventName}' activado en carga: {cargo.Name}");

        // Aplicar efectos inmediatos
        ApplyImmediateEffects(cargo);
    }

    /// <summary>
    /// Desactiva el evento.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        Debug.Log($"Evento '{eventName}' desactivado");
    }

    /// <summary>
    /// Aplica los efectos inmediatos del evento a una carga.
    /// </summary>
    /// <param name="cargo">La carga afectada</param>
    private void ApplyImmediateEffects(Cargo cargo)
    {
        if (cargo == null) return;

        // Aplicar retraso
        if (delayDays > 0)
        {
            // Nota: En una implementación real, esto afectaría el tiempo estimado de entrega
            Debug.Log($"Evento causa {delayDays} días de retraso");
        }

        // Verificar daño o pérdida
        float damageRoll = Random.value;
        float lossRoll = Random.value;

        if (lossRoll <= lossChance)
        {
            // Pérdida total de la carga
            cargo.Fail();
            Debug.Log($"Evento causa pérdida total de la carga: {cargo.Name}");
            return;
        }
        else if (damageRoll <= damageChance)
        {
            // Daño parcial - podría implementarse como reducción de valor
            Debug.Log($"Evento causa daño parcial a la carga: {cargo.Name}");
        }

        // Aplicar costo adicional
        if (costMultiplier > 1f && EconomyManager.Instance != null)
        {
            float extraCost = cargo.TransportCost * (costMultiplier - 1f);
            EconomyManager.Instance.SpendMoney(extraCost, $"Costo adicional por evento: {eventName}");
        }
    }

    /// <summary>
    /// Verifica si este evento puede afectar a una carga específica.
    /// </summary>
    /// <param name="cargo">La carga a verificar</param>
    /// <returns>True si puede afectar</returns>
    public bool CanAffectCargo(Cargo cargo)
    {
        if (cargo == null) return false;

        // Verificar modo de transporte
        if (affectedTransportModes.Count > 0 && !affectedTransportModes.Contains(cargo.TransportMode))
            return false;

        // Verificar tipo de carga
        if (affectedCargoTypes.Count > 0 && !affectedCargoTypes.Contains(cargo.CargoType))
            return false;

        // Verificar región
        if (affectedRegions.Count > 0)
        {
            bool regionMatch = false;
            if (cargo.OriginCity != null && affectedRegions.Contains(cargo.OriginCity.Region))
                regionMatch = true;
            if (cargo.DestinationCity != null && affectedRegions.Contains(cargo.DestinationCity.Region))
                regionMatch = true;

            if (!regionMatch) return false;
        }

        return true;
    }

    /// <summary>
    /// Calcula la probabilidad efectiva de que ocurra este evento.
    /// </summary>
    /// <param name="cargo">La carga (opcional, para modificadores específicos)</param>
    /// <returns>Probabilidad efectiva (0-1)</returns>
    public float GetEffectiveProbability(Cargo cargo = null)
    {
        float probability = baseProbability;

        // Modificadores por carga específica
        if (cargo != null)
        {
            // Cargas valiosas tienen más riesgo
            if (cargo.CargoType == Constants.CargoType.Valuable)
                probability *= 1.2f;

            // Cargas peligrosas tienen más riesgo
            if (cargo.CargoType == Constants.CargoType.Dangerous)
                probability *= 1.3f;

            // Ciudades con alto riesgo multiplican la probabilidad
            if (cargo.OriginCity != null)
                probability *= cargo.OriginCity.RiskMultiplier;
            if (cargo.DestinationCity != null)
                probability *= cargo.DestinationCity.RiskMultiplier;
        }

        return Mathf.Clamp(probability, 0f, 1f);
    }

    /// <summary>
    /// Actualiza el estado del evento (llamado cada día).
    /// </summary>
    public void UpdateEvent()
    {
        if (!isActive) return;

        if (IsExpired)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Obtiene una descripción detallada del evento.
    /// </summary>
    /// <returns>Descripción formateada</returns>
    public string GetDescription()
    {
        string desc = $"{eventName}\n";
        desc += $"{description}\n\n";
        desc += $"Tipo: {eventType}\n";
        desc += $"Duración: {durationDays} días\n";
        desc += $"Probabilidad base: {baseProbability:0.##}\n";

        if (delayDays > 0)
            desc += $"Retraso causado: {delayDays} días\n";
        if (damageChance > 0)
            desc += $"Chance de daño: {damageChance:0.##}\n";
        if (lossChance > 0)
            desc += $"Chance de pérdida: {lossChance:0.##}\n";
        if (costMultiplier > 1f)
            desc += $"Multiplicador de costo: {costMultiplier:0.##}x\n";

        if (affectedTransportModes.Count > 0)
        {
            desc += "Modos de transporte afectados: ";
            foreach (var mode in affectedTransportModes)
                desc += $"{mode}, ";
            desc = desc.TrimEnd(',', ' ') + "\n";
        }

        if (affectedCargoTypes.Count > 0)
        {
            desc += "Tipos de carga afectados: ";
            foreach (var type in affectedCargoTypes)
                desc += $"{type}, ";
            desc = desc.TrimEnd(',', ' ') + "\n";
        }

        if (affectedRegions.Count > 0)
        {
            desc += "Regiones afectadas: ";
            foreach (var region in affectedRegions)
                desc += $"{region}, ";
            desc = desc.TrimEnd(',', ' ') + "\n";
        }

        if (isActive)
        {
            desc += $"Activo: Sí (días restantes: {DaysRemaining})\n";
        }
        else
        {
            desc += "Activo: No\n";
        }

        return desc;
    }

    /// <summary>
    /// Crea un evento de prueba con valores predeterminados.
    /// </summary>
    public static GameEvent CreateTestEvent(string name, Constants.EventType type, float probability)
    {
        GameEvent gameEvent = CreateInstance<GameEvent>();
        gameEvent.eventName = name;
        gameEvent.description = "Evento de prueba";
        gameEvent.eventType = type;
        gameEvent.delayDays = 1f;
        gameEvent.damageChance = 0.1f;
        gameEvent.lossChance = 0.05f;
        gameEvent.costMultiplier = 1.2f;
        gameEvent.affectedTransportModes = new List<Constants.TransportMode>();
        gameEvent.affectedCargoTypes = new List<Constants.CargoType>();
        gameEvent.affectedRegions = new List<string>();
        gameEvent.durationDays = 3;
        gameEvent.baseProbability = probability;
        gameEvent.isActive = false;
        gameEvent.startDay = 0;
        gameEvent.endDay = 0;

        return gameEvent;
    }
}