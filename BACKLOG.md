# FF-Prot Backlog - Tareas Técnicas Priorizadas

## Estado Actual
- **Sin TODOs/FIXMEs** en el código fuente (`Assets/_Game`)
- Código bien documentado con comentarios en español (sección headers)
- Clases principales identificadas y revisadas

---

## Prioridad ALTA (Críticas para MVP)

### 1. Implementar SaveManager.cs (Save/Load)
- **Estado**: `SaveData` definido pero sin lógica de guardado
- **Tareas**:
  - [ ] Implementar `SaveGame()` que recolecte datos de todos los managers
  - [ ] Implementar `LoadGame()` que restaure el estado completo
  - [ ] Usar `JsonUtility` para serialización
  - [ ] Guardar en `Application.persistentDataPath`
  - [ ] Integración con `GameManager.StartNewGame()` y `GameManager.TriggerGameOver()`
  - [ ] Manejo de versiones de save (migración futura)
- **Archivos**: `Assets/_Game/Models/SaveManager.cs`

### 2. Completar UI Panels (MVP mínimo jugable)
- **Tareas**:
  - [ ] `QuotePanel.cs`: UI funcional para cotizar y negociar (3 rondas)
  - [ ] `MarketPanel.cs`: Listar cargas disponibles, botón cotizar
  - [ ] `ActiveCargosPanel.cs`: Seguimiento de cargas en tránsito
  - [ ] `FinancesPanel.cs`: Mostrar dinero, reputación, XP, estadísticas
  - [ ] Conectar botones UI con `GameUI.cs`
- **Archivos**: `Assets/_Game/UI/Panels/*.cs`, `Assets/_Game/UI/GameUI.cs`

### 3. Conectar CargoManager con flujo completo
- **Tareas**:
  - [ ] Verificar que `CargoManager.OnDayPassed()` actualice días restantes
  - [ ] Verificar que cargas expiradas se muevan a `FailedCargos`
  - [ ] Integrar EventManager con cargas activas
  - [ ] Disparar `EconomyManager.RecordCargoCompleted()` al completar
- **Archivos**: `Assets/_Game/Managers/CargoManager.cs`

---

## Prioridad MEDIA (Mejoras técnicas y refactors)

### 4. Refactor Client.cs (~793 líneas)
- **Problema**: Clase muy larga, muchas responsabilidades
- **Propuesta**:
  - [ ] Extraer `ClientPersonality` (líneas ~33-117)
  - [ ] Extraer `ClientRelations` (relación, enojo, blacklist)
  - [ ] Extraer `ClientStats` (histórico de entregas, quejas)
  - [ ] Mantener `Client.cs` como fachada (facade pattern)
- **Archivos**: `Assets/_Game/Models/Client.cs`

### 5. Refactor Quote.cs (~484 líneas)
- **Problema**: Lógica de negociación mezclada con datos
- **Propuesta**:
  - [ ] Extraer `NegotiationEngine` (cálculos de contraofertas)
  - [ ] Extraer `QuoteMath` (márgenes, precios sugeridos)
  - [ ] Mantener `Quote.cs` como modelo puro
- **Archivos**: `Assets/_Game/Models/Quote.cs`

### 6. Completar EventManager.cs (~817 líneas)
- **Tareas**:
  - [ ] Verificar que eventos se apliquen correctamente a cargas
  - [ ] Implementar `ResolveEvent()` con las 3 opciones
  - [ ] Conectar con UI para mostrar eventos al jugador
  - [ ] Evaluar si el pool de eventos debe cargarse desde JSON
- **Archivos**: `Assets/_Game/Managers/EventManager.cs`

### 7. WorldMap - Mejoras visuales
- **Tareas**:
  - [ ] Verificar que `RouteRenderer` funcione correctamente
  - [ ] Conectar `CityMarker.OnClick` con `GameUI` para mostrar info
  - [ ] Efectos climáticos (lluvia, nieve) según textura mensual
- **Archivos**: `Assets/_Game/Map/WorldMap.cs`, `RouteRenderer.cs`, `CityMarker.cs`

---

## Prioridad BAJA (Optimizaciones y pulido)

### 8. Estandarizar comentarios
- **Tareas**:
  - [ ] Decidir: ¿Documentación XML (`///`) o comentarios simples (`//`)?
  - [ ] Uniformar headers de sección ("MÉTODOS DE..." → ¿Estandar inglés/español?)
  - [ ] Evaluar si `Constants.cs` es "Dios objeto" (considerar dividir)

### 9. AgentManager - Comportamientos avanzados
- **Tareas**:
  - [ ] Probar todos los comportamientos: PriceSurge, Disappear, Scam, Lie, Sabotage, Abandon
  - [ ] Ajustar probabilidades según dificultad
  - [ ] UI para reportar comportamientos de agentes al jugador

### 10. CityDatabase y WorldCity
- **Tareas**:
  - [ ] Verificar que `CityDatabase` esté poblada correctamente
  - [ ] Añadir más ciudades (mínimo 12 para MVP)
  - [ ] Implementar `DistanceTo()` con coordenadas reales (lat/long)
- **Archivos**: `Assets/_Game/Models/WorldCity.cs`, `CityDatabase` (por encontrar)

---

## Métricas de Código Actual

| Módulo | Líneas | Estado | Prioridad Refactor |
|--------|---------|--------|-------------------|
| Client.cs | ~793 | ✅ Funcional | 🔧 Media (dividir) |
| EventManager.cs | ~817 | ⚠️ Revisar | 🔧 Media (completar) |
| Quote.cs | ~484 | ✅ Funcional | 🔧 Baja (extraer lógica) |
| CargoManager.cs | ~621 | ⚠️ Conectar | 🔧 Baja |
| Cargo.cs | ~350 | ✅ Funcional | ✅ OK |
| AgentManager.cs | ~231 | ✅ Funcional | ✅ OK |
| EconomyManager.cs | ~247 | ✅ Funcional | ✅ OK |
| TimeManager.cs | ~83 | ✅ Funcional | ✅ OK |
| GameManager.cs | ~84 | ✅ Funcional | ✅ OK |
| SaveManager.cs | ~123 | ❌ Sin lógica | 🔴 Alta (implementar) |
| WorldMap.cs | ~459 | ⚠️ Revisar | 🔧 Baja |

---

## Sprints Sugeridos (2 semanas cada uno)

### Sprint 1: "MVP Playable"
- [ ] Implementar SaveManager (Save/Load)
- [ ] Completar UI básica (QuotePanel, MarketPanel)
- [ ] Conectar flujo Cargo → Quote → Agent → Transit → Complete

### Sprint 2: "Events & Polish"
- [ ] Completar EventManager UI (mostrar eventos al jugador)
- [ ] Pulir WorldMap (marcadores, rutas)
- [ ] Probar ciclo completo 0 → Game Over

### Sprint 3: "Refactors & QoL"
- [ ] Refactor Client.cs (si es necesario)
- [ ] Ajustar balance (precios, probabilidades)
- [ ] Efectos visuales y audio
