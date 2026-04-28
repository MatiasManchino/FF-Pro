using System;

namespace FreightForwarder.Models
{
    /// <summary>
    /// Quote.cs — Modelo de una cotización enviada a un cliente.
    /// 
    /// Una cotización es la oferta que el jugador hace al cliente para transportar una carga.
    /// Incluye precio, modo de transporte, agente, y seguimiento de intentos de negociación.
    /// 
    /// QUÉ ES UN STRUCT?
    /// A diferencia de class, struct se copia por valor (no por referencia).
    /// Es más liviano y adecuado para objetos temporales como resultados de negociación.
    /// </summary>
    [Serializable]
    public class Quote
    {
        // =========================================================================
        // IDENTIFICACIÓN
        // =========================================================================
        
        /// <summary>
        /// ID único de la cotización
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// ID de la carga asociada
        /// </summary>
        public string CargoId { get; set; }
        
        /// <summary>
        /// ID del cliente (para referencia rápida)
        /// </summary>
        public string ClientId { get; set; }
        
        /// <summary>
        /// Nombre del cliente (para UI)
        /// </summary>
        public string ClientName { get; set; }
        
        // =========================================================================
        // CONTENIDO DE LA COTIZACIÓN
        // =========================================================================
        
        /// <summary>
        /// Precio ofrecido al cliente (en USD)
        /// </summary>
        public int OfferedPrice { get; set; }
        
        /// <summary>
        /// Costo que cobra el agente (para calcular margen)
        /// </summary>
        public int AgentCost { get; set; }
        
        /// <summary>
        /// Margen calculado: (OfferedPrice - AgentCost) / OfferedPrice
        /// </summary>
        public float Margin { get; set; }
        
        /// <summary>
        /// Modo de transporte propuesto
        /// </summary>
        public Constants.TransportMode TransportMode { get; set; }
        
        /// <summary>
        /// ID del agente propuesto
        /// </summary>
        public string AgentId { get; set; }
        
        /// <summary>
        /// Nombre del agente (para UI)
        /// </summary>
        public string AgentName { get; set; }
        
        /// <summary>
        /// Días estimados de tránsito
        /// </summary>
        public int EstimatedDays { get; set; }
        
        /// <summary>
        /// ¿Incluye seguro?
        /// </summary>
        public bool HasInsurance { get; set; }
        
        /// <summary>
        /// Costo adicional del seguro (si aplica)
        /// </summary>
        public int InsuranceCost { get; set; }
        
        // =========================================================================
        // ESTADO DE LA COTIZACIÓN
        // =========================================================================
        
        /// <summary>
        /// Número de intento (1, 2, o 3 - máximo)
        /// </summary>
        public int AttemptNumber { get; set; }
        
        /// <summary>
        /// ¿Fue aceptada por el cliente?
        /// </summary>
        public bool WasAccepted { get; set; }
        
        /// <summary>
        /// ¿Fue rechazada?
        /// </summary>
        public bool WasRejected { get; set; }
        
        /// <summary>
        /// ¿Hay una contraoferta del cliente?
        /// </summary>
        public bool HasCounterOffer { get; set; }
        
        /// <summary>
        /// Precio de la contraoferta del cliente (si aplica)
        /// </summary>
        public int CounterOfferPrice { get; set; }
        
        /// <summary>
        /// Mensaje del cliente (para UI)
        /// </summary>
        public string ClientMessage { get; set; }
        
        /// <summary>
        /// Ronda de negociación actual (1, 2, o 3)
        /// </summary>
        public int NegotiationRound { get; set; }
        
        /// <summary>
        /// Día en que se envió la cotización
        /// </summary>
        public int DaySent { get; set; }
        
        /// <summary>
        /// Día en que expira la cotización (si no hay respuesta)
        /// </summary>
        public int ExpirationDay { get; set; }
        
        /// <summary>
        /// ¿La cotización ya expiró?
        /// </summary>
        public bool IsExpired { get; set; }
        
        // =========================================================================
        // RESULTADO DE NEGOCIACIÓN
        // =========================================================================
        
        /// <summary>
        /// ¿La negociación terminó en acuerdo?
        /// </summary>
        public bool IsAgreementReached { get; set; }
        
        /// <summary>
        /// Precio final acordado (puede ser el ofrecido o la contraoferta)
        /// </summary>
        public int FinalPrice { get; set; }
        
        /// <summary>
        /// Razón del rechazo (si aplica)
        /// </summary>
        public string RejectionReason { get; set; }
        
        // =========================================================================
        // CONSTRUCTORES
        // =========================================================================
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public Quote()
        {
            Id = Guid.NewGuid().ToString();
            CargoId = string.Empty;
            ClientId = string.Empty;
            ClientName = string.Empty;
            AgentId = string.Empty;
            AgentName = string.Empty;
            ClientMessage = string.Empty;
            RejectionReason = string.Empty;
            WasAccepted = false;
            WasRejected = false;
            HasCounterOffer = false;
            IsExpired = false;
            IsAgreementReached = false;
            AttemptNumber = 1;
            NegotiationRound = 1;
            HasInsurance = false;
            InsuranceCost = 0;
        }
        
        /// <summary>
        /// Constructor para crear una nueva cotización.
        /// </summary>
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
            Margin = (float)(offeredPrice - agentCost) / offeredPrice;
            TransportMode = transportMode;
            AgentId = agentId;
            AgentName = agentName;
            EstimatedDays = estimatedDays;
            DaySent = daySent;
            ExpirationDay = daySent + 3; // Expira en 3 días
            AttemptNumber = attemptNumber;
            NegotiationRound = attemptNumber;
            HasInsurance = hasInsurance;
            InsuranceCost = hasInsurance ? (int)(offeredPrice * 0.08f) : 0;
            
            WasAccepted = false;
            WasRejected = false;
            HasCounterOffer = false;
            IsExpired = false;
            IsAgreementReached = false;
            ClientMessage = string.Empty;
            RejectionReason = string.Empty;
        }
        
        // =========================================================================
        // MÉTODOS DE NEGOCIACIÓN
        // =========================================================================
        
        /// <summary>
        /// Marca la cotización como aceptada.
        /// </summary>
        public void Accept()
        {
            WasAccepted = true;
            WasRejected = false;
            HasCounterOffer = false;
            IsAgreementReached = true;
            FinalPrice = OfferedPrice;
            ClientMessage = "✅ ¡Trato cerrado! Acepto tu cotización.";
        }
        
        /// <summary>
        /// Marca la cotización como rechazada.
        /// </summary>
        public void Reject(string reason = "")
        {
            WasAccepted = false;
            WasRejected = true;
            HasCounterOffer = false;
            IsAgreementReached = false;
            RejectionReason = string.IsNullOrEmpty(reason) ? "El cliente rechazó la oferta." : reason;
            ClientMessage = $"❌ {RejectionReason}";
        }
        
        /// <summary>
        /// Establece una contraoferta del cliente.
        /// </summary>
        public void SetCounterOffer(int counterPrice, string message = "")
        {
            HasCounterOffer = true;
            CounterOfferPrice = counterPrice;
            WasAccepted = false;
            WasRejected = false;
            ClientMessage = string.IsNullOrEmpty(message) ? 
                $"🔄 Contraoferta: ${counterPrice}. ¿Aceptas?" : 
                $"🔄 {message} Te ofrezco ${counterPrice}.";
        }
        
        /// <summary>
        /// Acepta la contraoferta del cliente.
        /// </summary>
        public void AcceptCounterOffer()
        {
            WasAccepted = true;
            WasRejected = false;
            HasCounterOffer = false;
            IsAgreementReached = true;
            FinalPrice = CounterOfferPrice;
            Margin = (float)(FinalPrice - AgentCost) / FinalPrice;
            ClientMessage = "✅ ¡Aceptaste mi contraoferta! Trato cerrado.";
        }
        
        /// <summary>
        /// Rechaza la contraoferta del cliente.
        /// </summary>
        public void RejectCounterOffer(string reason = "")
        {
            WasAccepted = false;
            WasRejected = true;
            HasCounterOffer = false;
            IsAgreementReached = false;
            RejectionReason = string.IsNullOrEmpty(reason) ? "Rechazaste la contraoferta." : reason;
            ClientMessage = $"❌ Rechazaste mi oferta. {RejectionReason}";
        }
        
        /// <summary>
        /// Envía una nueva contraoferta del jugador.
        /// </summary>
        public void SendPlayerCounterOffer(int playerOffer)
        {
            OfferedPrice = playerOffer;
            Margin = (float)(playerOffer - AgentCost) / playerOffer;
            NegotiationRound++;
            ClientMessage = $"📝 Enviaste una nueva oferta: ${playerOffer}. Esperando respuesta...";
        }
        
        /// <summary>
        /// Verifica si la cotización expiró.
        /// </summary>
        public bool CheckExpiration(int currentDay)
        {
            if (!IsExpired && currentDay >= ExpirationDay)
            {
                IsExpired = true;
                WasRejected = true;
                RejectionReason = "La cotización expiró por falta de respuesta.";
                ClientMessage = "⏰ La cotización expiró. El cliente no respondió a tiempo.";
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Incrementa el número de intento.
        /// </summary>
        public bool IncrementAttempt()
        {
            if (AttemptNumber >= Constants.MAX_QUOTES_PER_CARGO)
                return false;
            
            AttemptNumber++;
            NegotiationRound = AttemptNumber;
            return true;
        }
        
        // =========================================================================
        // MÉTODOS DE VALIDACIÓN
        // =========================================================================
        
        /// <summary>
        /// Verifica si la cotización es válida (precio > costo del agente).
        /// </summary>
        public bool IsValid()
        {
            return OfferedPrice > AgentCost && OfferedPrice > 0;
        }
        
        /// <summary>
        /// Verifica si el margen es aceptable (no demasiado bajo).
        /// </summary>
        public bool HasAcceptableMargin()
        {
            return Margin >= 0.05f; // Mínimo 5% de margen
        }
        
        /// <summary>
        /// Verifica si el margen es excesivo (puede enojar al cliente).
        /// </summary>
        public bool HasExcessiveMargin()
        {
            return Margin > 0.35f; // Más de 35% de margen
        }
        
        // =========================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================
        
        /// <summary>
        /// Obtiene el estado de la cotización como texto.
        /// </summary>
        public string GetStatusText()
        {
            if (IsExpired)
                return "⏰ Expirada";
            if (WasAccepted)
                return "✅ Aceptada";
            if (WasRejected)
                return "❌ Rechazada";
            if (HasCounterOffer)
                return "🔄 Contraoferta";
            return "⌛ Pendiente";
        }
        
        /// <summary>
        /// Obtiene el color del estado para UI.
        /// </summary>
        public string GetStatusColor()
        {
            if (IsExpired)
                return "#FF8C00"; // Naranja
            if (WasAccepted)
                return "#00C853"; // Verde
            if (WasRejected)
                return "#FF3D3D"; // Rojo
            if (HasCounterOffer)
                return "#1E90FF"; // Azul
            return "#E8EDF5"; // Blanco
        }
        
        /// <summary>
        /// Devuelve un resumen legible de la cotización.
        /// </summary>
        public override string ToString()
        {
            return $"[Quote] {ClientName} | ${OfferedPrice} | {Constants.GetTransportModeName(TransportMode)} | " +
                   $"{EstimatedDays} días | {GetStatusText()}";
        }
    }
    
    /// <summary>
    /// NegotiationResult — Resultado de una negociación (struct liviano).
    /// 
    /// Los structs son ideales para resultados temporales porque se copian por valor
    /// y no generan basura (garbage collection).
    /// </summary>
    public struct NegotiationResult
    {
        public bool Accepted;              // ¿Aceptó el cliente?
        public bool HasCounterOffer;       // ¿Hay contraoferta?
        public int CounterOfferPrice;      // Precio de la contraoferta
        public string ClientMessage;       // Mensaje del cliente
        public float AcceptanceChance;     // Probabilidad de aceptación calculada
        public int NegotiationRound;       // Ronda actual
        
        /// <summary>
        /// Constructor para resultado de aceptación.
        /// </summary>
        public static NegotiationResult Acceptance(string message, float chance)
        {
            return new NegotiationResult
            {
                Accepted = true,
                HasCounterOffer = false,
                CounterOfferPrice = 0,
                ClientMessage = message,
                AcceptanceChance = chance,
                NegotiationRound = 1
            };
        }
        
        /// <summary>
        /// Constructor para resultado de contraoferta.
        /// </summary>
        public static NegotiationResult CounterOffer(int price, string message, float chance, int round)
        {
            return new NegotiationResult
            {
                Accepted = false,
                HasCounterOffer = true,
                CounterOfferPrice = price,
                ClientMessage = message,
                AcceptanceChance = chance,
                NegotiationRound = round
            };
        }
        
        /// <summary>
        /// Constructor para resultado de rechazo.
        /// </summary>
        public static NegotiationResult Rejection(string message, float chance)
        {
            return new NegotiationResult
            {
                Accepted = false,
                HasCounterOffer = false,
                CounterOfferPrice = 0,
                ClientMessage = message,
                AcceptanceChance = chance,
                NegotiationRound = 1
            };
        }
        
        public override string ToString()
        {
            if (Accepted)
                return $"✅ Aceptado: {ClientMessage}";
            if (HasCounterOffer)
                return $"🔄 Contraoferta: ${CounterOfferPrice} - {ClientMessage}";
            return $"❌ Rechazado: {ClientMessage}";
        }
    }
}