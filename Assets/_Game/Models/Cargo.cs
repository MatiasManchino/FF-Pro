using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    [Serializable]
    public class Cargo
    {
        // Identificación
        public string Id { get; set; }
        public Constants.CargoType CargoType { get; set; }

        // Origen y Destino
        public string OriginCityId { get; set; }
        public string DestinationCityId { get; set; }

        // Características físicas
        public float Weight { get; set; }
        public float Volume { get; set; }
        public int DeclaredValue { get; set; }

        // Cliente
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public Constants.ClientType ClientType { get; set; }

        // Estado
        public Constants.CargoStatus Status { get; set; }
        public int ExpirationDay { get; set; }
        public int DayCreated { get; set; }

        // Cotización y precios
        public int QuotedPrice { get; set; }
        public int FinalPrice { get; set; }
        public int AgentCost { get; set; }
        public float Margin { get; set; }

        // Tiempos
        public int StartDay { get; set; }
        public int EstimatedArrivalDay { get; set; }
        public int ActualArrivalDay { get; set; }
        public int DaysRemaining { get; set; }
        public int TotalTransitDays { get; set; }

        // Transporte y agente
        public Constants.TransportMode TransportMode { get; set; }
        public string AgentId { get; set; }
        public bool HasInsurance { get; set; }

        // Eventos y rutas
        public List<string> EventsEncountered { get; set; }
        public List<string> RouteWaypoints { get; set; }

        // Transporte preferido (recomendación)
        public Constants.TransportMode PreferredTransport { get; set; }
        public string TransportReason { get; set; }

        // Intervención del agente
        public bool HasAgentIntervened { get; set; }
        public string AgentInterventionType { get; set; }
        public bool WasAbandonedByAgent { get; set; }
        public int AgentExtraCost { get; set; }

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

        public bool IsExpired(int currentDay) => currentDay >= ExpirationDay && Status == Constants.CargoStatus.Available;
        public bool IsActive() => Status == Constants.CargoStatus.Active;
        public bool IsCompleted() => Status == Constants.CargoStatus.Completed;
        public bool IsFailed() => Status == Constants.CargoStatus.Failed;
        public int DaysUntilExpiration(int currentDay) => ExpirationDay - currentDay;

        public override string ToString()
            => $"{Constants.GetCargoTypeName(CargoType)}: {OriginCityId} → {DestinationCityId} | {Weight}t | {Status}";
    }
}
