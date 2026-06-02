using System;

namespace FreightForwarder.Models
{
    // Una "Cotización": la oferta de precio que el jugador (el agente de cargas) le presenta a un
    // cliente para transportar una carga. Guarda el precio ofrecido, lo que cuesta el transportista,
    // y todo el estado del regateo (aceptada, rechazada, contraoferta, vencida).
    // [Serializable] permite guardarla en disco y verla en el inspector de Unity.
    [Serializable]
    public class Quote
    {
        public string Id { get; set; }          // identificador único de esta cotización
        public string CargoId { get; set; }     // a qué carga corresponde
        public string ClientId { get; set; }    // a qué cliente se le ofrece
        public string ClientName { get; set; }  // nombre del cliente (para mostrar en pantalla)

        public int OfferedPrice { get; set; }   // precio que el jugador le cobra al cliente
        public int AgentCost { get; set; }      // lo que le cuesta al jugador el transportista
        public float Margin { get; set; }       // ganancia relativa: (precio - costo) / precio
        public Constants.TransportMode TransportMode { get; set; }  // modo de transporte (marítimo, aéreo, etc.)
        public string AgentId { get; set; }      // identificador del agente/transportista elegido
        public string AgentName { get; set; }    // nombre del agente (para mostrar)
        public int EstimatedDays { get; set; }   // días estimados de tránsito
        public bool HasInsurance { get; set; }   // si la carga lleva seguro contratado
        public int InsuranceCost { get; set; }   // costo de ese seguro

        public int AttemptNumber { get; set; }      // número de intento (se permite reintentar una cotización)
        public bool WasAccepted { get; set; }       // el cliente aceptó
        public bool WasRejected { get; set; }       // el cliente rechazó
        public bool HasCounterOffer { get; set; }   // el cliente respondió con una contraoferta
        public int CounterOfferPrice { get; set; }  // precio de esa contraoferta
        public string ClientMessage { get; set; }   // mensaje del cliente (para mostrar en pantalla)
        public int NegotiationRound { get; set; }   // ronda actual del regateo
        public int DaySent { get; set; }            // día (de juego) en que se envió
        public int ExpirationDay { get; set; }      // día en que vence si no hay respuesta
        public bool IsExpired { get; set; }         // true si venció sin respuesta

        public bool IsAgreementReached { get; set; }  // true si se cerró el trato
        public int FinalPrice { get; set; }           // precio final acordado
        public string RejectionReason { get; set; }   // motivo del rechazo (si lo hubo)

        // Constructor vacío: necesario para poder guardar y cargar la cotización desde disco.
        public Quote() { }

        // Constructor principal: arma la cotización con todos sus datos y calcula el margen y el seguro.
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
            ExpirationDay = daySent + 3;    // la cotización vence 3 días después de enviada
            NegotiationRound = 1;
            ClientMessage = string.Empty;
            RejectionReason = string.Empty;

            Margin = offeredPrice > 0 ? (float)(offeredPrice - agentCost) / offeredPrice : 0f;
            InsuranceCost = hasInsurance ? (int)(offeredPrice * 0.08f) : 0;   // el seguro cuesta 8% del precio
        }

        // ═══════════════════════════════════
        // MÉTODOS DE NEGOCIACIÓN (regateo)
        // ═══════════════════════════════════

        // El cliente acepta la oferta tal cual: se cierra el trato al precio ofrecido.
        public void Accept()
        {
            WasAccepted = true;
            IsAgreementReached = true;
            FinalPrice = OfferedPrice;
            ClientMessage = "✅ ¡Trato cerrado!";
        }

        // El cliente rechaza la oferta (opcionalmente con un motivo).
        public void Reject(string reason = "")
        {
            WasRejected = true;
            RejectionReason = reason;
            ClientMessage = $"❌ {reason}";
        }

        // El cliente devuelve una contraoferta: propone pagar otro precio (counterPrice).
        public void SetCounterOffer(int counterPrice, string message = "")
        {
            HasCounterOffer = true;
            CounterOfferPrice = counterPrice;
            ClientMessage = string.IsNullOrEmpty(message) ? $"🔄 Contraoferta: ${counterPrice:N0}" : message;
        }

        // El jugador acepta la contraoferta del cliente: el trato se cierra a ese precio
        // y se recalcula el margen con el precio nuevo.
        public void AcceptCounterOffer()
        {
            WasAccepted = true;
            IsAgreementReached = true;
            FinalPrice = CounterOfferPrice;
            Margin = OfferedPrice > 0 ? (float)(FinalPrice - AgentCost) / FinalPrice : 0f;
            ClientMessage = "✅ ¡Acepté tu contraoferta!";
        }

        // El jugador rechaza la contraoferta del cliente.
        public void RejectCounterOffer(string reason = "")
        {
            WasRejected = true;
            RejectionReason = reason;
            ClientMessage = "❌ Rechazaste mi oferta.";
        }

        // El jugador insiste con un precio nuevo (su propia contraoferta): pasa a la ronda siguiente.
        public void SendPlayerCounterOffer(int playerOffer)
        {
            OfferedPrice = playerOffer;
            Margin = OfferedPrice > 0 ? (float)(OfferedPrice - AgentCost) / OfferedPrice : 0f;
            NegotiationRound++;
            HasCounterOffer = false;
        }

        // Verifica si la cotización venció: si pasó el día límite sin que se acepte ni rechace,
        // se marca como expirada. Devuelve true si en esta llamada acaba de vencer.
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

        // Suma un intento más, si todavía no se llegó al máximo permitido por carga.
        // Devuelve true si se pudo reintentar, false si ya no quedan intentos.
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

        // La oferta es válida si el precio cubre el costo y es positivo (no se pierde plata).
        public bool IsValid() => OfferedPrice > AgentCost && OfferedPrice > 0;
        // ¿El margen llega al mínimo razonable (5%)?
        public bool HasAcceptableMargin() => Margin >= 0.05f;
        // ¿El margen es excesivo (más de 35%)? Sirve para detectar precios demasiado altos.
        public bool HasExcessiveMargin() => Margin > 0.35f;

        // ═══════════════════════════════════
        // AUXILIARES (para la interfaz)
        // ═══════════════════════════════════

        // Devuelve un texto corto con el estado actual de la cotización (para mostrar en pantalla).
        public string GetStatusText()
        {
            if (IsExpired)       return "⏰ Expirada";
            if (WasAccepted)     return "✅ Aceptada";
            if (WasRejected)     return "❌ Rechazada";
            if (HasCounterOffer) return "🔄 Contraoferta";
            return "⌛ Pendiente";
        }

        // Devuelve el color asociado a ese estado (para pintar el texto en la interfaz).
        public string GetStatusColor()
        {
            if (IsExpired)       return "orange";
            if (WasAccepted)     return "green";
            if (WasRejected)     return "red";
            if (HasCounterOffer) return "blue";
            return "white";
        }

        // ═══════════════════════════════════
        // RESULTADO DE NEGOCIACIÓN (estructura liviana)
        //
        // El motor de negociación devuelve uno de estos para indicar qué decidió el cliente:
        // aceptar, contraofertar o rechazar, junto con un mensaje y la probabilidad de aceptación.
        // ═══════════════════════════════════

        public struct NegotiationResult
        {
            public bool Accepted;            // el cliente aceptó
            public bool HasCounterOffer;     // el cliente contraofertó
            public int CounterOfferPrice;    // precio de la contraoferta
            public string ClientMessage;     // mensaje para mostrar
            public float AcceptanceChance;   // probabilidad (0..1) que tenía de aceptar
            public int NegotiationRound;     // ronda del regateo

            // Atajo para construir un resultado de "aceptación".
            public static NegotiationResult Acceptance(string message, float chance) => new NegotiationResult
            {
                Accepted = true, ClientMessage = message, AcceptanceChance = chance
            };

            // Atajo para construir un resultado de "contraoferta".
            public static NegotiationResult CounterOffer(int price, string message, float chance, int round) => new NegotiationResult
            {
                HasCounterOffer = true, CounterOfferPrice = price,
                ClientMessage = message, AcceptanceChance = chance, NegotiationRound = round
            };

            // Atajo para construir un resultado de "rechazo".
            public static NegotiationResult Rejection(string message, float chance) => new NegotiationResult
            {
                Accepted = false, HasCounterOffer = false,
                ClientMessage = message, AcceptanceChance = chance
            };
        }
    }
}
