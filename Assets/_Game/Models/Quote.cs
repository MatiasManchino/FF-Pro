using System;

namespace FreightForwarder.Models
{
    [Serializable]
    public class Quote
    {
        // Identificación
        public string Id { get; set; }
        public string CargoId { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }

        // Contenido
        public int OfferedPrice { get; set; }
        public int AgentCost { get; set; }
        public float Margin { get; set; }
        public Constants.TransportMode TransportMode { get; set; }
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public int EstimatedDays { get; set; }
        public bool HasInsurance { get; set; }
        public int InsuranceCost { get; set; }

        // Estado de negociación
        public int AttemptNumber { get; set; }
        public bool WasAccepted { get; set; }
        public bool WasRejected { get; set; }
        public bool HasCounterOffer { get; set; }
        public int CounterOfferPrice { get; set; }
        public string ClientMessage { get; set; }
        public int NegotiationRound { get; set; }
        public int DaySent { get; set; }
        public int ExpirationDay { get; set; }
        public bool IsExpired { get; set; }

        // Resultado
        public bool IsAgreementReached { get; set; }
        public int FinalPrice { get; set; }
        public string RejectionReason { get; set; }

        public Quote() { }

        public Quote(string cargoId, string clientId, string clientName,
                     int offeredPrice, int agentCost, Constants.TransportMode transportMode,
                     string agentId, string agentName, int estimatedDays,
                     int daySent, int attemptNumber = 1, bool hasInsurance = false)
        {
            Id = Guid.NewGuid().ToString();
            CargoId = cargoId;
            ClientId = clientId;
            ClientName = clientName;
            OfferedPrice = offeredPrice;
            AgentCost = agentCost;
            TransportMode = transportMode;
            AgentId = agentId;
            AgentName = agentName;
            EstimatedDays = estimatedDays;
            DaySent = daySent;
            AttemptNumber = attemptNumber;
            HasInsurance = hasInsurance;
            ExpirationDay = daySent + 3;
            NegotiationRound = 1;
            ClientMessage = string.Empty;
            RejectionReason = string.Empty;

            Margin = offeredPrice > 0 ? (float)(offeredPrice - agentCost) / offeredPrice : 0f;
            InsuranceCost = hasInsurance ? (int)(offeredPrice * 0.08f) : 0;
        }

        // ═══════════════════════════════════
        // MÉTODOS DE NEGOCIACIÓN
        // ═══════════════════════════════════

        public void Accept()
        {
            WasAccepted = true;
            IsAgreementReached = true;
            FinalPrice = OfferedPrice;
            ClientMessage = "✅ ¡Trato cerrado!";
        }

        public void Reject(string reason = "")
        {
            WasRejected = true;
            RejectionReason = reason;
            ClientMessage = $"❌ {reason}";
        }

        public void SetCounterOffer(int counterPrice, string message = "")
        {
            HasCounterOffer = true;
            CounterOfferPrice = counterPrice;
            ClientMessage = string.IsNullOrEmpty(message) ? $"🔄 Contraoferta: ${counterPrice:N0}" : message;
        }

        public void AcceptCounterOffer()
        {
            WasAccepted = true;
            IsAgreementReached = true;
            FinalPrice = CounterOfferPrice;
            Margin = OfferedPrice > 0 ? (float)(FinalPrice - AgentCost) / FinalPrice : 0f;
            ClientMessage = "✅ ¡Acepté tu contraoferta!";
        }

        public void RejectCounterOffer(string reason = "")
        {
            WasRejected = true;
            RejectionReason = reason;
            ClientMessage = "❌ Rechazaste mi oferta.";
        }

        public void SendPlayerCounterOffer(int playerOffer)
        {
            OfferedPrice = playerOffer;
            Margin = OfferedPrice > 0 ? (float)(OfferedPrice - AgentCost) / OfferedPrice : 0f;
            NegotiationRound++;
            HasCounterOffer = false;
        }

        public bool CheckExpiration(int currentDay)
        {
            if (currentDay >= ExpirationDay && !WasAccepted && !WasRejected)
            {
                IsExpired = true;
                WasRejected = true;
                ClientMessage = "⏰ La cotización expiró sin respuesta.";
                return true;
            }
            return false;
        }

        public bool IncrementAttempt()
        {
            if (AttemptNumber < Constants.MAX_QUOTES_PER_CARGO)
            {
                AttemptNumber++;
                return true;
            }
            return false;
        }

        // ═══════════════════════════════════
        // VALIDACIÓN
        // ═══════════════════════════════════

        public bool IsValid() => OfferedPrice > AgentCost && OfferedPrice > 0;
        public bool HasAcceptableMargin() => Margin >= 0.05f;
        public bool HasExcessiveMargin() => Margin > 0.35f;

        // ═══════════════════════════════════
        // AUXILIARES (UI)
        // ═══════════════════════════════════

        public string GetStatusText()
        {
            if (IsExpired)       return "⏰ Expirada";
            if (WasAccepted)     return "✅ Aceptada";
            if (WasRejected)     return "❌ Rechazada";
            if (HasCounterOffer) return "🔄 Contraoferta";
            return "⌛ Pendiente";
        }

        public string GetStatusColor()
        {
            if (IsExpired)       return "orange";
            if (WasAccepted)     return "green";
            if (WasRejected)     return "red";
            if (HasCounterOffer) return "blue";
            return "white";
        }

        // ═══════════════════════════════════
        // RESULTADO DE NEGOCIACIÓN (struct liviano)
        // ═══════════════════════════════════

        public struct NegotiationResult
        {
            public bool Accepted;
            public bool HasCounterOffer;
            public int CounterOfferPrice;
            public string ClientMessage;
            public float AcceptanceChance;
            public int NegotiationRound;

            public static NegotiationResult Acceptance(string message, float chance) => new NegotiationResult
            {
                Accepted = true, ClientMessage = message, AcceptanceChance = chance
            };

            public static NegotiationResult CounterOffer(int price, string message, float chance, int round) => new NegotiationResult
            {
                HasCounterOffer = true, CounterOfferPrice = price,
                ClientMessage = message, AcceptanceChance = chance, NegotiationRound = round
            };

            public static NegotiationResult Rejection(string message, float chance) => new NegotiationResult
            {
                Accepted = false, HasCounterOffer = false,
                ClientMessage = message, AcceptanceChance = chance
            };
        }
    }
}
