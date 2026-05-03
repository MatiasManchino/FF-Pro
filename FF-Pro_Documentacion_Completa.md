# FREIGHT FORWARDER — DOCUMENTACIÓN COMPLETA DEL CÓDIGO FUENTE

## Información General del Proyecto

- **Motor:** Unity 6000.4.4f1
- **Lenguaje:** C# (.NET)
- **Tipo de Juego:** Simulador de logística / freight forwarding
- **Estructura de Archivos:** `Assets/_Game/` (Models, Managers, UI, Map, Utils, Core)
- **Total de Archivos C#:** 31
- **Total de Líneas de Código:** 9,152

## Descripción General del Juego

Freight Forwarder es un simulador de gestión logística donde el jugador administra una empresa de freight forwarding (intermediario de transporte de carga). El jugador debe:

1. **Aceptar cargas del mercado** — Cargas con diferentes tipos (general, refrigerada, peligrosa, urgente, valiosa), rutas, y clientes aparecen en un mercado dinámico.
2. **Cotizar precios a los clientes** — Elegir un modo de transporte (marítimo, aéreo, terrestre), un agente de transporte, y establecer un precio que equilibre margen de ganancia con satisfacción del cliente.
3. **Negociar con clientes** — Los clientes tienen personalidades activas y pueden aceptar, rechazar, o hacer contraofertas.
4. **Gestionar agentes de transporte** — 10 agentes con personalidades únicas (confiable, estafador, perezoso, etc.) que toman decisiones activas (subir precios, abandonar cargas, desaparecer).
5. **Manejar eventos aleatorios** — Más de 20 eventos contextuales (huelgas, tormentas, robos, inspecciones) que afectan las cargas en tránsito.
6. **Expandir operaciones** — Desbloquear nuevas ciudades y oficinas alrededor del mundo.
7. **Mantener reputación y finanzas** — El juego termina si el dinero baja de -$2,000 o la reputación llega a 0.

## Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                    GameBootstrapper (Core)                    │
│   Orquesta la inicialización de todos los managers           │
└──────────────┬───────────────────────────────────────────────┘
               │ Inicializa en orden:
               ▼
┌──────────────────────────────────────────────────────────────┐
│                      MANAGERS (Singletons)                    │
│                                                               │
│  TimeManager ──► EconomyManager ──► ClientManager             │
│       │              │                    │                    │
│       ▼              ▼                    ▼                    │
│  AgentManager    CargoManager        EventManager             │
│       │              │                    │                    │
│       ▼              ▼                    ▼                    │
│  SaveManager     GameManager         SunController            │
└──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────┐
│                         MODELOS                               │
│  Agent, Cargo, Client, Quote, WorldCity, GameEvent, SaveData  │
│  Constants (Enums + Balance), CityDatabase, Singleton<T>      │
└──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────┐
│                     UI (UIElements/UIToolkit)                  │
│  GameUI, MarketPanel, QuotePanel, ActiveCargosPanel           │
│  AgentsPanel, FinancesPanel, OfficesPanel                      │
└──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────┐
│                      MAPA 3D                                  │
│  WorldMap, CameraController, CityMarker, RouteRenderer        │
└──────────────────────────────────────────────────────────────┘
```

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 1: MODELOS (Assets/_Game/Models/)
# ═══════════════════════════════════════════════════════════════

---

## 1.1 — Constants.cs (339 líneas)

**Ruta:** `Assets/_Game/Models/Constants.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System.Collections.Generic`

### Propósito
Contiene TODOS los enums de balance del juego, las constantes numéricas de configuración, y los multiplicadores que definen la economía y mecánicas del juego. Es una clase `static` (no se instancia, se usa directamente como `Constants.INITIAL_MONEY`).

### Código Completo: Enums

```csharp
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
```

**Explicación detallada:**
- `General` — La carga más común (40% de probabilidad de aparición). No tiene requisitos especiales de manejo. Multiplicador de valor: 1.0x.
- `Refrigerated` — Necesita cadena de frío. Más cara (multiplicador 1.4x). Prioriza transporte rápido para distancias largas para preservar la cadena de frío.
- `Dangerous` — Materiales peligrosos. Multiplicador 1.6x. Prioriza transporte marítimo porque tiene menos restricciones regulatorias.
- `Urgent` — Entrega rápida requerida. Multiplicador 2.0x (el más alto). Prioriza transporte aéreo.
- `Valuable` — Alto valor declarado. Multiplicador 1.8x. Prioriza aéreo por seguridad.

```csharp
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
```

**Explicación detallada:**
- `Maritime` — El más económico (multiplicador 0.7x sobre precio base). Requiere que ambas ciudades tengan puerto (`HasPort`). Ideal para cargas peligrosas.
- `Air` — El más caro (2.5x) pero más rápido. Requiere que ambas ciudades tengan aeropuerto (`HasAirport`). Ideal para cargas urgentes y valiosas.
- `Land` — Precio intermedio (1.0x). Requiere que ambas ciudades sean hubs terrestres (`IsLandHub`) Y estén en la misma zona terrestre (`LandZone`). Ideal para distancias cortas (<3000 km).
- `Rail` — Ligeramente más barato que terrestre (0.8x). Bonus para grandes volúmenes.
- `Multimodal` — Combinación (1.5x). Más flexible pero más caro.

```csharp
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
```

**Flujo de estados:**
```
Available → Quoting → Negotiating → Active → Completed
                                         └──→ Failed
Available → Expired (si pasan 7 días sin cotización)
```

```csharp
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
```

**Probabilidades de aparición en mercado:**
- `ContractClient`: 15%
- `GoodPayer`: 15%
- `UrgentClient`: 15%
- `CreditClient`: 15%
- `BadPayer`: 20%
- `VeryBadClient`: 20%

**Efecto en aceptación de cotización:**
- `UrgentClient`: +20% probabilidad (pagan lo que sea)
- `GoodPayer`: +10%
- `ContractClient`: +5%
- `CreditClient`: -5%
- `BadPayer`: -15%
- `VeryBadClient`: -25%

```csharp
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
```

```csharp
    // ═══════════════════════════════════════════════════
    // ENUM: EventType — 20 tipos de eventos aleatorios
    // ═══════════════════════════════════════════════════
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
```

```csharp
    // ═══════════════════════════════════════════════════
    // ENUM: AgentPersonality — 14 personalidades de agentes
    // ═══════════════════════════════════════════════════
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
```

**Efecto de cada personalidad en el gameplay:**
- `Reliable` — Nunca falla, pero cobra caro (BasePriceMultiplier alto). No abandona cargas.
- `Cheap` — Barato pero con baja confiabilidad. Puede "perder" cargas.
- `Ambitious` — Activa "price surge" (subida de precio sorpresa) un 10% de los días. Multiplicador 1.25x durante el surge.
- `Lazy` — Responde lento. Puede abandonar cargas si tiene muchas (15% chance si sobrecargado).
- `Friendly` — Da descuentos por lealtad. Bonus de confianza más rápido.
- `Elusive` — Puede desaparecer por 3-7 días sin aviso. No se puede usar durante ese período.
- `Efficient` — Muy rápido pero si se sobrecarga (>MaxCapacity), se pone en estado Overworked y va 30% más lento.
- `Scammer` — 25% chance de cobrar un extra falso ($100-$500). Más probable si el jugador confía mucho en él.
- `Liar` — 15% chance de mentir sobre el estado de la entrega. Más probable si el jugador confía.
- `Bipolar` — Impredecible. Comportamiento aleatorio entre bueno y malo.
- `Envious` — 20% chance de sabotaje si el jugador tiene nivel ≥5 y la relación es neutral o peor. 15% si nunca usó al agente y nivel ≥3.
- `Disappearing` — Puede ir a quiebra (1% chance por entrega después de 20 entregas totales).
- `Loyal` — Da mejor precio por usar siempre al mismo agente. -3% por cada 5 entregas consecutivas.
- `Rival` — 30% chance de sabotear a otro agente rival.

```csharp
    // ═══════════════════════════════════════════════════
    // ENUM: AgentState — 7 estados posibles del agente
    // ═══════════════════════════════════════════════════
    public enum AgentState
    {
        Idle,           // Normal, disponible
        Overworked,     // Sobrecargado, 30% más lento
        Stressed,       // Estresado, 15% más lento, más propenso a errores
        Angry,          // Enojado con el jugador, 40% más lento, riesgo de sabotaje
        Greedy,         // Codicioso, busca subir precios
        Disappeared,    // Desapareció, no disponible por X días
        Bankrupt,       // En quiebra, eliminado permanentemente
    }
```

```csharp
    // ═══════════════════════════════════════════════════
    // ENUM: AgentRelationship — Relación agente-jugador
    // ═══════════════════════════════════════════════════
    public enum AgentRelationship
    {
        Enemy,      // Promedio confianza -50 a -31 → precios +30%
        Bad,        // -30 a -11 → precios +15%
        Neutral,    // -10 a 10 → precios normales
        Good,       // 11 a 30
        Friend,     // 31 a 50
        Ally,       // 51 a 70 → precios -5%
        Partner,    // 71 a 100 → precios -5%
    }
```

### Código Completo: Constantes de Balance

```csharp
    // ═══════════════════════════════════════════════════
    // CONSTANTES ECONÓMICAS
    // ═══════════════════════════════════════════════════
    public const int INITIAL_MONEY = 5000;              // Dinero inicial del jugador
    public const int INITIAL_REPUTATION = 50;           // Reputación inicial (0-100)
    public const int GAME_OVER_DEBT_THRESHOLD = -2000;  // Si el dinero baja de esto, game over

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE TIEMPO
    // ═══════════════════════════════════════════════════
    public const float DAY_DURATION_SECONDS = 30f;      // 30 segundos reales = 1 día de juego

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE MERCADO
    // ═══════════════════════════════════════════════════
    public const int MAX_MARKET_CARGOS = 7;             // Máximo 7 cargas simultáneas en mercado
    public const int CARGO_EXPIRATION_DAYS = 7;         // Cargas expiran en 7 días si no se cotizan
    public const int MAX_QUOTES_PER_CARGO = 3;          // Máximo 3 intentos de cotización por carga

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE NEGOCIACIÓN
    // ═══════════════════════════════════════════════════
    public const float NEGOTIATION_BASE_ACCEPTANCE = 0.15f;  // 15% probabilidad base de aceptación

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE EVENTOS
    // ═══════════════════════════════════════════════════
    public const float EVENT_BASE_PROBABILITY = 0.05f;       // 5% chance por carga por día

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE PROGRESIÓN
    // ═══════════════════════════════════════════════════
    public const int XP_PER_CARGO = 50;                 // 50 XP por carga completada
    public const int XP_PER_LEVEL = 200;                // 200 XP base por nivel (se multiplica)

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE OFICINAS
    // ═══════════════════════════════════════════════════
    public const int OFFICE_BASE_COST = 10000;          // $10,000 para abrir oficina
    public const int OFFICE_MONTHLY_COST = 100;         // $100/mes por nivel de oficina
    public const float OFFICE_UPGRADE_MULTIPLIER = 1.5f; // Costo sube 1.5x por nivel
    public const int MAX_OFFICE_LEVEL = 5;              // Nivel máximo 5

    // ═══════════════════════════════════════════════════
    // CONSTANTES DE AGENTES ACTIVOS
    // ═══════════════════════════════════════════════════
    public const float AGENT_PRICE_SURGE_MULTIPLIER = 1.25f;     // +25% en surge
    public const float AGENT_TRUST_GAIN_PER_SUCCESS = 2f;        // +2 confianza por éxito
    public const float AGENT_TRUST_LOSS_PER_FAILURE = 8f;        // -8 confianza por fallo
    public const float AGENT_TRUST_LOSS_PER_ABANDON = 15f;       // -15 si abandona carga
    public const float AGENT_RELATIONSHIP_GAIN_PER_LOYALTY = 3f; // +3 por usar mismo agente
    public const float AGENT_MAX_OVERWORK_LOAD = 5f;             // Colapsa con >5 cargas
    public const int AGENT_DISAPPEAR_DAYS_MIN = 3;               // Mínimo 3 días desaparecido
    public const int AGENT_DISAPPEAR_DAYS_MAX = 7;               // Máximo 7 días desaparecido
```

### Código Completo: Diccionario de Multiplicadores

```csharp
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
```

### Código Completo: Métodos Helper

```csharp
    // ═══════════════════════════════════════════════════
    // MÉTODOS DE NOMBRE (para UI)
    // ═══════════════════════════════════════════════════

    public static string GetCargoTypeName(CargoType type)
    {
        switch (type)
        {
            case CargoType.General: return "General";
            case CargoType.Refrigerated: return "Refrigerada";
            case CargoType.Dangerous: return "Peligrosa";
            case CargoType.Urgent: return "Urgente";
            case CargoType.Valuable: return "Valiosa";
            default: return "Desconocida";
        }
    }

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

    public static string GetClientTypeName(ClientType type)
    {
        switch (type)
        {
            case ClientType.GoodPayer: return "Buen Pagador";
            case ClientType.BadPayer: return "Mal Pagador";
            case ClientType.UrgentClient: return "Cliente Urgente";
            case ClientType.CreditClient: return "Cliente Crédito";
            case ClientType.VeryBadClient: return "Cliente Muy Difícil";
            case ClientType.ContractClient: return "Cliente Contrato";
            default: return "Desconocido";
        }
    }

    public static string GetAgentPersonalityName(AgentPersonality personality)
    {
        switch (personality)
        {
            case AgentPersonality.Reliable: return "Confiable";
            case AgentPersonality.Cheap: return "Económico";
            case AgentPersonality.Ambitious: return "Ambicioso";
            case AgentPersonality.Lazy: return "Perezoso";
            case AgentPersonality.Friendly: return "Amigable";
            case AgentPersonality.Elusive: return "Esquivo";
            case AgentPersonality.Efficient: return "Eficiente";
            case AgentPersonality.Scammer: return "Estafador";
            case AgentPersonality.Liar: return "Mentiroso";
            case AgentPersonality.Bipolar: return "Bipolar";
            case AgentPersonality.Envious: return "Envidioso";
            case AgentPersonality.Disappearing: return "Fugaz";
            case AgentPersonality.Loyal: return "Leal";
            case AgentPersonality.Rival: return "Rival";
            default: return "Desconocido";
        }
    }

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

    public static string GetAgentRelationshipName(AgentRelationship rel)
    {
        switch (rel)
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
```

---

## 1.2 — Cargo.cs (350 líneas)

**Ruta:** `Assets/_Game/Models/Cargo.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`, `System.Collections.Generic`
**Atributo:** `[Serializable]` — Permite serialización JSON para guardado.

### Propósito
Es el modelo MÁS IMPORTANTE del juego. Representa cada envío de mercancía que el jugador puede cotizar y transportar. Cada carga tiene un ciclo de vida completo: aparece en el mercado → es cotizada → entra en tránsito → se completa o falla.

### Todas las Propiedades

#### Identificación
```csharp
public string Id { get; set; }                          // GUID único (ej: "a1b2c3d4-...")
public Constants.CargoType CargoType { get; set; }      // Tipo de carga (General, Refrigerated, etc.)
```

#### Origen y Destino
```csharp
public string OriginCityId { get; set; }                // ID ciudad origen (ej: "buenos_aires")
public string DestinationCityId { get; set; }           // ID ciudad destino (ej: "miami")
```

#### Características Físicas
```csharp
public float Weight { get; set; }                       // Peso en toneladas (1-500)
public float Volume { get; set; }                       // Volumen en m³ (1-200)
public int DeclaredValue { get; set; }                  // Valor declarado en USD (1,000-500,000)
```

#### Cliente
```csharp
public string ClientId { get; set; }                    // Referencia a Client.Id
public string ClientName { get; set; }                  // Nombre para UI
public Constants.ClientType ClientType { get; set; }    // Tipo de cliente
```

#### Estado
```csharp
public Constants.CargoStatus Status { get; set; }       // Estado actual
public int ExpirationDay { get; set; }                  // Día en que expira la oferta
public int DayCreated { get; set; }                     // Día de creación
```

#### Cotización y Precios
```csharp
public int QuotedPrice { get; set; }                    // Precio cotizado al cliente
public int FinalPrice { get; set; }                     // Precio final (cotizado o contraoferta)
public int AgentCost { get; set; }                      // Lo que cobra el agente
public float Margin { get; set; }                       // Margen: (QuotedPrice - AgentCost) / QuotedPrice
```

#### Tiempos
```csharp
public int StartDay { get; set; }                       // Día de inicio del tránsito
public int EstimatedArrivalDay { get; set; }            // Día estimado de llegada
public int ActualArrivalDay { get; set; }               // Día real de llegada
public int DaysRemaining { get; set; }                  // Días restantes (decrementa cada día)
public int TotalTransitDays { get; set; }               // Total días estimados de tránsito
```

#### Transporte y Agente
```csharp
public Constants.TransportMode TransportMode { get; set; }  // Modo elegido
public string AgentId { get; set; }                         // ID del agente asignado
public bool HasInsurance { get; set; }                      // ¿Tiene seguro?
```

#### Eventos y Rutas
```csharp
public List<string> EventsEncountered { get; set; }     // IDs de eventos ocurridos
public List<string> RouteWaypoints { get; set; }        // Ciudades en la ruta (para mapa)
```

#### Transporte Preferido (generado por el sistema)
```csharp
public Constants.TransportMode PreferredTransport { get; set; }  // Modo recomendado
public string TransportReason { get; set; }                       // Razón de la recomendación
```

#### Intervención del Agente
```csharp
public bool HasAgentIntervened { get; set; }             // ¿El agente intervino activamente?
public string AgentInterventionType { get; set; }       // "PriceSurge", "Abandoned", "Scam", "Lie", "Sabotage"
public bool WasAbandonedByAgent { get; set; }           // ¿El agente abandonó esta carga?
public int AgentExtraCost { get; set; }                 // Extra cobrado por estafa
```

### Constructores

#### Constructor por defecto (serialización JSON)
```csharp
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
```

#### Constructor para nueva carga en mercado
```csharp
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
```

### Métodos

#### CalculateMargin()
```csharp
public void CalculateMargin()
{
    if (QuotedPrice > 0)
        Margin = (float)(QuotedPrice - AgentCost) / QuotedPrice;
    else
        Margin = 0;
}
```
Calcula el margen de ganancia como porcentaje. Ejemplo: QuotedPrice=$10,000, AgentCost=$7,000 → Margin = 0.30 (30%).

#### IsNearExpiration(int currentDay)
```csharp
public bool IsNearExpiration(int currentDay)
{
    return (ExpirationDay - currentDay) <= 2;
}
```
Retorna `true` si quedan 2 días o menos antes de la expiración. Usado por la UI para mostrar advertencia naranja.

#### IsExpired(int currentDay)
```csharp
public bool IsExpired(int currentDay)
{
    return currentDay >= ExpirationDay;
}
```
Retorna `true` si la carga ya expiró. El CargoManager la mueve a la lista de fallidas.

#### RecordAgentIntervention(string interventionType, int extraCost = 0)
```csharp
public void RecordAgentIntervention(string interventionType, int extraCost = 0)
{
    HasAgentIntervened = true;
    AgentInterventionType = interventionType;
    AgentExtraCost = extraCost;
    if (interventionType == "Abandoned")
        WasAbandonedByAgent = true;
}
```
Registra que un agente intervino en esta carga. Tipos posibles: "PriceSurge", "Abandoned", "Scam", "Lie", "Sabotage".

#### ClearAgentIntervention()
```csharp
public void ClearAgentIntervention()
{
    HasAgentIntervened = false;
    AgentInterventionType = string.Empty;
    WasAbandonedByAgent = false;
    AgentExtraCost = 0;
}
```

#### ToString()
```csharp
public override string ToString()
{
    string interventionText = HasAgentIntervened ? $" | Intervención: {AgentInterventionType}" : "";
    return $"[Cargo] {Constants.GetCargoTypeName(CargoType)} | " +
           $"{OriginCityId} → {DestinationCityId} | " +
           $"Valor: ${DeclaredValue} | Estado: {Status}{interventionText}";
}
```

---

## 1.3 — Agent.cs (882 líneas)

**Ruta:** `Assets/_Game/Models/Agent.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`, `System.Collections.Generic`
**Atributo:** `[Serializable]`

### Propósito
Modelo de agente de transporte con PERSONALIDAD ACTIVA. Los agentes NO son pasivos — tienen personalidad, memoria de interacciones con el jugador, toman decisiones activas (subir precios, abandonar cargas, estafar), y mantienen una relación bidireccional con el jugador.

### Todas las Propiedades

#### Identificación Básica
```csharp
public string Id { get; set; }                          // "maersk", "fedex", etc.
public string Name { get; set; }                        // "Maersk Logistics"
public string BaseCityId { get; set; }                  // Ciudad sede: "rotterdam"
```

#### Personalidad
```csharp
public Constants.AgentPersonality Personality { get; set; }  // Determina comportamiento
public string PersonalityDescription { get; set; }            // Descripción para UI
```

#### Servicios Ofrecidos
```csharp
public List<Constants.TransportMode> TransportModes { get; set; }    // Modos disponibles
public List<Constants.CargoType> SpecializedCargoTypes { get; set; } // Especializaciones
public List<string> OperatingRegions { get; set; }                   // Regiones (vacío = global)
```

#### Factores de Precio y Velocidad
```csharp
public float BasePriceMultiplier { get; set; }          // 1.0 = normal (menor = más barato)
public float BaseSpeedMultiplier { get; set; }          // 1.0 = normal (mayor = más rápido)
public float Reliability { get; set; }                  // 0-1 (0.95 = 95% confiable)
```

#### Precio Dinámico
```csharp
public float CurrentPriceMultiplier { get; set; }       // Puede cambiar por eventos
public bool IsPriceSurgeActive { get; set; }            // ¿En modo subida de precio?
public int PriceSurgeDaysRemaining { get; set; }        // Días restantes de surge
```

#### Relación con el Jugador (Memoria Bidireccional)
```csharp
public float PlayerTrust { get; set; }                  // Confianza jugador→agente (0-100)
public float AgentTrust { get; set; }                   // Confianza agente→jugador (0-100)
public Constants.AgentRelationship Relationship { get; set; }  // Relación calculada
```

#### Historial de Trabajo
```csharp
public int TotalDeliveries { get; set; }                // Total entregas
public int SuccessfulDeliveries { get; set; }           // Exitosas
public int FailedDeliveries { get; set; }               // Fallidas
public int AbandonedDeliveries { get; set; }            // Abandonadas
public int ConsecutiveDeliveries { get; set; }          // Seguidas con éxito
```

#### Estado Actual
```csharp
public Constants.AgentState CurrentState { get; set; }  // Idle, Overworked, etc.
public int CurrentLoad { get; set; }                    // Cargas activas actualmente
public int MaxCapacity { get; set; }                    // Capacidad máxima (3-8)
public bool IsAvailable => CurrentState != Constants.AgentState.Disappeared
                        && CurrentState != Constants.AgentState.Bankrupt;
```

#### Tracking Temporal
```csharp
public int DaysSinceLastUse { get; set; }               // Días sin ser usado
public int DaysUntilReturn { get; set; }                // Días hasta que vuelve (si desapareció)
public bool WasAbandonedByPlayer { get; set; }          // ¿El jugador lo abandonó?
```

### Constructor Completo
```csharp
public Agent(string id, string name, string baseCityId,
             Constants.AgentPersonality personality,
             List<Constants.TransportMode> transportModes,
             float basePriceMultiplier, float baseSpeedMultiplier,
             float reliability, int maxCapacity)
{
    Id = id;
    Name = name;
    BaseCityId = baseCityId;
    Personality = personality;
    PersonalityDescription = GetPersonalityDescription(personality);
    TransportModes = transportModes;
    SpecializedCargoTypes = new List<Constants.CargoType>();
    OperatingRegions = new List<string>();
    BasePriceMultiplier = basePriceMultiplier;
    BaseSpeedMultiplier = baseSpeedMultiplier;
    Reliability = reliability;
    MaxCapacity = maxCapacity;
    CurrentPriceMultiplier = 1.0f;
    PlayerTrust = 50;
    AgentTrust = 50;
    Relationship = Constants.AgentRelationship.Neutral;
    CurrentState = Constants.AgentState.Idle;
    CurrentLoad = 0;
    // ... demás inicializaciones en 0/false
}
```

### Métodos de Decisión Activa

#### TriggerPriceSurge()
```csharp
public void TriggerPriceSurge()
{
    if (Personality != Constants.AgentPersonality.Ambitious) return;
    IsPriceSurgeActive = true;
    CurrentPriceMultiplier = Constants.AGENT_PRICE_SURGE_MULTIPLIER; // 1.25
    PriceSurgeDaysRemaining = UnityEngine.Random.Range(2, 5);
}
```
Solo agentes `Ambitious`. Activa subida de precio de 25% por 2-5 días.

#### DecideToAbandonCargo()
```csharp
public bool DecideToAbandonCargo()
{
    // Solo personalidades Lazy o Cheap abandonan
    if (Personality != Constants.AgentPersonality.Lazy &&
        Personality != Constants.AgentPersonality.Cheap)
        return false;

    float abandonChance = 0.05f; // 5% base
    if (CurrentLoad > MaxCapacity) abandonChance += 0.10f;  // Sobrecargado: +10%
    if (AgentTrust < 30) abandonChance += 0.10f;            // Baja confianza: +10%
    if (Personality == Constants.AgentPersonality.Lazy && CurrentLoad >= 3)
        abandonChance += 0.15f;  // Perezoso con 3+ cargas: +15%

    return UnityEngine.Random.value < abandonChance;
}
```

#### DecideToDisappear()
```csharp
public (bool willDisappear, int days) DecideToDisappear()
{
    if (Personality != Constants.AgentPersonality.Elusive &&
        Personality != Constants.AgentPersonality.Disappearing)
        return (false, 0);

    float disappearChance = 0.05f;
    if (DaysSinceLastUse > 10) disappearChance += 0.10f;
    if (Personality == Constants.AgentPersonality.Disappearing)
        disappearChance += 0.05f;

    if (UnityEngine.Random.value < disappearChance)
    {
        int days = UnityEngine.Random.Range(
            Constants.AGENT_DISAPPEAR_DAYS_MIN,  // 3
            Constants.AGENT_DISAPPEAR_DAYS_MAX   // 7
        );
        return (true, days);
    }
    return (false, 0);
}
```

#### DecideToScam(int baseCost)
```csharp
public (bool willScam, int extraCost) DecideToScam(int baseCost)
{
    if (Personality != Constants.AgentPersonality.Scammer) return (false, 0);
    float scamChance = 0.25f;
    if (PlayerTrust > 70) scamChance += 0.15f;  // Más confianza = más aprovecha
    if (CurrentState == Constants.AgentState.Stressed) scamChance += 0.10f;
    if (UnityEngine.Random.value < scamChance)
    {
        int extra = UnityEngine.Random.Range(100, 500);
        return (true, extra);
    }
    return (false, 0);
}
```

#### DecideToLie()
```csharp
public bool DecideToLie()
{
    if (Personality != Constants.AgentPersonality.Liar) return false;
    float lieChance = 0.15f;
    if (PlayerTrust > 60) lieChance += 0.10f;
    return UnityEngine.Random.value < lieChance;
}
```

#### DecideToSabotage(int playerLevel)
```csharp
public bool DecideToSabotage(int playerLevel)
{
    if (Personality != Constants.AgentPersonality.Envious) return false;
    if (playerLevel >= 5 && Relationship <= Constants.AgentRelationship.Neutral)
        return UnityEngine.Random.value < 0.20f;
    if (TotalDeliveries == 0 && playerLevel >= 3)
        return UnityEngine.Random.value < 0.15f;
    return false;
}
```

#### DecideToBeCompetitive(string rivalAgentId)
```csharp
public bool DecideToBeCompetitive(string rivalAgentId)
{
    if (Personality != Constants.AgentPersonality.Rival) return false;
    return UnityEngine.Random.value < 0.30f;  // 30% chance
}
```

### Métodos de Relación

#### RecordDelivery(bool wasSuccessful, bool wasAbandoned = false)
```csharp
public void RecordDelivery(bool wasSuccessful, bool wasAbandoned = false)
{
    TotalDeliveries++;
    CurrentLoad = Math.Max(0, CurrentLoad - 1);
    DaysSinceLastUse = 0;

    if (wasAbandoned)
    {
        AbandonedDeliveries++;
        FailedDeliveries++;
        ConsecutiveDeliveries = 0;
        AgentTrust -= Constants.AGENT_TRUST_LOSS_PER_ABANDON;      // -15
        PlayerTrust -= Constants.AGENT_TRUST_LOSS_PER_ABANDON / 2; // -7.5
    }
    else if (wasSuccessful)
    {
        SuccessfulDeliveries++;
        ConsecutiveDeliveries++;
        PlayerTrust = Math.Min(100, PlayerTrust + Constants.AGENT_TRUST_GAIN_PER_SUCCESS);   // +2
        AgentTrust = Math.Min(100, AgentTrust + Constants.AGENT_TRUST_GAIN_PER_SUCCESS / 2); // +1
    }
    else
    {
        FailedDeliveries++;
        ConsecutiveDeliveries = 0;
        PlayerTrust = Math.Max(0, PlayerTrust - Constants.AGENT_TRUST_LOSS_PER_FAILURE);     // -8
        AgentTrust = Math.Max(0, AgentTrust - Constants.AGENT_TRUST_LOSS_PER_FAILURE / 2);   // -4
    }

    UpdateRelationship();

    // Lealtad: 5 entregas consecutivas → -3% en precio
    if (ConsecutiveDeliveries >= 5)
        CurrentPriceMultiplier = Math.Max(0.7f, CurrentPriceMultiplier - 0.03f);
}
```

#### UpdateRelationship() (privado)
```csharp
private void UpdateRelationship()
{
    float avgTrust = (PlayerTrust + AgentTrust) / 2f;
    if (avgTrust >= 71)      Relationship = Constants.AgentRelationship.Partner;
    else if (avgTrust >= 51) Relationship = Constants.AgentRelationship.Ally;
    else if (avgTrust >= 31) Relationship = Constants.AgentRelationship.Friend;
    else if (avgTrust >= 11) Relationship = Constants.AgentRelationship.Good;
    else if (avgTrust >= -10) Relationship = Constants.AgentRelationship.Neutral;
    else if (avgTrust >= -30) Relationship = Constants.AgentRelationship.Bad;
    else                      Relationship = Constants.AgentRelationship.Enemy;
}
```

### Métodos de Estado

#### UpdateState()
```csharp
public void UpdateState()
{
    if (CurrentState == Constants.AgentState.Disappeared)
    {
        DaysUntilReturn--;
        if (DaysUntilReturn <= 0)
        {
            CurrentState = Constants.AgentState.Idle;
            DaysUntilReturn = 0;
        }
        return;
    }

    // Quiebra: 1% chance para Disappearing con >20 entregas
    if (Personality == Constants.AgentPersonality.Disappearing && TotalDeliveries > 20)
    {
        if (UnityEngine.Random.value < 0.01f)
        {
            CurrentState = Constants.AgentState.Bankrupt;
            return;
        }
    }

    if (CurrentLoad > MaxCapacity)
        CurrentState = Constants.AgentState.Overworked;
    else if (AgentTrust < 20)
        CurrentState = Constants.AgentState.Angry;
    else if (CurrentLoad >= MaxCapacity - 1 && MaxCapacity > 0)
        CurrentState = Constants.AgentState.Stressed;
    else
        CurrentState = Constants.AgentState.Idle;
}
```

### Métodos de Cálculo

#### GetCurrentPriceMultiplier()
```csharp
public float GetCurrentPriceMultiplier()
{
    float multiplier = BasePriceMultiplier * CurrentPriceMultiplier;
    if (ConsecutiveDeliveries >= 10)     multiplier *= 0.90f;  // -10% por lealtad alta
    else if (ConsecutiveDeliveries >= 5) multiplier *= 0.95f;  // -5% por lealtad
    if (Relationship <= Constants.AgentRelationship.Bad)    multiplier *= 1.15f; // +15%
    else if (Relationship <= Constants.AgentRelationship.Enemy) multiplier *= 1.30f; // +30%
    if (Relationship >= Constants.AgentRelationship.Ally)   multiplier *= 0.95f; // -5%
    return multiplier;
}
```

#### GetCurrentSpeedMultiplier()
```csharp
public float GetCurrentSpeedMultiplier()
{
    float multiplier = BaseSpeedMultiplier;
    if (CurrentState == Constants.AgentState.Overworked) multiplier *= 0.70f; // -30%
    if (CurrentState == Constants.AgentState.Stressed)   multiplier *= 0.85f; // -15%
    if (CurrentState == Constants.AgentState.Angry)       multiplier *= 0.60f; // -40%
    return multiplier;
}
```

#### GetEventRiskModifier()
```csharp
public float GetEventRiskModifier()
{
    float riskModifier = 1.0f;
    if (AgentTrust < 30)   riskModifier *= 1.5f;   // Baja confianza +50% riesgo
    if (CurrentState == Constants.AgentState.Overworked) riskModifier *= 1.3f;
    if (CurrentState == Constants.AgentState.Stressed)   riskModifier *= 1.2f;
    if (CurrentState == Constants.AgentState.Angry)       riskModifier *= 1.4f;
    if (Relationship <= Constants.AgentRelationship.Bad)  riskModifier *= 1.25f;
    return riskModifier;
}
```

#### CalculateCost(Cargo cargo, float distanceKm)
```csharp
public int CalculateCost(Cargo cargo, float distanceKm)
{
    float transportMultiplier = GetTransportModeMultiplier(cargo.TransportMode);
    float cargoMultiplier = GetCargoTypeMultiplier(cargo.CargoType);
    float priceMultiplier = GetCurrentPriceMultiplier();
    // Base: $0.50 por km por tonelada
    float baseCost = distanceKm * (cargo.Weight / 1000f) * 0.5f;
    int finalCost = (int)(baseCost * transportMultiplier * cargoMultiplier * priceMultiplier);
    return Math.Max(100, finalCost);  // Mínimo $100
}
```

**Multiplicadores por modo de transporte:**
- Maritime: 0.7x | Air: 2.5x | Land: 1.0x | Rail: 0.8x | Multimodal: 1.5x

**Multiplicadores por tipo de carga:**
- General: 1.0x | Refrigerated: 1.3x | Dangerous: 1.5x | Urgent: 1.2x | Valuable: 1.4x

### Métodos Auxiliares

```csharp
public bool CanOperateInRegion(string region)
// Retorna true si opera en la región o si no tiene restricciones (lista vacía)

public bool CanHandleCargoType(Constants.CargoType cargoType)
// Retorna true si puede manejar el tipo de carga

public bool OffersTransportMode(Constants.TransportMode mode)
// Retorna true si ofrece ese modo de transporte

public float GetSuccessRate()
// TotalDeliveries == 0 → 0.5f; sino: SuccessfulDeliveries / TotalDeliveries

public string GetRelationshipEmoji()
// Partner→"💍 Socio", Ally→"🤝 Aliado", Friend→"😊 Amigo",
// Good→"👍 Bueno", Neutral→"😐 Neutral", Bad→"😠 Malo", Enemy→"👎 Enemigo"

public string GetStateEmoji()
// Idle→"✅", Overworked→"⚠️", Stressed→"😰", Angry→"😤",
// Greedy→"💰", Disappeared→"👻", Bankrupt→"💀"
```

### GetPersonalityDescription (privado)
```csharp
private string GetPersonalityDescription(Constants.AgentPersonality personality)
{
    // Reliable → "🛡️ Confiable. Nunca falla, pero es caro y no negocia."
    // Cheap → "💰 Económico. Barato, pero a veces 'pierde' cargas."
    // Ambitious → "📈 Ambicioso. Sube precios si detecta desesperación."
    // Lazy → "😴 Perezoso. Responde lento, deja cargas olvidadas."
    // Friendly → "🤗 Amigable. Avisa antes de subir precios, descuentos por lealtad."
    // Elusive → "👻 Esquivo. Desaparece por días sin avisar."
    // Efficient → "⚡ Eficiente. Siempre a tiempo, pero colapsa si lo sobrecargas."
    // Scammer → "🎭 Estafador. Cobra extras falsos. ¡Cuidado!"
    // Liar → "🤥 Mentiroso. Dice que entregó pero no entregó."
    // Bipolar → "🎢 Bipolar. Impredecible, un día excelente, otro horrible."
    // Envious → "😤 Envidioso. Te sabotea si creces mucho."
    // Disappearing → "💨 Fugaz. Puede desaparecer con tu carga si quiebra."
    // Loyal → "🤝 Leal. Mejor precio por usar siempre el mismo."
    // Rival → "⚔️ Rival. Odia a otros agentes, te penaliza si cambias."
}
```

---

## 1.4 — Client.cs (793 líneas)

**Ruta:** `Assets/_Game/Models/Client.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`, `System.Collections.Generic`
**Atributo:** `[Serializable]`

### Propósito
Modelo de cliente con PERSONALIDAD ACTIVA. Los clientes tienen memoria de interacciones, reaccionan a entregas (quejas, bloqueos, recomendaciones), y su relación evoluciona con el tiempo.

### Todas las Propiedades

#### Identificación
```csharp
public string Id { get; set; }                          // GUID
public string CompanyName { get; set; }                 // "Aceros del Cono Sur"
public Constants.ClientType ClientType { get; set; }    // GoodPayer, BadPayer, etc.
public string PersonalityDescription { get; set; }      // Descripción para UI
```

#### Relación con el Jugador
```csharp
public float RelationshipLevel { get; set; }            // 0-100
public int AngerLevel { get; set; }                     // 0-5 (5 = bloqueado)
public bool IsBlacklisted { get; set; }                 // No acepta más cotizaciones
public int DaysUntilAngerDecay { get; set; }            // Días para que baje el enojo
```

#### Comportamiento de Pago
```csharp
public int PaymentDelay { get; set; }                   // Días para pagar (0 = contado)
public float EarlyPaymentChance { get; set; }           // Probabilidad pago anticipado
public float LatePaymentChance { get; set; }            // Probabilidad pago tardío
public float LatePaymentPenalty { get; set; }           // Penalidad por pago tardío
```

#### Tolerancia
```csharp
public int DelayTolerance { get; set; }                 // Días de retraso tolerados
public float DamageTolerance { get; set; }              // % daño tolerado
public bool AcceptsNegotiation { get; set; }            // ¿Acepta negociar?
public float MaxMarginTolerance { get; set; }           // Margen máximo tolerado
```

#### Historial
```csharp
public int TotalDeliveries { get; set; }
public int SuccessfulDeliveries { get; set; }
public int FailedDeliveries { get; set; }
public int ComplaintsCount { get; set; }
public int RecommendationsGiven { get; set; }
public int PendingOffers { get; set; }
public int LastInteractionDay { get; set; }
```

#### Estado Especial
```csharp
public bool IsActive { get; set; }                      // ¿Activo?
public bool IsVip { get; set; }                         // ¿VIP? (mejores condiciones)
public bool HasActiveContract { get; set; }             // ¿Tiene contrato?
public int ContractDaysRemaining { get; set; }          // Días restantes de contrato
public List<string> FavoriteRoutes { get; set; }        // Rutas favoritas (máx 5)
```

### Constructor
```csharp
public Client(string companyName, Constants.ClientType clientType)
```
Establece valores iniciales según el tipo de cliente:

| Propiedad | GoodPayer | BadPayer | UrgentClient | CreditClient | VeryBadClient | ContractClient |
|-----------|-----------|----------|--------------|--------------|---------------|----------------|
| RelationshipLevel | 60 | 40 | 50 | 45 | 30 | 55 |
| PaymentDelay | 0 | 15 | 0 | 45 | 30 | 5 |
| EarlyPaymentChance | 0.50 | 0.05 | 0.80 | 0.10 | 0.01 | 0.30 |
| LatePaymentChance | 0.02 | 0.40 | 0.05 | 0.15 | 0.60 | 0.08 |
| DelayTolerance | 5 | 3 | 1 | 3 | 2 | 4 |
| DamageTolerance | 0.15 | 0.05 | 0.10 | 0.08 | 0.03 | 0.10 |
| AcceptsNegotiation | true | false | true | true | false | true |
| MaxMarginTolerance | 0.35 | 0.15 | 0.50 | 0.20 | 0.10 | 0.25 |

### Métodos de Reacción

#### ReactToDelay(int delayDays)
```csharp
public (float complaintChance, bool becomesAngry) ReactToDelay(int delayDays)
```
Si `delayDays > DelayTolerance`, calcula chance de queja. Clientes urgentes tienen 2x sensibilidad.

#### ReactToDamage(float damagePercentage)
```csharp
public (float complaintChance, bool becomesAngry) ReactToDamage(float damagePercentage)
```
Si `damagePercentage > DamageTolerance`, calcula chance de queja. Clientes con contrato son 2x sensibles.

#### ReactToHighPrice(float marginPercentage)
```csharp
public (float rejectionChance, bool becomesAngry) ReactToHighPrice(float marginPercentage)
```
Si margen > `MaxMarginTolerance`: chance de rechazo = exceso * 2. Margen >50% = siempre enoja.

#### DecideToRecommend()
```csharp
public bool DecideToRecommend()
```
Solo si RelationshipLevel > 70. Probabilidad base 5%, +5% con 5+ éxitos, +5% con 10+ éxitos, +10% si VIP.

#### DecideToBecomeVip()
```csharp
public bool DecideToBecomeVip()
```
Requisitos: 10+ entregas exitosas, RelationshipLevel ≥ 80, no ya VIP. 10% chance.

#### DecideToRenewContract()
```csharp
public bool DecideToRenewContract()
```
Renueva si RelationshipLevel ≥ 60 y 0 fallos, o ≥ 70 y ≤ 1 fallo. Renovación = 180 días + 10 relación.

### Métodos de Actualización

#### RecordDelivery(...)
```csharp
public void RecordDelivery(bool wasSuccessful, string originCityId, string destinationCityId,
                           int currentDay, bool wasDelayed = false, bool wasDamaged = false)
```
- Exitosa sin problemas: +8 relación, guarda ruta favorita
- Exitosa con problemas: +2 a +5 relación
- Fallida: -5 a -15 relación, +2 enojo

#### IncreaseAnger(int amount) (privado)
```csharp
private void IncreaseAnger(int amount)
{
    AngerLevel = Math.Min(5, AngerLevel + amount);
    if (AngerLevel >= 5)
    {
        IsBlacklisted = true;  // BLOQUEO
        IsActive = false;
    }
}
```

#### DecayAnger()
```csharp
public void DecayAnger()
```
Cada 5 días sin incidentes, el enojo baja 1 punto. Si baja de 5, se desbloquea.

### Métodos de Cálculo

```csharp
public float GetNegotiationBonus()
// RelationshipLevel ≥80: +20%, ≥60: +10%, ≥40: 0%, ≥20: -10%, <20: -20%

public float GetDesiredPriceMultiplier()
// VIP: 1.15x, Urgente: 1.25x, Contrato: 0.90x, buena relación: +0.05

public bool WillPayEarly()
// Probabilidad = EarlyPaymentChance + VIP(+20%) + buena relación(+10%)

public bool WillPayLate()
// Probabilidad = LatePaymentChance + mala relación(+20%) + enojo ≥3(+30%)
```

### Métodos Auxiliares (UI)
```csharp
public string GetRelationshipEmoji()
// ≥90: "💎 Excelente", ≥70: "😊 Muy Buena", ≥50: "😐 Buena",
// ≥30: "😠 Regular", ≥10: "😤 Mala", <10: "👎 Pésima"

public string GetAngerEmoji()
// 0→"😊", 1→"😐", 2→"😠", 3→"😤", 4→"💢", 5→"🚫"

public float GetSuccessRate()
// SuccessfulDeliveries / TotalDeliveries (0.5 si sin historial)

public override string ToString()
// "CompanyName | RelationshipEmoji VIP Contrato BLOQUEADO | Éxito: XX%"
```

---

## 1.5 — Quote.cs (484 líneas)

**Ruta:** `Assets/_Game/Models/Quote.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`
**Atributo:** `[Serializable]`

### Propósito
Modelo de cotización enviada a un cliente. Incluye precio, modo de transporte, agente, y todo el seguimiento de negociación (contraofertas, rondas, expiración).

### Todas las Propiedades

```csharp
// Identificación
public string Id { get; set; }                          // GUID
public string CargoId { get; set; }                     // Referencia a Cargo
public string ClientId { get; set; }                    // Referencia a Client
public string ClientName { get; set; }                  // Para UI

// Contenido
public int OfferedPrice { get; set; }                   // Precio ofrecido (USD)
public int AgentCost { get; set; }                      // Costo del agente
public float Margin { get; set; }                       // (OfferedPrice - AgentCost) / OfferedPrice
public Constants.TransportMode TransportMode { get; set; }
public string AgentId { get; set; }
public string AgentName { get; set; }
public int EstimatedDays { get; set; }                  // Días de tránsito
public bool HasInsurance { get; set; }
public int InsuranceCost { get; set; }                  // 8% del precio si tiene seguro

// Estado
public int AttemptNumber { get; set; }                  // 1, 2 o 3
public bool WasAccepted { get; set; }
public bool WasRejected { get; set; }
public bool HasCounterOffer { get; set; }
public int CounterOfferPrice { get; set; }
public string ClientMessage { get; set; }               // Mensaje del cliente para UI
public int NegotiationRound { get; set; }
public int DaySent { get; set; }
public int ExpirationDay { get; set; }                  // DaySent + 3
public bool IsExpired { get; set; }

// Resultado
public bool IsAgreementReached { get; set; }
public int FinalPrice { get; set; }
public string RejectionReason { get; set; }
```

### Constructor Completo
```csharp
public Quote(string cargoId, string clientId, string clientName,
             int offeredPrice, int agentCost, Constants.TransportMode transportMode,
             string agentId, string agentName, int estimatedDays,
             int daySent, int attemptNumber = 1, bool hasInsurance = false)
{
    Id = Guid.NewGuid().ToString();
    // ... asigna todo
    Margin = (float)(offeredPrice - agentCost) / offeredPrice;
    ExpirationDay = daySent + 3;  // Expira en 3 días
    InsuranceCost = hasInsurance ? (int)(offeredPrice * 0.08f) : 0;
}
```

### Métodos de Negociación

```csharp
public void Accept()
// WasAccepted=true, FinalPrice=OfferedPrice, "✅ ¡Trato cerrado!"

public void Reject(string reason = "")
// WasRejected=true, "❌ {reason}"

public void SetCounterOffer(int counterPrice, string message = "")
// HasCounterOffer=true, CounterOfferPrice=counterPrice

public void AcceptCounterOffer()
// WasAccepted=true, FinalPrice=CounterOfferPrice, recalcula Margin

public void RejectCounterOffer(string reason = "")
// WasRejected=true, "❌ Rechazaste mi oferta."

public void SendPlayerCounterOffer(int playerOffer)
// OfferedPrice=playerOffer, recalcula Margin, NegotiationRound++

public bool CheckExpiration(int currentDay)
// Si currentDay >= ExpirationDay → IsExpired=true, WasRejected=true

public bool IncrementAttempt()
// Si AttemptNumber < MAX_QUOTES_PER_CARGO(3) → AttemptNumber++, retorna true
```

### Métodos de Validación

```csharp
public bool IsValid()          // OfferedPrice > AgentCost && OfferedPrice > 0
public bool HasAcceptableMargin() // Margin >= 0.05 (mínimo 5%)
public bool HasExcessiveMargin()  // Margin > 0.35 (más de 35% irrita al cliente)
```

### Métodos Auxiliares

```csharp
public string GetStatusText()
// Expirada→"⏰ Expirada", Aceptada→"✅ Aceptada", Rechazada→"❌ Rechazada",
// Contraoferta→"🔄 Contraoferta", Pendiente→"⌛ Pendiente"

public string GetStatusColor()
// Expirada→naranja, Aceptada→verde, Rechazada→rojo, Contraoferta→azul, Pendiente→blanco
```

### NegotiationResult (struct)
```csharp
public struct NegotiationResult
{
    public bool Accepted;
    public bool HasCounterOffer;
    public int CounterOfferPrice;
    public string ClientMessage;
    public float AcceptanceChance;
    public int NegotiationRound;

    public static NegotiationResult Acceptance(string message, float chance)
    public static NegotiationResult CounterOffer(int price, string message, float chance, int round)
    public static NegotiationResult Rejection(string message, float chance)
}
```
Struct liviano para resultados temporales de negociación. Se copia por valor, no genera garbage collection.

---

## 1.6 — WorldCity.cs (278 líneas)

**Ruta:** `Assets/_Game/Models/WorldCity.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`, `System.Collections.Generic`, `UnityEngine`
**Atributo:** `[Serializable]`

### Propósito
Modelo de una ciudad del mundo con coordenadas geográficas reales, infraestructura logística, y sistema de desbloqueo progresivo.

### Todas las Propiedades

```csharp
// Identificación
public string Id { get; set; }              // "buenos_aires"
public string DisplayName { get; set; }     // "Buenos Aires"
public string Country { get; set; }         // "Argentina"
public string Continent { get; set; }       // "South America"

// Coordenadas
public float Latitude { get; set; }         // -34.6 (negativo = Sur)
public float Longitude { get; set; }        // -58.4 (negativo = Oeste)

// Infraestructura
public bool HasPort { get; set; }           // Necesario para transporte marítimo
public bool HasAirport { get; set; }        // Necesario para transporte aéreo
public bool IsLandHub { get; set; }         // Necesario para transporte terrestre
public bool IsMajorHub { get; set; }        // Afecta rutas y costos

// Progresión
public bool IsUnlocked { get; set; }        // Buenos Aires = true por defecto
public int UnlockCost { get; set; }         // Base $10,000
public int UnlockTier { get; set; }         // 0 = inicio, 1-6 progresión
public int Popularity { get; set; }         // 0-100 (afecta frecuencia de cargas)

// Zona Terrestre
public string LandZone { get; set; }        // "south_america", "europe", etc.
```

### Método: DetermineLandZone (privado)
```csharp
private string DetermineLandZone(string continent, string country)
```
Determina automáticamente la zona terrestre:
- South America → "south_america"
- North America (excepto Panamá) → "north_america"
- Panamá → "central_america"
- Europe (excepto UK) → "europe"
- Asia (excepto islas: Japón, Filipinas, Indonesia, Sri Lanka, Taiwán) → "asia_continental"
- Africa → "africa_continental"
- Otros (islas) → "" (sin conexión terrestre)

### Método: CanLandTransportTo(WorldCity other)
```csharp
public bool CanLandTransportTo(WorldCity other)
{
    if (string.IsNullOrEmpty(LandZone) || string.IsNullOrEmpty(other.LandZone))
        return false;
    return LandZone == other.LandZone;  // Misma zona = pueden conectarse por tierra
}
```

### Método: ToSpherePosition(float radius)
```csharp
public Vector3 ToSpherePosition(float radius)
```
Convierte latitud/longitud a posición 3D en una esfera para el mapa del globo:
```
x = R * cos(lat) * cos(lon)
y = R * sin(lat)
z = R * cos(lat) * sin(lon)
```

### Método: DistanceTo(WorldCity other)
```csharp
public float DistanceTo(WorldCity other)
```
Calcula distancia real en km usando la fórmula Haversine (radio de la Tierra = 6,371 km).

---

## 1.7 — CityDatabase.cs (76 líneas)

**Ruta:** `Assets/_Game/Models/CityDatabase.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System.Collections.Generic`, `UnityEngine`

### Propósito
Base de datos estática con las 10 ciudades del juego. Se accede globalmente via `CityDatabase.AllCities`.

### Estructura
```csharp
public static class CityDatabase
{
    public static Dictionary<string, WorldCity> AllCities { get; private set; }
```

### 10 Ciudades Predefinidas

| ID | Nombre | País | Continente | Lat | Lon | Puerto | Aerop. | Tierra | Hub | Tier | Pop |
|----|--------|------|------------|-----|-----|--------|--------|--------|-----|------|-----|
| buenos_aires | Buenos Aires | Argentina | South America | -34.6 | -58.4 | ✅ | ✅ | ✅ | ✅ | 0 | 60 |
| miami | Miami | Estados Unidos | North America | 25.8 | -80.2 | ✅ | ✅ | ✅ | ✅ | 1 | 80 |
| shanghai | Shanghai | China | Asia | 31.2 | 121.5 | ✅ | ✅ | ✅ | ✅ | 2 | 95 |
| rotterdam | Rotterdam | Países Bajos | Europe | 51.9 | 4.5 | ✅ | ✅ | ✅ | ✅ | 2 | 85 |
| dubai | Dubai | Emiratos Árabes | Middle East | 25.2 | 55.3 | ✅ | ✅ | ❌ | ✅ | 3 | 75 |
| hamburg | Hamburg | Alemania | Europe | 53.6 | 10.0 | ✅ | ✅ | ❌ | ✅ | 3 | 80 |
| sao_paulo | São Paulo | Brasil | South America | -23.5 | -46.6 | ✅ | ✅ | ✅ | ✅ | 1 | 70 |
| los_angeles | Los Ángeles | Estados Unidos | North America | 34.0 | -118.2 | ✅ | ✅ | ✅ | ✅ | 4 | 85 |
| antwerp | Amberes | Bélgica | Europe | 51.2 | 4.4 | ✅ | ✅ | ❌ | ❌ | 5 | 82 |
| copenhagen | Copenhague | Dinamarca | Europe | 55.7 | 12.6 | ✅ | ❌ | ❌ | ❌ | 6 | 52 |

### Métodos

```csharp
public static void Initialize()
// Crea las 10 ciudades y las agrega al diccionario

public static WorldCity GetCity(string id)
// Busca ciudad por ID. Retorna null si no existe.

public static float GetDistance(string cityId1, string cityId2)
// Calcula distancia Haversine entre dos ciudades (km)
```

---

## 1.8 — GameEvent.cs (334 líneas)

**Ruta:** `Assets/_Game/Models/GameEvent.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`, `System.Collections.Generic`, `UnityEngine`
**Atributo:** `[Serializable]`

### Propósito
Modelo de evento aleatorio con condiciones contextuales. Los eventos NO son aleatorios puros — se evalúan según ubicación, fecha, modo de transporte, etapa del viaje, tipo de carga, y confianza del agente.

### Todas las Propiedades

```csharp
// Identificación
public string Id { get; set; }
public string Name { get; set; }                        // "Inspección Aduanera"
public string Description { get; set; }

// Tipo y Severidad
public Constants.EventType Type { get; set; }
public int Severity { get; set; }                       // 1 (leve) a 5 (catastrófico)

// Condiciones (TODAS deben cumplirse)
public List<Constants.TransportMode> AffectedTransportModes { get; set; }  // null = todos
public List<string> AffectedStages { get; set; }           // "origin", "transit", "destination"
public List<string> AffectedCountries { get; set; }        // null = todos
public List<string> AffectedCities { get; set; }           // null = todas
public List<Constants.CargoType> AffectedCargoTypes { get; set; }  // null = todos
public List<int> AffectedMonths { get; set; }              // 1-12, null = todos
public List<int> AffectedDays { get; set; }                // Día del mes, null = todos
public int? AgentTrustThreshold { get; set; }              // Si confianza < esto, más probable

// Probabilidad
public float BaseProbability { get; set; }                 // 0-1

// Efectos
public int DaysExtra { get; set; }                         // Días de retraso
public int MoneyCost { get; set; }                         // Costo económico
public int ReputationLoss { get; set; }                    // Pérdida de reputación

// Opciones de Respuesta
public bool RequiresChoice { get; set; }
public List<EventOption> Options { get; set; }
```

### Método: AppliesToCargo(Cargo cargo, ...)
```csharp
public bool AppliesToCargo(Cargo cargo, string currentStage, int currentMonth,
                           int currentDay, float agentTrust)
```
Verifica TODAS las condiciones:
1. Si hay modos de transporte afectados → verifica que el cargo use uno de ellos
2. Si hay etapas afectadas → verifica que la etapa actual coincida
3. Si hay países afectados → verifica ubicación
4. Si hay ciudades afectadas → verifica la ciudad actual
5. Si hay tipos de carga afectados → verifica tipo
6. Si hay meses afectados → verifica el mes actual
7. Si hay días afectados → verifica el día del mes
8. Si hay umbral de confianza → verifica confianza del agente

### Método: GetFinalProbability(float agentTrust, int cargoCount)
```csharp
public float GetFinalProbability(float agentTrust, int cargoCount)
```
Calcula probabilidad final:
- Base = `BaseProbability`
- Si agente tiene confianza < 40: +2%
- Dividido por Severity (eventos graves son menos comunes)
- Clamped entre 1% y 30%

Fórmula con efecto estacional (usa Mathf.Sin para modulación sinusoidal):
```
probability += Mathf.Sin(currentDay * 0.1f) * 0.02f;
```

### Clase: EventOption
```csharp
public class EventOption
{
    public string Text { get; set; }                    // "Pagar multa ($500)"
    public int Cost { get; set; }                       // Costo en dinero
    public int DaysExtra { get; set; }                  // Días de retraso
    public int ReputationImpact { get; set; }           // Cambio en reputación
    public float SuccessChance { get; set; } = 1.0f;    // 0-1
    public string RequiredFeature { get; set; }         // "insurance", "priority", "level3", "level5"

    public bool IsAvailable(bool hasInsurance, bool hasPriority, int playerLevel)
    // Verifica si el jugador tiene el feature requerido
}
```

---

## 1.9 — SaveData.cs (123 líneas)

**Ruta:** `Assets/_Game/Models/SaveData.cs`
**Namespace:** `FreightForwarder.Models`
**Using:** `System`, `System.Collections.Generic`, `UnityEngine`
**Atributo:** `[Serializable]`

### Propósito
Contenedor serializable para guardar/cargar la partida completa usando `JsonUtility`.

### Todas las Propiedades

```csharp
// Versión y fecha
public int SaveVersion { get; set; } = 1;
public string SaveDate { get; set; }

// Empresa
public string CompanyName { get; set; }

// Economía
public int Money { get; set; }
public int Reputation { get; set; }
public int Level { get; set; }
public int CurrentXP { get; set; }

// Estadísticas
public int TotalCargosCompleted { get; set; }
public int TotalCargosFailed { get; set; }
public int TotalRevenue { get; set; }
public int TotalCosts { get; set; }
public int TotalCargosAbandoned { get; set; }

// Tiempo
public int CurrentDay { get; set; }
public DateTime CurrentDate { get; set; }
public float ContinuousDays { get; set; }

// Cargas
public List<Cargo> MarketCargos { get; set; }
public List<Cargo> ActiveCargos { get; set; }
public List<Cargo> CompletedCargos { get; set; }
public List<Cargo> FailedCargos { get; set; }

// Clientes
public List<Client> Clients { get; set; }
public Dictionary<string, float> ClientRelationships { get; set; }

// Agentes
public List<Agent> Agents { get; set; }
public Dictionary<string, List<string>> AgentActiveCargos { get; set; }

// Oficinas
public Dictionary<string, int> Offices { get; set; }   // cityId → nivel
public List<string> UnlockedCityIds { get; set; }

// Cotizaciones
public List<Quote> PendingQuotes { get; set; }

// Eventos
public List<string> ActiveWorldEventIds { get; set; }
```

---

## 1.10 — Singleton.cs (70 líneas)

**Ruta:** `Assets/_Game/Utils/Singleton.cs`
**Namespace:** `FreightForwarder.Utils`
**Using:** `UnityEngine`

### Propósito
Clase base genérica para managers singleton. Garantiza una sola instancia de cada manager, thread-safe, con auto-creación si no existe en la escena.

### Código Completo

```csharp
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instancia de {typeof(T)} solicitada mientras la app se cierra.");
                return null;
            }

            lock (_lock)  // Thread-safe
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();  // Unity 6 API
                    if (_instance == null)
                    {
                        // Crea automáticamente si no existe en la escena
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        _instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnAwake();  // Hook para subclases
        }
        else if (_instance != this)
        {
            Destroy(gameObject);  // Destruye duplicados
        }
    }

    protected virtual void OnAwake() { }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
```

**Uso:** Todos los managers heredan de `Singleton<T>`:
```csharp
public class TimeManager : Singleton<TimeManager> { ... }
public class EconomyManager : Singleton<EconomyManager> { ... }
// etc.
```

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 2: MANAGERS (Assets/_Game/Managers/)
# ═══════════════════════════════════════════════════════════════

---

## 2.1 — GameManager.cs (87 líneas)

**Ruta:** `Assets/_Game/Managers/GameManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<GameManager>`

### Propósito
Orquestador global del estado del juego. Controla transiciones entre estados (menú, jugando, pausa, game over).

### Enum: GameState
```csharp
public enum GameState { MainMenu, Playing, Paused, GameOver }
```

### Propiedades
```csharp
public GameState CurrentState { get; private set; }
private int _startingMoney = Constants.INITIAL_MONEY;        // 5000 (CS0414 warning)
private int _startingReputation = Constants.INITIAL_REPUTATION; // 50 (CS0414 warning)
```

### Eventos
```csharp
public event Action<GameState> OnGameStateChanged;
public event Action OnNewGameStarted;
public event Action OnGamePaused;
public event Action OnGameResumed;
public event Action OnGameOver;
```

### Métodos
```csharp
public void StartNewGame()
// CurrentState = Playing, Time.timeScale = 1, dispara eventos

public void PauseGame()
// CurrentState = Paused, Time.timeScale = 0

public void ResumeGame()
// CurrentState = Playing, Time.timeScale = 1

public void TriggerGameOver()
// CurrentState = GameOver, Time.timeScale = 0

public void SetTimeScale(float scale)
// Time.timeScale = scale (0, 1, 2 o 3)
```

---

## 2.2 — TimeManager.cs (101 líneas)

**Ruta:** `Assets/_Game/Managers/TimeManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<TimeManager>`

### Propósito
Controla la progresión del tiempo del juego. 30 segundos reales = 1 día de juego. Calendario con fecha real (empieza 1/1/2025).

### Propiedades
```csharp
public int CurrentDay { get; private set; }             // Día actual (entero)
public DateTime CurrentDate { get; private set; }       // Fecha calendario (2025-01-01 + días)
public float TimeScale { get; private set; } = 1f;      // 0-3 (0=pausa, 3=rápido)
public bool IsPaused { get; private set; }
public float DayProgress { get; private set; }          // 0-1 (0=medianoche, 0.5=mediodía)
public float ContinuousDays { get; private set; }       // Días acumulados con decimales
```

### Configuración
```csharp
private float _dayDurationSeconds = Constants.DAY_DURATION_SECONDS; // 30
private int _startYear = 2025;
private int _startMonth = 1;
private int _startDay = 1;
```

### Eventos
```csharp
public event Action OnDayPassed;                        // Se dispara cada día
public event Action OnMonthPassed;                      // Se dispara cada mes
public event Action<DateTime> OnDateChanged;            // Se dispara al cambiar fecha
```

### Flujo en Update()
```
Cada frame:
  _dayAccumulator += Time.deltaTime * TimeScale
  DayProgress = _dayAccumulator / _dayDurationSeconds
  Si _dayAccumulator >= _dayDurationSeconds:
    CurrentDay++
    CurrentDate += 1 día
    ContinuousDays++
    OnDayPassed?.Invoke()
    Si cambió el mes → OnMonthPassed?.Invoke()
    OnDateChanged?.Invoke(CurrentDate)
    _dayAccumulator = 0
```

### Métodos
```csharp
public void SetTimeScale(float scale)    // Cambia velocidad (0-3)
public void Pause()                       // TimeScale=0, IsPaused=true
public void Resume()                      // TimeScale=1, IsPaused=false
public string GetFormattedDate()          // CurrentDate.ToString("dd/MM/yyyy")
public void RestoreState(int day, DateTime date, float continuousDays)  // Para Save/Load
```

---

## 2.3 — EconomyManager.cs (272 líneas)

**Ruta:** `Assets/_Game/Managers/EconomyManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<EconomyManager>`

### Propósito
Sistema financiero completo con dinero, reputación, XP, niveles, y detección de game over.

### Propiedades
```csharp
public int Money { get; private set; }                  // Dinero actual
public int Reputation { get; private set; }             // 0-100
public int Level { get; private set; } = 1;
public int CurrentXP { get; private set; }
public int TotalCargosCompleted { get; private set; }
public int TotalCargosFailed { get; private set; }
public int TotalRevenue { get; private set; }
public int TotalCosts { get; private set; }
public int TotalCargosAbandoned { get; private set; }
public int MonthlyOfficeCosts { get; private set; }
```

### Eventos
```csharp
public event Action<int> OnMoneyChanged;                // Nuevo valor
public event Action<int> OnReputationChanged;           // Nuevo valor
public event Action<int> OnLevelUp;                     // Nuevo nivel
public event Action<int, int> OnXPGained;               // (XP ganada, XP total)
public event Action OnGameOver;
```

### Métodos Financieros

```csharp
public void AddMoney(int amount, string reason = "")
{
    Money += amount;
    TotalRevenue += amount;
    OnMoneyChanged?.Invoke(Money);
    CheckGameOver();
}

public bool SubtractMoney(int amount, string reason = "")
{
    Money -= amount;
    TotalCosts += amount;
    OnMoneyChanged?.Invoke(Money);
    CheckGameOver();
    return true;  // Permite deuda
}

public void AddReputation(int amount)
{
    Reputation = Mathf.Clamp(Reputation + amount, 0, 100);
    OnReputationChanged?.Invoke(Reputation);
    CheckGameOver();
}
```

### Sistema de XP y Niveles

```csharp
public void AddXP(int amount)
{
    CurrentXP += amount;
    int xpNeeded = Level * Constants.XP_PER_LEVEL;  // Nivel * 200

    while (CurrentXP >= xpNeeded)
    {
        CurrentXP -= xpNeeded;
        Level++;
        int levelUpBonus = Level * 100;  // Bono de dinero: $100 * nivel
        AddMoney(levelUpBonus);
        AddReputation(5);
        OnLevelUp?.Invoke(Level);
        xpNeeded = Level * Constants.XP_PER_LEVEL;
    }
}
```

**Fórmula de XP:** Nivel 1 necesita 200 XP, Nivel 2 necesita 400 XP, Nivel 3 necesita 600 XP, etc.

### Métodos de Estadísticas
```csharp
public void RecordCargoCompleted(int revenue, int cost)
// +revenue dinero, +50 XP

public void RecordCargoFailed(int penalty = 0)
// -penalty dinero, -5 reputación

public void RecordCargoAbandoned(int penalty)
// -penalty dinero, -10 reputación

public void ProcessMonthlyCosts(int monthlyCosts)
// Descuenta costos mensuales. Si no puede pagar: -10 reputación
```

### Game Over
```csharp
public bool IsGameOver()
{
    return Money <= Constants.GAME_OVER_DEBT_THRESHOLD || Reputation <= 0;
    // Money <= -2000 OR Reputation <= 0
}
```

---

## 2.4 — CargoManager.cs (647 líneas)

**Ruta:** `Assets/_Game/Managers/CargoManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<CargoManager>`

### Propósito
Gestiona el ciclo de vida completo de las cargas: generación en mercado, cotización, tránsito, completado/fallido, expiración.

### Propiedades
```csharp
public List<Cargo> MarketCargos { get; private set; }     // Cargas disponibles
public List<Cargo> ActiveCargos { get; private set; }     // En tránsito
public List<Cargo> CompletedCargos { get; private set; }  // Historial éxitos
public List<Cargo> FailedCargos { get; private set; }     // Historial fallos
```

### Configuración
```csharp
private int _maxMarketCargos = Constants.MAX_MARKET_CARGOS;   // 7
private float _newCargoChancePerDay = 0.3f;                    // 30% chance/día
private List<string> _unlockedCityIds;
```

### Eventos
```csharp
public event Action<Cargo> OnCargoAddedToMarket;
public event Action<Cargo> OnCargoAccepted;
public event Action<Cargo> OnCargoCompleted;
public event Action<Cargo> OnCargoFailed;
public event Action<Cargo> OnCargoExpired;
```

### Generación de Cargas

#### GenerateCargo()
Algoritmo completo de generación:
1. Necesita ≥2 ciudades desbloqueadas
2. No genera si hay ≥7 cargas en mercado
3. Elige origen y destino aleatorios (diferentes, máx 10 intentos)
4. Calcula distancia Haversine entre ciudades
5. Elige tipo de carga con probabilidades ponderadas:
   - General: 40% | Refrigerada: 20% | Urgente: 15% | Valiosa: 15% | Peligrosa: 10%
6. Elige tipo de cliente con probabilidades
7. Calcula peso (1-500 tons) y volumen (1-200 m³)
8. Calcula valor declarado: `baseValue = 1000 + (distancia/20000) * 500000`, multiplicado por tipo de carga, con ±20% aleatorio, clamped $1,000-$500,000
9. Determina transporte preferido según lógica:
   - Urgente/Valiosa → Aéreo primero
   - Peligrosa → Marítimo primero
   - <3000 km → Terrestre
   - 3000-10000 km → Marítimo
   - >10000 km → Marítimo
10. Genera razón legible del transporte recomendado
11. Establece expiración = día actual + 7

### Procesamiento Diario (OnDayPassed)

```csharp
private void OnDayPassed()
{
    int currentDay = TimeManager.Instance.CurrentDay;
    UpdateActiveCargos(currentDay);      // 1. Actualizar cargas en tránsito
    CheckExpiredCargos(currentDay);      // 2. Verificar expiración de mercado
    // 3. Generar nuevas cargas (30% chance si < máximo)
    if (MarketCargos.Count < _maxMarketCargos && Random.value < _newCargoChancePerDay)
        GenerateCargo();
}
```

#### UpdateActiveCargos(int currentDay)
```csharp
foreach (Cargo cargo in ActiveCargos)
{
    cargo.DaysRemaining--;
    if (cargo.DaysRemaining <= 0)
        completed.Add(cargo);  // → CompleteCargo()
}
```

### Aceptación de Cotización

```csharp
public bool AcceptQuote(Cargo cargo, Quote quote, int currentDay)
```
1. Mueve cargo de MarketCargos a ActiveCargos
2. Actualiza: Status=Active, precios, agente, seguro, tiempos
3. Registra en AgentManager (AssignCargoToAgent)
4. Calcula margen

### Completar/Fallar Cargas

```csharp
private void CompleteCargo(Cargo cargo, int currentDay)
```
1. Mueve de ActiveCargos a CompletedCargos (máx 100 en historial)
2. Registra en EconomyManager (RecordCargoCompleted)
3. Registra en AgentManager (RecordDelivery + RemoveCargoFromAgent)

```csharp
private void FailCargo(Cargo cargo, int currentDay, string reason)
```
1. Mueve de ActiveCargos a FailedCargos
2. Penalidad = FinalPrice / 2
3. Registra en EconomyManager y AgentManager

### Métodos de Consulta
```csharp
public Cargo GetCargoById(string id)  // Busca en todas las listas
public List<Cargo> GetAvailableCargos()  // Filtra Status == Available
public int GetTotalCargos()
public float GetSuccessRate()
```

---

## 2.5 — AgentManager.cs (262 líneas)

**Ruta:** `Assets/_Game/Managers/AgentManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<AgentManager>`

### Propósito
Gestiona el pool de 10 agentes de transporte predefinidos y procesa sus decisiones activas diariamente.

### 10 Agentes Predefinidos

| ID | Nombre | Ciudad | Personalidad | Modos | Precio | Velocidad | Confiab. | Cap |
|----|--------|--------|-------------|-------|--------|-----------|----------|-----|
| maersk | Maersk Logistics | rotterdam | Reliable | Maritime | 1.20x | 0.95x | 0.95 | 5 |
| cosco | COSCO Shipping | shanghai | Ambitious | Maritime | 0.90x | 1.10x | 0.70 | 8 |
| fedex | FedEx Express | miami | Efficient | Air | 1.35x | 1.40x | 0.92 | 6 |
| emirates | Emirates SkyCargo | dubai | Cheap | Air | 0.80x | 1.00x | 0.65 | 4 |
| dhl | DHL Ground | buenos_aires | Friendly | Land | 0.95x | 0.90x | 0.85 | 5 |
| transporte_sur | Transporte Sur SA | sao_paulo | Lazy | Land | 0.70x | 0.70x | 0.60 | 3 |
| kuehne | Kuehne+Nagel | hamburg | Loyal | Maritime+Air+Land | 1.25x | 1.20x | 0.88 | 7 |
| agf | AGF Logistics | antwerp | Scammer | Maritime+Land | 0.85x | 0.95x | 0.70 | 4 |
| blue_water | Blue Water Shipping | copenhagen | Envious | Maritime | 1.05x | 1.00x | 0.80 | 5 |
| swift | Swift Logistics | los_angeles | Elusive | Land+Air | 1.10x | 1.15x | 0.75 | 4 |

### Eventos
```csharp
public event Action<Agent, string, float> OnPriceSurge;
public event Action<Agent, string> OnCargoAbandoned;
public event Action<Agent, int> OnAgentDisappeared;
public event Action<Agent, string, int> OnAgentScam;
public event Action<Agent, string> OnAgentLied;
public event Action<Agent, string> OnAgentSabotage;
public event Action<Agent> OnAgentReturned;
public event Action<Agent> OnAgentBankrupt;
```

### Procesamiento Diario
```csharp
private void ProcessAgentDecisions()
{
    foreach (var agent in _agents.Values)
    {
        agent.UpdateState();
        agent.UpdatePriceSurge();
        agent.DaysSinceLastUse++;

        // Si desapareció y ya pasaron sus días → vuelve
        // Si está en quiebra → skip
        // Si está Idle → intenta price surge (Ambitious) y desaparición (Elusive)
    }
}
```

### Métodos de Gestión
```csharp
public void AssignCargoToAgent(string agentId, string cargoId)
// Registra cargo en lista del agente, incrementa CurrentLoad

public void RemoveCargoFromAgent(string agentId, string cargoId)
// Remueve cargo, decrementa CurrentLoad

public void RecordDelivery(string agentId, string cargoId, bool wasSuccessful, bool wasAbandoned)
// Delega a agent.RecordDelivery()

public void RecordAgentChange(string oldAgentId)
// Notifica al agente anterior que fue abandonado → -15 AgentTrust
```

### Verificaciones de Comportamiento
```csharp
public bool CheckCargoAbandonment(Agent agent, Cargo cargo)
// Verifica si el agente decide abandonar la carga

public (bool willScam, int extraCost) CheckScam(Agent agent, Cargo cargo, int baseCost)
// Verifica si el agente decide estafar

public bool CheckLie(Agent agent, Cargo cargo)
// Verifica si el agente decide mentir

public bool CheckSabotage(Agent agent, Cargo cargo, int playerLevel)
// Verifica si el agente decide sabotear
```

### Consultas
```csharp
public Agent GetAgent(string id)
public List<Agent> GetAllAgents()
public List<Agent> GetAvailableAgents()
public List<Agent> GetAvailableAgents(Constants.TransportMode mode)
public List<Agent> GetAvailableAgents(Constants.TransportMode mode, Constants.CargoType cargoType)
public float GetEventRiskModifier(string agentId)
```

---

## 2.6 — ClientManager.cs (577 líneas)

**Ruta:** `Assets/_Game/Managers/ClientManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<ClientManager>`

### Propósito
Gestiona el pool de clientes predefinidos, relaciones, negociación de cotizaciones, y actualización diaria de estados.

### Datos
```csharp
public Dictionary<string, Client> Clients { get; private set; }
public Dictionary<string, float> RelationshipWithClients { get; private set; }
public Dictionary<string, List<Quote>> PendingQuotes { get; private set; }
```

### Clientes Predefinidos (14 clientes)
- **GoodPayer:** "Aceros del Cono Sur", "Farmacéutica Rioplatense", "Agro Export SA"
- **BadPayer:** "Importadora del Pacífico", "Textiles Unidos"
- **UrgentClient:** "Tech Components Inc", "Auto Parts Global"
- **CreditClient:** "Megastore Retail", "Consumer Goods Co"
- **VeryBadClient:** "FastDeal LLC", "QuickShip Ltd"
- **ContractClient:** "Minera Andina", "Petroquímica del Sur"

### Sistema de Negociación

#### GetQuoteAcceptanceProbability(Client, Quote, int currentDay)
Algoritmo completo de cálculo:
```
base = 15% (NEGOTIATION_BASE_ACCEPTANCE)

1. Precio vs referencia (referencia = AgentCost * 1.5):
   - ratio ≤ 0.7 → +35% (muy competitivo)
   - ratio ≤ 0.9 → +20% (competitivo)
   - ratio ≤ 1.0 → +10% (justo)
   - ratio ≤ 1.2 → -10% (caro)
   - ratio > 1.2 → -30% (muy caro)

2. Relación: (RelationshipLevel - 50) / 100 * 0.3

3. Reputación: (Reputation - 50) / 100 * 0.2

4. Tipo de cliente:
   UrgentClient: +20%, GoodPayer: +10%, ContractClient: +5%,
   CreditClient: -5%, BadPayer: -15%, VeryBadClient: -25%

5. Margen excesivo (>35%): -15%

6. Enojo: -(AngerLevel * 5%)

7. VIP: +10%

Resultado: clamp(0%, 95%)
```

#### ProcessNegotiation(Client, Cargo, Quote, int currentDay, int attemptNumber)
```csharp
public NegotiationResult ProcessNegotiation(...)
{
    float acceptanceChance = GetQuoteAcceptanceProbability(...);
    float roll = Random.value;

    if (roll < acceptanceChance)
        return Acceptance;           // Acepta
    else if (!isLastAttempt && Random.value < 0.7f)
        return CounterOffer;         // 70% chance de contraoferta
    else
        return Rejection;            // Rechaza definitivamente
}
```

#### CalculateCounterOffer(Client, Cargo, Quote)
```csharp
float referenceValue = quote.AgentCost * 1.5f;
float multiplier = tipo de cliente:
    UrgentClient: 1.0-1.25 | VeryBadClient: 0.7-0.9 | ContractClient: 0.85-1.05 | otros: 0.9-1.1
multiplier += (RelationshipLevel - 50) / 200;
int counterOffer = max(referenceValue * multiplier, AgentCost + 50);
```

### Mensajes de Cliente (para UI)
```csharp
// Aceptación según margen:
// >30%: "💰 Acepto, pero no abuses con los márgenes."
// >20%: "🤝 Trato hecho. Me parece justo."
// >10%: "✅ Acepto. Buen precio para ambas partes."
// ≤10%: "👍 Acepto. Gracias por la buena oferta."

// Rechazo según margen:
// >40%: "😤 ¡Es un robo! Buscaré otro freight forwarder."
// >30%: "😐 Tu precio es demasiado alto."
// ≤30%: "❌ No me convence tu oferta."
```

### Actualización Diaria (OnDailyUpdate)
Para cada cliente:
1. `DecayAnger()` — Baja enojo gradualmente
2. `UpdateContract()` — Actualiza contrato (auto-renueva si condiciones se cumplen)
3. `DecideToBecomeVip()` — 10% chance si cumple requisitos
4. `DecideToRecommend()` — Da recomendación (+5 reputación)

### Actualización de Relaciones
```csharp
// Después de aceptación:
gain = 5 base + (margen bajo → +5) + (VIP → *1.5)
RelationshipLevel += gain, calma enojo

// Después de rechazo:
loss = 3 base + (margen >35% → +5) + (VIP → *1.5)
RelationshipLevel -= loss, aumenta enojo
Si AngerLevel >= 5 → BLACKLIST
```

---

## 2.7 — EventManager.cs (818 líneas)

**Ruta:** `Assets/_Game/Managers/EventManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<EventManager>`

### Propósito
Gestiona el sistema de 20+ eventos aleatorios contextuales que afectan las cargas en tránsito. Los eventos se evalúan según condiciones geográficas, temporales, y de confianza.

### Estado
```csharp
public GameEvent PendingEvent { get; private set; }
public Dictionary<string, List<GameEvent>> EventHistory { get; private set; }
private List<GameEvent> _eventPool;  // 20+ eventos predefinidos
```

### Eventos C#
```csharp
public event Action<GameEvent, Cargo> OnEventTriggered;
public event Action<GameEvent, Cargo, int> OnEventResolved;
```

### Pool de 20+ Eventos Predefinidos

Cada evento tiene condiciones, probabilidad, severidad, efectos, y opciones de respuesta:

1. **Inspección Aduanera** — Severidad 2, etapa "destination", 8% base
   - Opciones: Esperar (+3 días, -2 rep) | Agente urgente (-$600, +1 día) | Documentación electrónica (-$300)

2. **Congestión Portuaria** — Severidad 2, solo Maritime, 10% base
   - Opciones: Esperar (+5 días, -3 rep) | Redirigir (-$800, +2 días)

3. **Clima Adverso** — Severidad 3, meses 6-8 (invierno sur), Maritime/Air, 12% base

4. **Daño a Mercancía** — Severidad 3, 5% base
   - Opciones: Reclamar seguro (requiere "insurance") | Absorber costo

5. **Huelga** — Severidad 4, países específicos (Argentina, Brasil), meses 3-5

6. **Error Documentación** — Severidad 1, 8% base

7. **Falta de Contenedores** — Severidad 2, solo Maritime, 7% base

8. **Ruta Cortada** — Severidad 3, solo Land, 6% base

9. **Aeropuerto Cerrado** — Severidad 3, solo Air, 4% base

10. **Robo de Carga** — Severidad 5, solo Land, solo Valuable, 3% base

11. **Sobrecosto Combustible** — Severidad 1, 15% base

12. **Quiebra Transportista** — Severidad 5, 2% base

13. **Peso Mal Declarado** — Severidad 2, 6% base

14. **Incendio en Almacén** — Severidad 5, etapa "origin", 1% base

15. **Inspección Fitosanitaria** — Severidad 2, solo Refrigerated, 10% base

16. **Feriado No Laborable** — Severidad 1, días específicos (1, 25), 20% base

17. **Demora en Frontera** — Severidad 2, solo Land, 8% base

18. **Carga Rechazada** — Severidad 4, etapa "destination", 3% base

19. **Disputa con Seguro** — Severidad 3, 4% base

20. **Día del Trabajador** — Severidad 1, mes 5, día 1, 50% base

### Procesamiento Diario
```csharp
private void OnDayPassed()
{
    if (CargoManager.Instance == null) return;
    foreach (var cargo in CargoManager.Instance.ActiveCargos)
    {
        CheckForEvent(cargo);
    }
}
```

Para cada carga activa, evalúa todos los eventos del pool. Si un evento aplica (todas sus condiciones se cumplen), calcula la probabilidad final y hace un roll aleatorio.

### Resolución de Eventos
```csharp
public void ResolveEvent(GameEvent evt, Cargo cargo, int optionIndex)
```
Aplica las consecuencias de la opción elegida:
- Agrega días de retraso
- Cobra costo económico
- Afecta reputación

---

## 2.8 — SaveManager.cs (242 líneas)

**Ruta:** `Assets/_Game/Managers/SaveManager.cs`
**Namespace:** `FreightForwarder.Managers`
**Herencia:** `Singleton<SaveManager>`

### Propósito
Persistencia del estado completo del juego en JSON.

### Propiedades
```csharp
public string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");
public bool IsSaveAvailable => File.Exists(SavePath);
```

### Eventos
```csharp
public event Action OnSaveCompleted;
public event Action OnLoadCompleted;
public event Action<string> OnSaveFailed;
public event Action<string> OnLoadFailed;
```

### SaveGame(string companyName)
Recolecta datos de TODOS los managers:
1. EconomyManager: Money, Reputation, Level, XP, estadísticas
2. TimeManager: CurrentDay, CurrentDate, ContinuousDays
3. CargoManager: 4 listas de cargas
4. ClientManager: Clientes, relaciones, cotizaciones pendientes
5. AgentManager: Agentes, cargas activas por agente
6. CityDatabase: Ciudades desbloqueadas

Serializa con `JsonUtility.ToJson(saveData, true)` y escribe a disco.

### LoadGame()
Lee JSON, deserializa, y restaura estado de cada manager:
```csharp
EconomyManager.Instance.RestoreState(money, reputation, level, xp, ...);
TimeManager.Instance.RestoreState(currentDay, currentDate, continuousDays);
CargoManager.Instance.RestoreState(marketCargos, activeCargos, ...);
ClientManager.Instance.RestoreState(clients, relationships);
AgentManager.Instance.RestoreState(agents, agentActiveCargos);
```

### DeleteSave()
Elimina el archivo de guardado.

---

## 2.9 — GameBootstrapper.cs (134 líneas)

**Ruta:** `Assets/_Game/Managers/GameBootstrapper.cs`
**Namespace:** `FreightForwarder.Core`
**Herencia:** `MonoBehaviour` (NO es Singleton)

### Propósito
Orquesta la inicialización de TODO el juego en el orden correcto de dependencias.

### Orden de Inicialización
```
1. TimeManager (primero, otros dependen de él)
2. EconomyManager
3. ClientManager (antes que CargoManager)
4. AgentManager
5. CargoManager
6. EventManager
--- Carga de escena asíncrona ---
7. WorldMap
8. SunController
9. GameUI
```

### Flujo
```csharp
private IEnumerator Start()
{
    yield return new WaitForSeconds(0.5f);   // Boot delay
    InitializeManagers();                     // Instancia prefabs
    SetupInitialData();                       // Datos iniciales
    yield return LoadGameSceneAsync();        // Carga escena "Game"
}
```

### SetupInitialData()
```csharp
private void SetupInitialData()
{
    CityDatabase.Initialize();                           // Base de datos de ciudades
    CityDatabase.GetCity("buenos_aires").IsUnlocked = true;  // Ciudad inicial
    EconomyManager.Instance.ResetGame();                 // Dinero y reputación inicial
    CargoManager.Instance.InitializeNewGame(unlockedCities); // 2-3 cargas iniciales
}
```

---

## 2.10 — SunController.cs (129 líneas)

**Ruta:** `Assets/_Game/Managers/SunController.cs`
**Namespace:** `FreightForwarder`
**Herencia:** `Singleton<SunController>`

### Propósito
Controla la rotación del sol y la iluminación basada en la hora del día del juego. Crea un ciclo día/noche visual.

### Configuración
```csharp
private Light _sunLight;                                // Luz direccional del sol
private Material _skyboxMaterial;                       // Material del cielo
private float _currentTimeOfDay = 0.5f;                 // 0=medianoche, 0.5=mediodía

// Colores del cielo
private Color _midnightColor = new Color(0.05f, 0.05f, 0.1f);   // Azul muy oscuro
private Color _sunriseColor = new Color(0.3f, 0.2f, 0.1f);      // Naranja
private Color _noonColor = new Color(0.3f, 0.6f, 0.9f);         // Azul cielo
private Color _sunsetColor = new Color(0.5f, 0.2f, 0.1f);       // Rojo-naranja

// Intensidades
private float _minIntensity = 0.05f;                    // Noche
private float _maxIntensity = 1.2f;                     // Mediodía
```

### Ciclo de Iluminación
```
Update():
  _currentTimeOfDay = TimeManager.DayProgress (0-1)
  
  ApplySunRotation():
    sunAngle = _currentTimeOfDay * 180° - 90°
    Rota el sol de -90° (abajo) a +90° (arriba)
  
  ApplyLighting():
    intensityFactor = sin(_currentTimeOfDay * PI)  // Pico al mediodía
    intensity = Lerp(min, max, intensityFactor)
    
    4 fases de color:
    0.00-0.25: Medianoche → Amanecer (negro → naranja)
    0.25-0.50: Amanecer → Mediodía (naranja → azul)
    0.50-0.75: Mediodía → Atardecer (azul → rojo)
    0.75-1.00: Atardecer → Medianoche (rojo → negro)
```

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 3: UI (Assets/_Game/UI/)
# ═══════════════════════════════════════════════════════════════

El juego usa **UIElements / UI Toolkit** (el sistema moderno de Unity), NO UGUI.

---

## 3.1 — GameUI.cs (247 líneas)

**Ruta:** `Assets/_Game/UI/GameUI.cs`
**Namespace:** `FreightForwarder.UI`
**Singleton manual** (no hereda de Singleton<T>)

### Propósito
Controlador principal de la UI. Gestiona el HUD superior, el sistema de tabs, y la navegación entre paneles.

### Elementos del HUD
```csharp
private Label _companyLabel;        // Nombre de la empresa
private Label _dateLabel;           // Fecha actual
private Label _moneyLabel;          // Dinero
private Label _reputationLabel;     // Reputación
private Label _levelLabel;          // Nivel
private Label _newsLabel;           // Noticias/eventos
```

### Sistema de Tabs
```csharp
// 7 tabs: Market, Active, Finances, Offices, Agents, Clients, Map
private Button _tabMarket, _tabActive, _tabFinances, _tabOffices,
               _tabAgents, _tabClients, _tabMap;
```

### Botones de Velocidad
```csharp
private Button _pauseBtn;           // Pausa (TimeScale=0)
private Button _speed1Btn;          // Normal (TimeScale=1)
private Button _speed2Btn;          // Rápido (TimeScale=2)
private Button _speed3Btn;          // Muy rápido (TimeScale=3)
```

### Binding por nombre (UIElements Q<>)
```csharp
_companyLabel = _root.Q<Label>("company-label");
_tabMarket = _root.Q<Button>("tab-market");
// etc.
```

### ShowPanel(string panelId)
Muestra un panel y oculta los demás. PanelIds: "market", "active", "finances", "offices", "agents", "clients", "map".

---

## 3.2 — MarketPanel.cs (157 líneas)

**Ruta:** `Assets/_Game/UI/Panels/MarketPanel.cs`
**Namespace:** `FreightForwarder.UI.Panels`

### Propósito
Muestra las cargas disponibles en el mercado como tarjetas con información completa.

### Evento
```csharp
public event Action<Cargo> OnQuoteRequested;  // Cuando el jugador hace click en "Cotizar"
```

### Tarjeta de Carga (CreateCargoCard)
Cada tarjeta muestra:
```
[Emoji] Tipo de Carga
📍 Buenos Aires → Miami
👤 Aceros del Cono Sur
💰 $45,000
⚖️ 250 kg | 📦 120 m³
⏰ Expira en 5 días
🚛 Recomendado: Marítimo
[💼 Cotizar]
```

### Emojis por tipo de carga
```csharp
General → "📦", Refrigerated → "❄️", Dangerous → "☢️", Urgent → "⚡", Valuable → "💎"
```

Si quedan ≤2 días: tarjeta tiene clase CSS "market-card-expiring" (visual de urgencia).

---

## 3.3 — QuotePanel.cs (242 líneas)

**Ruta:** `Assets/_Game/UI/Panels/QuotePanel.cs`
**Namespace:** `FreightForwarder.UI.Panels`

### Propósito
Interfaz de cotización donde el jugador elige modo de transporte, agente, precio, y envía la oferta al cliente.

### Elementos UI
- Panel overlay oscuro semi-transparente (modal)
- Información de carga y cliente
- Dropdown: Modo de transporte
- Dropdown: Agente disponible
- Label: Costo del transporte
- Label: Ganancia estimada
- IntegerField: Precio ofrecido
- Botón: Enviar Cotización
- Botón: Cancelar

### Flujo
1. Jugador selecciona modo de transporte → filtra agentes disponibles
2. Jugador selecciona agente → calcula costo de transporte
3. Jugador ingresa precio → calcula ganancia (precio - costo)
4. Validación: precio debe ser > costo del agente
5. Enviar → crea Quote y la procesa con ClientManager.ProcessNegotiation

---

## 3.4 — ActiveCargosPanel.cs (239 líneas)

**Ruta:** `Assets/_Game/UI/Panels/ActiveCargosPanel.cs`
**Namespace:** `FreightForwarder.UI.Panels`

### Propósito
Muestra las cargas activas (en tránsito) con barra de progreso, y un historial de las últimas 20 cargas completadas/fallidas.

### Tarjeta de Carga Activa
```
[Emoji modo] Tipo de Carga
📍 Buenos Aires → Miami
⏰ 3 días restantes
[▓▓▓▓▓▓░░░░] 60%
💰 $12,000 → Ganancia: $3,500
```

### Cálculo de Progreso
```csharp
float progress = (float)(cargo.TotalTransitDays - cargo.DaysRemaining) / cargo.TotalTransitDays;
```

### Clases CSS por modo
- Maritime → "active-card-maritime"
- Air → "active-card-air"
- Default → "active-card-land"

### Historial
Botón "📜 Ver Historial" toggle. Muestra últimas 20 cargas completadas/fallidas.

---

## 3.5 — AgentsPanel.cs (227 líneas)

**Ruta:** `Assets/_Game/UI/Panels/AgentsPanel.cs`
**Namespace:** `FreightForwarder.UI.Panels`

### Propósito
Muestra todos los agentes de transporte con su información, personalidad, estado, y nivel de confianza.

### Tarjeta de Agente
```
[Emoji personalidad] Maersk Logistics    🤝 Confianza: 75%
🎭 Confiable
🚛 Marítimo
💰 Precio: x1.20 | ⚡ Velocidad: x0.95
💍 Socio | ✅ Disponible | 📦 Carga: 2/5
```

### Clases CSS por confianza
```csharp
if (agent.PlayerTrust >= 70)      card.AddToClassList("agent-trust-high");
else if (agent.PlayerTrust >= 40) card.AddToClassList("agent-trust-medium");
else                               card.AddToClassList("agent-trust-low");
```

### Emojis de Personalidad
```csharp
Reliable→"🛡️", Cheap→"💰", Ambitious→"📈", Lazy→"😴", Friendly→"🤗",
Elusive→"👻", Efficient→"⚡", Scammer→"🎭", Liar→"🤥", Bipolar→"🎢",
Envious→"😤", Disappearing→"💨", Loyal→"🤝", Rival→"⚔️"
```

---

## 3.6 — FinancesPanel.cs (177 líneas)

**Ruta:** `Assets/_Game/UI/Panels/FinancesPanel.cs`
**Namespace:** `FreightForwarder.UI.Panels`

### Propósito
Dashboard financiero con todas las métricas económicas del juego.

### Métricas Mostradas
Columna izquierda:
- 💰 EFECTIVO: Dinero actual
- 📈 Ingresos Totales
- 📉 Gastos Totales
- 💵 Beneficio Neto (Ingresos - Gastos)
- 🏢 Costos Mensuales

Columna derecha:
- ⭐ Nivel
- 📦 Cargas Completadas
- 📊 Tasa de Éxito

### Auto-actualización
Se suscribe a:
- `EconomyManager.OnMoneyChanged`
- `EconomyManager.OnReputationChanged`
- `EconomyManager.OnLevelUp`
- `CargoManager.OnCargoCompleted`
- `CargoManager.OnCargoFailed`

---

## 3.7 — OfficesPanel.cs (200 líneas)

**Ruta:** `Assets/_Game/UI/Panels/OfficesPanel.cs`
**Namespace:** `FreightForwarder.UI.Panels`

### Propósito
Gestión de ciudades y oficinas. Muestra progreso de desbloqueo, ciudades desbloqueadas con opción de upgrade, y ciudades bloqueadas con opción de desbloqueo.

### Secciones
1. Barra de progreso: "🌍 Progreso: X/10 ciudades"
2. ✅ DESBLOQUEADAS — Tarjetas con:
   - 🏙️ Nombre, País
   - Infraestructura: 🚢 puerto | ✈️ aeropuerto | 🚛 hub terrestre
   - ⭐ Nivel X
   - Botón ⬆️ $X,XXX (upgrade)
3. 🔒 BLOQUEADAS — Tarjetas con:
   - Información de la ciudad
   - Botón 🔓 $10,000 (desbloquear)

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 4: MAPA 3D (Assets/_Game/Map/)
# ═══════════════════════════════════════════════════════════════

---

## 4.1 — WorldMap.cs (460 líneas)

**Ruta:** `Assets/_Game/Map/WorldMap.cs`
**Namespace:** `FreightForwarder.Map`
**Herencia:** `Singleton<WorldMap>`

### Propósito
Controlador del globo terrestre 3D con texturas estacionales (12 texturas, una por mes), marcadores de ciudades interactivos, rutas comerciales, y efectos climáticos.

### Configuración
```csharp
private float _earthRadius = 10f;                       // Radio del globo
private Texture2D[] _monthlyTextures;                   // 12 texturas estacionales
private bool _enableSeasonalTextures = true;
private GameObject _cityMarkerPrefab;                   // Prefab de marcador
private float _markerScale = 0.2f;
private Color _unlockedCityColor = verde;
private Color _lockedCityColor = gris;
private Color _officeCityColor = dorado;
```

### Efectos Climáticos
```csharp
private ParticleSystem _rainEffect;                     // Lluvia (abril-junio)
private ParticleSystem _snowEffect;                     // Nieve (diciembre-febrero)
private ParticleSystem _cloudsEffect;                   // Nubes
```

### Eventos
```csharp
public event Action<WorldCity> OnCityClicked;
public event Action<WorldCity> OnCityHovered;
```

### Métodos Principales

#### CreateEarth()
Crea una esfera primitiva de Unity, la escala al radio configurado, y le asigna el material de textura.

#### LoadMonthlyTexturesFromResources()
Carga 12 texturas desde `Resources/Map/Textures/01` a `Resources/Map/Textures/12`.

#### CreateCityMarkers()
Para cada ciudad en CityDatabase, crea un marcador 3D en la posición esférica correspondiente:
```csharp
Vector3 position = city.ToSpherePosition(_earthRadius * 1.01f);  // Ligeramente sobre la superficie
```

#### SetMonthTexture(int month) / BlendToMonthTexture(int month)
Cambia la textura del globo según el mes del calendario del juego.

#### UpdateWeatherEffects()
```csharp
// Lluvia: abril-junio | Nieve: diciembre-febrero
if (month >= 4 && month <= 6) → activar lluvia
if (month == 12 || month <= 2) → activar nieve
```

#### CreateRoute(string originCityId, string destinationCityId, TransportMode mode)
Crea una línea arco entre dos ciudades en el globo con color según modo:
```csharp
Maritime → azul | Air → blanco | Land → verde
```

#### FocusOnCity(WorldCity city)
Mueve la cámara para enfocar una ciudad específica.

#### UpdateMarkerColor(string cityId)
Actualiza el color del marcador según el estado de la ciudad.

---

## 4.2 — CameraController.cs (109 líneas)

**Ruta:** `Assets/_Game/Map/CameraController.cs`
**Namespace:** `FreightForwarder.Map`

### Propósito
Control de cámara orbital alrededor del globo terrestre.

### Controles
- **Click derecho + arrastrar** → Rotar vista (yaw/pitch)
- **Scroll del mouse** → Zoom (acercar/alejar)

### Configuración
```csharp
private float _rotationSpeed = 2f;
private float _smoothSpeed = 5f;                        // Suavizado de movimiento
private float _zoomSpeed = 2f;
private float _minZoomDistance = 5f;                     // Zoom mínimo
private float _maxZoomDistance = 25f;                    // Zoom máximo
```

### Métodos
```csharp
public void Initialize(Camera camera, Transform target)
// Configura cámara y objetivo (el globo)

public void FocusOnPoint(Vector3 worldPoint)
// Mueve cámara para enfocar un punto (usado al click en ciudad)

public void ResetView()
// Vuelve a vista predeterminada (pitch=20°, distancia=15)
```

---

## 4.3 — CityMarker.cs (52 líneas)

**Ruta:** `Assets/_Game/Map/CityMarker.cs`
**Namespace:** `FreightForwarder.Map`

### Propósito
Marcador visual 3D para cada ciudad en el globo. Maneja interacción del mouse.

```csharp
public WorldCity City => _city;

public void Initialize(WorldCity city, WorldMap worldMap)
// Configura referencia a ciudad y mapa, agrega SphereCollider si no tiene

public void SetColor(Color color)
// Cambia color del material del marcador

private void OnMouseEnter()
// Agranda marcador a 1.3x, notifica hover al WorldMap

private void OnMouseExit()
// Vuelve al tamaño original

private void OnMouseDown()
// Notifica click al WorldMap
```

---

## 4.4 — RouteRenderer.cs (70 líneas)

**Ruta:** `Assets/_Game/Map/RouteRenderer.cs`
**Namespace:** `FreightForwarder.Map`

### Propósito
Renderiza rutas comerciales como arcos entre ciudades en el globo 3D.

### Método: Initialize
```csharp
public void Initialize(Vector3 origin, Vector3 destination, float radius,
                       Constants.TransportMode mode, Color color)
```

### DrawArc()
Dibuja un arco con 50 segmentos entre origen y destino:
```csharp
private Vector3 GetArcPoint(float t)
{
    Vector3 linearPoint = Vector3.Lerp(_origin, _destination, t);
    float arcFactor = Mathf.Sin(t * Mathf.PI) * 1.2f;  // Arco parabólico
    Vector3 outward = linearPoint.normalized;
    return linearPoint + outward * arcFactor;
}
```

La ruta se eleva sobre la superficie del globo en forma de arco, con el punto más alto en el medio del trayecto.

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 5: FLUJOS DEL JUEGO
# ═══════════════════════════════════════════════════════════════

---

## 5.1 — Flujo de Inicialización

```
1. GameBootstrapper.Awake() → DontDestroyOnLoad
2. GameBootstrapper.Start() → espera 0.5s
3. InitializeManagers():
   - TimeManager (Singleton auto-creado)
   - EconomyManager
   - ClientManager → crea 14 clientes predefinidos
   - AgentManager → crea 10 agentes predefinidos
   - CargoManager
   - EventManager → crea 20+ eventos predefinidos
4. SetupInitialData():
   - CityDatabase.Initialize() → 10 ciudades
   - Buenos Aires → IsUnlocked = true
   - EconomyManager.ResetGame() → $5,000, 50 rep
   - CargoManager.InitializeNewGame() → genera 2-3 cargas iniciales
5. LoadGameSceneAsync() → carga escena "Game"
6. Post-carga:
   - WorldMap → globo 3D con marcadores
   - SunController → ciclo día/noche
   - GameUI → interfaz completa
```

## 5.2 — Flujo de un Día de Juego

```
TimeManager.Update() acumula tiempo (30 seg reales = 1 día)
Cuando se completa un día:
  → TimeManager.OnDayPassed dispara:
    → CargoManager.OnDayPassed():
      1. UpdateActiveCargos: DaysRemaining-- por cada carga activa
         Si DaysRemaining <= 0 → CompleteCargo()
      2. CheckExpiredCargos: elimina cargas expiradas del mercado
      3. 30% chance de GenerateCargo() si < 7 en mercado
    
    → AgentManager.OnDayPassed():
      1. UpdateState() para cada agente
      2. UpdatePriceSurge() para cada agente
      3. DaysSinceLastUse++ para cada agente
      4. Verificar retorno de agentes desaparecidos
      5. TryPriceSurge() para agentes Ambitious
      6. TryDisappear() para agentes Elusive
    
    → EventManager.OnDayPassed():
      Para cada carga activa → CheckForEvent()
      Si un evento aplica → OnEventTriggered
    
    → SunController.UpdateSunPosition()
```

## 5.3 — Flujo de Cotización y Negociación

```
1. Jugador ve cargo en MarketPanel → click "Cotizar"
2. Se abre QuotePanel:
   - Elige TransportMode → filtra agentes disponibles
   - Elige Agent → calcula costo (Agent.CalculateCost)
   - Ingresa precio → calcula margen
3. Submit → crea Quote
4. ClientManager.ProcessNegotiation():
   - Calcula acceptanceChance (fórmula compleja)
   - Roll aleatorio:
     a) < acceptanceChance → ACEPTA
        → CargoManager.AcceptQuote() → cargo pasa a Active
     b) ≥ acceptanceChance && no último intento && 70% chance → CONTRAOFERTA
        → Quote.SetCounterOffer(calculatedPrice)
        → Jugador decide: aceptar o rechazar
     c) Rechaza → pierde intento (máx 3)
5. Si acepta → carga entra en tránsito
   → AgentManager.AssignCargoToAgent()
   → DaysRemaining empieza a decrementar cada día
```

## 5.4 — Flujo de Completar Carga

```
CargoManager.UpdateActiveCargos():
  cargo.DaysRemaining <= 0
  → CompleteCargo(cargo):
    1. Status = Completed
    2. Mueve a CompletedCargos (máx 100)
    3. EconomyManager.RecordCargoCompleted(revenue, cost)
       → +dinero, +50 XP, posible level up
    4. AgentManager.RecordDelivery(agentId, cargoId, true)
       → Agent: +PlayerTrust(2), +AgentTrust(1), ConsecutiveDeliveries++
       → Si 5+ consecutivas → descuento lealtad -3%
    5. AgentManager.RemoveCargoFromAgent()
    6. OnCargoCompleted event
```

## 5.5 — Flujo de Game Over

```
EconomyManager.CheckGameOver():
  Si Money <= -2000 O Reputation <= 0:
    → OnGameOver?.Invoke()
    → GameManager.TriggerGameOver()
    → Time.timeScale = 0
```

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 6: SISTEMA DE EVENTOS DETALLADO
# ═══════════════════════════════════════════════════════════════

Los eventos NO son aleatorios puros. Cada evento tiene condiciones contextuales:

| Evento | Severidad | Base% | Modos | Etapa | Países | Meses | Tipo Carga |
|--------|-----------|-------|-------|-------|--------|-------|------------|
| Inspección Aduanera | 2 | 8% | Todos | Destino | Todos | Todos | Todos |
| Congestión Portuaria | 2 | 10% | Maritime | Todos | Todos | Todos | Todos |
| Clima Adverso | 3 | 12% | Mar/Air | Todos | Todos | 6-8 | Todos |
| Daño a Mercancía | 3 | 5% | Todos | Todos | Todos | Todos | Todos |
| Huelga | 4 | 7% | Todos | Todos | AR/BR | 3-5 | Todos |
| Error Documentación | 1 | 8% | Todos | Todos | Todos | Todos | Todos |
| Falta Contenedores | 2 | 7% | Maritime | Todos | Todos | Todos | Todos |
| Ruta Cortada | 3 | 6% | Land | Todos | Todos | Todos | Todos |
| Aeropuerto Cerrado | 3 | 4% | Air | Todos | Todos | Todos | Todos |
| Robo de Carga | 5 | 3% | Land | Todos | Todos | Todos | Valuable |
| Sobrecosto Combustible | 1 | 15% | Todos | Todos | Todos | Todos | Todos |
| Quiebra Transportista | 5 | 2% | Todos | Todos | Todos | Todos | Todos |
| Peso Mal Declarado | 2 | 6% | Todos | Todos | Todos | Todos | Todos |
| Incendio Almacén | 5 | 1% | Todos | Origen | Todos | Todos | Todos |
| Inspección Fitosanitaria | 2 | 10% | Todos | Todos | Todos | Todos | Refrigerated |
| Feriado | 1 | 20% | Todos | Todos | Todos | Todos* | Todos |
| Demora Frontera | 2 | 8% | Land | Todos | Todos | Todos | Todos |
| Carga Rechazada | 4 | 3% | Todos | Destino | Todos | Todos | Todos |
| Disputa Seguro | 3 | 4% | Todos | Todos | Todos | Todos | Todos |
| Día del Trabajador | 1 | 50% | Todos | Todos | Todos | Mayo | Todos |

*Feriado: días 1 y 25 de cualquier mes.

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 7: RELACIONES Y CONFIANZA
# ═══════════════════════════════════════════════════════════════

## 7.1 — Agentes: Sistema de Confianza Bidireccional

```
PlayerTrust (0-100): Cómo el jugador confía en el agente
  +2 por entrega exitosa
  -8 por entrega fallida
  -15 por abandono de carga
  -7.5 si el agente abandonó

AgentTrust (0-100): Cómo el agente confía en el jugador
  +1 por entrega exitosa
  -4 por entrega fallida
  -15 por abandono de carga
  -15 si el jugador cambia de agente

Relación = promedio de ambas confianzas:
  ≥71 → Partner (precios -5%, descuentos lealtad)
  51-70 → Ally (precios -5%)
  31-50 → Friend
  11-30 → Good
  -10 a 10 → Neutral
  -30 a -11 → Bad (precios +15%)
  ≤-31 → Enemy (precios +30%)
```

## 7.2 — Clientes: Sistema de Relación y Enojo

```
RelationshipLevel (0-100):
  +2 a +8 por entrega exitosa (según problemas)
  -5 a -15 por entrega fallida
  +5 por aceptación de cotización (base)
  -3 por rechazo de cotización (base)
  VIP multiplica cambios por 1.5x
  Margen bajo (<15%) da +5 extra en aceptación

AngerLevel (0-5):
  Sube con rechazos, malos precios, fallos
  Baja 1 punto cada 5 días sin incidentes
  Si llega a 5 → BLACKLIST (no acepta más cotizaciones)
  Si baja de 5 → se desbloquea
```

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 8: FÓRMULAS Y BALANCE
# ═══════════════════════════════════════════════════════════════

## 8.1 — Cálculo de Costo del Agente
```
baseCost = distanciaKm * (pesoToneladas / 1000) * $0.50
transportMultiplier = Maritime(0.7), Air(2.5), Land(1.0), Rail(0.8), Multi(1.5)
cargoMultiplier = General(1.0), Refrig(1.3), Danger(1.5), Urgent(1.2), Valuable(1.4)
priceMultiplier = basePriceMultiplier * currentPriceMultiplier * lealtad * relación

finalCost = max(100, baseCost * transport * cargo * price)
```

## 8.2 — Cálculo de Valor Declarado
```
baseValue = 1000 + (distanciaKm / 20000) * 500000
multiplier = CargoValueMultipliers[tipo]
declaredValue = clamp(baseValue * multiplier * random(0.8, 1.2), 1000, 500000)
```

## 8.3 — Nivel Up
```
XP necesaria = nivel_actual * 200
XP por carga = 50
Bono al subir = nivel * $100 + 5 reputación
```

## 8.4 — Distancia Haversine
```
R = 6371 km
a = sin²(Δlat/2) + cos(lat1) * cos(lat2) * sin²(Δlon/2)
c = 2 * atan2(√a, √(1-a))
d = R * c
```

---

# ═══════════════════════════════════════════════════════════════
# SECCIÓN 9: NAMESPACES Y DEPENDENCIAS
# ═══════════════════════════════════════════════════════════════

```
FreightForwarder.Models    → Constants, Cargo, Agent, Client, Quote,
                              WorldCity, CityDatabase, GameEvent, SaveData

FreightForwarder.Managers  → GameManager, TimeManager, EconomyManager,
                              CargoManager, AgentManager, ClientManager,
                              EventManager, SaveManager

FreightForwarder.UI        → GameUI
FreightForwarder.UI.Panels → MarketPanel, QuotePanel, ActiveCargosPanel,
                              AgentsPanel, FinancesPanel, OfficesPanel

FreightForwarder.Map       → WorldMap, CameraController, CityMarker, RouteRenderer

FreightForwarder.Utils     → Singleton<T>

FreightForwarder.Core      → GameBootstrapper

FreightForwarder            → SunController (namespace raíz)
```

### Dependencias entre Managers
```
TimeManager       → (independiente, otros dependen de él)
EconomyManager    → (independiente)
ClientManager     → AgentManager (GetAllAgents)
AgentManager      → TimeManager (OnDayPassed)
CargoManager      → TimeManager, EconomyManager, AgentManager, CityDatabase
EventManager      → TimeManager, CargoManager
SaveManager       → Todos los managers
GameBootstrapper  → Todos los managers, CityDatabase
SunController     → TimeManager
```

---

# ═══════════════════════════════════════════════════════════════
# FIN DEL DOCUMENTO
# ═══════════════════════════════════════════════════════════════

**Total de archivos documentados:** 31
**Total de líneas de código fuente:** 9,152
**Namespaces cubiertos:** 7
**Clases documentadas:** 31 (incluyendo structs)
**Enums documentados:** 9
**Constantes de balance:** 20+
**Eventos aleatorios:** 20+
**Agentes predefinidos:** 10
**Ciudades predefinidas:** 10
**Clientes predefinidos:** 14
