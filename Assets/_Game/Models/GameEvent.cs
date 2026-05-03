using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    /// <summary>
    /// GameEvent.cs — Modelo de evento aleatorio con condiciones contextuales.
    /// 
    /// DIFERENCIA CLAVE CON EL DISEÑO ANTERIOR:
    /// - Antes: evento aleatorio puro (5% chance siempre)
    /// - Ahora: evento que se evalúa según condiciones (ubicación, fecha, modo, etapa)
    /// 
    /// Esto permite que:
    /// - Una huelga en Brasil solo afecte cargas que pasan por Brasil
    /// - Una tormenta solo afecte cargas en el mar
    /// - El Día del Trabajador solo afecte el 1ro de mayo
    /// </summary>
    [Serializable]
    public class GameEvent
    {
        // =========================================================================
        // IDENTIFICACIÓN
        // =========================================================================
        
        public string Id { get; set; }                      // "customs_delay_002"
        public string Name { get; set; }                    // "Inspección Aduanera"
        public string Description { get; set; }             // Descripción del evento
        
        // =========================================================================
        // TIPO Y SEVERIDAD
        // =========================================================================
        
        public Constants.EventType Type { get; set; }
        public int Severity { get; set; }                   // 1 = leve, 5 = catastrófico
        
        // =========================================================================
        // CONDICIONES PARA QUE OCURRA (TODAS deben cumplirse)
        // =========================================================================
        
        /// <summary>
        /// Modos de transporte afectados (null = todos)
        /// </summary>
        public List<Constants.TransportMode> AffectedTransportModes { get; set; }
        
        /// <summary>
        /// Etapas del viaje afectadas (null = todas)
        /// Valores posibles: "origin", "transit", "destination"
        /// </summary>
        public List<string> AffectedStages { get; set; }
        
        /// <summary>
        /// Países donde puede ocurrir (null = todos)
        /// </summary>
        public List<string> AffectedCountries { get; set; }
        
        /// <summary>
        /// Ciudades específicas donde puede ocurrir (null = todas)
        /// </summary>
        public List<string> AffectedCities { get; set; }
        
        /// <summary>
        /// Tipos de carga afectados (null = todos)
        /// </summary>
        public List<Constants.CargoType> AffectedCargoTypes { get; set; }
        
        /// <summary>
        /// Meses en que puede ocurrir (1-12, null = todos)
        /// </summary>
        public List<int> AffectedMonths { get; set; }
        
        /// <summary>
        /// Días específicos del mes (ej: 1 para primer día del mes, null = todos)
        /// </summary>
        public List<int> AffectedDays { get; set; }
        
        /// <summary>
        /// Umbral de confianza del agente (si confianza < esto, más probable)
        /// </summary>
        public int? AgentTrustThreshold { get; set; }
        
        /// <summary>
        /// Probabilidad base (0-1) cuando TODAS las condiciones se cumplen
        /// </summary>
        public float BaseProbability { get; set; }
        
        // =========================================================================
        // EFECTOS DEL EVENTO
        // =========================================================================
        
        public int DaysExtra { get; set; }                  // Días de retraso
        public int MoneyCost { get; set; }                  // Costo económico directo
        public int ReputationLoss { get; set; }             // Pérdida de reputación si se maneja mal
        
        public bool RequiresChoice { get; set; }            // ¿El jugador debe elegir?
        public List<EventOption> Options { get; set; }      // Opciones de respuesta
        
        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        
        public GameEvent()
        {
            Id = Guid.NewGuid().ToString();
            AffectedTransportModes = new List<Constants.TransportMode>();
            AffectedStages = new List<string>();
            AffectedCountries = new List<string>();
            AffectedCities = new List<string>();
            AffectedCargoTypes = new List<Constants.CargoType>();
            AffectedMonths = new List<int>();
            AffectedDays = new List<int>();
            Options = new List<EventOption>();
        }
        
        // =========================================================================
        // MÉTODO PARA VERIFICAR SI EL EVENTO APLICA A UNA CARGA
        // =========================================================================
        
        /// <summary>
        /// Verifica si este evento puede ocurrirle a una carga específica.
        /// </summary>
        /// <param name="cargo">La carga en tránsito</param>
        /// <param name="currentStage">Etapa actual ("origin", "transit", "destination")</param>
        /// <param name="currentMonth">Mes actual (1-12)</param>
        /// <param name="currentDay">Día del mes actual</param>
        /// <param name="agentTrust">Confianza del agente (0-100)</param>
        /// <param name="cityCountryMap">Diccionario para obtener país desde ID de ciudad</param>
        /// <returns>True si el evento puede ocurrir, False si no aplica</returns>
        public bool AppliesToCargo(Cargo cargo, string currentStage, 
                                   int currentMonth, int currentDay, 
                                   float agentTrust,
                                   Dictionary<string, string> cityCountryMap = null)
        {
            // 1. Verificar modo de transporte
            if (AffectedTransportModes != null && AffectedTransportModes.Count > 0)
            {
                if (!AffectedTransportModes.Contains(cargo.TransportMode))
                    return false;
            }
            
            // 2. Verificar etapa del viaje
            if (AffectedStages != null && AffectedStages.Count > 0)
            {
                if (!AffectedStages.Contains(currentStage))
                    return false;
            }
            
            // 3. Verificar ubicación (según etapa)
            string locationId = GetLocationId(cargo, currentStage);
            if (!string.IsNullOrEmpty(locationId))
            {
                // Verificar ciudades específicas
                if (AffectedCities != null && AffectedCities.Count > 0)
                {
                    if (!AffectedCities.Contains(locationId))
                        return false;
                }
                
                // Verificar países
                if (AffectedCountries != null && AffectedCountries.Count > 0 && cityCountryMap != null)
                {
                    if (cityCountryMap.ContainsKey(locationId))
                    {
                        string country = cityCountryMap[locationId];
                        if (!AffectedCountries.Contains(country))
                            return false;
                    }
                }
            }
            
            // 4. Verificar tipo de carga
            if (AffectedCargoTypes != null && AffectedCargoTypes.Count > 0)
            {
                if (!AffectedCargoTypes.Contains(cargo.CargoType))
                    return false;
            }
            
            // 5. Verificar mes
            if (AffectedMonths != null && AffectedMonths.Count > 0)
            {
                if (!AffectedMonths.Contains(currentMonth))
                    return false;
            }
            
            // 6. Verificar día específico
            if (AffectedDays != null && AffectedDays.Count > 0)
            {
                if (!AffectedDays.Contains(currentDay))
                    return false;
            }
            
            // 7. Verificar confianza del agente (si aplica)
            if (AgentTrustThreshold.HasValue)
            {
                if (agentTrust >= AgentTrustThreshold.Value)
                    return false;   // Agente confiable -> evento menos probable
            }
            
            return true;
        }
        
        /// <summary>
        /// Obtiene el ID de ubicación según la etapa actual.
        /// </summary>
        private string GetLocationId(Cargo cargo, string stage)
        {
            switch (stage)
            {
                case "origin":
                    return cargo.OriginCityId;
                case "destination":
                    return cargo.DestinationCityId;
                case "transit":
                    // En tránsito, puede ser cualquier ciudad en la ruta
                    // Esto se manejará más adelante con el sistema de rutas
                    return null;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Calcula la probabilidad final considerando modificadores adicionales.
        /// </summary>
        public float GetFinalProbability(Cargo cargo, float agentTrust)
        {
            float probability = BaseProbability;
            
            // Cargas peligrosas: +3%
            if (cargo.CargoType == Constants.CargoType.Dangerous)
                probability += 0.03f;
            
            // Cargas valiosas: +2% (más riesgo de robo)
            if (cargo.CargoType == Constants.CargoType.Valuable)
                probability += 0.02f;
            
            // Agentes con baja confianza: +2% adicional
            if (agentTrust < 40)
                probability += 0.02f;
            
            // Severidad afecta la probabilidad (eventos más graves son menos comunes)
            probability /= Severity;
            
            return Mathf.Clamp(probability, 0.01f, 0.30f);
        }
    }
    
    /// <summary>
    /// EventOption.cs — Opción de respuesta para un evento que requiere decisión.
    /// </summary>
    [Serializable]
    public class EventOption
    {
        public string Text { get; set; }                    // "Pagar multa ($500)"
        public int Cost { get; set; }                       // Costo en dinero
        public int DaysExtra { get; set; }                  // Días adicionales de retraso
        public int ReputationImpact { get; set; }           // Cambio en reputación
        public float SuccessChance { get; set; } = 1.0f;    // Probabilidad de éxito (0-1)
        public string RequiredFeature { get; set; }         // Feature necesario (ej: "insurance")
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public EventOption()
        {
            Text = string.Empty;
            SuccessChance = 1.0f;
            RequiredFeature = string.Empty;
        }
        
        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        public EventOption(string text, int cost, int daysExtra, int reputationImpact)
        {
            Text = text;
            Cost = cost;
            DaysExtra = daysExtra;
            ReputationImpact = reputationImpact;
            SuccessChance = 1.0f;
            RequiredFeature = string.Empty;
        }
        
        /// <summary>
        /// Constructor completo
        /// </summary>
        public EventOption(string text, int cost, int daysExtra, int reputationImpact, float successChance, string requiredFeature = null)
        {
            Text = text;
            Cost = cost;
            DaysExtra = daysExtra;
            ReputationImpact = reputationImpact;
            SuccessChance = successChance;
            RequiredFeature = requiredFeature ?? string.Empty;
        }
        
        /// <summary>
        /// Verifica si el jugador tiene el feature requerido para esta opción.
        /// </summary>
        public bool IsAvailable(bool hasInsurance, bool hasPriority, int playerLevel)
        {
            if (string.IsNullOrEmpty(RequiredFeature))
                return true;
            
            switch (RequiredFeature.ToLower())
            {
                case "insurance":
                    return hasInsurance;
                case "priority":
                    return hasPriority;
                case "level3":
                    return playerLevel >= 3;
                case "level5":
                    return playerLevel >= 5;
                default:
                    return true;
            }
        }
        
        public override string ToString()
        {
            string result = Text;
            if (Cost > 0)
                result += $" (-${Cost})";
            if (DaysExtra > 0)
                result += $" (+{DaysExtra} días)";
            if (ReputationImpact < 0)
                result += $" ({ReputationImpact} rep)";
            if (SuccessChance < 1.0f)
                result += $" ({SuccessChance * 100:F0}% éxito)";
            return result;
        }
    }
}