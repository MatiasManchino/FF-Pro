using System.Collections.Generic;

namespace FreightForwarder.Models
{
    /// <summary>
    /// Constants.cs — Enums y constantes de balance del juego.
    /// 
    /// QUÉ ES UN NAMESPACE?
    /// Es como un "apellido" para tus clases. Evita conflictos con otras clases
    /// que tengan el mismo nombre. Para usar esta clase, escribirías:
    /// using FreightForwarder.Models;
    /// 
    /// QUÉ ES UN ENUM?
    /// Un enum (enumeración) es una lista de valores fijos. Es más legible que
    /// usar números (ej: 0 = General, 1 = Refrigerated...)
    /// </summary>
    public static class Constants
    {
        // =========================================================================
        // ENUMS ORIGINALES
        // =========================================================================
        
        /// <summary>
        /// Tipo de carga: qué mercancía se transporta.
        /// </summary>
        public enum CargoType
        {
            General,        // Carga común, sin requisitos especiales
            Refrigerated,   // Necesita cadena de frío (alimentos, medicinas)
            Dangerous,      // Materiales peligrosos (químicos, explosivos)
            Urgent,         // El cliente necesita entrega rápida
            Valuable        // Alto valor (electrónicos, joyas, obras de arte)
        }
        
        /// <summary>
        /// Modo de transporte: cómo viaja la carga.
        /// </summary>
        public enum TransportMode
        {
            Maritime,   // Barco (lento pero barato)
            Air,        // Avión (rápido pero caro)
            Land,       // Camión/tren (intermedio)
            Rail,       // Ferrocarril (bonus para grandes volúmenes)
            Multimodal  // Combinación de varios modos
        }
        
        /// <summary>
        /// Estado de la carga durante su ciclo de vida.
        /// </summary>
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
        
        /// <summary>
        /// Tipo de cliente: afecta el comportamiento de pago y negociación.
        /// </summary>
        public enum ClientType
        {
            GoodPayer,      // Paga al contado, confiable
            BadPayer,       // Se retrasa en pagos
            UrgentClient,   // Necesita rapidez, paga más
            CreditClient,   // Paga a 30-60 días
            VeryBadClient,  // Difícil, reclama siempre
            ContractClient  // Contrato a largo plazo
        }
        
        /// <summary>
        /// Calificación del agente de transporte.
        /// </summary>
        public enum AgentRating
        {
            Poor,       // Baja confiabilidad
            Regular,    // Aceptable
            Good,       // Bueno
            VeryGood,   // Muy bueno
            Excellent   // Excelente
        }
        
        /// <summary>
        /// Tipo de evento aleatorio durante el tránsito.
        /// </summary>
        public enum EventType
        {
            CustomsDelay,           // Demora por aduana
            PortCongestion,         // Puerto congestionado
            Weather,                // Clima adverso
            Damage,                 // Daño a la mercancía
            Strike,                 // Huelga de trabajadores
            DocumentationError,     // Error en papeles
            EquipmentShortage,      // Falta de contenedores
            RoadClosure,            // Ruta cortada
            AirportClosure,         // Aeropuerto cerrado
            CargoTheft,             // Robo de carga
            FuelSurcharge,          // Sobrecosto por combustible
            CarrierBankruptcy,      // El transportista quiebra
            WeightMisdeclaration,   // Peso mal declarado
            WarehouseFire,          // Incendio en almacén
            QuarantineInspection,   // Inspección fitosanitaria
            FestivityDelay,         // Feriado no laborable
            BorderDelay,            // Demora en frontera
            RejectedCargo,          // Cliente rechaza la carga
            InsuranceDispute,       // Disputa con seguro
            LaborDay                // Día del trabajador
        }
        
        // =========================================================================
        // NUEVOS ENUMS DE AGENTES (PERSONALIDAD ACTIVA)
        // =========================================================================
        
        /// <summary>
        /// Personalidad del agente. Determina su comportamiento activo.
        /// </summary>
        public enum AgentPersonality
        {
            Reliable,       // Confiable, caro, no negocia
            Cheap,          // Barato, pero a veces "pierde" cargas
            Ambitious,      // Sube precios cuando detecta desesperación
            Lazy,           // Responde lento, deja cargas olvidadas
            Friendly,       // Amigable, avisa antes de subir precios
            Elusive,        // Esquivo, desaparece por días
            Efficient,      // Eficiente pero colapsa si se sobrecarga
            Scammer,        // Estafador, cobra extras falsos
            Liar,           // Mentiroso, dice que entregó pero no
            Bipolar,        // Impredecible, un día bien otro mal
            Envious,        // Envidioso, sabotea si creces mucho
            Disappearing,   // Desaparece con la carga si quiebra
            Loyal,          // Leal, mejor precio por usar siempre
            Rival,          // Rival de otros agentes
        }
        
        /// <summary>
        /// Estado actual del agente (para eventos activos)
        /// </summary>
        public enum AgentState
        {
            Idle,           // Normal, disponible
            Overworked,     // Sobrecargado, más lento
            Stressed,       // Estresado, más propenso a errores
            Angry,          // Enojado con el jugador, sabotaje
            Greedy,         // Codicioso, busca subir precios
            Disappeared,    // Desapareció, no responde
            Bankrupt,       // En quiebra, no disponible
        }
        
        /// <summary>
        /// Relación del agente con el jugador
        /// </summary>
        public enum AgentRelationship
        {
            Enemy,      // -50 a -31
            Bad,        // -30 a -11
            Neutral,    // -10 a 10
            Good,       // 11 a 30
            Friend,     // 31 a 50
            Ally,       // 51 a 70
            Partner,    // 71 a 100
        }
        
        // =========================================================================
        // CONSTANTES DE BALANCE (NO CAMBIAR EN RUNTIME)
        // =========================================================================
        
        // --- Económicas ---
        public const int INITIAL_MONEY = 5000;              // Pesos/dólares iniciales
        public const int INITIAL_REPUTATION = 50;           // Reputación inicial (0-100)
        public const int GAME_OVER_DEBT_THRESHOLD = -2000;  // Deuda máxima permitida
        
        // --- Tiempo ---
        public const float DAY_DURATION_SECONDS = 30f;      // Segundos reales por día (x1)
        
        // --- Mercado de cargas ---
        public const int MAX_MARKET_CARGOS = 7;             // Máximo de cargas en el mercado
        public const int CARGO_EXPIRATION_DAYS = 7;         // Días hasta que expira una carga
        public const int MAX_QUOTES_PER_CARGO = 3;          // Intentos máximos de cotización
        
        // --- Negociación ---
        public const float NEGOTIATION_BASE_ACCEPTANCE = 0.15f;  // Probabilidad base (15%)
        
        // --- Eventos ---
        public const float EVENT_BASE_PROBABILITY = 0.05f;       // 5% por carga por día
        
        // --- Progresión ---
        public const int XP_PER_CARGO = 50;                 // XP por carga completada
        public const int XP_PER_LEVEL = 200;                // XP necesaria por nivel
        
        // --- Oficinas ---
        public const int OFFICE_BASE_COST = 10000;          // Costo de abrir una oficina
        public const int OFFICE_MONTHLY_COST = 100;         // Costo mensual por nivel
        public const float OFFICE_UPGRADE_MULTIPLIER = 1.5f; // Multiplicador por nivel
        public const int MAX_OFFICE_LEVEL = 5;              // Nivel máximo de oficina
        
        // =========================================================================
        // CONSTANTES DE AGENTES ACTIVOS
        // =========================================================================
        
        public const float AGENT_PRICE_SURGE_MULTIPLIER = 1.25f;     // Aumento de precio sorpresa
        public const float AGENT_TRUST_GAIN_PER_SUCCESS = 2f;        // Ganancia de confianza por éxito
        public const float AGENT_TRUST_LOSS_PER_FAILURE = 8f;        // Pérdida por fallo
        public const float AGENT_TRUST_LOSS_PER_ABANDON = 15f;       // Pérdida si abandona carga
        public const float AGENT_RELATIONSHIP_GAIN_PER_LOYALTY = 3f; // Ganancia por usar mismo agente
        public const float AGENT_MAX_OVERWORK_LOAD = 5f;             // Máximo cargas antes de colapsar
        public const int AGENT_DISAPPEAR_DAYS_MIN = 3;               // Días mínimos desaparecido
        public const int AGENT_DISAPPEAR_DAYS_MAX = 7;               // Días máximos desaparecido
        
        // =========================================================================
        // MULTIPLICADORES POR TIPO DE CARGA (afectan el valor base)
        // =========================================================================
        public static readonly Dictionary<CargoType, float> CargoValueMultipliers = 
            new Dictionary<CargoType, float>
        {
            { CargoType.General, 1.0f },
            { CargoType.Refrigerated, 1.4f },
            { CargoType.Dangerous, 1.6f },
            { CargoType.Urgent, 2.0f },
            { CargoType.Valuable, 1.8f }
        };
        
        // =========================================================================
        // MÉTODOS AUXILIARES (nombres legibles)
        // =========================================================================
        
        /// <summary>
        /// Devuelve el nombre legible de un tipo de carga.
        /// </summary>
        public static string GetCargoTypeName(CargoType type)
        {
            switch (type)
            {
                case CargoType.General: return "General";
                case CargoType.Refrigerated: return "Refrigerada";
                case CargoType.Dangerous: return "Peligrosa";
                case CargoType.Urgent: return "Urgente";
                case CargoType.Valuable: return "Valiosa";
                default: return "Desconocido";
            }
        }
        
        /// <summary>
        /// Devuelve el nombre legible de un modo de transporte.
        /// </summary>
        public static string GetTransportModeName(TransportMode mode)
        {
            switch (mode)
            {
                case TransportMode.Maritime: return "Marítimo";
                case TransportMode.Air: return "Aéreo";
                case TransportMode.Land: return "Terrestre";
                case TransportMode.Rail: return "Ferroviario";
                case TransportMode.Multimodal: return "Multimodal";
                default: return "Desconocido";
            }
        }
        
        /// <summary>
        /// Devuelve el nombre legible de un tipo de cliente.
        /// </summary>
        public static string GetClientTypeName(ClientType type)
        {
            switch (type)
            {
                case ClientType.GoodPayer: return "Buen Pagador";
                case ClientType.BadPayer: return "Mal Pagador";
                case ClientType.UrgentClient: return "Cliente Urgente";
                case ClientType.CreditClient: return "Cuenta Corriente";
                case ClientType.VeryBadClient: return "Cliente Difícil";
                case ClientType.ContractClient: return "Cliente por Contrato";
                default: return "Desconocido";
            }
        }
        
        /// <summary>
        /// Devuelve el nombre legible de una personalidad de agente.
        /// </summary>
        public static string GetAgentPersonalityName(AgentPersonality personality)
        {
            switch (personality)
            {
                case AgentPersonality.Reliable: return "Confiabilidad";
                case AgentPersonality.Cheap: return "Económico";
                case AgentPersonality.Ambitious: return "Ambicioso";
                case AgentPersonality.Lazy: return "Perezoso";
                case AgentPersonality.Friendly: return "Amigable";
                case AgentPersonality.Elusive: return "Esquivo";
                case AgentPersonality.Efficient: return "Eficiente";
                case AgentPersonality.Scammer: return "Estafador";
                case AgentPersonality.Liar: return "Mentiroso";
                case AgentPersonality.Bipolar: return "Impredecible";
                case AgentPersonality.Envious: return "Envidioso";
                case AgentPersonality.Disappearing: return "Fugaz";
                case AgentPersonality.Loyal: return "Leal";
                case AgentPersonality.Rival: return "Rival";
                default: return "Estándar";
            }
        }
        
        /// <summary>
        /// Devuelve el nombre legible de un estado de agente.
        /// </summary>
        public static string GetAgentStateName(AgentState state)
        {
            switch (state)
            {
                case AgentState.Idle: return "Disponible";
                case AgentState.Overworked: return "Sobrecargado";
                case AgentState.Stressed: return "Estresado";
                case AgentState.Angry: return "Enojado";
                case AgentState.Greedy: return "Codicioso";
                case AgentState.Disappeared: return "Desaparecido";
                case AgentState.Bankrupt: return "En Quiebra";
                default: return "Desconocido";
            }
        }
        
        /// <summary>
        /// Devuelve el nombre legible de una relación con agente.
        /// </summary>
        public static string GetAgentRelationshipName(AgentRelationship relationship)
        {
            switch (relationship)
            {
                case AgentRelationship.Enemy: return "Enemigo";
                case AgentRelationship.Bad: return "Malo";
                case AgentRelationship.Neutral: return "Neutral";
                case AgentRelationship.Good: return "Bueno";
                case AgentRelationship.Friend: return "Amigo";
                case AgentRelationship.Ally: return "Aliado";
                case AgentRelationship.Partner: return "Socio";
                default: return "Neutral";
            }
        }
    }
}