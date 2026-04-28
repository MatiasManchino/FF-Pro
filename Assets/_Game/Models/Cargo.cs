using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    /// <summary>
    /// Cargo.cs — Modelo de una carga de mercancía.
    /// 
    /// Este es el objeto MÁS IMPORTANTE del juego. Representa cada envío
    /// que el jugador puede cotizar y transportar.
    /// 
    /// QUÉ ES GUID?
    /// GUID = Identificador único global. Cada carga tiene su propio ID único
    /// para que podamos buscarla aunque haya muchas.
    /// </summary>
    [Serializable]
    public class Cargo
    {
        // =========================================================================
        // IDENTIFICACIÓN
        // =========================================================================
        
        /// <summary>
        /// ID único de la carga (ej: "cargo_001")
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Tipo de carga (General, Refrigerada, Peligrosa, Urgente, Valiosa)
        /// </summary>
        public Constants.CargoType CargoType { get; set; }
        
        // =========================================================================
        // ORIGEN Y DESTINO
        // =========================================================================
        
        /// <summary>
        /// ID de la ciudad de origen (ej: "buenos_aires")
        /// </summary>
        public string OriginCityId { get; set; }
        
        /// <summary>
        /// ID de la ciudad de destino (ej: "miami")
        /// </summary>
        public string DestinationCityId { get; set; }
        
        // =========================================================================
        // CARACTERÍSTICAS FÍSICAS
        // =========================================================================
        
        /// <summary>
        /// Peso en toneladas (1-500 tons)
        /// </summary>
        public float Weight { get; set; }
        
        /// <summary>
        /// Volumen en metros cúbicos (1-200 m3)
        /// </summary>
        public float Volume { get; set; }
        
        /// <summary>
        /// Valor declarado de la carga en USD (1000 - 500000)
        /// </summary>
        public int DeclaredValue { get; set; }
        
        // =========================================================================
        // CLIENTE
        // =========================================================================
        
        /// <summary>
        /// ID del cliente (referencia a Client.Id)
        /// </summary>
        public string ClientId { get; set; }
        
        /// <summary>
        /// Nombre del cliente (para mostrar en UI)
        /// </summary>
        public string ClientName { get; set; }
        
        /// <summary>
        /// Tipo de cliente (BuenPagador, MalPagador, etc.)
        /// </summary>
        public Constants.ClientType ClientType { get; set; }
        
        // =========================================================================
        // ESTADO ACTUAL
        // =========================================================================
        
        /// <summary>
        /// Estado de la carga (Available, Quoting, Active, Completed, Failed, Expired)
        /// </summary>
        public Constants.CargoStatus Status { get; set; }
        
        /// <summary>
        /// Día en que expira la oferta (si no se cotiza a tiempo)
        /// </summary>
        public int ExpirationDay { get; set; }
        
        /// <summary>
        /// Día en que se creó la carga
        /// </summary>
        public int DayCreated { get; set; }
        
        // =========================================================================
        // COTIZACIÓN Y PRECIOS
        // =========================================================================
        
        /// <summary>
        /// Precio que el jugador cotizó al cliente
        /// </summary>
        public int QuotedPrice { get; set; }
        
        /// <summary>
        /// Precio final acordado (puede ser el cotizado o una contraoferta)
        /// </summary>
        public int FinalPrice { get; set; }
        
        /// <summary>
        /// Costo que cobra el agente de transporte
        /// </summary>
        public int AgentCost { get; set; }
        
        /// <summary>
        /// Margen calculado: (QuotedPrice - AgentCost) / QuotedPrice
        /// </summary>
        public float Margin { get; set; }
        
        // =========================================================================
        // TIEMPOS
        // =========================================================================
        
        /// <summary>
        /// Día en que comenzó el tránsito (cuando se aceptó la cotización)
        /// </summary>
        public int StartDay { get; set; }
        
        /// <summary>
        /// Día estimado de llegada (StartDay + días de tránsito)
        /// </summary>
        public int EstimatedArrivalDay { get; set; }
        
        /// <summary>
        /// Día real de llegada (cuando se completa o falla)
        /// </summary>
        public int ActualArrivalDay { get; set; }
        
        /// <summary>
        /// Días restantes de tránsito (decrementa cada día)
        /// </summary>
        public int DaysRemaining { get; set; }
        
        /// <summary>
        /// Total de días de tránsito estimados
        /// </summary>
        public int TotalTransitDays { get; set; }
        
        // =========================================================================
        // TRANSPORTE Y AGENTE
        // =========================================================================
        
        /// <summary>
        /// Modo de transporte elegido (Marítimo, Aéreo, Terrestre, Ferroviario, Multimodal)
        /// </summary>
        public Constants.TransportMode TransportMode { get; set; }
        
        /// <summary>
        /// ID del agente de transporte asignado
        /// </summary>
        public string AgentId { get; set; }
        
        /// <summary>
        /// ¿Tiene seguro contratado?
        /// </summary>
        public bool HasInsurance { get; set; }
        
        // =========================================================================
        // EVENTOS Y RUTAS
        // =========================================================================
        
        /// <summary>
        /// Lista de eventos que ocurrieron durante el tránsito
        /// </summary>
        public List<string> EventsEncountered { get; set; }
        
        /// <summary>
        /// Lista de IDs de ciudades en la ruta (para el mapa)
        /// </summary>
        public List<string> RouteWaypoints { get; set; }
        
        // =========================================================================
        // MODO DE TRANSPORTE PREFERIDO (generado por el sistema)
        // =========================================================================
        
        /// <summary>
        /// Modo de transporte preferido según la carga (el sistema sugiere esto)
        /// </summary>
        public Constants.TransportMode PreferredTransport { get; set; }
        
        /// <summary>
        /// Razón por la cual se recomienda este modo de transporte
        /// </summary>
        public string TransportReason { get; set; }
        
        // =========================================================================
        // INTERVENCIÓN DEL AGENTE
        // =========================================================================

        /// <summary>
        /// ¿El agente ha intervenido activamente en esta carga?
        /// </summary>
        public bool HasAgentIntervened { get; set; }

        /// <summary>
        /// Tipo de intervención del agente (si aplica)
        /// Valores posibles: "PriceSurge", "Abandoned", "Scam", "Lie", "Sabotage"
        /// </summary>
        public string AgentInterventionType { get; set; }

        /// <summary>
        /// ¿El agente abandonó esta carga?
        /// </summary>
        public bool WasAbandonedByAgent { get; set; }

        /// <summary>
        /// Extra cobrado por el agente (si aplica, ej: estafa)
        /// </summary>
        public int AgentExtraCost { get; set; }

        // =========================================================================
        // CONSTRUCTORES
        // =========================================================================
        
        /// <summary>
        /// Constructor por defecto (necesario para serialización JSON)
        /// </summary>
        public Cargo()
        {
            Id = Guid.NewGuid().ToString();
            EventsEncountered = new List<string>();
            RouteWaypoints = new List<string>();
            Status = Constants.CargoStatus.Available;
            HasInsurance = false;
            HasAgentIntervened = false;
            AgentInterventionType = string.Empty;
            WasAbandonedByAgent = false;
            AgentExtraCost = 0;
        }
        
        /// <summary>
        /// Constructor para crear una nueva carga en el mercado.
        /// </summary>
        public Cargo(string originCityId, string destinationCityId, 
                     Constants.CargoType cargoType, Constants.ClientType clientType,
                     string clientName, float weight, float volume, int declaredValue,
                     int expirationDay, int dayCreated)
        {
            Id = Guid.NewGuid().ToString();
            OriginCityId = originCityId;
            DestinationCityId = destinationCityId;
            CargoType = cargoType;
            ClientType = clientType;
            ClientName = clientName;
            Weight = weight;
            Volume = volume;
            DeclaredValue = declaredValue;
            ExpirationDay = expirationDay;
            DayCreated = dayCreated;
            Status = Constants.CargoStatus.Available;
            EventsEncountered = new List<string>();
            RouteWaypoints = new List<string>();
            HasInsurance = false;
            HasAgentIntervened = false;
            AgentInterventionType = string.Empty;
            WasAbandonedByAgent = false;
            AgentExtraCost = 0;
        }
        
        // =========================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================
        
        /// <summary>
        /// Calcula el margen de ganancia.
        /// </summary>
        public void CalculateMargin()
        {
            if (QuotedPrice > 0)
            {
                Margin = (float)(QuotedPrice - AgentCost) / QuotedPrice;
            }
            else
            {
                Margin = 0;
            }
        }
        
        /// <summary>
        /// Verifica si la carga está cerca de expirar (menos de 2 días)
        /// </summary>
        public bool IsNearExpiration(int currentDay)
        {
            return (ExpirationDay - currentDay) <= 2;
        }
        
        /// <summary>
        /// Verifica si la carga ya expiró.
        /// </summary>
        public bool IsExpired(int currentDay)
        {
            return currentDay >= ExpirationDay;
        }
        
        /// <summary>
        /// Registra una intervención del agente en esta carga.
        /// </summary>
        public void RecordAgentIntervention(string interventionType, int extraCost = 0)
        {
            HasAgentIntervened = true;
            AgentInterventionType = interventionType;
            AgentExtraCost = extraCost;
            
            if (interventionType == "Abandoned")
            {
                WasAbandonedByAgent = true;
            }
        }
        
        /// <summary>
        /// Limpia las marcas de intervención del agente.
        /// </summary>
        public void ClearAgentIntervention()
        {
            HasAgentIntervened = false;
            AgentInterventionType = string.Empty;
            WasAbandonedByAgent = false;
            AgentExtraCost = 0;
        }
        
        /// <summary>
        /// Devuelve un resumen legible de la carga.
        /// </summary>
        public override string ToString()
        {
            string interventionText = HasAgentIntervened ? $" | 🚨 Intervención: {AgentInterventionType}" : "";
            return $"[Cargo] {Constants.GetCargoTypeName(CargoType)} | " +
                   $"{OriginCityId} → {DestinationCityId} | " +
                   $"Valor: ${DeclaredValue} | Estado: {Status}{interventionText}";
        }
    }
}