using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    [Serializable]
    public class Agent
    {
        // Identificación
        public string Id { get; set; }
        public string Name { get; set; }
        public string HomeCityId { get; set; }
        public string Description { get; set; }

        // Personalidad y estado
        public Constants.AgentPersonality Personality { get; set; }
        public Constants.AgentState CurrentState { get; set; }
        public Constants.AgentRelationship Relationship { get; set; }

        // Capacidad y carga
        public int MaxCapacity { get; set; }
        public int CurrentLoad { get; set; }
        public List<string> CurrentCargoIds { get; set; }
        public List<string> OperatingRegions { get; set; }
        public List<Constants.TransportMode> SupportedModes { get; set; }
        public List<Constants.CargoType> SupportedCargoTypes { get; set; }

        // Multiplicadores base
        public float BasePriceMultiplier { get; set; }
        public float BaseSpeedMultiplier { get; set; }
        public float BaseReliability { get; set; }

        // Multiplicadores actuales (modificados por estado)
        public float CurrentPriceMultiplier { get; set; }

        // Confianza (0-100)
        public float PlayerTrust { get; set; }
        public float AgentTrust { get; set; }

        // Estadísticas de entregas
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public int AbandonedDeliveries { get; set; }
        public int ConsecutiveDeliveries { get; set; }

        // Estado temporal
        public int DaysUntilReturn { get; set; }
        public int DaysSinceLastUse { get; set; }
        public bool IsInPriceSurge { get; set; }
        public int PriceSurgeDaysRemaining { get; set; }

        public Agent()
        {
            CurrentCargoIds = new List<string>();
            OperatingRegions = new List<string>();
            SupportedModes = new List<Constants.TransportMode>();
            SupportedCargoTypes = new List<Constants.CargoType>();
            CurrentState = Constants.AgentState.Idle;
            Relationship = Constants.AgentRelationship.Neutral;
            PlayerTrust = 50f;
            AgentTrust = 50f;
            CurrentPriceMultiplier = 1f;
        }

        public Agent(string id, string name, string homeCityId,
                     Constants.AgentPersonality personality,
                     float basePriceMultiplier, float baseSpeedMultiplier, float baseReliability,
                     int maxCapacity) : this()
        {
            Id = id;
            Name = name;
            HomeCityId = homeCityId;
            Personality = personality;
            BasePriceMultiplier = basePriceMultiplier;
            BaseSpeedMultiplier = baseSpeedMultiplier;
            BaseReliability = baseReliability;
            MaxCapacity = maxCapacity;
            Description = GetPersonalityDescription(personality);
        }

        // ═══════════════════════════════════
        // REGISTRO DE ENTREGA
        // ═══════════════════════════════════

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
                PlayerTrust -= Constants.AGENT_TRUST_LOSS_PER_ABANDON / 2f;
            }
            else if (wasSuccessful)
            {
                SuccessfulDeliveries++;
                ConsecutiveDeliveries++;
                PlayerTrust = Math.Min(100, PlayerTrust + Constants.AGENT_TRUST_GAIN_PER_SUCCESS);
                AgentTrust  = Math.Min(100, AgentTrust  + Constants.AGENT_TRUST_GAIN_PER_SUCCESS / 2f);
            }
            else
            {
                FailedDeliveries++;
                ConsecutiveDeliveries = 0;
                PlayerTrust = Math.Max(0, PlayerTrust - Constants.AGENT_TRUST_LOSS_PER_FAILURE);
                AgentTrust  = Math.Max(0, AgentTrust  - Constants.AGENT_TRUST_LOSS_PER_FAILURE / 2f);
            }

            // Descuento por lealtad (Loyal personality)
            if (ConsecutiveDeliveries >= 5)
                CurrentPriceMultiplier = Math.Max(0.7f, CurrentPriceMultiplier - 0.03f);

            UpdateRelationship();
        }

        private void UpdateRelationship()
        {
            float avg = (PlayerTrust + AgentTrust) / 2f;
            if      (avg >= 71) Relationship = Constants.AgentRelationship.Partner;
            else if (avg >= 51) Relationship = Constants.AgentRelationship.Ally;
            else if (avg >= 31) Relationship = Constants.AgentRelationship.Friend;
            else if (avg >= 11) Relationship = Constants.AgentRelationship.Good;
            else if (avg >= -10) Relationship = Constants.AgentRelationship.Neutral;
            else if (avg >= -30) Relationship = Constants.AgentRelationship.Bad;
            else                 Relationship = Constants.AgentRelationship.Enemy;
        }

        // ═══════════════════════════════════
        // ACTUALIZACIÓN DE ESTADO
        // ═══════════════════════════════════

        public void UpdateState()
        {
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

            if (CurrentState == Constants.AgentState.Bankrupt) return;

            // Quiebra: 1% chance para Disappearing con >20 entregas
            if (Personality == Constants.AgentPersonality.Disappearing && TotalDeliveries > 20)
            {
                if (UnityEngine.Random.value < 0.01f)
                {
                    CurrentState = Constants.AgentState.Bankrupt;
                    return;
                }
            }

            if (CurrentLoad > MaxCapacity)
                CurrentState = Constants.AgentState.Overworked;
            else if (AgentTrust < 20)
                CurrentState = Constants.AgentState.Angry;
            else if (CurrentLoad >= MaxCapacity - 1 && MaxCapacity > 0)
                CurrentState = Constants.AgentState.Stressed;
            else
                CurrentState = Constants.AgentState.Idle;
        }

        public void UpdatePriceSurge()
        {
            if (IsInPriceSurge)
            {
                PriceSurgeDaysRemaining--;
                if (PriceSurgeDaysRemaining <= 0)
                {
                    IsInPriceSurge = false;
                    CurrentPriceMultiplier = 1f;
                }
            }
            else if (Personality == Constants.AgentPersonality.Ambitious && CurrentState == Constants.AgentState.Idle)
            {
                if (UnityEngine.Random.value < 0.10f)
                {
                    IsInPriceSurge = true;
                    PriceSurgeDaysRemaining = UnityEngine.Random.Range(3, 8);
                    CurrentPriceMultiplier = Constants.AGENT_PRICE_SURGE_MULTIPLIER;
                }
            }
        }

        // ═══════════════════════════════════
        // CÁLCULO DE COSTOS Y VELOCIDAD
        // ═══════════════════════════════════

        public float GetCurrentPriceMultiplier()
        {
            float mult = BasePriceMultiplier * CurrentPriceMultiplier;
            if (ConsecutiveDeliveries >= 10) mult *= 0.90f;
            else if (ConsecutiveDeliveries >= 5) mult *= 0.95f;

            if (Relationship <= Constants.AgentRelationship.Enemy) mult *= 1.30f;
            else if (Relationship <= Constants.AgentRelationship.Bad) mult *= 1.15f;
            else if (Relationship >= Constants.AgentRelationship.Ally) mult *= 0.95f;
            return mult;
        }

        public float GetCurrentSpeedMultiplier()
        {
            float mult = BaseSpeedMultiplier;
            if (CurrentState == Constants.AgentState.Overworked) mult *= 0.70f;
            if (CurrentState == Constants.AgentState.Stressed)   mult *= 0.85f;
            if (CurrentState == Constants.AgentState.Angry)      mult *= 0.60f;
            return mult;
        }

        public float GetEventRiskModifier()
        {
            float risk = 1.0f;
            if (AgentTrust < 30) risk *= 1.5f;
            if (CurrentState == Constants.AgentState.Overworked) risk *= 1.3f;
            if (CurrentState == Constants.AgentState.Stressed)   risk *= 1.2f;
            if (CurrentState == Constants.AgentState.Angry)      risk *= 1.4f;
            if (Relationship <= Constants.AgentRelationship.Bad) risk *= 1.25f;
            return risk;
        }

        public int CalculateCost(Cargo cargo, float distanceKm)
        {
            float transportMult = GetTransportModeMultiplier(cargo.TransportMode);
            float cargoMult     = GetCargoTypeMultiplier(cargo.CargoType);
            float priceMult     = GetCurrentPriceMultiplier();
            float baseCost      = distanceKm * (cargo.Weight / 1000f) * 0.5f;
            int finalCost       = (int)(baseCost * transportMult * cargoMult * priceMult);
            return Math.Max(100, finalCost);
        }

        private float GetTransportModeMultiplier(Constants.TransportMode mode)
        {
            switch (mode)
            {
                case Constants.TransportMode.Maritime:   return 0.7f;
                case Constants.TransportMode.Air:        return 2.5f;
                case Constants.TransportMode.Land:       return 1.0f;
                case Constants.TransportMode.Rail:       return 0.8f;
                case Constants.TransportMode.Multimodal: return 1.5f;
                default:                                 return 1.0f;
            }
        }

        private float GetCargoTypeMultiplier(Constants.CargoType type)
        {
            switch (type)
            {
                case Constants.CargoType.General:      return 1.0f;
                case Constants.CargoType.Refrigerated: return 1.3f;
                case Constants.CargoType.Dangerous:    return 1.5f;
                case Constants.CargoType.Urgent:       return 1.2f;
                case Constants.CargoType.Valuable:     return 1.4f;
                default:                               return 1.0f;
            }
        }

        // ═══════════════════════════════════
        // AUXILIARES
        // ═══════════════════════════════════

        public bool CanOperateInRegion(string region)
            => OperatingRegions.Count == 0 || OperatingRegions.Contains(region);

        public bool CanHandleCargoType(Constants.CargoType cargoType)
            => SupportedCargoTypes.Count == 0 || SupportedCargoTypes.Contains(cargoType);

        public bool OffersTransportMode(Constants.TransportMode mode)
            => SupportedModes.Count == 0 || SupportedModes.Contains(mode);

        public float GetSuccessRate()
            => TotalDeliveries == 0 ? 0.5f : (float)SuccessfulDeliveries / TotalDeliveries;

        public bool IsAvailable()
            => CurrentState != Constants.AgentState.Disappeared &&
               CurrentState != Constants.AgentState.Bankrupt &&
               CurrentLoad < MaxCapacity;

        public string GetRelationshipEmoji()
        {
            switch (Relationship)
            {
                case Constants.AgentRelationship.Partner: return "💍 Socio";
                case Constants.AgentRelationship.Ally:    return "🤝 Aliado";
                case Constants.AgentRelationship.Friend:  return "😊 Amigo";
                case Constants.AgentRelationship.Good:    return "👍 Bueno";
                case Constants.AgentRelationship.Neutral: return "😐 Neutral";
                case Constants.AgentRelationship.Bad:     return "😠 Malo";
                case Constants.AgentRelationship.Enemy:   return "👎 Enemigo";
                default:                                  return "😐 Neutral";
            }
        }

        public string GetStateEmoji()
        {
            switch (CurrentState)
            {
                case Constants.AgentState.Idle:        return "✅";
                case Constants.AgentState.Overworked:  return "⚠️";
                case Constants.AgentState.Stressed:    return "😰";
                case Constants.AgentState.Angry:       return "😤";
                case Constants.AgentState.Greedy:      return "💰";
                case Constants.AgentState.Disappeared: return "👻";
                case Constants.AgentState.Bankrupt:    return "💀";
                default:                               return "✅";
            }
        }

        private string GetPersonalityDescription(Constants.AgentPersonality personality)
        {
            switch (personality)
            {
                case Constants.AgentPersonality.Reliable:     return "🛡️ Confiable. Nunca falla, pero es caro y no negocia.";
                case Constants.AgentPersonality.Cheap:        return "💰 Económico. Barato, pero a veces 'pierde' cargas.";
                case Constants.AgentPersonality.Ambitious:    return "📈 Ambicioso. Sube precios si detecta desesperación.";
                case Constants.AgentPersonality.Lazy:         return "😴 Perezoso. Responde lento, deja cargas olvidadas.";
                case Constants.AgentPersonality.Friendly:     return "🤗 Amigable. Avisa antes de subir precios, descuentos por lealtad.";
                case Constants.AgentPersonality.Elusive:      return "👻 Esquivo. Desaparece por días sin avisar.";
                case Constants.AgentPersonality.Efficient:    return "⚡ Eficiente. Siempre a tiempo, pero colapsa si lo sobrecargas.";
                case Constants.AgentPersonality.Scammer:      return "🎭 Estafador. Cobra extras falsos. ¡Cuidado!";
                case Constants.AgentPersonality.Liar:         return "🤥 Mentiroso. Dice que entregó pero no entregó.";
                case Constants.AgentPersonality.Bipolar:      return "🎢 Bipolar. Impredecible, un día excelente, otro horrible.";
                case Constants.AgentPersonality.Envious:      return "😤 Envidioso. Te sabotea si creces mucho.";
                case Constants.AgentPersonality.Disappearing: return "💨 Fugaz. Puede desaparecer con tu carga si quiebra.";
                case Constants.AgentPersonality.Loyal:        return "🤝 Leal. Mejor precio por usar siempre el mismo.";
                case Constants.AgentPersonality.Rival:        return "⚔️ Rival. Odia a otros agentes, te penaliza si cambias.";
                default:                                      return "Agente de transporte.";
            }
        }

        public override string ToString()
            => $"{Name} [{Constants.GetAgentPersonalityName(Personality)}] | {GetStateEmoji()} | Carga: {CurrentLoad}/{MaxCapacity}";
    }
}
