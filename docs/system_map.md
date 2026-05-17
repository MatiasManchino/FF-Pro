# FF-Map-Only — System Map
_Generado: 2026-05-17_

## Árbol de dependencias

```
GameBootstrapper (global namespace, Assets/Scripts/)
 ├── TimeManager          (global namespace) — reloj UTC real, dispara OnNewDay/OnNewMonth
 ├── GameManager          — estados Playing/Paused/GameOver, delega TimeScale a TimeManager
 ├── FFTimeManager        — puente: escucha TimeManager.OnNewDay → dispara OnDayPassed
 ├── EconomyManager       — dinero, reputación, XP, niveles
 ├── AgentManager         — pool de 10 agentes fijos, decisions diarias
 ├── ClientManager        — clientes, negociación básica
 ├── CargoManager         — mercado, tránsito, completado
 ├── EventManager         — 20 eventos aleatorios diarios en cargas activas
 ├── RouteManager         — spawn/destroy CargoRoute (visual 3D) al aceptar/completar carga
 ├── WeatherManager       — wrapper, sin lógica propia
 ├── CloudRenderer        — sprites de nubes sobre el globo
 ├── WeatherImpact        — aplica impacto del clima a cargas
 ├── HurricaneController  — sprite animado de huracán
 └── WeatherSystem        — tick de simulación: noise → grid → CloudRenderer.Refresh
```

## Detalle por manager

### GameManager
- **Inputs:** nada externo (se inicializa en MainMenu)
- **Outputs:** `OnGameStateChanged`, `OnNewGameStarted`, `OnGameOver`
- **Dependencias:** `TimeManager` (para pausar el reloj)
- **Eventos disparados:** `StartNewGame()`, `PauseGame()`, `ResumeGame()`, `TriggerGameOver()`

### FFTimeManager
- **Inputs:** `TimeManager.OnNewDay`, `TimeManager.OnNewMonth`
- **Outputs:** `OnDayPassed`, `OnMonthPassed`, `OnDateChanged`
- **Dependencias:** `TimeManager` (global)
- **Consumidores:** `CargoManager`, `ClientManager`, `AgentManager`, `EventManager`

### EconomyManager
- **Inputs:** llamadas directas desde CargoManager, EventManager
- **Outputs:** `OnMoneyChanged`, `OnReputationChanged`, `OnLevelUp`, `OnXPGained`, `OnGameOver`
- **Dependencias:** `GameManager` (para trigger game over), `Constants` (thresholds)
- **⚠️ Acoplamiento:** CargoManager llama `RecordCargoCompleted` con revenue+cost calculados externamente

### CargoManager
- **Inputs:** `FFTimeManager.OnDayPassed`, `ClientManager.GetOrCreateClient`, `AgentManager.GetAgent`
- **Outputs:** `OnCargoAddedToMarket`, `OnCargoAccepted`, `OnCargoCompleted`, `OnCargoFailed`, `OnCargoExpired`
- **Dependencias:** `CityDatabase`, `FFTimeManager`, `ClientManager`, `AgentManager`, `EconomyManager`
- **Calcula:** distancia (Haversine via `CityDatabase.GetDistance`), precio base, días de tránsito
- **⚠️ Acoplamiento peligroso:** llama a `EconomyManager`, `AgentManager`, `ClientManager` directamente desde `CompleteCargo`/`FailCargo`

### AgentManager
- **Inputs:** `FFTimeManager.OnDayPassed` → `ProcessAgentDecisions`
- **Outputs:** `OnPriceSurge`, `OnCargoAbandoned`, `OnAgentDisappeared`, `OnAgentScam`, `OnAgentLied`, `OnAgentSabotage`, `OnAgentReturned`, `OnAgentBankrupt`
- **Dependencias:** `FFTimeManager`, `Constants`
- **Estado interno:** 10 agentes fijos con personalidad, trust, carga actual

### ClientManager
- **Inputs:** `FFTimeManager.OnDayPassed`, llamadas de `CargoManager`
- **Outputs:** `OnClientAdded`, `OnClientBlacklisted`, `OnClientBecameVip`, `OnNegotiationResult`
- **Lógica de negociación:** `EvaluateQuote()` — probabilística con controferta en ≤2 rondas

### EventManager
- **Inputs:** `FFTimeManager.OnDayPassed` → itera `CargoManager.ActiveCargos`
- **Outputs:** `OnEventTriggered`, `OnEventResolved`
- **Dependencias:** `CargoManager`, `AgentManager`, `EconomyManager`, `FFTimeManager`
- **Pool:** 20 eventos definidos en `InitializeEventPool()`

### RouteManager
- **Inputs:** `CargoManager.OnCargoAccepted`, `OnCargoCompleted`, `OnCargoFailed`
- **Outputs:** spawn/destroy `CargoRoute` GameObjects en la escena 3D
- **Dependencias:** `CityDatabase`, `CargoManager`

### WeatherSystem
- **Inputs:** Unity Update (ticker propio)
- **Outputs:** `CloudRenderer.Refresh()`, grid de celdas 64×32
- **Dependencias:** `CloudRenderer`, `WeatherImpact`

## Flujo completo de una carga

```
1. CargoManager.GenerateCargo()
   → crea Cargo con origen, destino, tipo, cliente, precio
   → dispara OnCargoAddedToMarket

2. UI: jugador acepta cotización → CargoManager.AcceptQuote(cargo, quote)
   → calcula días de tránsito según modo y agente
   → asigna agente (AgentManager.AssignCargoToAgent)
   → dispara OnCargoAccepted → RouteManager crea visual

3. Cada día (FFTimeManager.OnDayPassed):
   → CargoManager.UpdateActiveCargos: DaysRemaining--
   → EventManager.ProcessDailyEvents: 12% chance evento
   → AgentManager.ProcessAgentDecisions: behaviors

4. DaysRemaining == 0 → CargoManager.CompleteCargo()
   → EconomyManager.RecordCargoCompleted(revenue, agentCost)
   → AgentManager.RecordDelivery
   → ClientManager.NotifyDelivery
   → dispara OnCargoCompleted → RouteManager destruye visual
```

## Acoplamientos peligrosos detectados

| Lugar | Problema | Riesgo |
|-------|----------|--------|
| `CargoManager.CompleteCargo()` | Llama directamente a EconomyManager, AgentManager, ClientManager | Si uno falla, el cargo queda en estado inconsistente |
| `EconomyManager.SubtractMoney()` | Siempre retorna `true` aunque falle | UI puede mostrar operación exitosa con dinero negativo |
| `CargoManager.GenerateCargo()` | Itera `CityDatabase.AllCities.Keys` sin verificar que haya ≥2 ciudades | Puede romper si CityDatabase no se inicializó |
| `AgentManager.CheckCargoAbandonment()` | Nunca se llama desde ningún manager | Comportamiento de abandono nunca se ejecuta |
| `EventManager._eventPool` | `OnEventResolved` nunca se dispara | El evento no tiene resolución registrada |

## Código muerto identificado

| Archivo | Símbolo | Estado |
|---------|---------|--------|
| `AgentManager` | `CheckLie()`, `CheckSabotage()`, `CheckScam()` | Nunca llamados desde afuera |
| `AgentManager` | `CheckCargoAbandonment()` | Nunca llamado |
| `ClientManager` | `OnNegotiationResult` evento | Nunca disparado |
| `EventManager` | `OnEventResolved` evento | Nunca disparado |
| `EconomyManager` | `MonthlyOfficeCosts`, `ProcessMonthlyCosts()` | Nunca llamado |
| `CargoRoute.cs` | Necesita revisión — ¿renderiza algo? | Por verificar |
| `FFUIManager_Final.cs` | Duplica GameHUD.cs — por revisar cuál es activo | Por verificar |
| `InteractionSystem.cs` | Por revisar uso | Por verificar |
| `SpeedController.cs` | Por revisar uso — parece duplicar TimeManager | Por verificar |
