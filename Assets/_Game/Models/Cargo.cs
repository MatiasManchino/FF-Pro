using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    [Serializable]
    public class Cargo
    {
        // Gestiona id.
        public string Id { get; set; }
// Devuelve el cargo type
        public Constants.CargoType CargoType { get; set; }

        // Origen ciudad id.
        public string OriginCityId { get; set; }
// Destino ciudad id.
        public string DestinationCityId { get; set; }

        // Gestiona weight.
        public float Weight { get; set; }
// Gestiona volume.
        public float Volume { get; set; }
// Gestiona declared valor.
        public int DeclaredValue { get; set; }

        // Cliente id.
        public string ClientId { get; set; }
// Cliente name.
        public string ClientName { get; set; }
// Devuelve el client type
        public Constants.ClientType ClientType { get; set; }

        // Estado
        public Constants.CargoStatus Status { get; set; }
// Gestiona expiration día.
        public int ExpirationDay { get; set; }
// Día created.
        public int DayCreated { get; set; }

        // Gestiona quoted precio.
        public int QuotedPrice { get; set; }
// Gestiona final precio.
        public int FinalPrice { get; set; }
// Agente cost.
        public int AgentCost { get; set; }
// Gestiona margin.
        public float Margin { get; set; }

        // Inicio día.
        public int StartDay { get; set; }
// Gestiona estimated arrival día.
        public int EstimatedArrivalDay { get; set; }
// Gestiona actual arrival día.
        public int ActualArrivalDay { get; set; }
// Días remaining.
        public int DaysRemaining { get; set; }
// Gestiona total transit días.
        public int TotalTransitDays { get; set; }

        // Transporte y agente
        public Constants.TransportMode TransportMode { get; set; }
// Agente id.
        public string AgentId { get; set; }
// Determina si tiene insurance.
        public bool HasInsurance { get; set; }

        // Eventos encountered.
        public List<string> EventsEncountered { get; set; }
// Ruta waypoints.
        public List<string> RouteWaypoints { get; set; }

        // Transporte preferido (recomendación)
        public Constants.TransportMode PreferredTransport { get; set; }
// Gestiona transport reason.
        public string TransportReason { get; set; }

        // Determina si tiene agent intervened.
        public bool HasAgentIntervened { get; set; }
// Agente intervention type.
        public string AgentInterventionType { get; set; }
// Gestiona was abandoned by agente.
        public bool WasAbandonedByAgent { get; set; }
// Agente extra cost.
        public int AgentExtraCost { get; set; }

// Realiza cargamento
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

        public Cargo(string originCityId, string destinationCityId,
                     Constants.CargoType cargoType, Constants.ClientType clientType,
                     string clientName, float weight, float volume, int declaredValue,
                     int expirationDay, int dayCreated) : this()
        {
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
        }

// Indica si expirado.
        public bool IsExpired(int currentDay) => currentDay >= ExpirationDay && Status == Constants.CargoStatus.Available;
// Indica si active.
        public bool IsActive() => Status == Constants.CargoStatus.Active;
// Indica si completado.
        public bool IsCompleted() => Status == Constants.CargoStatus.Completed;
// Indica si fallado.
        public bool IsFailed() => Status == Constants.CargoStatus.Failed;
// Días until expiration.
        public int DaysUntilExpiration(int currentDay) => ExpirationDay - currentDay;

// Gestiona to string.
        public override string ToString()
            => $"{Constants.GetCargoTypeName(CargoType)}: {OriginCityId} → {DestinationCityId} | {Weight}t | {Status}";
    }
}
