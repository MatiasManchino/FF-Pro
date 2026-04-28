using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    /// <summary>
    /// Agent.cs — Modelo de agente de transporte con PERSONALIDAD ACTIVA.
    /// 
    /// Este agente NO es pasivo. Tiene:
    /// - Personalidad única que afecta su comportamiento
    /// - Memoria de cómo el jugador lo trata
    /// - Decisiones activas (subir precios, abandonar cargas, etc.)
    /// - Relación bidireccional con el jugador
    /// - Estado emocional que afecta su desempeño
    /// 
    /// QUÉ ES UN DICCIONARIO?
    /// Es como una lista pero con "llaves" para buscar rápido.
    /// </summary>
    [Serializable]
    public class Agent
    {
        // =========================================================================
        // IDENTIFICACIÓN BÁSICA
        // =========================================================================
        
        /// <summary>
        /// ID único del agente (ej: "maersk", "fedex")
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Nombre comercial del agente (ej: "Maersk Logistics")
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Ciudad base del agente (donde tiene su sede)
        /// </summary>
        public string BaseCityId { get; set; }
        
        // =========================================================================
        // PERSONALIDAD (determina comportamientos activos)
        // =========================================================================
        
        /// <summary>
        /// Personalidad del agente. Determina su comportamiento activo.
        /// </summary>
        public Constants.AgentPersonality Personality { get; set; }
        
        /// <summary>
        /// Descripción legible de la personalidad (para UI)
        /// </summary>
        public string PersonalityDescription { get; set; }
        
        // =========================================================================
        // SERVICIOS OFRECIDOS
        // =========================================================================
        
        /// <summary>
        /// Modos de transporte que ofrece (marítimo, aéreo, terrestre, etc.)
        /// </summary>
        public List<Constants.TransportMode> TransportModes { get; set; }
        
        /// <summary>
        /// Tipos de carga con los que tiene experiencia (para bonos)
        /// </summary>
        public List<Constants.CargoType> SpecializedCargoTypes { get; set; }
        
        /// <summary>
        /// Regiones donde opera (ej: "South America", "Europe", "Asia")
        /// Si está vacío → opera globalmente
        /// </summary>
        public List<string> OperatingRegions { get; set; }
        
        // =========================================================================
        // FACTORES DE PRECIO Y VELOCIDAD (base)
        // =========================================================================
        
        /// <summary>
        /// Multiplicador de precio base (1.0 = normal)
        /// Menor = más barato, Mayor = más caro
        /// </summary>
        public float BasePriceMultiplier { get; set; }
        
        /// <summary>
        /// Multiplicador de velocidad base (1.0 = normal)
        /// Mayor = más rápido, Menor = más lento
        /// </summary>
        public float BaseSpeedMultiplier { get; set; }
        
        /// <summary>
        /// Confiabilidad (0-1). Probabilidad de que NO haya problemas.
        /// 0.95 = 95% confiable → solo 5% chance de evento negativo
        /// </summary>
        public float Reliability { get; set; }
        
        // =========================================================================
        // PRECIO DINÁMICO (puede cambiar por eventos activos)
        // =========================================================================
        
        /// <summary>
        /// Multiplicador de precio actual (puede cambiar por eventos)
        /// </summary>
        public float CurrentPriceMultiplier { get; set; }
        
        /// <summary>
        /// ¿El agente está en modo "subida de precio"?
        /// </summary>
        public bool IsPriceSurgeActive { get; set; }
        
        /// <summary>
        /// Días restantes de la subida de precio
        /// </summary>
        public int PriceSurgeDaysRemaining { get; set; }
        
        // =========================================================================
        // RELACIÓN CON EL JUGADOR (MEMORIA BIDIRECCIONAL)
        // =========================================================================
        
        /// <summary>
        /// Confianza del jugador hacia el agente (0-100)
        /// Aumenta con entregas exitosas, disminuye con fallos
        /// </summary>
        public float PlayerTrust { get; set; }
        
        /// <summary>
        /// Confianza del agente hacia el jugador (0-100)
        /// El agente confía menos si negocias agresivamente o lo cambias
        /// </summary>
        public float AgentTrust { get; set; }
        
        /// <summary>
        /// Relación global (calculada: (PlayerTrust + AgentTrust) / 2)
        /// </summary>
        public Constants.AgentRelationship Relationship { get; set; }
        
        /// <summary>
        /// Contador de cargas seguidas usando este agente (para lealtad)
        /// </summary>
        public int ConsecutiveDeliveries { get; set; }
        
        /// <summary>
        /// ¿El jugador alguna vez cambió a otro agente bruscamente?
        /// </summary>
        public bool WasAbandonedByPlayer { get; set; }
        
        /// <summary>
        /// Días desde que el jugador usó por última vez este agente
        /// </summary>
        public int DaysSinceLastUse { get; set; }
        
        // =========================================================================
        // ESTADO ACTUAL DEL AGENTE
        // =========================================================================
        
        /// <summary>
        /// Estado actual del agente (Idle, Overworked, Stressed, Angry, etc.)
        /// </summary>
        public Constants.AgentState CurrentState { get; set; }
        
        /// <summary>
        /// Cargas activas actualmente con este agente
        /// </summary>
        public int CurrentLoad { get; set; }
        
        /// <summary>
        /// Capacidad máxima (depende de personalidad)
        /// Agentes eficientes tienen más capacidad
        /// </summary>
        public int MaxCapacity { get; set; }
        
        /// <summary>
        /// ¿Está disponible para nuevas cargas?
        /// </summary>
        public bool IsAvailable 
        { 
            get 
            { 
                return CurrentState != Constants.AgentState.Bankrupt && 
                       CurrentState != Constants.AgentState.Disappeared &&
                       CurrentLoad < MaxCapacity;
            } 
        }
        
        /// <summary>
        /// Días hasta que vuelve a estar disponible (si está desaparecido)
        /// </summary>
        public int DaysUntilReturn { get; set; }
        
        // =========================================================================
        // HISTORIAL
        // =========================================================================
        
        /// <summary>
        /// Total de entregas realizadas
        /// </summary>
        public int TotalDeliveries { get; set; }
        
        /// <summary>
        /// Entregas exitosas
        /// </summary>
        public int SuccessfulDeliveries { get; set; }
        
        /// <summary>
        /// Entregas fallidas
        /// </summary>
        public int FailedDeliveries { get; set; }
        
        /// <summary>
        /// Cargas que el agente abandonó
        /// </summary>
        public int AbandonedDeliveries { get; set; }
        
        // =========================================================================
        // CONSTRUCTORES
        // =========================================================================
        
        /// <summary>
        /// Constructor por defecto (necesario para serialización JSON)
        /// </summary>
        public Agent()
        {
            Id = string.Empty;
            Name = string.Empty;
            BaseCityId = string.Empty;
            TransportModes = new List<Constants.TransportMode>();
            SpecializedCargoTypes = new List<Constants.CargoType>();
            OperatingRegions = new List<string>();
            BasePriceMultiplier = 1.0f;
            BaseSpeedMultiplier = 1.0f;
            Reliability = 0.80f;
            CurrentPriceMultiplier = 1.0f;
            PlayerTrust = 50f;
            AgentTrust = 50f;
            CurrentState = Constants.AgentState.Idle;
            MaxCapacity = 3;
            PersonalityDescription = string.Empty;
            ConsecutiveDeliveries = 0;
            DaysSinceLastUse = 0;
            WasAbandonedByPlayer = false;
            IsPriceSurgeActive = false;
            PriceSurgeDaysRemaining = 0;
            DaysUntilReturn = 0;
            TotalDeliveries = 0;
            SuccessfulDeliveries = 0;
            FailedDeliveries = 0;
            AbandonedDeliveries = 0;
            CurrentLoad = 0;
            UpdateRelationship();
        }
        
        /// <summary>
        /// Constructor para crear agentes predefinidos.
        /// </summary>
        public Agent(string id, string name, string baseCityId, 
                     Constants.AgentPersonality personality,
                     List<Constants.TransportMode> transportModes,
                     float basePriceMultiplier, float baseSpeedMultiplier, 
                     float reliability, int maxCapacity = 3)
        {
            Id = id;
            Name = name;
            BaseCityId = baseCityId;
            Personality = personality;
            TransportModes = transportModes ?? new List<Constants.TransportMode>();
            SpecializedCargoTypes = new List<Constants.CargoType>();
            OperatingRegions = new List<string>();
            BasePriceMultiplier = basePriceMultiplier;
            BaseSpeedMultiplier = baseSpeedMultiplier;
            Reliability = reliability;
            CurrentPriceMultiplier = 1.0f;
            PlayerTrust = 50f;
            AgentTrust = 50f;
            CurrentState = Constants.AgentState.Idle;
            MaxCapacity = maxCapacity;
            PersonalityDescription = GetPersonalityDescription(personality);
            ConsecutiveDeliveries = 0;
            DaysSinceLastUse = 0;
            WasAbandonedByPlayer = false;
            IsPriceSurgeActive = false;
            PriceSurgeDaysRemaining = 0;
            DaysUntilReturn = 0;
            TotalDeliveries = 0;
            SuccessfulDeliveries = 0;
            FailedDeliveries = 0;
            AbandonedDeliveries = 0;
            CurrentLoad = 0;
            UpdateRelationship();
        }
        
        // =========================================================================
        // COMPORTAMIENTOS ACTIVOS (decisiones del agente)
        // =========================================================================
        
        /// <summary>
        /// El agente decide subir el precio sorpresivamente.
        /// Retorna el nuevo multiplicador de precio.
        /// </summary>
        public float TriggerPriceSurge()
        {
            if (IsPriceSurgeActive)
                return CurrentPriceMultiplier;
            
            IsPriceSurgeActive = true;
            PriceSurgeDaysRemaining = 5; // Dura 5 días
            CurrentPriceMultiplier = Constants.AGENT_PRICE_SURGE_MULTIPLIER;
            
            return CurrentPriceMultiplier;
        }
        
        /// <summary>
        /// El agente decide abandonar una carga (por sobrecarga o malicia)
        /// </summary>
        public bool DecideToAbandonCargo()
        {
            // Personalidades que abandonan más seguido
            float abandonChance = 0f;
            
            switch (Personality)
            {
                case Constants.AgentPersonality.Lazy:
                    abandonChance = 0.15f;
                    break;
                case Constants.AgentPersonality.Scammer:
                    abandonChance = 0.20f;
                    break;
                case Constants.AgentPersonality.Disappearing:
                    abandonChance = 0.30f;
                    break;
                case Constants.AgentPersonality.Envious:
                    abandonChance = 0.12f;
                    break;
                case Constants.AgentPersonality.Bipolar:
                    abandonChance = 0.10f;
                    break;
                default:
                    abandonChance = 0.03f;
                    break;
            }
            
            // Sobrecarga aumenta chance
            if (CurrentLoad > MaxCapacity)
                abandonChance += 0.10f;
            
            // Baja confianza del agente aumenta chance
            if (AgentTrust < 30)
                abandonChance += 0.15f;
            
            // Relación mala aumenta chance
            if (Relationship <= Constants.AgentRelationship.Bad)
                abandonChance += 0.10f;
            
            return UnityEngine.Random.value < abandonChance;
        }
        
        /// <summary>
        /// El agente decide desaparecer (no responde por días)
        /// </summary>
        public (bool willDisappear, int days) DecideToDisappear()
        {
            float disappearChance = 0f;
            
            switch (Personality)
            {
                case Constants.AgentPersonality.Elusive:
                    disappearChance = 0.08f;
                    break;
                case Constants.AgentPersonality.Disappearing:
                    disappearChance = 0.15f;
                    break;
                default:
                    disappearChance = 0.02f;
                    break;
            }
            
            // Si fue abandonado por el jugador, más chance de desaparecer
            if (WasAbandonedByPlayer)
                disappearChance += 0.10f;
            
            // Si está sobrecargado, más chance
            if (CurrentLoad > MaxCapacity)
                disappearChance += 0.05f;
            
            if (UnityEngine.Random.value < disappearChance)
            {
                int days = UnityEngine.Random.Range(Constants.AGENT_DISAPPEAR_DAYS_MIN, 
                                                     Constants.AGENT_DISAPPEAR_DAYS_MAX + 1);
                return (true, days);
            }
            
            return (false, 0);
        }
        
        /// <summary>
        /// El agente decide si cobrar un extra falso (estafador)
        /// </summary>
        public (bool willScam, int extraCost) DecideToScam(int baseCost)
        {
            if (Personality != Constants.AgentPersonality.Scammer)
                return (false, 0);
            
            float scamChance = 0.25f;
            
            // Si el jugador confía mucho, más chance de aprovecharse
            if (PlayerTrust > 70)
                scamChance += 0.15f;
            
            // Si el agente está estresado, más chance
            if (CurrentState == Constants.AgentState.Stressed)
                scamChance += 0.10f;
            
            if (UnityEngine.Random.value < scamChance)
            {
                int extra = UnityEngine.Random.Range(100, 500);
                return (true, extra);
            }
            
            return (false, 0);
        }
        
        /// <summary>
        /// El agente decide si mentir sobre la entrega
        /// </summary>
        public bool DecideToLie()
        {
            if (Personality != Constants.AgentPersonality.Liar)
                return false;
            
            float lieChance = 0.15f;
            
            // Si el jugador no revisa seguido, más chance (simulado)
            if (PlayerTrust > 60)
                lieChance += 0.10f;
            
            return UnityEngine.Random.value < lieChance;
        }
        
        /// <summary>
        /// El agente decide si sabotear al jugador por envidia
        /// </summary>
        public bool DecideToSabotage(int playerLevel)
        {
            if (Personality != Constants.AgentPersonality.Envious)
                return false;
            
            // Sabotea si el jugador tiene nivel alto y relación es mala
            if (playerLevel >= 5 && Relationship <= Constants.AgentRelationship.Neutral)
                return UnityEngine.Random.value < 0.20f;
            
            // Sabotea si el jugador nunca usó este agente (envidia)
            if (TotalDeliveries == 0 && playerLevel >= 3)
                return UnityEngine.Random.value < 0.15f;
            
            return false;
        }
        
        /// <summary>
        /// El agente decide si es competitivo con otro agente (rivalidad)
        /// </summary>
        public bool DecideToBeCompetitive(string rivalAgentId)
        {
            if (Personality != Constants.AgentPersonality.Rival)
                return false;
            
            // 30% de chance de sabotear al rival
            return UnityEngine.Random.value < 0.30f;
        }
        
        // =========================================================================
        // MÉTODOS DE RELACIÓN (cómo responde el agente al jugador)
        // =========================================================================
        
        /// <summary>
        /// Registra una entrega y actualiza relaciones.
        /// </summary>
        public void RecordDelivery(bool wasSuccessful, bool wasAbandoned = false)
        {
            TotalDeliveries++;
            CurrentLoad = Math.Max(0, CurrentLoad - 1);
            DaysSinceLastUse = 0;
            
            if (wasAbandoned)
            {
                AbandonedDeliveries++;
                FailedDeliveries++;
                ConsecutiveDeliveries = 0;
                AgentTrust -= Constants.AGENT_TRUST_LOSS_PER_ABANDON;
                PlayerTrust -= Constants.AGENT_TRUST_LOSS_PER_ABANDON / 2;
            }
            else if (wasSuccessful)
            {
                SuccessfulDeliveries++;
                ConsecutiveDeliveries++;
                PlayerTrust = Math.Min(100, PlayerTrust + Constants.AGENT_TRUST_GAIN_PER_SUCCESS);
                AgentTrust = Math.Min(100, AgentTrust + Constants.AGENT_TRUST_GAIN_PER_SUCCESS / 2);
            }
            else
            {
                FailedDeliveries++;
                ConsecutiveDeliveries = 0;
                PlayerTrust = Math.Max(0, PlayerTrust - Constants.AGENT_TRUST_LOSS_PER_FAILURE);
                AgentTrust = Math.Max(0, AgentTrust - Constants.AGENT_TRUST_LOSS_PER_FAILURE / 2);
            }
            
            // Actualizar relación
            UpdateRelationship();
            
            // Lealtad: usar el mismo agente 5 veces seguidas da bono
            if (ConsecutiveDeliveries >= 5)
            {
                CurrentPriceMultiplier = Math.Max(0.7f, CurrentPriceMultiplier - 0.03f);
            }
        }
        
        /// <summary>
        /// El jugador cambió de agente bruscamente.
        /// </summary>
        public void OnPlayerChangedAgent()
        {
            WasAbandonedByPlayer = true;
            AgentTrust = Math.Max(0, AgentTrust - 15);
            UpdateRelationship();
        }
        
        /// <summary>
        /// Actualiza la relación global basada en confianzas.
        /// </summary>
        private void UpdateRelationship()
        {
            float avgTrust = (PlayerTrust + AgentTrust) / 2f;
            
            if (avgTrust >= 71)
                Relationship = Constants.AgentRelationship.Partner;
            else if (avgTrust >= 51)
                Relationship = Constants.AgentRelationship.Ally;
            else if (avgTrust >= 31)
                Relationship = Constants.AgentRelationship.Friend;
            else if (avgTrust >= 11)
                Relationship = Constants.AgentRelationship.Good;
            else if (avgTrust >= -10)
                Relationship = Constants.AgentRelationship.Neutral;
            else if (avgTrust >= -30)
                Relationship = Constants.AgentRelationship.Bad;
            else
                Relationship = Constants.AgentRelationship.Enemy;
        }
        
        /// <summary>
        /// Actualiza el estado del agente según carga y personalidad.
        /// </summary>
        public void UpdateState()
        {
            // Si está desaparecido, contar días
            if (CurrentState == Constants.AgentState.Disappeared)
            {
                DaysUntilReturn--;
                if (DaysUntilReturn <= 0)
                {
                    CurrentState = Constants.AgentState.Idle;
                    DaysUntilReturn = 0;
                }
                return;
            }
            
            // Verificar quiebra (solo para ciertas personalidades)
            if (Personality == Constants.AgentPersonality.Disappearing && TotalDeliveries > 20)
            {
                if (UnityEngine.Random.value < 0.01f) // 1% chance por entrega
                {
                    CurrentState = Constants.AgentState.Bankrupt;
                    return;
                }
            }
            
            // Estado por sobrecarga
            if (CurrentLoad > MaxCapacity)
            {
                CurrentState = Constants.AgentState.Overworked;
            }
            else if (AgentTrust < 20)
            {
                CurrentState = Constants.AgentState.Angry;
            }
            else if (CurrentLoad >= MaxCapacity - 1 && MaxCapacity > 0)
            {
                // Cerca de la capacidad máxima → estresado
                CurrentState = Constants.AgentState.Stressed;
            }
            else
            {
                CurrentState = Constants.AgentState.Idle;
            }
        }
        
        /// <summary>
        /// Disminuye días de subida de precio.
        /// </summary>
        public void UpdatePriceSurge()
        {
            if (IsPriceSurgeActive)
            {
                PriceSurgeDaysRemaining--;
                if (PriceSurgeDaysRemaining <= 0)
                {
                    IsPriceSurgeActive = false;
                    CurrentPriceMultiplier = 1.0f;
                }
            }
        }
        
        // =========================================================================
        // MÉTODOS DE CÁLCULO
        // =========================================================================
        
        /// <summary>
        /// Obtiene el multiplicador de precio actual (considerando eventos activos).
        /// </summary>
        public float GetCurrentPriceMultiplier()
        {
            float multiplier = BasePriceMultiplier * CurrentPriceMultiplier;
            
            // Bonos por lealtad
            if (ConsecutiveDeliveries >= 10)
                multiplier *= 0.90f;
            else if (ConsecutiveDeliveries >= 5)
                multiplier *= 0.95f;
            
            // Penalizaciones por mala relación
            if (Relationship <= Constants.AgentRelationship.Bad)
                multiplier *= 1.15f;
            else if (Relationship <= Constants.AgentRelationship.Enemy)
                multiplier *= 1.30f;
            
            // Bonos por buena relación
            if (Relationship >= Constants.AgentRelationship.Ally)
                multiplier *= 0.95f;
            
            return multiplier;
        }
        
        /// <summary>
        /// Obtiene el multiplicador de velocidad actual.
        /// </summary>
        public float GetCurrentSpeedMultiplier()
        {
            float multiplier = BaseSpeedMultiplier;
            
            // Si está sobrecargado, más lento
            if (CurrentState == Constants.AgentState.Overworked)
                multiplier *= 0.70f;
            
            // Si está estresado, más lento
            if (CurrentState == Constants.AgentState.Stressed)
                multiplier *= 0.85f;
            
            // Si está enojado, más lento
            if (CurrentState == Constants.AgentState.Angry)
                multiplier *= 0.60f;
            
            return multiplier;
        }
        
        /// <summary>
        /// Obtiene el factor de riesgo de evento (modifica probabilidad de eventos negativos).
        /// </summary>
        public float GetEventRiskModifier()
        {
            float riskModifier = 1.0f;
            
            // Confianza baja → más riesgo
            if (AgentTrust < 30)
                riskModifier *= 1.5f;
            
            // Estado afecta riesgo
            if (CurrentState == Constants.AgentState.Overworked)
                riskModifier *= 1.3f;
            if (CurrentState == Constants.AgentState.Stressed)
                riskModifier *= 1.2f;
            if (CurrentState == Constants.AgentState.Angry)
                riskModifier *= 1.4f;
            
            // Relación afecta riesgo
            if (Relationship <= Constants.AgentRelationship.Bad)
                riskModifier *= 1.25f;
            
            return riskModifier;
        }
        
        /// <summary>
        /// Calcula el costo de transportar una carga con este agente.
        /// </summary>
        public int CalculateCost(Cargo cargo, float distanceKm)
        {
            // Fórmula base: distancia * multiplicadores
            float transportMultiplier = GetTransportModeMultiplier(cargo.TransportMode);
            float cargoMultiplier = GetCargoTypeMultiplier(cargo.CargoType);
            float priceMultiplier = GetCurrentPriceMultiplier();
            
            // Costo base: $0.50 por km por tonelada
            float baseCost = distanceKm * (cargo.Weight / 1000f) * 0.5f;
            
            int finalCost = (int)(baseCost * transportMultiplier * cargoMultiplier * priceMultiplier);
            
            // Costo mínimo $100
            return Math.Max(100, finalCost);
        }
        
        /// <summary>
        /// Obtiene el multiplicador por modo de transporte.
        /// </summary>
        private float GetTransportModeMultiplier(Constants.TransportMode mode)
        {
            switch (mode)
            {
                case Constants.TransportMode.Maritime:
                    return 0.7f;
                case Constants.TransportMode.Air:
                    return 2.5f;
                case Constants.TransportMode.Land:
                    return 1.0f;
                case Constants.TransportMode.Rail:
                    return 0.8f;
                case Constants.TransportMode.Multimodal:
                    return 1.5f;
                default:
                    return 1.0f;
            }
        }
        
        /// <summary>
        /// Obtiene el multiplicador por tipo de carga.
        /// </summary>
        private float GetCargoTypeMultiplier(Constants.CargoType cargoType)
        {
            switch (cargoType)
            {
                case Constants.CargoType.General:
                    return 1.0f;
                case Constants.CargoType.Refrigerated:
                    return 1.3f;
                case Constants.CargoType.Dangerous:
                    return 1.5f;
                case Constants.CargoType.Urgent:
                    return 1.2f;
                case Constants.CargoType.Valuable:
                    return 1.4f;
                default:
                    return 1.0f;
            }
        }
        
        // =========================================================================
        // MÉTODOS AUXILIARES (verificaciones y utilidades)
        // =========================================================================
        
        /// <summary>
        /// Verifica si el agente puede operar en una región específica.
        /// </summary>
        public bool CanOperateInRegion(string region)
        {
            if (OperatingRegions.Count == 0)
                return true;  // Sin restricciones = opera globalmente
            
            return OperatingRegions.Contains(region);
        }
        
        /// <summary>
        /// Verifica si el agente puede transportar este tipo de carga.
        /// </summary>
        public bool CanHandleCargoType(Constants.CargoType cargoType)
        {
            if (SpecializedCargoTypes.Count == 0)
                return true;  // Sin especializaciones, puede con todo
            
            return SpecializedCargoTypes.Contains(cargoType);
        }
        
        /// <summary>
        /// Verifica si el agente ofrece un modo de transporte específico.
        /// </summary>
        public bool OffersTransportMode(Constants.TransportMode mode)
        {
            return TransportModes.Contains(mode);
        }
        
        /// <summary>
        /// Obtiene la tasa de éxito (0-1).
        /// </summary>
        public float GetSuccessRate()
        {
            if (TotalDeliveries == 0)
                return 0.5f;  // Neutral para agentes sin historial
            
            return (float)SuccessfulDeliveries / TotalDeliveries;
        }
        
        /// <summary>
        /// Obtiene el emoji de relación para UI.
        /// </summary>
        public string GetRelationshipEmoji()
        {
            switch (Relationship)
            {
                case Constants.AgentRelationship.Partner: return "💍 Socio";
                case Constants.AgentRelationship.Ally: return "🤝 Aliado";
                case Constants.AgentRelationship.Friend: return "😊 Amigo";
                case Constants.AgentRelationship.Good: return "👍 Bueno";
                case Constants.AgentRelationship.Neutral: return "😐 Neutral";
                case Constants.AgentRelationship.Bad: return "😠 Malo";
                case Constants.AgentRelationship.Enemy: return "👎 Enemigo";
                default: return "😐 Neutral";
            }
        }
        
        /// <summary>
        /// Obtiene el emoji de estado para UI.
        /// </summary>
        public string GetStateEmoji()
        {
            switch (CurrentState)
            {
                case Constants.AgentState.Idle: return "✅";
                case Constants.AgentState.Overworked: return "⚠️";
                case Constants.AgentState.Stressed: return "😰";
                case Constants.AgentState.Angry: return "😤";
                case Constants.AgentState.Greedy: return "💰";
                case Constants.AgentState.Disappeared: return "👻";
                case Constants.AgentState.Bankrupt: return "💀";
                default: return "❓";
            }
        }
        
        /// <summary>
        /// Obtiene la descripción de personalidad según el enum.
        /// </summary>
        private string GetPersonalityDescription(Constants.AgentPersonality personality)
        {
            switch (personality)
            {
                case Constants.AgentPersonality.Reliable:
                    return "🛡️ Confiable. Nunca falla, pero es caro y no negocia.";
                case Constants.AgentPersonality.Cheap:
                    return "💰 Económico. Barato, pero a veces 'pierde' cargas.";
                case Constants.AgentPersonality.Ambitious:
                    return "📈 Ambicioso. Sube precios si detecta desesperación.";
                case Constants.AgentPersonality.Lazy:
                    return "😴 Perezoso. Responde lento, deja cargas olvidadas.";
                case Constants.AgentPersonality.Friendly:
                    return "🤗 Amigable. Avisa antes de subir precios, descuentos por lealtad.";
                case Constants.AgentPersonality.Elusive:
                    return "👻 Esquivo. Desaparece por días sin avisar.";
                case Constants.AgentPersonality.Efficient:
                    return "⚡ Eficiente. Siempre a tiempo, pero colapsa si lo sobrecargas.";
                case Constants.AgentPersonality.Scammer:
                    return "🎭 Estafador. Cobra extras falsos. ¡Cuidado!";
                case Constants.AgentPersonality.Liar:
                    return "🤥 Mentiroso. Dice que entregó pero no entregó.";
                case Constants.AgentPersonality.Bipolar:
                    return "🎢 Bipolar. Impredecible, un día excelente, otro horrible.";
                case Constants.AgentPersonality.Envious:
                    return "😤 Envidioso. Te sabotea si creces mucho.";
                case Constants.AgentPersonality.Disappearing:
                    return "💨 Fugaz. Puede desaparecer con tu carga si quiebra.";
                case Constants.AgentPersonality.Loyal:
                    return "🤝 Leal. Mejor precio por usar siempre el mismo.";
                case Constants.AgentPersonality.Rival:
                    return "⚔️ Rival. Odia a otros agentes, te penaliza si cambias.";
                default:
                    return "Estándar.";
            }
        }
        
        /// <summary>
        /// Devuelve un resumen legible del agente.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} | {GetRelationshipEmoji()} | Confianza: {PlayerTrust}% | " +
                   $"Carga: {CurrentLoad}/{MaxCapacity} | Estado: {CurrentState}";
        }
    }
}