using System.Collections.Generic;

public static class Constants
{
    // ═══════════════════════════════════════════════════
    // ENUM: CargoType — Tipo de carga/mercancía
    // ═══════════════════════════════════════════════════
    public enum CargoType
    {
        General,        // Carga común, sin requisitos especiales
        Refrigerated,   // Necesita cadena de frío (alimentos, medicinas)
        Dangerous,      // Materiales peligrosos (químicos, explosivos)
        Urgent,         // El cliente necesita entrega rápida
        Valuable        // Alto valor (electrónicos, joyas, obras de arte)
    }

    // ═══════════════════════════════════════════════════
    // ENUM: TransportMode — Cómo viaja la carga
    // ═══════════════════════════════════════════════════
    public enum TransportMode
    {
        Maritime,   // Barco (lento pero barato) — multiplicador agente: 0.7x
        Air,        // Avión (rápido pero caro) — multiplicador agente: 2.5x
        Land,       // Camión/tren (intermedio) — multiplicador agente: 1.0x
        Rail,       // Ferrocarril (bonus volumen) — multiplicador agente: 0.8x
        Multimodal  // Combinación de varios — multiplicador agente: 1.5x
    }

    // ═══════════════════════════════════════════════════
    // ENUM: CargoStatus — Estado del ciclo de vida de la carga
    // ═══════════════════════════════════════════════════
    public enum CargoStatus
    {
        Available,      // En el mercado, esperando cotización
        Quoting,        // El jugador está cotizando
        Negotiating,    // En negociación con el cliente
        Active,         // En tránsito
        Completed,      // Entregada con éxito
        Failed,         // Falló (daño, pérdida, cancelación)
        Expired         // Nadie la cotizó a tiempo
    }

    // ═══════════════════════════════════════════════════
    // ENUM: ClientType — Personalidad del cliente
    // ═══════════════════════════════════════════════════
    public enum ClientType
    {
        GoodPayer,      // Paga al contado, confiable. Tolerante.
        BadPayer,       // Se retrasa en pagos. Baja tolerancia.
        UrgentClient,   // Necesita rapidez, paga más. Impaciente.
        CreditClient,   // Paga a 30-60 días. Exigente.
        VeryBadClient,  // Difícil, reclama siempre. ¡Cuidado!
        ContractClient  // Contrato a largo plazo. Busca estabilidad.
    }

    // ═══════════════════════════════════════════════════
    // ENUM: AgentRating — Calificación del agente
    // ═══════════════════════════════════════════════════
    public enum AgentRating
    {
        Poor,       // Baja confiabilidad
        Regular,    // Aceptable
        Good,       // Bueno
        VeryGood,   // Muy bueno
        Excellent   // Excelente
    }

    // ═══════════════════════════════════════════════════
    // ENUM: EventType — 20 tipos de eventos aleatorios
    // ═══════════════════════════════════════════════════
    public enum EventType
    {
        Storm,
        Strike,
        CustomsDelay,
        EquipmentFailure,
        Theft,
        Accident,
        FuelShortage,
        PoliticalInstability,
        NaturalDisaster,
        MarketCrash,
        HolidaySeason,
        PeakDemand,
        SupplyChainDisruption,
        RegulatoryChange,
        CurrencyFluctuation,
        Pandemic,
        CyberAttack,
        EnvironmentalIncident,
        LaborShortage,
        InfrastructureFailure
    }

    // ═══════════════════════════════════════════════════
    // CONSTANTES NUMÉRICAS DE BALANCE
    // ═══════════════════════════════════════════════════

    // ═══════════════ ECONOMÍA ═══════════════
    public const float INITIAL_MONEY = 10000f;
    public const float INITIAL_REPUTATION = 50f;
    public const float MIN_REPUTATION = 0f;
    public const float MAX_REPUTATION = 100f;
    public const float BANKRUPTCY_THRESHOLD = -2000f;

    // ═══════════════ TIEMPO ═══════════════
    public const int DAYS_PER_MONTH = 30;
    public const int HOURS_PER_DAY = 24;
    public const int GAME_START_YEAR = 2024;
    public const int GAME_START_MONTH = 1;
    public const int GAME_START_DAY = 1;

    // ═══════════════ CARGA ═══════════════
    public const int MAX_ACTIVE_CARGOS = 10;
    public const int CARGO_EXPIRY_DAYS = 7;
    public const float CARGO_VALUE_MULTIPLIER_GENERAL = 1.0f;
    public const float CARGO_VALUE_MULTIPLIER_REFRIGERATED = 1.4f;
    public const float CARGO_VALUE_MULTIPLIER_DANGEROUS = 1.6f;
    public const float CARGO_VALUE_MULTIPLIER_URGENT = 2.0f;
    public const float CARGO_VALUE_MULTIPLIER_VALUABLE = 1.8f;

    // ═══════════════ TRANSPORTE ═══════════════
    public const float TRANSPORT_MULTIPLIER_MARITIME = 0.7f;
    public const float TRANSPORT_MULTIPLIER_AIR = 2.5f;
    public const float TRANSPORT_MULTIPLIER_LAND = 1.0f;
    public const float TRANSPORT_MULTIPLIER_RAIL = 0.8f;
    public const float TRANSPORT_MULTIPLIER_MULTIMODAL = 1.5f;

    // ═══════════════ AGENTES ═══════════════
    public const int TOTAL_AGENTS = 10;
    public const float AGENT_RELIABILITY_PENALTY = 0.1f;
    public const float AGENT_COST_MULTIPLIER = 0.05f;

    // ═══════════════ CLIENTES ═══════════════
    public const float CLIENT_TOLERANCE_BASE = 0.2f;
    public const float CLIENT_NEGOTIATION_ROUNDS = 3;
    public const float CLIENT_COUNTEROFFER_CHANCE = 0.3f;

    // ═══════════════ EVENTOS ═══════════════
    public const float EVENT_PROBABILITY_BASE = 0.05f;
    public const int EVENTS_PER_DAY_MAX = 2;
    public const float EVENT_IMPACT_DURATION_DAYS = 3;

    // ═══════════════ UI ═══════════════
    public const int MARKET_REFRESH_RATE_MINUTES = 5;
    public const int NOTIFICATION_DURATION_SECONDS = 5;

    // ═══════════════════════════════════════════════════
    // DICCIONARIOS DE CONFIGURACIÓN
    // ═══════════════════════════════════════════════════

    public static readonly Dictionary<CargoType, float> CARGO_VALUE_MULTIPLIERS = new Dictionary<CargoType, float>
    {
        { CargoType.General, CARGO_VALUE_MULTIPLIER_GENERAL },
        { CargoType.Refrigerated, CARGO_VALUE_MULTIPLIER_REFRIGERATED },
        { CargoType.Dangerous, CARGO_VALUE_MULTIPLIER_DANGEROUS },
        { CargoType.Urgent, CARGO_VALUE_MULTIPLIER_URGENT },
        { CargoType.Valuable, CARGO_VALUE_MULTIPLIER_VALUABLE }
    };

    public static readonly Dictionary<TransportMode, float> TRANSPORT_MULTIPLIERS = new Dictionary<TransportMode, float>
    {
        { TransportMode.Maritime, TRANSPORT_MULTIPLIER_MARITIME },
        { TransportMode.Air, TRANSPORT_MULTIPLIER_AIR },
        { TransportMode.Land, TRANSPORT_MULTIPLIER_LAND },
        { TransportMode.Rail, TRANSPORT_MULTIPLIER_RAIL },
        { TransportMode.Multimodal, TRANSPORT_MULTIPLIER_MULTIMODAL }
    };

    public static readonly Dictionary<ClientType, float> CLIENT_PROBABILITIES = new Dictionary<ClientType, float>
    {
        { ClientType.ContractClient, 0.15f },
        { ClientType.GoodPayer, 0.15f },
        { ClientType.UrgentClient, 0.15f },
        { ClientType.CreditClient, 0.15f },
        { ClientType.BadPayer, 0.20f },
        { ClientType.VeryBadClient, 0.20f }
    };

    public static readonly Dictionary<ClientType, float> CLIENT_TOLERANCE_MODIFIERS = new Dictionary<ClientType, float>
    {
        { ClientType.GoodPayer, 0.1f },
        { ClientType.BadPayer, -0.15f },
        { ClientType.UrgentClient, -0.2f },
        { ClientType.CreditClient, -0.05f },
        { ClientType.VeryBadClient, -0.25f },
        { ClientType.ContractClient, 0.05f }
    };
}