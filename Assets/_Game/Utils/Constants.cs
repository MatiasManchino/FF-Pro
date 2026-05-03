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
        // PROGRESION
        // =========================================================================
        
        public const int XP_PER_LEVEL = 1000;
        public const int XP_PER_CARGO = 150;
        
        // =========================================================================
        // ENUMS - TIPOS DE CARGA
        // =========================================================================
        
        public enum CargoType
        {
            General,        // General cargo
            Refrigerated,   // Refrigerated cargo
            Dangerous,      // Hazardous materials
            Urgent,         // Urgent delivery
            Valuable        // High value items
        }
        
        // =========================================================================
        // ENUMS - TIPOS DE CLIENTE
        // =========================================================================
        
        public enum ClientType
        {
            GoodPayer,      // Paga al contado
            BadPayer,       // Paga tarde
            UrgentClient,   // Necesita rapidez
            CreditClient,    // Paga a 30-60 días
            VeryBadClient,   // Difícil, reclama siempre
            ContractClient  // Contrato a largo plazo
        }
        
        // =========================================================================
        // ENUMS - ESTADO DE CARGA
        // =========================================================================
        
        public enum CargoStatus
        {
            Available,      // En el mercado
            Quoting,        // Cotizando
            Active,         // En tránsito
            Completed,      // Entregada
            Failed,         // Fallida
            Expired         // Expirada
        }
        
        // =========================================================================
        // ENUMS - MODOS DE TRANSPORTE
        // =========================================================================
        
        public enum TransportMode
        {
            Maritime,       // Marítimo
            Air,            // Aéreo
            Land,           // Terrestre
            Rail,           // Ferroviario
            Multimodal      // Combinado
        }
        
        // =========================================================================
        // ENUMS - PERSONALIDAD DE AGENTE
        // =========================================================================
        
        public enum AgentPersonality
        {
            Reliable,       // Confiable
            Cheap,          // Económico
            Ambitious,      // Ambicioso
            Lazy,           // Perezoso
            Friendly,       // Amigable
            Elusive,        // Esquivo
            Efficient,      // Eficiente
            Scammer,        // Estafador
            Liar,           // Mentiroso
            Bipolar,        // Bipolar
            Envious,        // Envidioso
            Disappearing,   // Desaparece
            Loyal,          // Leal
            Rival           // Rival
        }
        
        // =========================================================================
        // ENUMS - ESTADO DE AGENTE
        // =========================================================================
        
        public enum AgentState
        {
            Idle,           // Libre
            Overworked,     // Sobrecargado
            Stressed,       // Estresado
            Angry,          // Enojado
            Greedy,         // Codicioso
            Disappeared,    // Desaparecido
            Bankrupt        // En quiebra
        }
        
        // =========================================================================
        // ENUMS - RELACIÓN CON AGENTE
        // =========================================================================
        
        public enum AgentRelationship
        {
            Partner,        // Socio
            Ally,           // Aliado
            Friend,         // Amigo
            Good,           // Bueno
            Neutral,        // Neutral
            Bad,            // Malo
            Enemy           // Enemigo
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
        
        public static Dictionary<CargoType, float> CargoValueMultipliers = new Dictionary<CargoType, float>
        {
            { CargoType.General, 1.0f },
            { CargoType.Refrigerated, 1.3f },
            { CargoType.Dangerous, 1.5f },
            { CargoType.Urgent, 1.2f },
            { CargoType.Valuable, 1.4f }
        };
        
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
