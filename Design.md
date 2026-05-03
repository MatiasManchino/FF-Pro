# Freight Forwarder - Design Doc (MVP)

## Concepto del Juego
Simulador de logística internacional donde el jugador gestiona una empresa de **Freight Forwarding**, conectando ciudades globales, cotizando cargas, y gestionando relaciones con clientes y agentes de transporte.

## MVP - Alcance Mínimo Viable

### Core Loop (Flujo principal)
1. **TimeManager** avanza días → `OnDayPassed`
2. **Clientes** generan cargas → aparecen en Market (máx 7)
3. Jugador **cotiza** (Quote) → hasta 3 rondas de negociación
4. Se asigna **Agente** + **Modo de Transporte** → Carga en tránsito
5. **EventManager** dispara eventos contextuales (aduana, clima, huelgas)
6. Carga llega → **EconomyManager** actualiza dinero, reputación, XP
7. **SaveManager** persiste progreso

### Mecánicas Centrales

#### Cargas (Cargo.cs)
- **Tipos**: General, Refrigerada, Peligrosa, Urgente, Valiosa
- **Estados**: Available → Quoting → Active → Completed/Failed/Expired
- **Atributos**: Peso (1-500t), Volumen (1-200m³), Valor ($1000-$500k)
- **Rutas**: Origin → Destination + Waypoints (visualizado en WorldMap)

#### Cotización y Negociación (Quote.cs)
- Máximo **3 intentos** por carga
- Cliente puede: Aceptar, Rechazar, Hacer contraoferta
- Margen calculado: `(OfferedPrice - AgentCost) / OfferedPrice`
- Factores: ClientType, relación, urgencia

#### Clientes (Client.cs)
- **Personalidad activa**: GoodPayer, BadPayer, UrgentClient, VIP, etc.
- **Sistema de relación**: 0-100, afecta tolerancia y negociación
- **Enojo**: 0-5 niveles, puede causar bloqueo (blacklist)
- **Histórico**: Entregas, quejas, recomendaciones

#### Agentes (Agent.cs + AgentManager.cs)
- **Personalidades**: Reliable, Ambitious, Efficient, Cheap, Friendly, Lazy, Loyal, Scammer, Envious, Elusive
- **Comportamiento dinámico**: Desapariciones, estafas, abandono, mentiras, sabotaje
- **Cargas activas** por agente, multiplicadores de precio

#### Eventos (GameEvent.cs + EventManager.cs)
- **Contextuales** (no puramente aleatorios):
  - Aduana, Congestión Portuaria, Clima, Daño, Huelgas
  - Dependen de: ubicación, transporte, etapa, fecha, tipo de carga
- **Opciones de resolución** con costos/demora/reputación

#### Economía (EconomyManager.cs)
- **Dinero inicial**: $5000
- **Reputación inicial**: 50/100
- **Niveles**: XP acumulada, bonos por nivel
- **Game Over**: Dinero ≤ -$10000 O Reputación ≤ 0
- **Costos mensuales**: Oficinas operativas

#### Tiempo (TimeManager.cs)
- Día dura **30 segundos reales** (configurable)
- Velocidades: 0x (pausa), 1x, 2x, 3x
- Eventos: `OnDayPassed`, `OnMonthPassed`

### Mapa (WorldMap.cs)
- Globo 3D con texturas estacionales (12 meses)
- **CityMarker**: Ciudades desbloqueadas, oficinas, rutas
- **RouteRenderer**: Visualización de rutas de carga activas
- Ciudades: Buenos Aires, Miami, Shanghai, Rotterdam, Dubai, etc.

### UI (Paneles)
- **QuotePanel**: Cotización y negociación
- **MarketPanel**: Cargas disponibles
- **ActiveCargosPanel**: Seguimiento de envíos
- **FinancesPanel**: Dinero, reputación, estadísticas
- **AgentsPanel**: Gestión de agentes
- **OfficesPanel**: Oficinas en ciudades

## Reglas del Juego

### Win Condition
No hay "victoria" tradicional. El juego es un **simulador de gestión** donde el objetivo es:
- Alcanzar **Nivel 10+** con reputación > 80
- Completar **100+ cargas** exitosamente
- Desbloquear **todas las ciudades** y tener oficinas en 5+ ciudades

### Lose Condition (Game Over)
- Dinero ≤ **-$10,000** (deuda insostenible)
- Reputación ≤ **0** (empresa destruida)

### Reglas de Negociación
- Máximo 3 rondas por cotización
- Contraoferta del cliente: 70%-95% del precio ofrecido
- Margen mínimo recomendado: **15%** (según tipo de cliente)

### Reglas de Transporte
| Modo | Costo | Días | Riesgo |
|------|-------|------|-------|
| Marítimo | Bajo | 15-40 | Clima, puertos |
| Aéreo | Alto | 2-7 | Costo, capacidad |
| Terrestre | Medio | 5-15 | Huelgas, daños |
| Ferroviario | Bajo | 10-25 | Infraestructura |
| Multimodal | Variable | Variable | Combinado |

## Modelo de Datos (Resumen)

```
GameManager (Singleton)
├── TimeManager (días, fecha, velocidad)
├── EconomyManager (dinero, reputación, XP, nivel)
├── CargoManager (MarketCargos, ActiveCargos, CompletedCargos)
├── ClientManager (Clients, relaciones)
├── AgentManager (Agents, cargas asignadas)
├── EventManager (Pool de eventos, historial)
├── SaveManager (SaveData, persistencia)
└── WorldMap (CityMarkers, Routes, Globo 3D)
    ├── CityMarker (ciudad, estado)
    └── RouteRenderer (rutas visuales)
```

## Próximos Pasos (Post-MVP)
1. **UI Polishing**: Completar paneles faltantes, animaciones
2. **Audio**: Efectos de sonido, música ambiental
3. **Tutorial**: Primeros 5 días guiados
4. **Logros**: Sistema de achievements
5. **Multijugador**: ¿Competitivo o cooperativo?
