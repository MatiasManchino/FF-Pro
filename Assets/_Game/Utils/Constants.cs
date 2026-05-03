using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Utils
{
    public static class Constants
    {
        // =========================================================================
        // DINERO Y REPUTACIÓN INICIAL
        // =========================================================================
        
        public const int INITIAL_MONEY = 5000;
        public const int INITIAL_REPUTATION = 50;
        public const int GAME_OVER_DEBT_THRESHOLD = -10000;
        
        // =========================================================================
        // PROGRESIÓN
        // =========================================================================
        
        public const int XP_PER_LEVEL = 1000;
        public const int XP_PER_CARGO = 150;
        
        // =========================================================================
        // ENUMS - TIPOS DE CARGA
        // =========================================================================
        
        public enum CargoType
        {
            General,
            Refrigerated,
            Dangerous,
            Urgent,
            Valuable
        }
        
        // =========================================================================
        // ENUMS - TIPOS DE CLIENTE
        // =========================================================================
        
        public enum ClientType
        {
            GoodPayer,
            BadPayer,
            UrgentClient,
            CreditClient,
            VeryBadClient,
            ContractClient
        }
        
        // =========================================================================
        // ENUMS - ESTADO DE CARGA
        // =========================================================================
        
        public enum CargoStatus
        {
            Available,
            Quoting,
            Active,
            Completed,
            Failed,
            Expired
        }
        
        // =========================================================================
        // ENUMS - MODOS DE TRANSPORTE
        // =========================================================================
        
        public enum TransportMode
        {
            Maritime,
            Air,
            Land,
            Rail,
            Multimodal
        }
        
        // =========================================================================
        // ENUMS - PERSONALIDAD DE AGENTE
        // =========================================================================
        
        public enum AgentPersonality
        {
            Reliable,
            Cheap,
            Ambitious,
            Lazy,
            Friendly,
            Elusive,
            Efficient,
            Scammer,
            Liar,
            Bipolar,
            Envious,
            Disappearing,
            Loyal,
            Rival
        }
        
        // =========================================================================
        // ENUMS - ESTADO DE AGENTE
        // =========================================================================
        
        public enum AgentState
        {
            Idle,
            Overworked,
            Stressed,
            Angry,
            Greedy,
            Disappeared,
            Bankrupt
        }
        
        // =========================================================================
        // ENUMS - RELACIÓN CON AGENTE
        // =========================================================================
        
        public enum AgentRelationship
        {
            Partner,
            Ally,
            Friend,
            Good,
            Neutral,
            Bad,
            Enemy
        }
        
        // =========================================================================
        // ENUMS - TIPOS DE EVENTO
        // =========================================================================
        
        public enum EventType
        {
            CustomsDelay,
            PortCongestion,
            Weather,
            Damage,
            Strike,
            DocumentationError,
            EquipmentShortage,
            RoadClosure,
            AirportClosure,
            CargoTheft,
            FuelSurcharge,
            CarrierBankruptcy,
            WeightMisdeclaration,
            WarehouseFire,
            QuarantineInspection,
            FestivityDelay,
            BorderDelay,
            RejectedCargo,
            InsuranceDispute,
            LaborDay
        }
        
        // =========================================================================
        // CONFIGURACIÓN DE MERCADO
        // =========================================================================
        
        public const int MAX_MARKET_CARGOS = 7;
        public const float NEW_CARGO_CHANCE_PER_DAY = 0.3f;
        public const int CARGO_EXPIRATION_DAYS = 7;
        public const float NEGOTIATION_BASE_ACCEPTANCE = 0.15f;
        public const int MAX_QUOTES_PER_CARGO = 3;
        
        // =========================================================================
        // CONFIGURACIÓN DE AGENTES
        // =========================================================================
        
        public const float AGENT_PRICE_SURGE_MULTIPLIER = 1.5f;
        public const float AGENT_TRUST_GAIN_PER_SUCCESS = 5f;
        public const float AGENT_TRUST_LOSS_PER_FAILURE = 3f;
        public const float AGENT_TRUST_LOSS_PER_ABANDON = 10f;
        public const int AGENT_DISAPPEAR_DAYS_MIN = 3;
        public const int AGENT_DISAPPEAR_DAYS_MAX = 10;
        
        // =========================================================================
        // CONFIGURACIÓN DE EVENTOS
        // =========================================================================
        
        public const float EVENT_BASE_PROBABILITY = 0.05f;
        
        // =========================================================================
        // CONFIGURACIÓN DE TRANSPORTE
        // =========================================================================
        
        public const float BASE_SHIPPING_COST_PER_KM = 0.5f;
        public const int DEFAULT_EXPIRATION_DAYS = 7;
        
        // =========================================================================
        // MULTIPLICADORES DE TIPOS DE CARGA
        // =========================================================================
        
        public static Dictionary<CargoType, float> CargoValueMultipliers { get; private set; }
        
        static Constants()
        {
            CargoValueMultipliers = new Dictionary<CargoType, float>();
            CargoValueMultipliers[CargoType.General] = 1.0f;
            CargoValueMultipliers[CargoType.Refrigerated] = 1.3f;
            CargoValueMultipliers[CargoType.Dangerous] = 1.5f;
            CargoValueMultipliers[CargoType.Urgent] = 1.2f;
            CargoValueMultipliers[CargoType.Valuable] = 1.4f;
        }
        
        // =========================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================
        
        public static string GetCargoTypeName(CargoType type)
        {
            switch (type)
            {
                case CargoType.General: return "General";
                case CargoType.Refrigerated: return "Refrigerated";
                case CargoType.Dangerous: return "Dangerous";
                case CargoType.Urgent: return "Urgent";
                case CargoType.Valuable: return "Valuable";
                default: return "Unknown";
            }
        }
        
        public static string GetClientTypeName(ClientType type)
        {
            switch (type)
            {
                case ClientType.GoodPayer: return "Good Payer";
                case ClientType.BadPayer: return "Bad Payer";
                case ClientType.UrgentClient: return "Urgent Client";
                case ClientType.CreditClient: return "Credit Client";
                case ClientType.VeryBadClient: return "Very Bad Client";
                case ClientType.ContractClient: return "Contract Client";
                default: return "Unknown";
            }
        }
        
        public static string GetTransportModeName(TransportMode mode)
        {
            switch (mode)
            {
                case TransportMode.Maritime: return "Maritime";
                case TransportMode.Air: return "Air";
                case TransportMode.Land: return "Land";
                case TransportMode.Rail: return "Rail";
                case TransportMode.Multimodal: return "Multimodal";
                default: return "Unknown";
            }
        }
    }
}
