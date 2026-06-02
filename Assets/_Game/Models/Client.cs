using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    // Nivel de lealtad del cliente según entregas exitosas acumuladas.
    public enum ClientTier { Nuevo, Frecuente, VIP, Diamante }

    [Serializable]
    public class Client
    {
        // Gestiona id.
        public string Id { get; set; }
// Gestiona company name.
        public string CompanyName { get; set; }
// Devuelve el client type
        public Constants.ClientType ClientType { get; set; }
// Gestiona personality description.
        public string PersonalityDescription { get; set; }

        // Gestiona relationship nivel.
        public float RelationshipLevel { get; set; }
// Gestiona anger nivel.
        public int AngerLevel { get; set; }
// Indica si lista negra.
        public bool IsBlacklisted { get; set; }
// Días until anger decay.
        public int DaysUntilAngerDecay { get; set; }

        // Pago delay.
        public int PaymentDelay { get; set; }
// Gestiona early pago chance.
        public float EarlyPaymentChance { get; set; }
// Tardío pago chance.
        public float LatePaymentChance { get; set; }
// Tardío pago penalty.
        public float LatePaymentPenalty { get; set; }

        // Gestiona delay tolerance.
        public int DelayTolerance { get; set; }
// Gestiona damage tolerance.
        public float DamageTolerance { get; set; }
// Gestiona accepts negotiation.
        public bool AcceptsNegotiation { get; set; }
// Gestiona max margin tolerance.
        public float MaxMarginTolerance { get; set; }

        // Gestiona total deliveries.
        public int TotalDeliveries { get; set; }
// Gestiona successful deliveries.
        public int SuccessfulDeliveries { get; set; }
// Fallado deliveries.
        public int FailedDeliveries { get; set; }
// Gestiona complaints count.
        public int ComplaintsCount { get; set; }
// Gestiona recommendations given.
        public int RecommendationsGiven { get; set; }
// Gestiona pending offers.
        public int PendingOffers { get; set; }
// Gestiona last interaction día.
        public int LastInteractionDay { get; set; }
// Gestiona total profit.
        public int TotalProfit { get; set; }
        public int LastRelationDecayDay { get; set; }

        // Indica si active.
        public bool IsActive { get; set; }
// Indica si vip.
        public bool IsVip { get; set; }
// Determina si tiene active contract.
        public bool HasActiveContract { get; set; }
// Gestiona contract días remaining.
        public int ContractDaysRemaining { get; set; }
// Gestiona favorite rutas.
        public List<string> FavoriteRoutes { get; set; }

// Realiza cliente
        public Client() { FavoriteRoutes = new List<string>(); }

// Client
        public Client(string companyName, Constants.ClientType clientType) : this()
        {
            Id = Guid.NewGuid().ToString();
            CompanyName = companyName;
            ClientType = clientType;
            IsActive = true;
            LatePaymentPenalty = 0.1f;

            switch (clientType)
            {
                case Constants.ClientType.GoodPayer:
                    RelationshipLevel = 60; PaymentDelay = 0;
                    EarlyPaymentChance = 0.50f; LatePaymentChance = 0.02f;
                    DelayTolerance = 5; DamageTolerance = 0.15f;
                    AcceptsNegotiation = true; MaxMarginTolerance = 0.35f;
                    PersonalityDescription = "Paga al contado, confiable y tolerante.";
                    break;
                case Constants.ClientType.BadPayer:
                    RelationshipLevel = 40; PaymentDelay = 15;
                    EarlyPaymentChance = 0.05f; LatePaymentChance = 0.40f;
                    DelayTolerance = 3; DamageTolerance = 0.05f;
                    AcceptsNegotiation = false; MaxMarginTolerance = 0.15f;
                    PersonalityDescription = "Se retrasa en los pagos. Baja tolerancia.";
                    break;
                case Constants.ClientType.UrgentClient:
                    RelationshipLevel = 50; PaymentDelay = 0;
                    EarlyPaymentChance = 0.80f; LatePaymentChance = 0.05f;
                    DelayTolerance = 1; DamageTolerance = 0.10f;
                    AcceptsNegotiation = true; MaxMarginTolerance = 0.50f;
                    PersonalityDescription = "Necesita rapidez. Paga más, pero no tolera demoras.";
                    break;
                case Constants.ClientType.CreditClient:
                    RelationshipLevel = 45; PaymentDelay = 45;
                    EarlyPaymentChance = 0.10f; LatePaymentChance = 0.15f;
                    DelayTolerance = 3; DamageTolerance = 0.08f;
                    AcceptsNegotiation = true; MaxMarginTolerance = 0.20f;
                    PersonalityDescription = "Paga a 30-60 días. Exigente en condiciones.";
                    break;
                case Constants.ClientType.VeryBadClient:
                    RelationshipLevel = 30; PaymentDelay = 30;
                    EarlyPaymentChance = 0.01f; LatePaymentChance = 0.60f;
                    DelayTolerance = 2; DamageTolerance = 0.03f;
                    AcceptsNegotiation = false; MaxMarginTolerance = 0.10f;
                    PersonalityDescription = "Difícil y reclamador. Proceder con cuidado.";
                    break;
                case Constants.ClientType.ContractClient:
                    RelationshipLevel = 55; PaymentDelay = 5;
                    EarlyPaymentChance = 0.30f; LatePaymentChance = 0.08f;
                    DelayTolerance = 4; DamageTolerance = 0.10f;
                    AcceptsNegotiation = true; MaxMarginTolerance = 0.25f;
                    HasActiveContract = true; ContractDaysRemaining = 180;
                    PersonalityDescription = "Contrato a largo plazo. Busca estabilidad y consistencia.";
                    break;
            }
        }

// Public
        public (float complaintChance, bool becomesAngry) ReactToDelay(int delayDays)
        {
            if (delayDays <= DelayTolerance) return (0f, false);
            float sensitivity = ClientType == Constants.ClientType.UrgentClient ? 2f : 1f;
            float excess = (delayDays - DelayTolerance) * sensitivity;
            float chance = Mathf.Clamp01(excess * 0.15f);
            return (chance, chance > 0.5f);
        }

// Public
        public (float complaintChance, bool becomesAngry) ReactToDamage(float damagePercentage)
        {
            if (damagePercentage <= DamageTolerance) return (0f, false);
            float sensitivity = HasActiveContract ? 2f : 1f;
            float excess = (damagePercentage - DamageTolerance) * sensitivity;
            float chance = Mathf.Clamp01(excess * 3f);
            return (chance, chance > 0.6f);
        }

// Public
        public (float rejectionChance, bool becomesAngry) ReactToHighPrice(float marginPercentage)
        {
            if (marginPercentage <= MaxMarginTolerance) return (0f, false);
            if (marginPercentage > 0.50f) return (1f, true);
            float excess = marginPercentage - MaxMarginTolerance;
            float chance = Mathf.Clamp01(excess * 2f);
            return (chance, chance > 0.7f);
        }

// Gestiona decide to recommend.
        public bool DecideToRecommend()
        {
            if (RelationshipLevel <= 70) return false;
            float chance = 0.05f;
            if (SuccessfulDeliveries >= 10) chance += 0.05f;
            if (SuccessfulDeliveries >= 5)  chance += 0.05f;
            if (IsVip) chance += 0.10f;
            return UnityEngine.Random.value < chance;
        }

// Gestiona decide to become vip.
        public bool DecideToBecomeVip()
        {
            if (IsVip || TotalDeliveries < 10 || RelationshipLevel < 80) return false;
            return UnityEngine.Random.value < 0.10f;
        }

// Gestiona decide to renew contract.
        public bool DecideToRenewContract()
        {
            if (!HasActiveContract) return false;
            bool canRenew = (RelationshipLevel >= 60 && FailedDeliveries == 0) ||
                            (RelationshipLevel >= 70 && FailedDeliveries <= 1);
            if (!canRenew) return false;
            ContractDaysRemaining = 180;
            RelationshipLevel = Mathf.Min(100, RelationshipLevel + 10);
            return true;
        }

        public void RecordDelivery(bool wasSuccessful, string originCityId, string destinationCityId,
                                   int currentDay, bool wasDelayed = false, bool wasDamaged = false)
        {
            TotalDeliveries++;
            LastInteractionDay = currentDay;
            DaysUntilAngerDecay = 5;

            if (wasSuccessful && !wasDelayed && !wasDamaged)
            {
                SuccessfulDeliveries++;
                RelationshipLevel = Mathf.Min(100, RelationshipLevel + 8);
                AddFavoriteRoute(originCityId, destinationCityId);
            }
            // Realiza if
            else if (wasSuccessful)
            {
                SuccessfulDeliveries++;
                RelationshipLevel = Mathf.Min(100, RelationshipLevel + (wasDelayed ? 2 : 5));
            }
            else
            {
                FailedDeliveries++;
                float loss = 5 + (FailedDeliveries * 2);
                RelationshipLevel = Mathf.Max(0, RelationshipLevel - loss);
                IncreaseAnger(2);
            }
        }

// Gestiona decay anger.
        public void DecayAnger()
        {
            if (AngerLevel <= 0) return;
            DaysUntilAngerDecay--;
            if (DaysUntilAngerDecay <= 0)
            {
                AngerLevel = Math.Max(0, AngerLevel - 1);
                DaysUntilAngerDecay = 5;
                if (AngerLevel < 5 && IsBlacklisted)
                {
                    IsBlacklisted = false;
                    IsActive = true;
                }
            }
        }

// Gestiona increase anger.
        private void IncreaseAnger(int amount)
        {
            AngerLevel = Math.Min(5, AngerLevel + amount);
            if (AngerLevel >= 5)
            {
                IsBlacklisted = true;
                IsActive = false;
            }
        }

// Añade favorite ruta
        private void AddFavoriteRoute(string originId, string destinationId)
        {
            string route = $"{originId}→{destinationId}";
            if (!FavoriteRoutes.Contains(route))
            {
                FavoriteRoutes.Add(route);
                if (FavoriteRoutes.Count > 5)
                    FavoriteRoutes.RemoveAt(0);
            }
        }

// Obtiene negotiation bonus
        public float GetNegotiationBonus()
        {
            if (RelationshipLevel >= 80) return 0.20f;
            if (RelationshipLevel >= 60) return 0.10f;
            if (RelationshipLevel >= 40) return 0f;
            if (RelationshipLevel >= 20) return -0.10f;
            return -0.20f;
        }

// Obtiene desired price multiplier
        public float GetDesiredPriceMultiplier()
        {
            float mult = 1f;
            if (IsVip)    mult *= 1.15f;
            if (ClientType == Constants.ClientType.UrgentClient) mult *= 1.25f;
            if (HasActiveContract) mult *= 0.90f;
            if (RelationshipLevel >= 70) mult += 0.05f;
            return mult;
        }

// Gestiona will pay early.
        public bool WillPayEarly()
        {
            float chance = EarlyPaymentChance;
            if (IsVip) chance += 0.20f;
            if (RelationshipLevel >= 70) chance += 0.10f;
            return UnityEngine.Random.value < chance;
        }

// Gestiona will pay tardío.
        public bool WillPayLate()
        {
            float chance = LatePaymentChance;
            if (RelationshipLevel < 30) chance += 0.20f;
            if (AngerLevel >= 3) chance += 0.30f;
            return UnityEngine.Random.value < chance;
        }

// Obtiene success rate
        public float GetSuccessRate()
            => TotalDeliveries == 0 ? 0.5f : (float)SuccessfulDeliveries / TotalDeliveries;

        // ── Lealtad / niveles ─────────────────────────────────────────────────
        public ClientTier Tier
        {
            get
            {
                if (SuccessfulDeliveries >= 20) return ClientTier.Diamante;
                if (SuccessfulDeliveries >= 10) return ClientTier.VIP;
                if (SuccessfulDeliveries >= 5)  return ClientTier.Frecuente;
                return ClientTier.Nuevo;
            }
        }

// Obtiene nivel nombre
        public string GetTierName()
        {
            switch (Tier)
            {
                case ClientTier.Diamante:  return "Diamante";
                case ClientTier.VIP:       return "VIP";
                case ClientTier.Frecuente: return "Frecuente";
                default:                   return "Nuevo";
            }
        }

// Obtiene nivel badge
        public string GetTierBadge()
        {
            switch (Tier)
            {
                case ClientTier.Diamante:  return "💎 Diamante";
                case ClientTier.VIP:       return "⭐ VIP";
                case ClientTier.Frecuente: return "Frecuente";
                default:                   return "Nuevo";
            }
        }

        // Bonus de aceptación de cotización por lealtad acumulada.
        public float GetTierAcceptanceBonus()
        {
            switch (Tier)
            {
                case ClientTier.Diamante:  return 0.15f;
                case ClientTier.VIP:       return 0.10f;
                case ClientTier.Frecuente: return 0.05f;
                default:                   return 0f;
            }
        }

// Registra profit.
        public void RecordProfit(int amount) { if (amount > 0) TotalProfit += amount; }

        // La relación se enfría lentamente por inactividad: pierde 'amountPer2Weeks' cada 2 semanas sin interacción.
        public void DecayRelationshipDaily(int currentDay, float amountPer2Weeks)
        {
            // Si hubo interacción más reciente que el último decaimiento, reinicia el contador de 2 semanas.
            if (LastInteractionDay > LastRelationDecayDay) LastRelationDecayDay = LastInteractionDay;
            if (currentDay - LastRelationDecayDay < 14) return;   // todavía no pasaron 2 semanas
            LastRelationDecayDay += 14;
            // La inactividad enfría la relación solo hasta 50; por debajo solo baja por hechos (fallas, etc.).
            if (RelationshipLevel > 50f)
                RelationshipLevel = Mathf.Max(50f, RelationshipLevel - amountPer2Weeks);
        }

// Obtiene relationship emoji
        public string GetRelationshipEmoji()
        {
            if (RelationshipLevel >= 90) return "💎 Excelente";
            if (RelationshipLevel >= 70) return "😊 Muy Buena";
            if (RelationshipLevel >= 50) return "😐 Buena";
            if (RelationshipLevel >= 30) return "😠 Regular";
            if (RelationshipLevel >= 10) return "😤 Mala";
            return "👎 Pésima";
        }

// Obtiene anger emoji
        public string GetAngerEmoji()
        {
            switch (AngerLevel)
            {
                case 0: return "😊";
                case 1: return "😐";
                case 2: return "😠";
                case 3: return "😤";
                case 4: return "💢";
                default: return "🚫";
            }
        }

// Gestiona to string.
        public override string ToString()
        {
            string extras = (IsVip ? " VIP" : "") + (HasActiveContract ? " CONTRATO" : "") + (IsBlacklisted ? " BLOQUEADO" : "");
            return $"{CompanyName} | {GetRelationshipEmoji()}{extras} | Éxito: {GetSuccessRate():P0}";
        }
    }
}