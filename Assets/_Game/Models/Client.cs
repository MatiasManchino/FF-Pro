using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    [Serializable]
    public class Client
    {
        // Identificación
        public string Id { get; set; }
        public string CompanyName { get; set; }
        public Constants.ClientType ClientType { get; set; }
        public string PersonalityDescription { get; set; }

        // Relación
        public float RelationshipLevel { get; set; }
        public int AngerLevel { get; set; }
        public bool IsBlacklisted { get; set; }
        public int DaysUntilAngerDecay { get; set; }

        // Comportamiento de pago
        public int PaymentDelay { get; set; }
        public float EarlyPaymentChance { get; set; }
        public float LatePaymentChance { get; set; }
        public float LatePaymentPenalty { get; set; }

        // Tolerancia
        public int DelayTolerance { get; set; }
        public float DamageTolerance { get; set; }
        public bool AcceptsNegotiation { get; set; }
        public float MaxMarginTolerance { get; set; }

        // Historial
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public int ComplaintsCount { get; set; }
        public int RecommendationsGiven { get; set; }
        public int PendingOffers { get; set; }
        public int LastInteractionDay { get; set; }

        // Estado especial
        public bool IsActive { get; set; }
        public bool IsVip { get; set; }
        public bool HasActiveContract { get; set; }
        public int ContractDaysRemaining { get; set; }
        public List<string> FavoriteRoutes { get; set; }

        public Client() { FavoriteRoutes = new List<string>(); }

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

        public (float complaintChance, bool becomesAngry) ReactToDelay(int delayDays)
        {
            if (delayDays <= DelayTolerance) return (0f, false);
            float sensitivity = ClientType == Constants.ClientType.UrgentClient ? 2f : 1f;
            float excess = (delayDays - DelayTolerance) * sensitivity;
            float chance = Mathf.Clamp01(excess * 0.15f);
            return (chance, chance > 0.5f);
        }

        public (float complaintChance, bool becomesAngry) ReactToDamage(float damagePercentage)
        {
            if (damagePercentage <= DamageTolerance) return (0f, false);
            float sensitivity = HasActiveContract ? 2f : 1f;
            float excess = (damagePercentage - DamageTolerance) * sensitivity;
            float chance = Mathf.Clamp01(excess * 3f);
            return (chance, chance > 0.6f);
        }

        public (float rejectionChance, bool becomesAngry) ReactToHighPrice(float marginPercentage)
        {
            if (marginPercentage <= MaxMarginTolerance) return (0f, false);
            if (marginPercentage > 0.50f) return (1f, true);
            float excess = marginPercentage - MaxMarginTolerance;
            float chance = Mathf.Clamp01(excess * 2f);
            return (chance, chance > 0.7f);
        }

        public bool DecideToRecommend()
        {
            if (RelationshipLevel <= 70) return false;
            float chance = 0.05f;
            if (SuccessfulDeliveries >= 10) chance += 0.05f;
            if (SuccessfulDeliveries >= 5)  chance += 0.05f;
            if (IsVip) chance += 0.10f;
            return UnityEngine.Random.value < chance;
        }

        public bool DecideToBecomeVip()
        {
            if (IsVip || TotalDeliveries < 10 || RelationshipLevel < 80) return false;
            return UnityEngine.Random.value < 0.10f;
        }

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

        private void IncreaseAnger(int amount)
        {
            AngerLevel = Math.Min(5, AngerLevel + amount);
            if (AngerLevel >= 5)
            {
                IsBlacklisted = true;
                IsActive = false;
            }
        }

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

        public float GetNegotiationBonus()
        {
            if (RelationshipLevel >= 80) return 0.20f;
            if (RelationshipLevel >= 60) return 0.10f;
            if (RelationshipLevel >= 40) return 0f;
            if (RelationshipLevel >= 20) return -0.10f;
            return -0.20f;
        }

        public float GetDesiredPriceMultiplier()
        {
            float mult = 1f;
            if (IsVip)    mult *= 1.15f;
            if (ClientType == Constants.ClientType.UrgentClient) mult *= 1.25f;
            if (HasActiveContract) mult *= 0.90f;
            if (RelationshipLevel >= 70) mult += 0.05f;
            return mult;
        }

        public bool WillPayEarly()
        {
            float chance = EarlyPaymentChance;
            if (IsVip) chance += 0.20f;
            if (RelationshipLevel >= 70) chance += 0.10f;
            return UnityEngine.Random.value < chance;
        }

        public bool WillPayLate()
        {
            float chance = LatePaymentChance;
            if (RelationshipLevel < 30) chance += 0.20f;
            if (AngerLevel >= 3) chance += 0.30f;
            return UnityEngine.Random.value < chance;
        }

        public float GetSuccessRate()
            => TotalDeliveries == 0 ? 0.5f : (float)SuccessfulDeliveries / TotalDeliveries;

        public string GetRelationshipEmoji()
        {
            if (RelationshipLevel >= 90) return "💎 Excelente";
            if (RelationshipLevel >= 70) return "😊 Muy Buena";
            if (RelationshipLevel >= 50) return "😐 Buena";
            if (RelationshipLevel >= 30) return "😠 Regular";
            if (RelationshipLevel >= 10) return "😤 Mala";
            return "👎 Pésima";
        }

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

        public override string ToString()
        {
            string extras = (IsVip ? " VIP" : "") + (HasActiveContract ? " CONTRATO" : "") + (IsBlacklisted ? " BLOQUEADO" : "");
            return $"{CompanyName} | {GetRelationshipEmoji()}{extras} | Éxito: {GetSuccessRate():P0}";
        }
    }
}
