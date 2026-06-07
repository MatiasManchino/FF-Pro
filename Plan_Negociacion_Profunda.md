# FF — Plan del Sistema de Negociación ("El Escritorio")

> Documento de rumbo del **núcleo de FreightForwarder**. Consolida el trabajo de:
> - `/office-hours` (Santiago) — rumbo del producto
> - `/plan-ceo-review` (Victoria) — versión "10 estrellas" + alcance
> - Análisis de: `FREIGHT FORWARDER – GDD + Ejemplos.md`, `Esto hice.md`, `FF_10_Ejemplos_Negociaciones.docx`
> - El código actual (`NegotiationEngine.cs`, `Client.cs`, `MarketPanel.cs`)
>
> Fecha: 2026-05-31 · Objetivo del proyecto: **comercializar (Steam/indie)**

---

## 0. La realización

**La tabla de negociación ya está casi diseñada** (en `Esto hice.md`): hay 6 personalidades,
7 tácticas, una fórmula de aceptación y 10 negociaciones completas de ejemplo. Lo que falta
**no es diseño — es decidir el alcance de la v1 y construirlo.**

## 1. Rumbo (Santiago + Victoria)
FF es un **tycoon cuyo núcleo es la negociación** con clientes. El loop:
- **Negociar / cerrar tratos** = el verbo (lo que hacés 100 veces).
- **Crecer** (sucursales, rutas, clientes más grandes) = lo que escala el verbo.
- **No quebrar / equilibrar la caja** = la presión.
- **Logros** = los mojones.

Primer hito jugable y testeable: **"El Escritorio"** — el núcleo de negociación pelado, sin
globo 3D ni vehículos, jugable 20 minutos por alguien que no sea el autor.

## 2. El insight clave (arregla el bug de "clientes difíciles imposibles")
El **piso de 10% de margen es sagrado** (regla real del oficio). Por lo tanto:

> Contra un cliente sensible al precio **no se puede ganar bajando el precio**. Hay que cerrar
> con las **otras palancas**: plazo de pago, transporte, volumen, servicio y tácticas.

Eso convierte "imposible" en "se puede, con oficio" = **la negociación profunda**. Es el corazón
del juego que hoy está a medio activar (hoy la negociación es un cálculo oculto + un dado, el
jugador "solo aprieta botones y siempre gana").

## 3. La tabla de tensión (validada)
Cada trato obliga a elegir entre cosas buenas que no se pueden maximizar a la vez.

| Palanca | Rango | Tensión real | ¿v1 "El Escritorio"? |
|---|---|---|---|
| **Precio** | costo ×1.10 → ×1.50 (**piso 10% sagrado**) | ganancia vs. probabilidad de cerrar | ✅ Sí (núcleo) |
| **Plazo de pago** | anticipo / contado / 15 / 30 / 45 / 60 días | **liquidez** (pagás al proveedor a ~30d; si das 60, trabajás con tu plata) vs. atraer al cliente | ✅ Sí |
| **Transporte** | marítimo (barato/lento) vs. aéreo (caro/rápido) | margen vs. rapidez (clave para el "Apurado") | ✅ Sí |
| **Seguro (lo paga el cliente)** | sí / no | comodidad vs. relación (si no asegura y se daña, te odia) | ⏸️ v1.1 |
| **Volumen / exclusividad** | normal / volumen / próximas N cargas | ganancia hoy vs. ingresos futuros | ⏸️ v1.1 |

### Variables del mundo real que hay que incluir sí o sí
- **Cash-flow = el filo del plazo de pago:** todos los meses se descuenta caja (sueldos/costos
  fijos). A veces se cierra un mal trato **solo para sobrevivir el mes**.
- **Condiciones de derrota reales:** (1) perder al cliente (lo peor); (2) que no pague (juicio,
  años); (3) que **el negocio del propio cliente no cierre** porque tu flete se lo encarece
  demasiado → se pierde la operación sin que haya competidor.
- **Servicio = retención:** "estar encima de la carga" (resolver documentación, aduana,
  transporte, siniestros) sube la relación → más cargas y precios más altos después.

## 4. Personalidades de cliente (ocultas, se infieren por pistas)
Fuente: `Esto hice.md` (PARTE 2). **v1 = las 3 marcadas.** Las demás, después.

| ID | Nombre | Rasgos (0-100) | Comportamiento | v1 |
|---|---|---|---|---|
| `rata` | El Rata | precio 90, urgencia 20, lealtad 20, pago 30, servicio 10 | siempre pide descuento, compara, no cierra fácil; maxRondas 3, concesión baja | ✅ |
| `apurado` | El Apurado | precio 30, urgencia 90, lealtad 40, pago 50, servicio 60 | quiere cerrar ya, acepta precios altos si es aéreo; maxRondas 1-2, concesión alta | ✅ |
| `confiable` | El Confiable | precio 40, urgencia 30, lealtad 85, pago 85, servicio 75 | paga bien, leal, valora servicio; maxRondas 3, concesión media | ✅ |
| `trampa` | El Trampa | precio 85, lealtad 10, pago 20, servicio 15 | promete volumen, después no paga; te hace perder rondas | ⏸️ v1.1 |
| `volumen` | El Volumen | precio 70, lealtad 50 | precio bajo pero muchas cargas | ⏸️ v1.1 |
| `desconfiado` | El Desconfiado | servicio 80, pide garantías | si ganás su confianza, se vuelve leal | ⏸️ v1.1 |

> Las personalidades arrancan **ocultas**; el jugador ve **pistas** ("parece tacaño", "parece
> pagador") al avanzar. Diseño extra del autor: que la personalidad **pueda mejorar** con el
> tiempo (un mal pagador se vuelve bueno) — raro, pero posible. (⏸️ v1.1)

## 5. Tácticas del jugador
Fuente: `Esto hice.md` (PARTE 5). **v1 = las 3 marcadas.**

| Táctica | Efecto | v1 |
|---|---|---|
| **Presión de tiempo** | +20% aceptación si el cliente es urgente; -10% si no. ("Los precios suben, cerremos hoy.") | ✅ |
| **Argumento de valor** | +15% si valora el servicio; +satisfacción. ("Te voy a estar encima de la carga 24/7.") | ✅ |
| **Descuento por pago anticipado** | +10% aceptación a cambio de menos margen. ("Al contado te bajo un 25%.") | ✅ |
| Farol al cliente | +10% pero riesgo de que lo descubra | ⏸️ |
| Exclusividad por descuento | -5% precio a cambio de próximas N cargas | ⏸️ |
| Revelar costo (transparencia) | +20% si confiable, -15% si rata | ⏸️ |
| Llamada al supervisor (simular) | baja automática de precio | ⏸️ |

## 6. Fórmula de aceptación y rondas
- Fórmula base ya escrita en `Esto hice.md` (PARTE 4): aceptación = base + precio vs. target +
  plazo + transporte + (volumen) + relación/memoria − enojo + táctica × mercado.
- **Ajuste clave:** tunear para que el **piso 10% + cliente duro** generen tensión real (que se
  gane con las otras palancas, no que sea imposible).
- **Rondas con memoria** (reemplaza el "contador con piel de sistema"): el cliente recuerda tus
  ofertas dentro de la negociación, cede más si estás cerca, se endurece si fuiste agresivo, y se
  retira si el enojo supera su umbral. Concesión y paciencia dependen de la personalidad.

## 7. Plan técnico (Mateo)
- **Sistema nuevo y apagable:** `AdvancedNegotiationEngine` detrás de
  `FeatureFlags.USE_ADVANCED_NEGOTIATION`, **sin tocar** `NegotiationEngine` actual. (Cumple el
  pedido: activar/desactivar, no romper los 62 archivos.)
- **Modelos** (ya especificados en `Esto hice.md` PARTE 1): `ClientPersonality`,
  `ClientNegotiationState`, `AdvancedQuote` (v1: precio + plazo + transporte).
- **UI del escritorio:** las palancas + barras de **satisfacción/enojo** + **pistas** +
  barra de **"distancia al acuerdo"** + contador de rondas + botones de táctica. (Extiende o
  reemplaza el panel de cotización actual.)
- **💡 Los 10 ejemplos del `.docx` = banco de pruebas.** Convertirlos en **escenarios
  scripteados** para testear "¿es divertido?" en minutos y validar que Rata / Apurado /
  Confiable se sientan distintos. (Hoy no hay forma de testear rápido — esto lo resuelve.)

## 8. Plan por fases (~2 semanas)
0. **Lockear diseño v1** (casi listo): 3 palancas + 3 personalidades + 3 tácticas. *(~½ día)*
1. **Motor + modelos** detrás del flag. *(2-3 días)*
2. **Las 3 palancas + fórmula + rondas con memoria.** *(2-3 días)*
3. **UI del escritorio** (palancas, barras, pistas, distancia al acuerdo). *(2-3 días)*
4. **Cargar los 10 ejemplos como escenarios de test** + jugar/medir + un amigo 20 min. *(1-2 días)*

## 9. Criterios de éxito
La prueba no es "quedó lindo". Es: **un amigo juega 20 min sin ayuda y vos mirás callado.**
- **SÍ:** pide "un trato más", pierde la noción del tiempo, se frustra cuando le va mal.
- **NO:** se aburre, solo aprieta "aceptar", no entiende qué decisión es interesante.
- Repetir con 3 personas. 2 de 3 con el "una más" → el núcleo está. Si no → rediseñar la
  negociación **antes** de invertir un minuto más en el globo.

## 10. Diferido a v2 (oro, pero después)
- **Negociación con el proveedor** (la 2da capa: pedirle precio para poder cerrar al cliente;
  faroles al proveedor; contratos de volumen con la línea marítima al 5% pero apuntando a
  volumen). Es enorme y muy real — pero es la capa siguiente.
- Empleados con habilidades (Pricing, Operaciones, Customer Service…) y la progresión 1-20.
- Seguro, volumen/exclusividad, las 6 personalidades completas, las 7 tácticas.
- El globo 3D, vehículos, multimodal, clima (ya construidos; se reconectan cuando el núcleo divierta).

## 11. Fuentes
- `FREIGHT FORWARDER – GDD + Ejemplos.md` — visión, loop, niveles, empleados, economía.
- `Esto hice.md` — entrevista de dominio (experiencia real del autor) + diseño de negociación.
- `FF_10_Ejemplos_Negociaciones.docx` — 10 negociaciones completas (marítimo/aéreo/terrestre).
- Documento de rumbo gstack: `~/.gstack/projects/MatiasManchino-FF-Pro/matia-dev_refactor_core-design-20260531-145002.md`

---

## 12. Aprendizajes del prototipo "El Escritorio" (web) — 2026-06-06

Se construyó un prototipo web jugable (HTML/JS single-file) en
`FF-V1-Testeo/index.html`, desplegado en
https://matiasmanchino.github.io/ff-escritorio-demo/, con un sistema de
analytics propio (eventos → Google Sheets) que registró sesiones reales.
Esta sección consolida lo que el prototipo **validó, refinó y dejó pendiente**.
Es la referencia para la implementación en Unity: **copiar lo que ya funciona.**

### 12.1 Layout que funcionó (clave de UX)
Tres paneles con swipe horizontal: **AGENTES (0) ← ESCRITORIO/HUB (1) → CLIENTES (2)**.
El flujo correcto es **primero conseguir tarifa con el agente, después cotizarle al
cliente**. Separarlo en paneles distintos hizo que el jugador entendiera ese orden
sin tutorial. (En Unity: replicar esta separación conceptual aunque cambie la forma.)

### 12.2 Mecánicas VALIDADAS (funcionan y enganchan)
- **Doble negociación agente→cliente** = el corazón. El feedback textual fue
  *"la negociación con agentes es adictiva"*. Es el diferencial.
- **Piso del 10% sagrado + Ahorrador** genera la tensión real: contra el sensible al
  precio no se gana bajando el precio, hay que usar plazo/transporte/tácticas.
- **Presión de caja (cash-flow)**: pagás al agente a ~15d, cobrás al cliente a 0-60d.
  Dar plazos largos te seca la caja. Los jugadores **aprenden esto a los golpes**
  (quiebran, reinician, ajustan) — es el segundo gran enganche junto a la negociación.
- **Tácticas con agente** (farol, prometer volumen, pedir prioridad) y **con cliente**
  (presión de tiempo, argumento de valor, pago anticipado): se usan y se entienden.
- **Regateo con el agente** (pedir descuento, máx 3 rondas, con memoria de relación).
- **Contraoferta del cliente con botón "Aceptar $X"**: cierre rápido muy usado.

### 12.3 Refinamientos que surgieron del testeo (incluir en el diseño)
1. **Precio inicial variable por operación** (`startMarkup` aleatorio ~1.12–1.45). Evita
   que exista una fórmula dominante ("agente barato + 18% + enviar"). Obliga a mirar las
   palancas en cada trato.
2. **Hint inteligente precio-vs-táctica**: cuando el cliente rechaza, si el precio está
   por encima de lo que banca → "bajá el precio" (las tácticas NO arreglan precio alto);
   si el precio ya es razonable → sugerir táctica/plazo. Esto **enseña la profundidad sin
   tutorial** y convirtió deals antes perdidos en cierres (validado en data real).
3. **Respuesta del cliente visible como modal** (no como texto perdido en pantalla), con
   su interés % y la ronda. Rompe el "enviar sin leer".
4. **Anti-spam de reenvío**: reenviar la misma oferta no gasta ronda y avisa que hay que
   cambiar algo. Antes la gente perdía deals insistiendo igual sin entender por qué.
5. **Rampa de dificultad (onboarding del minuto 1)**: los primeros 5 clientes son
   sencillos (nunca Ahorrador, mercado tranquilo, techo holgado, nunca imposibles, precio
   inicial cercano a lo aceptable). Del 6to en adelante, dificultad plena. Razón: los
   novatos rebotaban en el primer cliente duro antes de entender el loop.
6. **Decisión a pérdida (LTV simplificado)**: cuando un Ahorrador solo cerraría por debajo
   del piso, se ofrece elegir: cerrar a pérdida (apuesta por el cliente), seguir
   negociando, o abandonar — con una pista del potencial del cliente.
7. **Pantalla de victoria** al llegar a la meta de caja: celebración + elegir terminar o
   seguir en modo libre. Cierra el arco de la corrida (antes no había cierre).
8. **Fórmula de aceptación afinada**: base ~0.42, target nunca por debajo del piso
   (`max(costo*1.10, costo*targetMult)`), bonus por táctica según personalidad, penalización
   por enojo, factor mercado. Tunear para que el piso 10% + cliente duro sea *difícil pero
   posible*, no imposible.
9. **Capacidad de operaciones** (máx 4 en curso): no llegan clientes nuevos hasta liberar.
10. **Incidentes operativos**: costo extra que se decide absorber (+reputación) o pasar al
    cliente (−reputación). El costo extra **nunca supera la ganancia** del trato.
11. **Reputación −100..100** (centro 0): escala que desbloquea crédito de agentes y
    clientes más grandes; a −100 se pierde por mala fama.

### 12.4 Detalles de UX que sumaron (de la tanda de mejoras)
Barra de meta/objetivo, indicador de "cómo venís", resultado del envío visible, cierre
estimado en vivo con color, sonidos sutiles + mute, memoria visible del cliente, resumen
de fin de mes, accesibilidad (no solo color), tooltips contextuales de palancas,
onboarding jugado. Todos probados en el prototipo.

### 12.5 Lo que el prototipo NO validó todavía (honestidad)
- **El criterio de éxito del plan (sección 9) NO está cumplido:** *un desconocido juega
  ~15-20 min sin ayuda y pide "una más"*. Las sesiones largas y exitosas fueron del
  **autor o de conocidos**. Los desconocidos hasta ahora **cierran 1-2 tratos y se van**,
  muchos rebotando en el primer cliente difícil (lo que motivó la rampa de dificultad).
- Por lo tanto: **el núcleo se ve prometedor, pero la validación con extraños está
  pendiente.** Es la condición que el propio plan puso para invertir en el juego grande.

### 12.6 Sistema de analytics (mantener en Unity)
Tracking de cada click y evento semántico (game_start, new_client, open_agent,
agent_haggle, agent_tactic, agent_lock, use_tactic, send_quote, deal_closed, deal_lost,
deal_abandoned, loss_decision, incident, victory, game_over, feedback) con sesión, tiempo
desde inicio/evento previo, panel y snapshot de estado. Permite reconstruir el recorrido
de cada jugador y medir embudos/fricción. **Replicar en Unity** (aunque sea hacia un
backend propio) — es lo que convirtió las corazonadas en decisiones con datos.

## 13. Recomendación de implementación (gate)
1. **Terminar este .md = HECHO** (sección 12). Es el spec para Unity.
2. **NO codear en Unity todavía.** Antes: conseguir que **3 desconocidos** jueguen el
   prototipo web ~15 min sin ayuda y medir si piden "una más" (criterio sección 9).
   - Cómo: mandar el link sin explicar nada y mirar la data (o mirarlos jugar callado).
   - El prototipo web es el laboratorio barato: cualquier ajuste de balance se prueba ahí
     en minutos, no en semanas de Unity.
3. **Si 2 de 3 desconocidos muestran "una más"** → recién ahí portar el sistema validado a
   Unity detrás de `FeatureFlags.USE_ADVANCED_NEGOTIATION`, copiando los valores de la
   sección 12, sin rediseñar.
4. **Si no** → iterar el balance en el prototipo (probablemente la rampa, la fórmula de
   aceptación, o más variedad de eventos) hasta lograr la señal.

---

## 14. Primer test con DESCONOCIDOS (5 personas) — 2026-06-06/07

Primera tanda de feedback de gente que NO es el autor. **Resultado: señal de diversión
real, pero la complejidad/onboarding es el muro principal.**

### 14.1 Señal positiva (la tesis respira)
- *"¡Qué divertido!"*, *"está buenísimo"*, *"me re enganché"*.
- **Una jugadora volvió a jugar y la 2da vez la enganchó** ("entendí el tema de manejar
  los márgenes… más didáctico, más dinámico"). Es el "una más" del criterio de éxito,
  observado en un desconocido. **En MOBILE se enganchó más** ("más fácil y didáctico").

### 14.2 El muro: sobrecarga cognitiva (3 de 5)
- *"mucha información"*, *"más cortisol que dopamina"*, *"no entiendo la finalidad"*,
  *"hay muchos botones"*. El que no es del rubro se abruma antes de divertirse.
- **El riesgo del proyecto se movió:** ya no es "¿es divertido?" (lo es), es "¿se
  entiende rápido?". El trabajo siguiente es **reducir complejidad percibida**, no sumar.

### 14.3 Pedidos concretos de los testers (priorizados)
1. **Agrupar / mostrar de a poco las tácticas** (Farol, Volumen, Prioridad) — o por etapas.
   La táctica "Farol" no se entendió. (P1, P4)
2. **Quitar el cartel de feedback durante el juego** — interrumpía la primera partida.
   → HECHO: el feedback se ofrece solo al terminar la partida + botón manual "Opinar".
3. **Propósito y PROGRESIÓN del jugador** — *"¿qué hago con la plata en caja?"*, *"falta
   el progreso como jugador"*. El objetivo de $20k no alcanza como meta sentida; falta en
   qué gastar/crecer. (P5) → conecta con "Crecimiento" (secciones 1 y 4) y empleados (v2).
4. **Que al renegociar se NOTE la diferencia de precio** y haya más transportistas/variación. (P5)
5. **Mobile**: muy buena recepción, pero el botón "atrás" del navegador cierra el juego y
   se pierde la partida → considerar persistencia de estado o aviso de salida.

### 14.4 Idea de profundidad de un experto en comex (P2) — para v2
Desglosar la tarifa en **conceptos** (THC, freight, gastos administrativos, toll fee) y
permitir negociar/bonificar uno en particular ("esto te lo bonifico"). Es negociación real
y muy jugosa, PERO suma complejidad justo donde otros piden menos. **Diferido a v2**, no
ahora: primero bajar la barrera de entrada.

### 14.5 Conclusión para el rumbo
- El núcleo **engancha** (validado con extraños, incluido un "una más").
- La prioridad #1 pasa a ser **claridad/onboarding**: menos botones a la vista, revelación
  progresiva, propósito claro y un sistema de **progresión** (en qué gastar la caja).
- Recién con la barrera de entrada baja tiene sentido portar a Unity (sección 13).
