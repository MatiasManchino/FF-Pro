using System.Collections.Generic;

namespace FreightForwarder.Models
{
    public static class Constants
    {
        // ═══════════════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════════════

        public enum CargoType
        {
            General,
            Refrigerated,
            Dangerous,
            Urgent,
            Valuable
        }

        public enum TransportMode
        {
            Maritime,
            Air,
            Land,
            Rail,
            Multimodal
        }

        public enum CargoStatus
        {
            Available,
            Quoting,
            Negotiating,
            Active,
            Completed,
            Failed,
            Expired
        }

        public enum ClientType
        {
            GoodPayer,
            BadPayer,
            UrgentClient,
            CreditClient,
            VeryBadClient,
            ContractClient
        }

        public enum AgentRating
        {
            Poor,
            Regular,
            Good,
            VeryGood,
            Excellent
        }

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

        public enum AgentRelationship
        {
            Enemy,
            Bad,
            Neutral,
            Good,
            Friend,
            Ally,
            Partner
        }

        // ═══════════════════════════════════════════════════
        // CONSTANTES ECONÓMICAS
        // ═══════════════════════════════════════════════════

        public const int INITIAL_MONEY = 5000;
        public const int INITIAL_REPUTATION = 50;
        public const int GAME_OVER_DEBT_THRESHOLD = -2000;
        // Operaciones iniciales sin adelantar el costo del transportista (período de gracia).
        // A partir de la siguiente, el costo se paga al contado al entregar (riesgo de caja / bancarrota).
        public const int PAYMENT_GRACE_OPERATIONS = 5;
        // Puntos de relación que pierde un cliente cada 2 semanas sin interacción (no baja de 50 por inactividad).
        public const float CLIENT_RELATIONSHIP_DECAY_PER_2WEEKS = 5f;

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE TIEMPO
        // ═══════════════════════════════════════════════════

        public const float DAY_DURATION_SECONDS = 30f;

        // ═══════════════════════════════════════════════════
        // CONSTANTES VISUALES (mapa / rutas)
        // ═══════════════════════════════════════════════════

        // Ancho de las líneas de ruta en el globo (marítima, aérea y futuras). 50% del original (era 5).
        public const float ROUTE_LINE_WIDTH = 2.5f;

        // Días "operando" (carga/descarga) que el vehículo pasa DETENIDO en cada terminal.
        public const int PORT_OPERATION_DAYS     = 2;   // marítimo (ya incluidos en el TotalTTDays del envío)
        public const int TERMINAL_OPERATION_DAYS = 1;   // aéreo/terrestre (se suman al tránsito)

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE MERCADO
        // ═══════════════════════════════════════════════════

        public const int MAX_MARKET_CARGOS = 7;
        public const int CARGO_EXPIRATION_DAYS = 7;
        public const int MAX_QUOTES_PER_CARGO = 3;

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE NEGOCIACIÓN
        // ═══════════════════════════════════════════════════

        public const float NEGOTIATION_BASE_ACCEPTANCE = 0.15f;

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE EVENTOS
        // ═══════════════════════════════════════════════════

        public const float EVENT_BASE_PROBABILITY = 0.05f;

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE PROGRESIÓN
        // ═══════════════════════════════════════════════════

        public const int XP_PER_CARGO = 50;
        public const int XP_PER_LEVEL = 200;

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE OFICINAS
        // ═══════════════════════════════════════════════════

        public const int OFFICE_BASE_COST = 10000;
        public const int OFFICE_MONTHLY_COST = 100;
        public const float OFFICE_UPGRADE_MULTIPLIER = 1.5f;
        public const int MAX_OFFICE_LEVEL = 5;

        // ═══════════════════════════════════════════════════
        // CONSTANTES DE AGENTES
        // ═══════════════════════════════════════════════════

        public const float AGENT_PRICE_SURGE_MULTIPLIER = 1.25f;
        public const float AGENT_TRUST_GAIN_PER_SUCCESS = 2f;
        public const float AGENT_TRUST_LOSS_PER_FAILURE = 8f;
        public const float AGENT_TRUST_LOSS_PER_ABANDON = 15f;
        public const float AGENT_RELATIONSHIP_GAIN_PER_LOYALTY = 3f;
        public const float AGENT_MAX_OVERWORK_LOAD = 5f;
        public const int AGENT_DISAPPEAR_DAYS_MIN = 3;
        public const int AGENT_DISAPPEAR_DAYS_MAX = 7;

        // ═══════════════════════════════════════════════════
        // MULTIPLICADORES POR TIPO DE CARGA
        // ═══════════════════════════════════════════════════

        public static readonly Dictionary<CargoType, float> CargoValueMultipliers = new Dictionary<CargoType, float>
        {
            { CargoType.General, 1.0f },
            { CargoType.Refrigerated, 1.4f },
            { CargoType.Dangerous, 1.6f },
            { CargoType.Urgent, 2.0f },
            { CargoType.Valuable, 1.8f }
        };

        // ═══════════════════════════════════════════════════
        // MÉTODOS DE NOMBRE (para UI)
        // Obtiene cargamento type nombre

        public static string GetCargoTypeName(CargoType type)
        {
            switch (type)
            {
                case CargoType.General:      return "General";
                case CargoType.Refrigerated: return "Refrigerada";
                case CargoType.Dangerous:    return "Peligrosa";
                case CargoType.Urgent:       return "Urgente";
                case CargoType.Valuable:     return "Valiosa";
                default:                     return "Desconocida";
            }
        }

// Obtiene transport mode nombre
        public static string GetTransportModeName(TransportMode mode)
        {
            switch (mode)
            {
                case TransportMode.Maritime:   return "Marítimo";
                case TransportMode.Air:        return "Aéreo";
                case TransportMode.Land:       return "Terrestre";
                case TransportMode.Rail:       return "Ferroviario";
                case TransportMode.Multimodal: return "Multimodal";
                default:                       return "Desconocido";
            }
        }

// Obtiene cliente type nombre
        public static string GetClientTypeName(ClientType type)
        {
            switch (type)
            {
                case ClientType.GoodPayer:      return "Buen Pagador";
                case ClientType.BadPayer:       return "Mal Pagador";
                case ClientType.UrgentClient:   return "Cliente Urgente";
                case ClientType.CreditClient:   return "Cliente Crédito";
                case ClientType.VeryBadClient:  return "Cliente Muy Difícil";
                case ClientType.ContractClient: return "Cliente Contrato";
                default:                        return "Desconocido";
            }
        }

// Obtiene agent personality nombre
        public static string GetAgentPersonalityName(AgentPersonality personality)
        {
            switch (personality)
            {
                case AgentPersonality.Reliable:     return "Confiable";
                case AgentPersonality.Cheap:        return "Económico";
                case AgentPersonality.Ambitious:    return "Ambicioso";
                case AgentPersonality.Lazy:         return "Perezoso";
                case AgentPersonality.Friendly:     return "Amigable";
                case AgentPersonality.Elusive:      return "Esquivo";
                case AgentPersonality.Efficient:    return "Eficiente";
                case AgentPersonality.Scammer:      return "Estafador";
                case AgentPersonality.Liar:         return "Mentiroso";
                case AgentPersonality.Bipolar:      return "Bipolar";
                case AgentPersonality.Envious:      return "Envidioso";
                case AgentPersonality.Disappearing: return "Fugaz";
                case AgentPersonality.Loyal:        return "Leal";
                case AgentPersonality.Rival:        return "Rival";
                default:                            return "Desconocido";
            }
        }

// Obtiene agent estado nombre
        public static string GetAgentStateName(AgentState state)
        {
            switch (state)
            {
                case AgentState.Idle:        return "Disponible";
                case AgentState.Overworked:  return "Sobrecargado";
                case AgentState.Stressed:    return "Estresado";
                case AgentState.Angry:       return "Enojado";
                case AgentState.Greedy:      return "Codicioso";
                case AgentState.Disappeared: return "Desaparecido";
                case AgentState.Bankrupt:    return "En Quiebra";
                default:                     return "Desconocido";
            }
        }

// Obtiene agent relationship nombre
        public static string GetAgentRelationshipName(AgentRelationship rel)
        {
            switch (rel)
            {
                case AgentRelationship.Enemy:   return "Enemigo";
                case AgentRelationship.Bad:     return "Malo";
                case AgentRelationship.Neutral: return "Neutral";
                case AgentRelationship.Good:    return "Bueno";
                case AgentRelationship.Friend:  return "Amigo";
                case AgentRelationship.Ally:    return "Aliado";
                case AgentRelationship.Partner: return "Socio";
                default:                        return "Neutral";
            }
        }
    }
}