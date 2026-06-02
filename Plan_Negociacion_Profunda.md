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
