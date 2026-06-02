conocimiento y preguntame TOOOOOODO
Hemos recibido dos sistemas de negociación de un juego "Freight Forwarder". Uno es NegotiationEngine.cs (C#) y el otro es un enorme script GDScript (Godot) que contiene lógica de juego completa, incluyendo funciones de negociación como call_client, submit_quote, resolve_negotiation, accept_counter_offer, continue_negotiation, etc.

El usuario quiere incorporar las mejoras de Dealer's Life 2 a estos sistemas, pero primero necesita hacer preguntas para entender bien el contexto. El usuario trabaja en un agente de carga real, así que podemos preguntarle detalles del dominio.

Debo generar una lista de preguntas exhaustivas para clarificar:

Qué aspectos de Dealer's Life 2 quiere incorporar (psicología de clientes, tácticas, empleados, habilidades, etc.)

Cómo es el flujo actual de negociación en el juego (rondas, contraofertas, etc.)

Qué limitaciones o problemas tiene actualmente (por ejemplo, falta de personalidad, decisiones poco interesantes)

Qué datos reales del mundo del agente de carga pueden usarse (tipos de clientes, urgencias, condiciones de pago, riesgos aduaneros, etc.)

Si el juego tiene feature flags (se menciona FeatureFlags.USE_NEGOTIATION_V2) y cómo integrar.

Preferencias técnicas: ¿mantener separado o unificar? ¿Usar componentes?

También preguntar sobre la tabla de diseño que menciona (la tabla de tensión) y si ya tiene algo definido.

Debo hacer preguntas concretas, abiertas, para que el usuario pueda expresar su necesidad. Luego de sus respuestas, podré proponer una solución integrada.

Además, notar que el segundo sistema (GDScript) ya tiene algunos conceptos avanzados: memoria de clientes, rondas de negociación, flexibilidad, frases de rechazo, etc., pero el usuario dice que son "incompletos y precarios". Debo preguntar qué falta exactamente.

Voy a redactar una lista de preguntas organizadas por temas: negocio real, diseño de juego, mecánicas existentes, integración técnica, y prioridades.

¡Excelente! Tener a alguien que trabaja en una agencia de carga real es un activazo. Podemos construir un sistema de negociación que no solo sea divertido, sino auténtico.

Antes de proponer ninguna línea de código o diseñar la tabla, necesito hacerte todas las preguntas que se me ocurren. Respondé con lo que se te venga a la cabeza, sin filtro. Cuanto más específico, mejor. Luego con tus respuestas armo la solución concreta.

🧭 1. EL NEGOCIO REAL (VOS SOS EL EXPERTO)
Esto es clave para que el juego refleje la realidad y tenga decisiones genuinas.

En tu día a día, ¿cuáles son las 3 o 4 variables que más negocías con un cliente?
Ejemplo: precio, plazo de pago, responsabilidad por pérdidas, penalidades por demora, etc. ¿Cuál es la que más duele ceder?

¿Cómo es una negociación típica con un cliente nuevo vs uno recurrente?
¿Qué información tenés del cliente antes de empezar a negociar? ¿Podés saber si es confiable, si paga tarde, si es exigente?

¿Los agentes de carga usan “tácticas” como las de Dealer's Life?
Por ejemplo: “esto es mi mejor precio”, “te hago descuento si pagás hoy”, “te doy prioridad en la aduana”. ¿Cuáles funcionan en la vida real?

¿Cuál es el mayor riesgo que asumís en una operación?
¿Que el cliente no pague? ¿Que la carga se dañe? ¿Que la aduana la retenga? ¿Cómo se refleja eso en el precio?

¿Hay “clientes trampa”? Tipos que parecen buenos pero después son un dolor de cabeza. ¿Cómo los identificás temprano?

🎮 2. EL JUEGO ACTUAL (LO QUE YA TENÉS)
Necesito entender qué está funcionando mal, y qué partes están “precarias”.

¿Qué siente un jugador cuando negocia en tu juego ahora?

¿Es aburrido? ¿Mecánico (subir/bajar precio y listo)?

¿Hay alguna vez una decisión difícil donde no sepa qué hacer?

De las funciones que ya tenés escritas (call_client, submit_quote, resolve_negotiation, continue_negotiation, etc.), ¿cuáles te parecen que casi funcionan y cuáles son un desastre?

¿El sistema de rondas de negociación (negotiation_rounds) se siente bien o es solo un contador?
¿Los clientes cambian su oferta de forma creíble o es muy aleatoria?

¿Qué es lo que más te frustra de tu sistema actual?

¿Que el jugador solo mire el precio?

¿Que los clientes se comporten todos igual?

¿Que no haya consecuencia de las negociaciones anteriores?

¿El jugador puede ver el “humor” o “personalidad” del cliente en la UI?
¿Tiene alguna pista de si el cliente es agresivo, paciente, desconfiado?

🃏 3. INSPIRACIÓN EN DEALER'S LIFE 2
Querés incorporar mecánicas de ese juego. Pero necesito saber cuáles te interesan de verdad.

¿Qué features de Dealer's Life 2 te parecen más jugosas y que encajarían en tu juego?

¿La psicología de los clientes (rasgos ocultos)?

¿Las tácticas (farol, argumento de valor, presión de tiempo)?

¿Los empleados con habilidades pasivas?

¿El sistema de reputación que afecta cómo negocian?

¿El “regateo” con frases personalizadas?

¿Qué NO querrías copiar? Para no complicar al pedo.

¿El jugador debería poder “especular” con información incompleta?
Ejemplo: no saber el presupuesto real del cliente, solo intuirlo por su reacción.

📐 4. DISEÑO DE LA TABLA DE TENSIÓN
En tu documento de diseño hablas de una tabla de variables en tensión (precio, relación, riesgo, capacidad). Quiero concretarla.

¿Cuáles deberían ser las 4 o 5 “palancas” que el jugador mueve en cada negociación?
Propongo algunas, pero vos decime cuáles tienen sentido en la vida real:

Precio (que cobrás al cliente)

Plazo de pago (inmediato / 15 / 30 días)

Tipo de transporte (aéreo más caro pero rápido, marítimo más barato pero lento)

Responsabilidad por pérdidas (quién paga si la carga se daña)

Penalización por demora (por cada día de retraso)

Volumen mínimo o exclusividad

¿El jugador debería poder ofrecer “paquetes” o “descuentos por volumen”?
¿Es común en tu rubro?

¿Hay algún elemento de “relación a largo plazo” que valga la pena modelar?
Por ejemplo: si aceptás un mal trato ahora, el cliente te será más leal después.

¿Cómo se representa la “presión de caja” en la negociación?
¿El jugador a veces acepta un mal negocio porque necesita liquidez?

🛠️ 5. PREGUNTAS TÉCNICAS (IMPLEMENTACIÓN)
Para saber cómo integrar lo nuevo sin romper lo que ya anda.

¿Tu proyecto está en Unity o Godot?
Veo un .cs (NegotiationEngine) y un .gd (GameState). ¿Son dos versiones separadas o conviven?

¿La NegotiationEngine.cs está actualmente en uso?
Depende de FeatureFlags.USE_NEGOTIATION_V2. ¿Ese flag está activado? ¿O es código muerto?

¿Prefieres mantener la lógica de negociación en un solo lugar (por ejemplo, una nueva clase AdvancedNegotiation) o integrarla en los sistemas existentes?

¿El juego tiene una capa de UI separada que ya muestra el diálogo del cliente?
¿Podríamos añadir emoticonos, barras de “interés” o “humor” fácilmente?

¿Hay tests o algún mecanismo para probar que la negociación es divertida sin jugar 20 horas?
¿Se puede simular rápidamente?

🎯 6. PRIORIDAD Y ALCANCE
Para no marearnos.

¿Cuál es el mínimo cambio que te haría decir “esto ya es mucho mejor que antes”?
¿Agregar personalidades? ¿Tácticas? ¿Consecuencias reales?

¿Cuánto tiempo estimás que podemos dedicar a refactorizar la negociación?
¿Una semana? ¿Dos? Así ajustamos la complejidad.

¿Querés que la solución sea completamente nueva (reescribir) o que parchee lo actual?
Siendo honestos: lo actual tiene 62 archivos, tocar mucho puede romper. Tal vez sea mejor añadir un nuevo sistema que se pueda activar/desactivar.

✅ 7. LA TABLA PROMETIDA
Una vez que me respondas todo esto, voy a diseñar la tabla completa con:

Variables del trato (con rangos realistas).

Matriz de tensión (por qué no se pueden maximizar todas).

Personalidades de clientes (basadas en comportamientos reales).

Tácticas del jugador y sus efectos.

Empleados y habilidades.

Ejemplo concreto de una negociación paso a paso.

Pero primero: respondé lo que puedas. No hace falta que contestes todas ahora, empezá con las que más te resuenen. Entre más detalles, más sólido será el diseño.

¡Estoy listo para aprender de tu experiencia! 🚛💨

🧭 1. EL NEGOCIO REAL (VOS SOS EL EXPERTO)
Esto es clave para que el juego refleje la realidad y tenga decisiones genuinas.

En tu día a día, ¿cuáles son las 3 o 4 variables que más negocías con un cliente?
Ejemplo: precio, plazo de pago, responsabilidad por pérdidas, penalidades por demora, etc. ¿Cuál es la que más duele ceder?

El cliente siempre busca precio, uno siempre tiene que tener en la cabeza que no puede bajar del 10% de ganancia sobre el precio que le sale el flete. O sea el costo. El plazo de pago que uno se ganó con los proveedores después de 6 años de trabajo son 30 - 45 días de plazo de pago. Lo ideal es sostener ese plazo con el proveedor pero tener el cliente que pague al contado para hacerse de la plata antes. Aunque a veces el cliente pide plazos de pago. 15 días, 30 días, 45 días, 60 y hasta 90 días alguna vez. Uno puede moverse dentro de 0 a 30 días que es lo que uno logró con los proveedores. La responsabilidad de perdidas no tuvimos muchos ejemplos siempre le decimos al cliente que debe asegurar la carga porque hay muchos riesgos. Muchas veces el cliente tiene penalizaciones económicas en su rubro laboral por entrega tarde de la mercadería... Ellos te lloran pero uno no puede hacer mucho, por ahí usar estrategias para que la carga tome una ruta directa pero antes de cerrar la carga casi nunca cuando la carga ya esta operando. A veces el cliente en su propio negocio va a perdida (lo que sigue después de la entrega de la mercadería que no depende de nuestro servicio). El precio siempre se puede pelear siempre y cuando estemos dentro de ese 10% de ganancia, a veces el cliente te dice que otra empresa de FF le ofrece algo 50% más económico de lo que le ofreces vos y vos lo que tenes que hacer es pelear o reclamar a tu proveedor que encuentre ese precio bajo para poder cerrar la carga. Esto casi nunca funciona el proveedor te dice hasta donde llega lo máximo que te baja son 50 / 100 USD y vos le decis al cliente que podes bajar 25 / 50 USD cosa de quedarte con un margen para quedarte con más plata. Lo que el cliente también evalúa es el servicio que uno le brinda a parte del precio. Estar encima de la carga resolver los problemas como problemas de documentación estar encima del despachante o del shipper para que se resuelva y no atrase la entrega o si hay algún problema con la carga (pelear con la aseguradora para que le reconozcan que la mercadería se averió) o costos extras que no dependen de un problema climático sino por ejemplo de una mala gestión de la linea marítima por ejemplo llega al puerto un contenedor roto que no puede incrustarse en los pinos de un camión semirremolque para llevarlos a la ultima milla. Si el camión no sale del puerto el día que tiene que salir el cliente tiene que pagar 2.000 USD por recoordinación entonces uno tiene que estar encima del transporte terrestre para que saque el camión llevarlo a un lugar donde desconsolide el contenedor (otro costo extra) y trasladar el contenedor al destino final. Estos dos costos extras en el caso que el contenedor haya salido a destiempo y que el contenedor tenido que desconosolidar en otro lugar para luego ser transladada la mercadería al destino final estos extracostos los tiene que pagar la línea marítima no el cliente. este tipo de cosas son parte del servicio.
Insistir al shipper que le entregue la documentación al agente a tiempo para que pueda llegar al cut off para embarcar la mercadería y no perder el booking. Insistirle al agente para que no suba el precio ya pactado por falta de espacio, que se apure para cargar antes que haya aumentos por feriados en China o temporada de vacaciones en Europa. Saber en todo momento donde esta la carga del cliente para avisarle tu carga está en tal lado, en tal estado. darle oportunidad de decidir que hacer al cliente darle opciones de resolución. Informale siempre y cuando se pueda.

¿Cómo es una negociación típica con un cliente nuevo vs uno recurrente?
¿Qué información tenés del cliente antes de empezar a negociar? ¿Podés saber si es confiable, si paga tarde, si es exigente?

Lo mejor es conocerlo. Si es nuevo y no tenes información le consultas que necesita y te dice necesito traer esto Mercadería, Peso, Valor, Medidas, Volumen, Tipo de carga (General, peligrosa, etc). De tal origen, es necesario también saber el incoterm FOB/EXW/CIF/DDP/… etc. En base a esto le pedis a los proveedores que valor de la carga tienen. Ellos te dan un valor X. Vos le pones el profit: +10% es cliente nuevo así que vas bajo así lo agarras. El cliente puede no contestarte, puede decirte estas alto y que ya quedaste afuera, puede decirte estás alto por 200 USD entonces ves si podes igualar para cerrar, puede decirte estas alto si bajas voy con vos, o cierra y avanza (esto casi nunca pasa con los clientes nuevos). Cuando es un cliente que ya te conoce, te puede designar la carga directamente a vos (esto es lo ideal porque no tenes que darle ninguna cotización para que avance), puede pedirte cotización, cuando se la pasas cierra directamente o te pide un descuento (cuando es así y conoce al cliente uno ya le pone un poco más para luego acomodarse, pone la excusa de "voy a pedirle al agente un descuento le pide al proveedor un descuento y este a veces se lo da y otras veces no. Si se lo da siempre hay que bajarle un poco no mucho. puede decirte que estas alto que hay otros que le pasaron mas bajo entonces le preguntas cuanto te pueden decir o no. si te dicen revisas cuanto te queda si igualas y si te cierra (mas de un 10%) le decis que llegas y cerras. Pueden decirte que estas alto y van con el otro o que estas alto pero si bajas un poco van con vos igual porque les importa el servicio que brindas, porque te tienen confianza porque ya te conocen, porque saben como trabajas. Hay otros que no les importa mucho como trabajas pero les importa el numero si no llegas al número que quieren no avanzan.
También hay otra cosa importante cada empresa que te contrata tiene otro negocio, su negocio si el flete encarece mucho su producto o su servicio y no le cierra no hacen el flete. O sea acá no perdes contra otros sino que perdes el negocio simplemente porque al cliente no le cierra su negocio. 

¿Qué información tenés del cliente antes de empezar a negociar? ¿Podés saber si es confiable, si paga tarde, si es exigente? A veces sabes que tipo de cliente es si te pregunta en detalle los costos de los conceptos si te pregunta si se puede bajar algo si se puede buscar otra alternativa. O te dicen que cerraron con otro porque estaba mas barato. No, se sabe si es confiable o si paga tarde o si es exigente desde el vamos por ahí con las primeras interacciones si es exigente pero no si es confiable y si paga bien o tarde sin antes de tener la primera operación. Luego se va generando un historial del cliente si paga o no paga si es rata o no. Si quiere todo mas barato o le da igual minetras des un buen servicio.

¿Los agentes de carga usan “tácticas” como las de Dealer's Life?
Por ejemplo: “esto es mi mejor precio”, “te hago descuento si pagás hoy”, “te doy prioridad en la aduana”. ¿Cuáles funcionan en la vida real?
Completamente. Todas las tarifas tiene valides, todas son sujetas a espacio de la línea o de la aerolínea. Estan sujetas a la oferta o demanda del origen. Los precios fluctúan mucho si el shipper no entrega la mercadería a tiempo al agente y el agente no llega a reservar se pierde espacio para y la tarifa cambia, casi nunca para abajo sino que casi siempre aumenta. Si vuelve a tardar se repite el bucle y el cliente pierde el precio que tenía al comienzo. Entonces uno siempre está metiendo presión al cliente para advertirle que los precios fluctúan y moverse rápido es clave para agarrar los viajes. También avisarle que te has esmerado mucho para conseguir ese precio, también si te piden descuentos vos podes decirle si pagas anticipado te puedo descontar un 40%. si pagas al contado tenes un 25%. O le pedis cargas si yo te hago un descuento vos dame las próximas 5 cargas.


¿Cuál es el mayor riesgo que asumís en una operación?
¿Que el cliente no pague? ¿Que la carga se dañe? ¿Que la aduana la retenga? ¿Cómo se refleja eso en el precio?

Perder el cliente es lo peor. Después el resto de las variables son validas. Que el cliente no pague (entrar en juicio en el peor de los casos, siempre uno tiene las de ganar pero puede demorar años y tiempo). que la carga se dañe y el cliente te haga la cruz y no cierre mas con vos. Que se retenga en aduana y el cliente reciba tarde la mercadería y afronte problemas económicos de su empresa. Que la carga se retrase y que el precio suba.
También esta bueno siempre fijarse en la pagina del banco central de la republica argentina si la empresa que te pide fletes esta bien financieramente o debe plata a créditos o cheques rebotados. esto te da un paramentro también si es fiel pagador o vas a tener problemas credicticios en algún momento.

¿Hay “clientes trampa”? Tipos que parecen buenos pero después son un dolor de cabeza. ¿Cómo los identificás temprano?
No hay manera de indentificarlos al principio. hay pautas pero no siempre se cumplen: Cuando se quejan mucho de los precios: "Uhhh que rompí!" "che se puede bajar de algún lado?" "no tenes otro trasnporte que sea mas barato?" "El otro FF me hacía descuentos"... Pero no siempre se cumplen a veces es para ciertas cargas y el resto no se quejan. También: "che donde esta mi carga a las 2 de la mañana" "che ya salió la carga? mira que si no llega a tiempo me multan"

🎮 2. EL JUEGO ACTUAL (LO QUE YA TENÉS)
Necesito entender qué está funcionando mal, y qué partes están “precarias”. Todo lo que voy a decir es con la versión de Unity C# (la otra versión esta un poco mejor pero no es la que voy a continuar)

¿Qué siente un jugador cuando negocia en tu juego ahora? el jugador me dice que solo apreta botones y siempre gana la carga. Porque elige los clientes que son buenos pagadores, necesitan cargas urgentes o clientes con crédito. Los malos pagadores o clientes difíciles que piden entre un 15% y un 10% de ganancia no está funcionando por lo que no los eligen. 

¿Es aburrido? ¿Mecánico (subir/bajar precio y listo)? exactamente super mecanico. Sirve para testear el juego rápido pero no es un sistema de negociación solido.

¿Hay alguna vez una decisión difícil donde no sepa qué hacer? Los clientes difíciles y malos pagadores es imposible cerrar carga.

De las funciones que ya tenés escritas (call_client, submit_quote, resolve_negotiation, continue_negotiation, etc.), ¿cuáles te parecen que casi funcionan y cuáles son un desastre? estas son funciones de la versión que se va a descontinuar. igualmente contesto: que la mejor es call_client() Bastante sólida. Tiene estrategia, enojo, satisfacción y memoria. Es de las mejores partes. luego submit_quote() pero es bastante simple.

¿El sistema de rondas de negociación (negotiation_rounds) se siente bien o es solo un contador?
¿Los clientes cambian su oferta de forma creíble o es muy aleatoria? o. Es solo un contador con piel de sistema.
Problemas reales:

No hay memoria real entre rondas (el cliente no recuerda tus ofertas anteriores dentro de la misma negociación).
El cliente sube o baja de forma bastante aleatoria.
No hay tensión creciente (el cliente no se pone más impaciente o más flexible según las rondas).
Falta personalidad por tipo de cliente en las rondas (un cliente URGENT debería comportarse muy distinto a un CONTRACT_CLIENT).

¿Qué es lo que más te frustra de tu sistema actual?No. Es demasiado aleatorio.Según mi análisis, las frustraciones principales son:

El jugador solo mira el precio → No hay suficiente información emocional o contextual.
Los clientes se sienten todos parecidos → Falta personalidad diferenciada fuerte.
No hay consecuencia real de negociaciones anteriores → La memoria existe (client_memory), pero está poco conectada con la negociación actual.
Falta feedback claro → El jugador no sabe si está cerca o lejos del precio que el cliente quiere aceptar.
La UI probablemente no muestra el humor del cliente de forma visible (enojo, satisfacción, etc.).

¿Que el jugador solo mire el precio?

¿Que los clientes se comporten todos igual?

¿Que no haya consecuencia de las negociaciones anteriores?

¿El jugador puede ver el “humor” o “personalidad” del cliente en la UI? por ahí el humor del cliente no está mal pero la personalidad al comienzo estaría bueno que no se muestre... ya que puede hacer que tome decisiones y omita trabajar con clientes que pueden darle buena rentablidad. sería interesante que la personalidad del cliente cambie un poco de tacaño a o mal pagador a buen pagador o cliente con crédito cosa que corregís al cliente y este se vuelve mejor para tu empresa. pero que estos cambios sean poco comunes.
¿Tiene alguna pista de si el cliente es agresivo, paciente, desconfiado?

🃏 3. INSPIRACIÓN EN DEALER'S LIFE 2
Querés incorporar mecánicas de ese juego. Pero necesito saber cuáles te interesan de verdad.

¿Qué features de Dealer's Life 2 te parecen más jugosas y que encajarían en tu juego?
En este juego por lo que vi la negociación no es: che sale esto, listo te pago. Tiene mas variables, es mucho mas avanzado hay intereses intrínsecos que se acercan a la realidad. Me gustaría que mi juego en su contexto esté mas rico de las estrategias que usa este juego. No tanto con che te vendo este objeto que es trucho y le saqué mas plata de lo que vale. Pero si ver que hay oportunidades de negocio con el servicio de fletes internacionales. Agarrar una temporada alta y conseguir que un proveedor te consiga una tarifa baja y sacarle 120% de profit a una operación. O conseguir supongamos que el mercado dice que de Qingdao-Bue 1 contenedor sale de costo 3.500 USD pero vos conseguís un contrato con una linea marítima para un cliente donde le estas sacando 5% de ganancia pero apuntas al volumen (son 500 contenedores, cada uno 3000 USD de compra) Es poca plata de ganancia pero este contrato. Ahora si otro cliente te pide 1 solo contenedor de Qingdao al precio del mercado vos le dices listo te puedo conseguir 1 cnt a 3.850 USD la ganancia es un 10% peeero vos te subis al mismo contrato de la marítima de costo 3.000 USD entonces la ganancia es de 850 USD lo que representa casi un 30% de ganancia. por ejemplo.

¿La psicología de los clientes (rasgos ocultos)? Y deberían ser ocultos. pero al mismo tiempo debería estar lo que vos pensas que es cuando cerras o avanzas en la negociación. Por ejemplo "me da indicios de que es tacaño" o "parece pagador"

¿Las tácticas (farol, argumento de valor, presión de tiempo)? Si totalmente. El farol de mentir o exagerar se puede usar para los proveedores: "hay otro proveedor que me pasa un mejor numero, yo quiero hacerlo con vos pero necesito que ajustes" (el cliente puede usar la misma estrategia con vos), o "che con 50 USD cerramos" cuando en realidad ese proveedor es el mas competitivo y vos queres sacarle más profit. Con el cliente: "la verdad que esta muy complicado conseguir tarifa, en origen es un caos de precios voy a hacer lo imposible por conseguirlo, pero dependemos del espacio" Ya te avisaron que tenes una validez mensual y que en origen se consigue espacio. Pero le generas presión para que te cierre rápido. "El valor es alto porque hay mucha demanda" "Tengo un lugar de Qingdao para cubrir vos que tenes carga seguida de ese origen, tenes algo para darme?"

¿Los empleados con habilidades pasivas?
3. Progresión por Niveles 1–20
Cada nivel representa un hito empresarial (nombre) y se asocia a un tamaño de plantilla, roles nuevos desbloqueados, requisitos (ingresos acumulados, reputación mínima, oficinas) y beneficios específicos. A continuación se resume en tabla (todos en USD):

Nivel	Nombre	Empleados totales	Puestos desbloqueados (salarios USD anuales)	Requisitos (Ingresos, Reputación, Oficinas)	Beneficio / Desbloqueo principal
1	Freelance	1	– (tú haces todo)	– (inicio)	Comprendes todo el proceso logístico
2	Micro Forwarder	2	Operador Jr ($30k)	Ingresos≥10K; Rep≥5; Oficinas=1	+5 cargas simultáneas (↑capacidad)
3	Agencia Local	3	Inside Sales Jr ($40k)	Ingresos≥25K; Rep≥10; Oficinas=1	+20% clientes potenciales (↑cotizaciones)
4	Agencia Local Plus	4	Administrativo Jr ($30k)	Ingresos≥50K; Rep≥20; Oficinas=1	-10% tareas manuales (↑eficiencia admin)
5	Oficina Profesional	5	Analista de Pricing Jr ($35k)	Ingresos≥100K; Rep≥30; Oficinas=2	+10% margen promedio (tarifas optimizadas)
6	Forwarder Regional	7	Customer Service Jr ($30k)	Ingresos≥200K; Rep≥40; Oficinas=2	+15% retención de clientes
7	Forwarder Regional Plus	9	Comercial Jr ($45k)	Ingresos≥350K; Rep≥50; Oficinas=2	+15% nuevos clientes (venta activa)
8	Empresa Nacional	12	Operador Sr ($50k)	Ingresos≥600K; Rep≥60; Oficinas=3	-20% errores operativos
9	Empresa Nacional Plus	15	Coordinador de Operaciones ($70k)	Ingresos≥1M; Rep≥70; Oficinas=3	+15% eficiencia en operaciones
10	Empresa Consolidada	18	Gerente Comercial ($100k)	Ingresos≥2M; Rep≥80; Oficinas=4	Captación de grandes cuentas (↑contratos)
11	Empresa Consolidada Plus	22	Documentación ($30k)	Ingresos≥4M; Rep≥85; Oficinas=4	-15% multas/errores documentales
12	Empresa Exportadora	25	Analista de Pricing Sr ($60k)	Ingresos≥7M; Rep≥90; Oficinas=5	+15% margen extra
13	Empresa Internacional	28	Key Account Manager ($80k)	Ingresos≥10M; Rep≥95; Oficinas=6	Contratos recurrentes (lealtad)
14	Empresa Internacional +	31	Gerente de Operaciones ($120k)	Ingresos≥15M; Rep≥100; Oficinas=6	+20% capacidad logística (amplía red)
15	Grupo Logístico	34	Experto en Aduanas ($40k)	Ingresos≥20M; Rep≥120; Oficinas=8	Habilita envíos premium (servicios especiales)
16	Grupo Logístico Plus	38	Marketing ($70k)	Ingresos≥30M; Rep≥140; Oficinas=8	+10% leads pasivos (marketing digital)
17	Red Internacional	42	Business Development ($90k)	Ingresos≥45M; Rep≥160; Oficinas=10	Alianzas estratégicas (nuevos mercados)
18	Red Internacional +	45	Finanzas ($120k)	Ingresos≥60M; Rep≥180; Oficinas=10	Mejor flujo de caja (optimiza capital)
19	Multinacional	48	Compliance ($100k) + BI Analyst ($110k)	Ingresos≥80M; Rep≥200; Oficinas=12	Decisiones basadas en datos; +Reducción riesgos
20	Multinacional Global	50+	Directores Área (~$150k c/u)	Ingresos≥100M; Rep≥250; Oficinas=15	Empresa 360°: todos los sistemas maximizados

(Notas: Los sueldos son orientativos. “Requisitos” son niveles acumulativos mínimos para ascender. Los beneficios indican el desbloqueo clave de ese nivel).

4. Sistema de empleados
Cada rol en la empresa se estructura en cuatro rangos de seniority: Junior → Senior → Especialista → Experto. El costo de contratar o promover a cada nivel aumenta (por ejemplo, contratar Senior suele ser ~2× el coste de Junior; Especialista ~4×; Experto ~8×, considerando salarios y compensaciones). El efecto en el gameplay es:

Junior: Salario bajo. Productividad base (por ejemplo 60% de un trabajador promedio), comete más errores, pero son baratos y aprenden rápidamente. Su contratación tiene coste 1× base.
Senior: Salario medio-alto. +25% de productividad frente a un junior típico, y -30% de errores, gracias a su experiencia. Atiende más volumen de trabajo eficientemente. Costo ~2× salario base (pago inicial por contratación).
Especialista: Salario elevado. Posee una habilidad única en su área que desbloquea bonificaciones especiales. Ejemplos cuantificados:
Operaciones Especialista: reduce los retrasos en -40%.
Pricing Especialista: +15% de margen adicional.
Customer Service Especialista: +25% retención de clientes (menos churn).
Comercial Especialista: +20% en tasa de cierre de contratos.
Además mejoran la efectividad de las tácticas relacionadas. Costo ~4×.
Experto: Salario muy alto. Empleado estrella de cada departamento, poco frecuente (se desbloquean en niveles avanzados). Ejemplos:
Pricing Guru: +25% margen total.
Operador Maestro: -60% de problemas logísticos.
Comercial Estrella: +35% ventas.
Gerente Visionario: multiplica eficiencia global.
Estos ofrecen potentes bonificaciones pasivas (p.ej. ejecución ultraeficiente), a cambio de mucho costo (~8×).
Cada promoción (por ejemplo, entrenar de Senior a Especialista) requiere tiempo y recursos (capacitaciones, bonus), lo cual se reflejará en el coste de contratación/pago de bonus al ascender. El diseño permite que el jugador planifique el balance costo-beneficio de cada contratación/promoción.

¿El sistema de reputación que afecta cómo negocian?

¿El “regateo” con frases personalizadas?
Cliente Presionando Precio (1-10)
"Tu precio está bastante arriba de lo que esperaba."
"Tengo ofertas más competitivas."
"¿Hay algo que puedas mejorar?"
"Necesito que revises los números."
"Con ese valor se me complica avanzar."
"La diferencia con otros proveedores es importante."
"¿Es tu mejor precio?"
"Esperaba algo más agresivo."
"Estamos fuera de presupuesto."
"Necesito justificar esta diferencia internamente."
Cliente Comparando Competencia (11-20)
"Otra empresa me pasó una tarifa más baja."
"Estoy evaluando varias opciones."
"Todavía no tomamos una decisión."
"Tu propuesta es interesante, pero no es la única."
"¿Qué te diferencia de los demás?"
"¿Por qué debería elegirte a vos?"
"Necesito comparar todas las propuestas."
"La competencia está muy agresiva."
"Tengo una oferta similar por menos dinero."
"Convenceme de que vale la pena."
Cliente Apurado (21-30)
"Necesito una respuesta hoy."
"No tengo mucho tiempo para decidir."
"La carga sale esta semana."
"Esto es urgente."
"Mi proveedor está esperando confirmación."
"Necesito cerrar esto cuanto antes."
"No podemos perder más tiempo."
"Cada día de demora nos cuesta dinero."
"La fecha de embarque está muy cerca."
"Necesito una solución rápida."
Cliente Desconfiado (31-40)
"¿Hay algún costo que no estoy viendo?"
"Quiero evitar sorpresas."
"Ya tuve malas experiencias."
"Necesito todo por escrito."
"¿Estás seguro de que eso incluye todo?"
"Quiero entender exactamente qué estoy pagando."
"No quiero costos ocultos."
"¿Qué pasa si hay una demora?"
"¿Quién se hace cargo si algo sale mal?"
"Necesito garantías."
Cliente Cerca de Comprar (41-50)
"Estamos bastante cerca."
"La propuesta me gusta."
"Sólo necesito una pequeña mejora."
"Creo que podemos trabajar juntos."
"Estamos alineados en casi todo."
"Si resolvemos este punto avanzamos."
"Voy a presentarlo a dirección."
"Necesito una última confirmación."
"Estamos muy cerca de cerrar."
"En principio me parece razonable."

¿Qué NO querrías copiar? Para no complicar al pedo.

¿El jugador debería poder “especular” con información incompleta?
Ejemplo: no saber el presupuesto real del cliente, solo intuirlo por su reacción.

Si pero también se le puede pedir un target o el mismo cliente ya ir con el target para que baje el precio. También seria interesante que el FF pudiera revisar que tipo de negocio es el de la empresa y se pueda informar con alguna información previo a hablar con el cliente por ejemplo. La temporada de fruta arranca en septiembre aprox. si sabemos que es temporada alta en septiembre se sabe que si te piden cotización de fletes de fruta. Va a salir a buscar precios porque todo es caro así que sabemos que tenemos que esta deseperado por encontrar precios.

📐 4. DISEÑO DE LA TABLA DE TENSIÓN
En tu documento de diseño hablas de una tabla de variables en tensión (precio, relación, riesgo, capacidad). Quiero concretarla.

¿Cuáles deberían ser las 4 o 5 “palancas” que el jugador mueve en cada negociación?
Propongo algunas, pero vos decime cuáles tienen sentido en la vida real:

Precio (que cobrás al cliente)

Plazo de pago (anticipo / inmediato(Al contado) / 15 / 30 días / 45 días / 60 días) Transferencia/Echeq

Tipo de transporte (aéreo más caro pero rápido, marítimo más barato pero lento)

Responsabilidad por pérdidas (quién paga si la carga se daña) se asegura la carga o no? si no asegura la carga es responsabilidad del cliente

Penalización por demora (por cada día de retraso) Son cosas que no puede manejar el FF pero si puede estar encima de la carga y hacer todo lo posible porque no se demore

Volumen mínimo o exclusividad

¿El jugador debería poder ofrecer “paquetes” o “descuentos por volumen”?
¿Es común en tu rubro? Si por supuesto. Mientras mas volumen mejor contrato puede ofrecer y mas bajo precio pero también esto va anclado a distintos financiamientos

¿Hay algún elemento de “relación a largo plazo” que valga la pena modelar?
Por ejemplo: si aceptás un mal trato ahora, el cliente te será más leal después.

Mientras mejor le ofrezcas servicio al cliente y mejor negocios le cierres. Mejor reputación vas a tener y mas leal va a ser el cliente. Por lo tanto más carga te va a dar y va a cerrar precios mas altos.

¿Cómo se representa la “presión de caja” en la negociación?
¿El jugador a veces acepta un mal negocio porque necesita liquidez?

Tal cual. Y el jugador al mismo tiempo tiene también que pagar los costos de su empresa el como los sueldo de los empleados así que ahí se genera presión de la caja que todos los meses se descuenta plata. Y si cierra mal una carga o dan de baja una carga o el cliente no quiere pagar puede perder mucha plata.

🛠️ 5. PREGUNTAS TÉCNICAS (IMPLEMENTACIÓN)
Para saber cómo integrar lo nuevo sin romper lo que ya anda.

¿Tu proyecto está en Unity o Godot?
Veo un .cs (NegotiationEngine) y un .gd (GameState). ¿Son dos versiones separadas o conviven? el que vamos a seguir es el de Unity el otro es un prototipo que se descontinua

¿La NegotiationEngine.cs está actualmente en uso?
Depende de FeatureFlags.USE_NEGOTIATION_V2. ¿Ese flag está activado? ¿O es código muerto? 
Está en uso? → Sí, pero condicionado.
Depende de FeatureFlags.USE_NEGOTIATION_V2.
Si ese flag está en true, se usa este motor. Si está en false, se sigue usando la lógica vieja inline dentro de GameState.cs.

¿Prefieres mantener la lógica de negociación en un solo lugar (por ejemplo, una nueva clase AdvancedNegotiation) o integrarla en los sistemas existentes?

Me gustaría una lógica de negociación que funcione y que atrape mas allá de lo que esté hecho.


¿El juego tiene una capa de UI separada que ya muestra el diálogo del cliente?
¿Podríamos añadir emoticonos, barras de “interés” o “humor” fácilmente? Podría ser

¿Hay tests o algún mecanismo para probar que la negociación es divertida sin jugar 20 horas? NO.
¿Se puede simular rápidamente? No.

🎯 6. PRIORIDAD Y ALCANCE
Para no marearnos.

¿Cuál es el mínimo cambio que te haría decir “esto ya es mucho mejor que antes”?
¿Agregar personalidades? ¿Tácticas? ¿Consecuencias reales? Si podría ser.

¿Cuánto tiempo estimás que podemos dedicar a refactorizar la negociación? 
¿Una semana? ¿Dos? Así ajustamos la complejidad. No sabría decirlo

¿Querés que la solución sea completamente nueva (reescribir) o que parchee lo actual? Si.
Siendo honestos: lo actual tiene 62 archivos, tocar mucho puede romper. Tal vez sea mejor añadir un nuevo sistema que se pueda activar/desactivar. dale
¡Perfecto! Con tus respuestas tengo todo lo necesario para diseñar un sistema de negociación realista, profundo y divertido, basado en tu experiencia como agente de carga y en las mecánicas probadas de Dealer's Life 2.

A continuación te presento el diseño completo listo para implementar en Unity.

DISEÑO COMPLETO: SISTEMA DE NEGOCIACIÓN AVANZADA PARA FREIGHT FORWARDER (UNITY)
Filosofía central (lo que resuelve tus frustraciones)
Problema actual	Solución en el nuevo diseño
El jugador solo mira el precio	Añadimos plazo de pago, transporte, volumen/exclusividad, y seguro como palancas con trade-offs reales
Los clientes se sienten iguales	5 personalidades ocultas que se infieren por pistas y comportamiento
No hay consecuencia de negociaciones anteriores	La memoria del cliente (satisfacción, enojo, margen previo) afecta aceptación y frases
Falta feedback claro	Barra de "distancia al acuerdo" y pistas emergentes en UI
Sistema de rondas es solo un contador	Rondas con concesión dinámica, memoria dentro de la negociación, y tácticas que alteran el curso
Clientes malos son imposibles de cerrar	Ahora tienen trade-offs: ofrecerles mejores plazos de pago o volumen puede hacerlos viables
PARTE 1: MODELOS DE DATOS (NUEVOS)
1.1 Personalidad del cliente (oculta, se infiere)
csharp
[System.Serializable]
public class ClientPersonality
{
    public string id;
    public string name; // "El Rata", "El Apurado", etc. (no visible al inicio)
    
    // Rasgos internos (0-100) - NO visibles directamente
    public int priceSensitivity;      // 0=no le importa, 100=rata
    public int urgency;               // 0=sin prisa, 100=apura constante
    public int loyalty;               // 0=traicionero, 100=leal
    public int paymentDiscipline;     // 0=paga tarde, 100=paga al contado
    public int serviceValue;          // 0=solo precio, 100=valora buen servicio
    
    // Parámetros de negociación
    public int maxRounds;              // rondas máximas (1-4)
    public float concessionRate;       // cuánto cede por ronda (0.05 a 0.20)
    public float angerThreshold;       // a partir de qué enojo se va (0.3 a 0.7)
    
    // Pistas que se muestran al jugador (según interacción acumulada)
    public List<string> possibleHints; // ej: "Parece que solo le importa el precio"
}
1.2 Cliente con estado de negociación
csharp
public class ClientNegotiationState
{
    public string clientName;
    public ClientPersonality personality; // oculto
    
    // Estado dinámico visible (barras)
    public int currentAnger;          // 0-10 (se muestra)
    public int currentSatisfaction;   // 0-100 (se muestra)
    
    // Estado interno de negociación actual
    public int lastPlayerOffer;
    public int lastClientCounter;
    public int round;
    public bool isFinalOffer;
    public List<string> revealedHints; // pistas ya mostradas al jugador
    
    // Historial de esta negociación (para memoria dentro de la misma)
    public List<int> playerOffersHistory;
    public List<int> clientCounterHistory;
}
1.3 Oferta del jugador (Quote extendida)
csharp
public class AdvancedQuote
{
    public string cargoId;
    public int price;                  // precio al cliente (USD)
    public int paymentDays;            // 0, 15, 30, 45, 60 (plazo de pago)
    public int transportMode;          // Aéreo (2) o Marítimo (1)
    public bool clientInsures;         // true = cliente paga seguro, false = no asegura (riesgo él)
    public int volumeTier;             // 0=normal, 1=volumen (+20%), 2=exclusividad
    public int exclusivityCommitment;  // si volumeTier==2, cuántas cargas futuras (ej: 5)
}
PARTE 2: PERSONALIDADES REALES (BASADAS EN TU EXPERIENCIA)
ID	Nombre interno	Rasgos	Comportamiento	Pistas que se revelan
rata	El Rata	priceSens=90, urgency=20, loyalty=20, paymentDisc=30, serviceVal=10	Siempre pide descuento, compara con otros, nunca cierra fácil. MaxRounds=3, concesión baja (0.06)	"Parece que solo le importa el número", "Es de los que lloran el precio"
apurado	El Apurado	priceSens=30, urgency=90, loyalty=40, paymentDisc=50, serviceVal=60	Quiere cerrar rápido, acepta precios altos si es aéreo, se frustra con demoras. MaxRounds=1 o 2, concesión alta (0.15)	"Tiene mucha urgencia", "Cada día de demora le cuesta plata"
confiable	El Confiable	priceSens=40, urgency=30, loyalty=85, paymentDisc=85, serviceVal=75	Paga bien, es leal, valora el servicio. MaxRounds=3, concesión media (0.10)	"Parece un cliente serio", "Historial de pagos impecable"
trampa	El Trampa	priceSens=85, urgency=40, loyalty=10, paymentDisc=20, serviceVal=15	Se queja mucho, promete volumen, después no paga. MaxRounds=4 (te hace perder tiempo), concesión muy baja (0.04)	No se muestra al inicio. Después de 2 operaciones: "Cuidado, este cliente es problemático"
volumen	El Volumen	priceSens=70, urgency=30, loyalty=50, paymentDisc=40, serviceVal=30	Quiere precio bajo pero da muchas cargas. MaxRounds=2, concesión media (0.08)	"Tiene volumen, se puede negociar exclusividad", "Es de los que mueven mucho"
desconfiado	El Desconfiado	priceSens=50, urgency=40, loyalty=40, paymentDisc=60, serviceVal=80	Pregunta todo, quiere garantías. MaxRounds=4, concesión baja (0.05) pero si ganas su confianza, se vuelve leal	"Es meticuloso", "Quiere todo por escrito", "Valora la transparencia"
PARTE 3: PALANCAS DEL JUGADOR (TABLA DE TENSIÓN)
Palanca	Rango	Efecto en cliente (aceptación)	Efecto en ti (jugador)	Tensión principal
Precio	Costo × (1.10 a 1.50)	-15% a +15% según precio objetivo	Ganancia = precio - costo	Ganancia vs probabilidad de cerrar
Plazo de pago	0, 15, 30, 45, 60 días	Cliente prefiere plazos largos (+5% a +15% aceptación)	Cobras más tarde → presión de caja. Si pagas a proveedor a 30 días y das 60, te descapitalizas	Liquidez vs atraer cliente
Transporte	Marítimo (1) o Aéreo (2)	Aéreo: +20% aceptación si cliente es urgente; -10% si es rata	Aéreo cuesta 2-3× más, pero puedes cobrar más	Rapidez vs margen
Asegura cliente	Sí / No	Si asegura: +5% aceptación (tranquilidad). Si no asegura: 0%	Si no asegura y la carga se daña, cliente te odia (pérdida de relación)	Relación vs costo (seguro lo paga él)
Volumen / exclusividad	Normal / Volumen / Exclusividad	Volumen: +10% aceptación. Exclusividad: +15% aceptación pero te obliga a futuro	Menor margen ahora (ej: -5% precio), pero asegura ingresos futuros	Ganancia inmediata vs relación a largo plazo
PARTE 4: FÓRMULA DE ACEPTACIÓN (EL CÁLCULO CLAVE)
csharp
public float CalculateAcceptance(AdvancedQuote quote, ClientNegotiationState client, Cargo cargo)
{
    // Base: 30%
    float acceptance = 0.30f;
    
    // ===== 1. PRECIO =====
    int targetPrice = GetTargetPrice(client, cargo); // precio que el cliente considera "justo"
    float priceRatio = (float)quote.price / targetPrice;
    if (priceRatio <= 1.0f)
        acceptance += 0.25f * (1.0f - priceRatio); // precio bajo → +25%
    else
        acceptance -= 0.35f * (priceRatio - 1.0f); // precio alto → -35% máximo
    
    // ===== 2. PLAZO DE PAGO =====
    int preferredDays = GetPreferredPaymentDays(client); // según personalidad
    float daysDiff = Mathf.Abs(quote.paymentDays - preferredDays);
    float daysPenalty = daysDiff / 60f * 0.15f; // máx -15%
    acceptance -= daysPenalty;
    
    // ===== 3. TRANSPORTE =====
    int preferredMode = GetPreferredTransportMode(client, cargo);
    if (quote.transportMode == preferredMode)
        acceptance += 0.10f;
    else
        acceptance -= 0.08f;
    
    // Urgencia extra: si cliente es apurado y usas aéreo, +10% adicional
    if (client.personality.urgency > 70 && quote.transportMode == 2)
        acceptance += 0.10f;
    
    // ===== 4. VOLUMEN / EXCLUSIVIDAD =====
    if (quote.volumeTier == 1) // volumen
        acceptance += 0.10f;
    else if (quote.volumeTier == 2) // exclusividad
        acceptance += 0.15f;
    
    // ===== 5. RELACIÓN (MEMORIA) =====
    float relationshipBonus = (client.currentSatisfaction / 100f) * 0.20f;
    acceptance += relationshipBonus;
    
    // Penalización por enojo
    acceptance -= client.currentAnger * 0.03f; // -3% por cada punto de enojo
    
    // ===== 6. BONUS POR TÁCTICA ACTIVA (modificador temporal) =====
    acceptance += GetActiveTacticBonus();
    
    // ===== 7. VARIABLES DE MERCADO (temporada alta, etc.) =====
    float marketMultiplier = GetMarketMultiplier(cargo.origin, cargo.destination);
    acceptance *= marketMultiplier;
    
    return Mathf.Clamp01(acceptance);
}
PARTE 5: TÁCTICAS DEL JUGADOR (COMO EN DEALER'S LIFE 2)
Táctica	Efecto	Requisito	Riesgo / Costo	Frase asociada
Presión de tiempo	+20% aceptación si cliente urgency>60; si no, -10%	1 vez por negociación	Si falla, cliente se enoja (+2 anger)	"Mirá que los precios están subiendo, si no cerramos hoy se pierde la tarifa"
Argumento de valor	+15% aceptación si serviceValue>50; mejora satisfacción +5	Reputación > 30	Ninguno	"Yo te voy a estar encima de la carga 24/7, cualquier problema lo resolvemos al toque"
Farol (al cliente)	+10% aceptación, pero si cliente tiene expertise en mercado (5% chance), -20% y +3 anger	Nivel 3	Descubrimiento = pérdida de confianza	"En origen está complicado, hay mucha demanda. Es lo mejor que pude conseguir"
Descuento por pago anticipado	Ofrece -X% (ej: 25%) si paga hoy. Aumenta aceptación +10%	Margen > 15%	Menor ganancia	"Si pagás al contado te puedo bajar un 25%"
Exclusividad a cambio de descuento	Bajas precio -5% pero cliente da próximas 3 cargas	Nivel 5	Si no cumple, pierdes reputación	"Te hago precio especial si me das las próximas 3 cargas de este origen"
Revelar costo (transparencia)	Muestra tu costo. +20% aceptación si cliente confiable; -15% si rata	1 vez por cliente	Puede pedir más descuento	"Mirá, te voy a ser honesto: esto me cuesta X. Necesito Y para que me cierre."
Llamada al supervisor (simular)	Finge pedir permiso para bajar precio. Baja automática -5% precio sin negociar	Empleado "Negociador"	Si cliente lo sabe, -10% reputación	"Dame un segundo que consulto con mi gerente... Bueno, me autorizaron bajarlo a Z"
PARTE 6: FLUJO DE NEGOCIACIÓN PASO A PASO
text
┌─────────────────────────────────────────────────────────────────┐
│  RONDA 0: El jugador ve la carga y el cliente                   │
│  - Muestra pistas iniciales (si hay historial)                  │
│  - Ajusta las 5 palancas                                        │
│  - Envía cotización                                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  Cálculo de aceptación base                                     │
│  - Si acceptance >= 0.65 → ACEPTACIÓN DIRECTA (raro en nuevos)  │
│  - Si no → entra en negociación                                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  RONDA 1: Cliente responde                                      │
│  - Contraoferta de precio (según concesión)                     │
│  - Mensaje personalizado según personalidad                     │
│  - Aparece 1 pista nueva (si aplica)                            │
│  - Barra de "distancia al acuerdo" se actualiza                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  El jugador elige:                                              │
│  [Aceptar] → cerrar trato                                       │
│  [Contraofertar] → nueva ronda                                  │
│  [Usar táctica] → aplicar bonus, luego contraofertar            │
│  [Rechazar] → perder carga, cliente se enoja                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  RONDA 2, 3, etc.                                               │
│  - Cliente cede más si la oferta está cerca                     │
│  - Cliente se endurece si el jugador fue muy agresivo           │
│  - Si enojo > threshold → cliente se retira                     │
│  - Si ronda == maxRounds → última oferta                        │
└─────────────────────────────────────────────────────────────────┘
PARTE 7: MEMORIA ENTRE NEGOCIACIONES (AMPLIACIÓN DE client_memory)
csharp
// Estructura que ya tienes, ampliar con:
public class ClientMemory
{
    public int deliveries;           // entregas completadas
    public int failed;               // fallos
    public int totalProfit;          // ganancia total que le generaste
    public float avgPriceRatio;      // precio promedio / costo
    public int firstSeen;            // día
    public int lastSeen;
    public List<string> favoriteRoutes;
    public int satisfaction;         // 0-100
    
    // NUEVOS:
    public float lastMargin;          // último margen que aceptó (lo recuerda)
    public float averageDelay;        // demora promedio en días
    public int trustLevel;            // 0-100 (confianza)
    public List<string> brokenPromises; // promesas incumplidas (ej: "dijo que daría exclusividad")
    public bool isBlocked;            // si el jugador lo bloqueó o él bloqueó al jugador
}
La memoria afecta:

avgPriceRatio → el cliente espera un margen similar o mejor. Si intentas subirlo mucho, rechaza.

averageDelay → si >3 días, el cliente exige descuentos.

trustLevel → influye en aceptación y en si acepta tácticas como "revelar costo".

PARTE 8: UI RECOMENDADA (NUEVOS ELEMENTOS)
Elemento	Descripción	Dónde se muestra
Barra de satisfacción	Verde (0-100)	Panel del cliente
Barra de enojo	Naranja a rojo (0-10)	Panel del cliente
Pistas emergentes	Texto como "Parece que solo le importa el precio"	Debajo del nombre del cliente
Barra de "distancia al acuerdo"	Porcentaje de cercanía al precio/condiciones que aceptaría	Al lado del precio ofertado
Contador de rondas	"Ronda 2/3"	En el diálogo
Historial de ofertas	Lista de tus ofertas y las contraofertas del cliente	Colapsable en la UI
Botones de tácticas	Iconos + tooltips, se habilitan según nivel/empleados	Debajo de las palancas
Feedback visual	El cliente sonríe o frunce el ceño según aceptación	Icono animado
PARTE 9: INTEGRACIÓN CON TU CÓDIGO EXISTENTE (UNITY)
9.1 Nueva clase: AdvancedNegotiationEngine.cs
csharp
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Systems.Negotiation
{
    public class AdvancedNegotiationEngine
    {
        private ClientManager clientManager;
        private PlayerProfile playerProfile;
        
        public AdvancedNegotiationEngine(ClientManager cm, PlayerProfile pp)
        {
            clientManager = cm;
            playerProfile = pp;
        }
        
        // Punto de entrada: el jugador envía una cotización avanzada
        public NegotiationResult EvaluateAdvancedQuote(AdvancedQuote quote, Cargo cargo, Client client)
        {
            var clientState = GetOrCreateNegotiationState(client);
            float acceptance = CalculateAcceptance(quote, clientState, cargo);
            
            if (acceptance >= 0.65f)
                return AcceptDeal(quote, cargo, clientState);
            
            if (clientState.round >= clientState.personality.maxRounds)
                return FinalOffer(quote, cargo, clientState);
            
            int counterPrice = CalculateCounterOffer(quote, clientState, cargo);
            string message = GetClientMessage(clientState, counterPrice);
            RevealHintIfNeeded(clientState);
            
            clientState.lastClientCounter = counterPrice;
            clientState.round++;
            
            return NegotiationResult.CounterOffer(counterPrice, message, clientState.round, 
                clientState.personality.maxRounds, GetDistanceToAgreement(quote, clientState));
        }
        
        // El jugador usa una táctica
        public NegotiationResult ApplyTactic(TacticType tactic, AdvancedQuote quote, Cargo cargo, Client client)
        {
            var clientState = GetOrCreateNegotiationState(client);
            var tacticBonus = CalculateTacticBonus(tactic, clientState, quote);
            
            // Aplicar efecto temporal
            clientState.activeTacticBonus = tacticBonus;
            
            // Re-evaluar aceptación
            float newAcceptance = CalculateAcceptance(quote, clientState, cargo);
            
            // Devolver nuevo estado
            return NegotiationResult.TacticApplied(tactic, newAcceptance, GetClientReaction(tactic, clientState));
        }
        
        // ... resto de métodos
    }
}
9.2 Feature flag
En FeatureFlags.cs:

csharp
public static class FeatureFlags
{
    public static bool USE_NEGOTIATION_V2 = false; // vieja
    public static bool USE_ADVANCED_NEGOTIATION = true; // nueva
}
9.3 En GameState.cs (Unity), modificar submit_quote y resolve_negotiation para usar la nueva engine si está activada:
csharp
public Dictionary submit_quote(string cargo_id, int price, int transport_mode, string agent_id) 
{
    // ... código existente ...
    
    if (FeatureFlags.USE_ADVANCED_NEGOTIATION)
    {
        var advancedQuote = new AdvancedQuote {
            price = price,
            paymentDays = GetSelectedPaymentDays(),
            transportMode = transport_mode,
            clientInsures = GetClientInsures(),
            volumeTier = GetVolumeTier(),
            exclusivityCommitment = GetExclusivityCommitment()
        };
        
        var result = advancedNegotiationEngine.EvaluateAdvancedQuote(advancedQuote, cargo, client);
        // Procesar resultado...
    }
    else
    {
        // Lógica vieja
    }
}
PARTE 10: TABLA DE TENSIÓN COMPLETA (PARA PEGAR EN TU DOCUMENTO)
Variable	Rango	Efecto en el cliente (aceptación)	Efecto en el jugador (consecuencia real)	Tensión principal
Precio	+10% a +50% sobre costo del flete	+15% a -35% según cercanía al target del cliente	Ganancia directa. El margen mínimo del 10% es sagrado	Ganancia vs probabilidad de cerrar el trato
Plazo de pago	0, 15, 30, 45, 60 días	Hasta +15% si das plazos largos (cliente feliz)	Si pagas a tu proveedor a 30 días y das 60, trabajás 30 días con tu plata. Presión de caja real	Liquidez vs atraer al cliente
Transporte	Marítimo (barato/lento) vs Aéreo (caro/rápido)	Urgente: +20% si usas aéreo; Rata: -10% si usas aéreo	Aéreo cuesta 2-3× más pero puedes cobrar más. Margen absoluto puede ser mayor	Rapidez (retención cliente) vs margen
Asegura cliente	Sí (paga seguro) / No (asume riesgo él)	+5% si asegura (tranquilidad). Si no asegura y se daña, te odia (-20 satisfacción)	Si no asegura, te ahorras gestión pero arriesgas relación. Si asegura, menos discusión por siniestros	Relación a largo plazo vs comodidad
Volumen / exclusividad	Normal / Volumen (+20% carga) / Exclusividad (próximas 3-5 cargas)	+10% a +15% aceptación por el descuento implícito	Bajas precio ~5-10% ahora, pero aseguras ingresos futuros. Si el cliente no cumple, pierdes tiempo	Ganancia inmediata vs seguridad futura
PRÓXIMOS PASOS (TU DECISIÓN)
¿Validamos este diseño? Dime si quieres ajustar algún número, añadir o quitar personalidades, cambiar porcentajes de tácticas, etc.

¿Quieres que escriba el código concreto de AdvancedNegotiationEngine.cs completo (incluyendo CalculateCounterOffer, GetClientMessage, etc.)?

¿Prefieres primero una versión simplificada con solo 2 personalidades y 2 tácticas para probar rápido el concepto?







FREIGHT FORWARDER
10 Ejemplos de Negociaciones
Maritimo - Aereo - Terrestre
Simulador de Gestion y Negociacion Logistica  |  Junio 2026
Indice
Clic derecho > Actualizar campo para refrescar numeros de pagina

Ejemplo 1: Maritimo FCL - Vinos Argentinos a Miami	3
Ejemplo 2: Aereo - Repuestos Urgentes de Mineria	5
Ejemplo 3: Terrestre - Maquinaria Agricola	7
Ejemplo 4: Maritimo LCL - Muebles de Diseno desde Italia	9
Ejemplo 5: Aereo - Flores Perecederas Colombia-EE.UU.	11
Ejemplo 6: Terrestre - Carga Peligrosa IMO	13
Ejemplo 7: Maritimo - Autos 0km Corea-Chile	15
Ejemplo 8: Aereo - Langostinos Patagonicos a Espana	17
Ejemplo 9: Terrestre - Cross-Border Retail Mexico-EE.UU.	19
Ejemplo 10: Maritimo Project Cargo - Turbina Eolica	21

Ejemplo 1: Maritimo FCL - Exportacion de Vinos Argentinos a Miami
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 6.800
Precio objetivo	USD 5.500
Paciencia	4 rondas
Sensibilidad al precio	Media
Confianza en tu empresa	Alta (cliente fiel - 3 anos)
Urgencia	Baja
Personalidad	Negociador calmado, valora relacion a largo plazo

Solicitud Visible
Bodega Los Andes S.A.
Necesitamos exportar: 3 contenedores 40'HC de vinos
Origen: Mendoza, Argentina
Destino: Miami, FL, EE.UU.
Fecha: Flexible, dentro de las proximas 4 semanas
Valor de la carga: USD 180.000
Temp control: 15-18C (container reefer)

Somos clientes suyos desde 2023. Esperamos condiciones acordes a nuestra relacion.

Que valor pueden ofrecernos?

Negociacion
FF (Vos):
Buenos dias Martin, un placer atenderlos nuevamente.

Por los 3 x 40'HC reefer Mendoza -> Miami, les puedo ofrecer: USD 6.200 todo incluido.

Incluye: flete maritimo, THC origen, doc fee, seguro basico (0.3%), y coordinacion del pre-carriage desde Mendoza hasta Zarate.
Cliente:
Agradezco la rapidez, pero debo ser honesto.

Estamos pagando hoy en dia USD 5.800 con otra forwarder para una ruta similar. No puedo justificar un aumento del 7% sin razon.

Entiendo que son reefer y que la temporada de exportacion de vinos esta en curso, pero necesito que mejoren ese numero.
FF (Vos):
Entiendo perfectamente, Martin. Dejame ser transparente con vos.

El flete reefer esta caro este mes. La linea nos esta cobrando USD 4.200 por contenedor. Solo en flete ya son USD 12.600. A eso sumale THC (USD 180 x cnt), seguro, documentacion, y el transporte terrestre desde Mendoza que ronda los USD 450 por caja.

Sin embargo, por ser ustedes y por el volumen anual que mueven, puedo bajar a USD 5.900. Ese es mi piso. Y les mantengo el seguro incluido sin costo adicional.
Cliente:
USD 5.900... esa cifra tiene mas sentido.

Pero necesito que me confirmen algo mas. El seguro basico del 0.3% me cubre hasta USD 540.000. Mi carga vale USD 180.000. Eso esta bien.

Ahora, que pasa con el control de temperatura durante el transbordo en Zarate? Tuvimos un incidente el ano pasado con otra operadora donde se corto la cadena de frio y perdimos 2 palets.
FF (Vos):
Esa es una preocupacion legitima y te la voy a resolver de dos maneras.

Primero: los contenedores reefer que usamos tienen data logger integrado. Vos recibis un reporte de temperatura cada 4 horas durante todo el trayecto.

Segundo: puedo ofrecerte un upgrade a seguro all-risk por solo USD 180 adicionales (total USD 6.080). Cubre danos por rotura de cadena de frio, con un limite de USD 200.000.

A USD 5.900 base + USD 180 el upgrade de seguro = USD 6.080 totales.
Cliente:
Me quedo con el seguro basico. El data logger me da suficiente tranquilidad.

Vamos con los USD 5.900. Pero necesito que me agreguen una condicion: si hay demora mayor a 7 dias en transito que no sea por fuerza mayor, nos bonifican USD 200 por contenedor.

Eso si, quiero el booking para la semana que viene. Tengo las exportaciones listas.
FF (Vos):
Acepto la clausula de demora. Es justa y confio en nuestra operativa.

Resumen del trato: USD 5.900 por los 3 x 40'HC reefer, Mendoza -> Miami, seguro basico 0.3%, data logger incluido, booking para la semana entrante.

Le mando la cotizacion formal en las proximas 2 horas. Un gusto como siempre, Martin.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Flete maritimo reefer (3 x 40'HC)	12.600	-
THC origen (3 cnt)	540	-
Doc fee	120	-
Pre-carriage Mendoza-Zarate (3 cnt)	1.350	-
Seguro basico (0.3% x USD 180.000)	540	-
Gastos operativos locales	250	-
TOTAL COSTOS	15.400	-
TOTAL COBRADO AL CLIENTE	-	19.700
MARGEN NETO	-	4.300 (22%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐☆☆ (buen precio, no agresivo)
Relacion	⭐⭐⭐⭐⭐ (cliente fiel, confianza alta)
Velocidad	⭐⭐⭐⭐☆ (booking rapido)
Gestion de Riesgo	⭐⭐⭐⭐☆ (data logger, seguro basico)
Margen	⭐⭐⭐⭐☆ (22% es solido)

Ejemplo 2: Aereo - Repuestos Urgentes de Mineria
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 18.000
Precio objetivo	USD 12.000
Paciencia	1 ronda (URGENTE)
Sensibilidad al precio	Baja
Confianza en tu empresa	Media
Urgencia	EXTREMA (mina parada)
Personalidad	Directo, no le interesa negociar, quiere solucion YA

Solicitud Visible
Minera Cerro Grande S.A.C. (Chile)
EMERGENCIA OPERATIVA

Necesitamos enviar repuestos criticos para excavadoras Komatsu.
Origen: Santiago, Chile
Destino: Lima, Peru
Peso: 850 kg
Dimensiones: 120 x 80 x 95 cm (1 pallet)
Valor: USD 45.000

La mina esta PARADA. Perdemos USD 120.000 por dia de inactividad.

Necesitamos que salga HOY. No manana. HOY.

Diganme que necesitan y cuanto cuesta. No me den vueltas.

Negociacion
FF (Vos):
Entendido. Emergencia confirmada. Estoy trabajando en eso ahora mismo.

Vuelo directo Santiago-Lima: LATAM Cargo tiene salida hoy a las 22:30, arribo manana 01:15. Espacio disponible.

Opcion 1 - Charter dedicado: USD 14.500. Garantizado. Salida en 4 horas.
Opcion 2 - Carga consolidada express: USD 8.900. Prioridad alta. Mismo vuelo de las 22:30.

Con la consolidada llega manana a la 1 AM. Con el charter, manana a medianoche.
Cliente:
La diferencia son 12 horas pero casi el doble de precio.

Mi gerente de operaciones dice que con la consolidada de las 22:30 resolvemos. Si llega a Lima a la 1 AM, con el clearance aduanero express y el transporte terrestre a la mina, podemos tener las piezas operando manana al mediodia.

USD 8.900. Esa es la opcion. Pero necesito garantias: si el vuelo se retrasa mas de 6 horas, que pasa?
FF (Vos):
Te entiendo perfectamente. La consolidada express es la decision correcta.

Te doy dos garantias:
1. Vuelo confirmado con booking number en las proximas 2 horas.
2. Si hay retraso mayor a 6 horas por culpa de la aerolinea, te bonifico el 30% del flete aereo.

Adicionalmente: incluyo seguro all-risk sin costo (cubrimos los USD 45.000 de valor). Y gestiono el despacho aduanero en Lima con nuestro agente local, clearance en maximo 3 horas.

Total: USD 8.900 incluye: flete aereo, fuel surcharge, security fee, handling en ambos aeropuertos, seguro all-risk, y gestion aduanera express en Lima.
Cliente:
Perfecto. Eso es lo que necesitaba escuchar.

USD 8.900 con esas garantias me cierra. El costo de oportunidad de perder un dia mas de produccion es USD 120.000. Comparado con eso, el flete es irrelevante.

Necesito: booking hoy, AWB antes de las 18:00, y contacto directo de su agente en Lima para coordinar el pickup en el aeropuerto.

Emitan la factura proforma. Pago contra embarque.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Tarifa aerea (850kg, N-class)	4.200	-
Fuel surcharge	890	-
Security surcharge	120	-
Handling SCL + LIM	340	-
Seguro all-risk (0.6%)	270	-
Agente aduanero Lima	180	-
Gestion emergencia + overtime	150	-
TOTAL COSTOS	6.150	-
TOTAL COBRADO AL CLIENTE	-	8.900
MARGEN NETO	-	2.750 (31%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐⭐☆ (premium justificado por urgencia)
Velocidad	⭐⭐⭐⭐⭐ (misma noche)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (seguro all-risk, garantia de retraso)
Confianza	⭐⭐⭐⭐☆ (cliente satisfecho con garantias)
Margen	⭐⭐⭐⭐⭐ (31% excelente)

Ejemplo 3: Terrestre - Maquinaria Agricola
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 3.500
Precio objetivo	USD 2.800
Paciencia	5 rondas (no tiene apuro)
Sensibilidad al precio	MUY ALTA
Confianza en tu empresa	Baja (primera vez)
Urgencia	Baja (cosecha es en 3 meses)
Personalidad	Tacaño, comparador compulsivo, desconfiado

Solicitud Visible
AgroServicios del Centro SRL
Necesito trasladar una cosechadora John Deere S760.
Origen: Fabriquita de maquinaria, Ciudad de Cordoba
Destino: Campo propio, departamento Rio Cuarto (220 km)

Peso: 12.500 kg aprox
Dimensiones: 10.5m largo x 3.8m ancho x 4.2m alto

Ya me cotizaron USD 2.600 con otra transportadora. Si no me mejoran ese precio ni me gasten en responder.

Negociacion
FF (Vos):
Buenos dias. Entiendo que buscan la mejor tarifa, y se lo voy a ser honesto desde el primer momento.

Una cosechadora de esas dimensiones requiere transporte especial: camion plataforma extendida + vehiculo de acompañamiento obligatorio (exceso de dimensiones).

Mi precio es USD 3.100. Incluye: permisos especiales de circulacion por ruta nacional (obligatorios para cargas superdimensionadas), seguro de transporte hasta USD 150.000, vehiculo de acompañamiento con permisovial, y coordino el embarque/desembarque con grua en origen y destino.
Cliente:
USD 3.100? Estan locos.

La otra empresa me cotizo USD 2.600. Les estoy dando USD 500 mas de diferencia. Ni hablar.

Seguro que me estan metiendo un vehiculo de acompañamiento que no necesito. La ruta es nacional, no hay peajes restrictivos en ese trayecto.
FF (Vos):
Entiendo su punto. Pero le voy a explicar por que hay diferencia:

La cotizacion de USD 2.600 que recibio probablemente NO incluye:
- Permisos especiales de circulacion: son OBLIGATORIOS para carga > 3m de ancho. Multa si no los tiene: USD 5.000+
- Seguro de carga: ¿cubre los USD 350.000 que vale esa maquina?
- Vehiculo de acompañamiento: es OBLIGATORIO por ley nacional.

Mi cotizacion es todo incluido. Pero mire, por ser la primera vez que trabajamos juntos y porque quiero ganarme su confianza, le puedo bajar a USD 2.850.
Eso es solo USD 250 mas que la otra, pero con todo legal y asegurado.
Cliente:
USD 2.850...

Mejoro, pero sigue siendo mas caro. Y la verdad, me da desconfianza que bajen tan rapido. Me hace pensar que el precio original estaba inflado.

Escucheme. Yo tengo 3 campos mas y una flota de 8 tractores. Si me dan buen precio ahora, les traigo todo el negocio de transporte de maquinaria para los proximos 2 años. Pero necesito que me traten como cliente potencial grande, no como un desconocido.
FF (Vos):
Lo respeto. Y le creo sobre los otros campos.

Le voy a hacer una propuesta distinta. No bajo mas el precio. Pero le agrego valor:

USD 2.850 finales, y le incluyo GRATIS:
- Monitoreo GPS en tiempo real del camion (sabe donde esta la maquina todo el tiempo)
- Fotografias del estado de la carga al embarque y desembarque
- Prioridad absoluta: si hay retraso por falla mecanica del camion, reemplazo en 2 horas (tengo flota propia)
- Y le congelo ese precio para los proximos 3 viajes similares dentro de los 12 meses.

USD 2.850 con garantias y precio congelado. Eso es lo mejor que puedo hacer.
Cliente:
Esa propuesta me gusta mas.

El precio congelado para 3 viajes mas es inteligente. Me da previsibilidad y a ustedes les asegura volumen.

Me quedo con USD 2.850. Pero una condicion: el viaje se hace el proximo martes, temprano. Necesito la maquina en el campo antes del mediodia.

Y quiero el contrato firmado con la clausula de precio congelado explicita.
FF (Vos):
Hecho. Martes salida 05:00 AM de Cordoba, estimo llegada 10:30 AM a Rio Cuarto.

Le preparo el contrato con la clausula de precio congelado y se lo envio hoy.

Bienvenido a bordo. Vamos a cuidar esa maquina como si fuera nuestra.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Camion plataforma extendida (ida)	1.100	-
Vehiculo de acompañamiento	280	-
Permisos especiales circulacion	120	-
Seguro de transporte (0.2%)	700	-
Combustible	350	-
Peajes	85	-
Grua embarque/desembarque	200	-
TOTAL COSTOS	2.835	-
TOTAL COBRADO AL CLIENTE	-	2.850
MARGEN NETO	-	15 (0.5%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐☆☆☆☆ (margen casi nulo, estrategia de captacion)
Relacion	⭐⭐⭐⭐☆ (cliente potencial grande, apuesta a largo plazo)
Velocidad	⭐⭐⭐⭐☆ (acordado para martes)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (todo legal, seguro completo)
Estrategia	⭐⭐⭐⭐⭐ (inversion en relacion, 3 viajes futuros garantizados)

Ejemplo 4: Maritimo LCL - Muebles de Diseno desde Italia
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 8.500
Precio objetivo	USD 6.000
Paciencia	4 rondas
Sensibilidad al precio	Media-Baja
Confianza en tu empresa	Media
Urgencia	Media (showroom abre en 6 semanas)
Personalidad	Sofisticado, exigente, valora el servicio premium

Solicitud Visible
Estudio Varela - Interior Design (Buenos Aires)
Necesito importar una coleccion de muebles italianos.
Origen: Milano, Italia
Destino: Buenos Aires, Argentina

Carga: 18 CBM (sillas, mesas, lamparas de diseno)
Peso: 2.400 kg
Valor declarado: USD 85.000

Requisitos especiales:
- Embalaje premium obligatorio (madera+carton+acolchado)
- Seguro all-risk por valor total
- No apilar. Muebles delicados con acabados de laca.
- Necesito ETA exacto. Mi showroom abre el 15 de julio y estos muebles son la pieza central.

Busco un servicio premium. No el precio mas barato.

Negociacion
FF (Vos):
Buenos dias, Estudio Varela. Entiendo perfectamente el tipo de operacion que necesitan.

Para 18 CBM desde Milano a Buenos Aires, les propongo lo siguiente: USD 7.800 todo incluido.

El servicio incluye:
- Consolidacion LCL semanal directa Genova-Buenos Aires (sin transbordos intermedios)
- Embalaje premium en origen: cajas de madera triple corrugado con interior foam + cartón+acolchado
- Etiquetado 'NO APILAR' en 4 idiomas + handling priority en puerto
- Seguro all-risk por USD 85.000 (sin franquicia)
- Gestion aduanera completa en Buenos Aires
- Delivery final con camion hidraulico y 2 operarios para descarga cuidadosa

Transito estimado: 28-32 dias puerta a puerta.
Cliente:
USD 7.800 es un numero importante, pero la propuesta me cierra en terminos de servicio.

Sin embargo, tengo dos inquietudes:

1. El transito de 28-32 dias me deja justo. Si hay una demora de 1 semana, no llego para la inauguracion del showroom. Eso me genera un costo de oportunidad enorme.

2. Quiero ver fotos del proceso de embalaje. Necesito saber que mis piezas van a estar bien protegidas. Tengo experiencias previas desastrosas con muebles italianos mal embalados.
FF (Vos):
Ambas preocupaciones son validas y las voy a resolver.

Sobre el transito: la consolidacion LCL sale todos los martes de Genova. Puedo asegurarles espacio en el barco del 10 de junio, que arriba a Buenos Aires el 8 de julio. Eso les da 7 dias de margen antes de la inauguracion.

Pero para darles tranquilidad total, les ofrezco esta garantia: si la carga no esta en Buenos Aires para el 10 de julio por demora del transporte maritimo, les descuento el 50% del flete. Eso esta en el contrato.

Sobre el embalaje: les envio ahora mismo fotos y videos del proceso de embalaje que hacemos en nuestro warehouse de Milano. Trabajamos con una empresa especializada en muebles de diseño. Pueden ver ejemplos de embalajes similares que hicimos para un proyecto de un hotel 5 estrellas en Punta del Este.
Cliente:
La garantia de los 7 dias de margen y el descuento del 50% me dan mucha tranquilidad.

Las fotos y videos del embalaje serian clave. Por favor envienmelas hoy.

Ahora, respecto al precio: USD 7.800 es justo para el servicio, pero necesito saber si hay algo que puedan flexibilizar. No es que no pueda pagarlo, pero siempre hay margen, ¿no?
FF (Vos):
Le aprecio la honestidad. Y si, hay un margen.

Le puedo ofrecer USD 7.400 si me confirman hoy mismo. Es un descuento de USD 400 por decision rapida. Me permite cerrar el espacio en el barco del martes sin riesgo de cancelacion de ultimo momento.

USD 7.400 incluye TODO lo que mencione: embalaje premium, seguro all-risk, no-apilar con prioridad, garantia de fecha, delivery con hidraulico y 2 operarios.

El unico condicionante es la confirmacion de hoy.
Cliente:
USD 7.400 con todo incluido y confirmacion hoy.

Me parece justo. El descuento por decision rapida es inteligente de su parte y me beneficia a mi tambien.

Confirmo el servicio. Por favor envienme:
- Fotos/videos del embalaje hoy
- Cotizacion formal con la garantia de fecha explicita
- Instrucciones para el pickup en Milano (tenemos un deposito en Via Tortona)

Y les anticipo: si todo sale bien, tengo 2 importaciones mas este año. Una de sofas de B&B Italia y otra de iluminacion de Flos.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Flete LCL Genova-BUE (18 CBM)	3.600	-
Embalaje premium Milano	890	-
Seguro all-risk (USD 85.000)	680	-
THC destino Buenos Aires	320	-
Desconsolidacion + handling	280	-
Gestion aduanera completa	350	-
Delivery hidraulico + 2 operarios	420	-
TOTAL COSTOS	6.540	-
TOTAL COBRADO AL CLIENTE	-	7.400
MARGEN NETO	-	860 (13%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐☆☆ (buen precio, no agresivo)
Servicio Premium	⭐⭐⭐⭐⭐ (embalaje, garantia, delivery especial)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (garantia de fecha, seguro all-risk)
Relacion	⭐⭐⭐⭐☆ (cliente potencial recurrente)
Velocidad de cierre	⭐⭐⭐⭐⭐ (descuento por decision rapida funciono)

Ejemplo 5: Aereo - Flores Perecederas Colombia-EE.UU.
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 4.200
Precio objetivo	USD 3.500
Paciencia	2 rondas
Sensibilidad al precio	Media
Confianza en tu empresa	Alta
Urgencia	Alta (flores se deterioran rapido)
Personalidad	Pragmatico, conoce el negocio, no tolera errores

Solicitud Visible
Flores del Valle Export S.A.S. (Colombia)
Exportacion semanal de rosas premium.
Origen: Rionegro (cerca de Medellin)
Destino: Miami, FL (distribucion a todo EE.UU.)

Volumen: 5 pallets de cajas de flores
Peso: 1.800 kg
Temperatura: 2-4C constante (cadena de frio ininterrumpida)

Salida: todos los martes y viernes
Volumen anual: 104 envios (2 por semana)

Condicion: si llega con temperatura fuera de rango, la carga es perdida total. El seguro no alcanza. La reputacion con mis clientes de Miami tampoco.

Necesito un precio fijo por envio para todo el ano.

Negociacion
FF (Vos):
Flores del Valle, un gusto como siempre.

Entiendo la operacion. LCL aereo con cadena de frio Rionegro-Miami, 2 vuelos semanales, 104 envios al año, precio fijo.

Mi propuesta: USD 3.800 por envio. Precio fijo por 12 meses.

Incluye:
- Flete aereo AVIANCA Cargo, vuelos directos MDE-MIA (martes y viernes 23:15)
- Pre-cooling en Rionegro hasta 2C antes del vuelo
- Contenedores refrigerados activos (no pasivos, ACTIVOS con monitoreo)
- Monitoreo de temperatura en tiempo real cada 15 minutos
- Prioridad de descarga en Miami (primeros 30 minutos del aterrizaje)
- Custodia refrigerada en Miami hasta el pickup del cliente

Y lo mas importante: si la temperatura sale del rango 2-4C por responsabilidad nuestra, cubrimos el 100% del valor de la carga.
Cliente:
USD 3.800 por envio...

Hago las cuentas: 104 envios x USD 3.800 = USD 395.200 anuales.

Estoy pagando hoy USD 3.600 promedio, pero con un proveedor que no me da monitoreo en tiempo real ni la garantia de temperatura. Tengo 2-3 incidentes por año que me cuestan USD 8.000-12.000 cada uno.

Su propuesta es mejor en terminos de servicio, pero el precio base es mas alto.
FF (Vos):
Exacto. Y hagamos la matematica juntos:

Su costo actual: 104 x USD 3.600 = USD 374.400
+ 3 incidentes promedio x USD 10.000 = USD 30.000
= USD 404.400 costo real anual

Con nosotros: 104 x USD 3.800 = USD 395.200
Incidentes: cercanos a cero (monitoreo activo + garantia)
= USD 395.200 costo real anual

Ahorro anual con nosotros: USD 9.200 minimos. Y eso sin contar el valor de su reputacion con los distribuidores de Miami.

Pero le voy a dar mas: a USD 3.800 por envio, si confirman los 104 envios con contrato anual, les bonificamos el ultimo mes (4 envios gratis). Eso baja el costo efectivo a USD 3.653 por envio.
Cliente:
Esas cuentas me cierran.

USD 3.800 con bonificacion del ultimo mes. Monitoreo en tiempo real. Garantia de temperatura. Y precio fijo por 12 meses.

Pero quiero una clausula de escape: si en los primeros 3 meses tienen mas de 1 incidente de temperatura, puedo rescindir el contrato sin penalidad y buscar otro proveedor.

Si ustedes confian tanto en su servicio, esa clausula no deberia ser un problema.
FF (Vos):
Acepto la clausula. Confio 100% en nuestra operativa de cadena de frio.

Resumen: USD 3.800 por envio, 104 envios anuales, precio fijo 12 meses, monitoreo en tiempo real, garantia de temperatura, bonificacion de 4 envios gratis en el ultimo mes, y clausula de rescision si mas de 1 incidente en los primeros 3 meses.

Le mando el contrato mañana. El primer vuelo seria el martes que viene.
Desglose de Costos del FF
Concepto	Costo (USD/envio)	Cobro (USD/envio)
Flete aereo MDE-MIA (1.800kg)	2.100	-
Contenedor refrigerado activo	340	-
Pre-cooling Rionegro	85	-
Monitoreo IoT temperatura	45	-
Handling MDE + MIA	180	-
Custodia refrigerada Miami	120	-
TOTAL COSTOS	2.870	-
TOTAL COBRADO	-	3.800
MARGEN NETO/envio	-	930 (24%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐⭐☆ (precio fijo anual, volumen garantizado)
Servicio Especializado	⭐⭐⭐⭐⭐ (cadena de frio activa, monitoreo)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (garantia de temperatura, clausula de escape aceptada)
Volumen Anual	⭐⭐⭐⭐⭐ (104 envios, contrato recurrente)
Margen	⭐⭐⭐⭐⭐ (24% excelente, volumen compensa)

Ejemplo 6: Terrestre - Carga Peligrosa IMO Brasil-Uruguay
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 9.500
Precio objetivo	USD 7.000
Paciencia	3 rondas
Sensibilidad al precio	Media
Confianza en tu empresa	Media (trabajaron 1 vez antes)
Urgencia	Media (pedido de cliente final con fecha comprometida)
Personalidad	Metodico, pregunta mucho, necesita certezas legales

Solicitud Visible
Quimica Industrial Porto Alegre Ltda.
Necesitamos transportar productos quimicos industrial.
Origen: Porto Alegre, RS, Brasil
Destino: Montevideo, Uruguay

Clase IMO: 8 (Corrosivos)
UN: 2796
Descripcion: Acido sulfurico en solucion concentrada
Cantidad: 12 tambores de 200 litros c/u
Peso total: 3.200 kg (incluyendo tambores)
Embalaje: Tambores de acero inoxidable UN-approved

Necesito que me confirmen:
- Licencias para transporte de carga peligrosa en ruta internacional BR-UY
- Seguro especializado para productos corrosivos
- Ruta autorizada (no cualquier ruta, la autorizada por los dos paises)

Documentacion completa y legal. Sin atajos.

Negociacion
FF (Vos):
Buenos dias. IMO 8, Porto Alegre-Montevideo. Operacion que requiere precision total.

Les propongo USD 8.200 todo incluido.

El servicio incluye:
- Camion cisterna/tanque certificado para IMO 8 con doble pared de seguridad
- Conductor con certificacion MOPP especializada en productos corrosivos (no basica, ESPECIALIZADA)
- Licencias de transito peligroso Brasil: ANTT + IBAMA (activas y vigentes)
- Licencia de transito Uruguay: MTOP + DINAMA (ya gestionadas, tenemoslas)
- Ruta autorizada: BR-116 -> Paso de los Libres -> Ruta 3 UY (unica habilitada para IMO 8)
- Seguro RCC (Responsabilidad Civil de Carga) por USD 500.000 + seguro de carga por valor declarado
- Escolta vehicular obligatoria en territorio uruguayo (normativa DINAMA)
- Permiso de transito fronterizo con pre-clearance

Transit time: 3-4 dias dependiendo del clearance aduanero.
Cliente:
USD 8.200...

La propuesta es completa, eso lo valoro. Pero tengo una preocupacion principal: el seguro.

Mi carga tiene un valor asegurable de USD 25.000, pero el riesgo de derrame corrosivo en via publica puede generar un dano ambiental de millones. El seguro RCC de USD 500.000 me da tranquilidad, pero quiero confirmar: cubre dano ambiental por derrame accidental?
FF (Vos):
Excelente pregunta. Y la respuesta es si, pero dejame ser especifico.

El seguro RCC de USD 500.000 cubre:
- Dano a terceros (personas y bienes)
- Dano ambiental DIRECTO (contaminacion de suelo, agua superficial)
- Costos de remediacion basica

Pero NO cubre:
- Multas punitivas del estado
- Dano a la reputacion de la empresa
- Paralizacion de la actividad del tercero afectado

Por eso, adicionalmente, ofrezco un upgrade a RCC Plus por USD 420 adicionales (total USD 8.620) que eleva la cobertura a USD 1.500.000 y cubre tambien remediacion extendida + asistencia legal especializada. Es lo que usamos para transportes de hidrocarburos y quimicos en zonas urbanas.
Cliente:
USD 8.620 con RCC Plus...

Me quedo con el RCC estandar de USD 500.000. Mi carga es corrosiva pero esta perfectamente embalada en tambores UN-approved. El riesgo de derrame es minimisimo si el transporte se hace bien.

Ahora, respecto al precio base de USD 8.200: necesito que mejoren. Mi cliente final me presiona por el costo logistica. Si no puedo mostrarle un numero razonable, me va a pedir que busque alternativas.

Les propongo: USD 7.800 y les garantizo 2 transportes mas este semestre (misma ruta, volumen similar). Eso son casi USD 25.000 en ingresos garantizados.
FF (Vos):
USD 7.800 con 2 transportes adicionales garantizados...

Le voy a ser directo: USD 7.800 es mi costo operativo + 8% de margen. Es bajo.

Pero el volumen recurrente me interesa. Asi que acepto USD 7.800 con una condicion: los 2 transportes adicionales se confirman con orden de compra firmada en los proximos 15 dias. Si no se confirman, este primer envio se refactura a USD 8.200.

Eso protege a ambos: ustedes obtienen el precio bajo y nosotros aseguramos el volumen.
Cliente:
Acepto la condicion. Es justa.

USD 7.800, RCC estandar USD 500.000, todo legal, ruta autorizada BR-116 -> Paso de los Libres -> Ruta 3.

Necesito que me envien hoy:
- Copia de las licencias ANTT, IBAMA, MTOP y DINAMA
- Ficha tecnica del camion y del conductor
- Poliza de seguro RCC con nuestro nombre como beneficiario adicional
- Cronograma detallado con puntos de control

Y les confirmo: la orden de compra de los 2 envios adicionales la firmo la semana que viene.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Camion IMO 8 certificado (ida)	2.800	-
Conductor certificado MOPP	450	-
Licencias ANTT + IBAMA	180	-
Licencias MTOP + DINAMA UY	220	-
Escolta vehicular Uruguay	380	-
Seguro RCC USD 500.000	890	-
Seguro de carga (0.4%)	100	-
Permiso transito fronterizo	150	-
Peajes ruta autorizada	120	-
Combustible	850	-
TOTAL COSTOS	6.140	-
TOTAL COBRADO AL CLIENTE	-	7.800
MARGEN NETO	-	1.660 (21%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐☆☆ (margen ajustado pero volumen recurrente)
Cumplimiento Legal	⭐⭐⭐⭐⭐ (todas las licencias, ruta autorizada)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (RCC, seguro de carga, camion certificado)
Relacion	⭐⭐⭐⭐☆ (volumen recurrente garantizado)
Estrategia	⭐⭐⭐⭐⭐ (3 envios confirmados, cliente fidelizado)

Ejemplo 7: Maritimo - Autos 0km Corea del Sur a Chile
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 25.000
Precio objetivo	USD 18.000
Paciencia	3 rondas
Sensibilidad al precio	Media
Confianza en tu empresa	Alta (concesionario oficial, relacion de 4 anos)
Urgencia	Media (stock de showroom)
Personalidad	Corporativo, proceso de aprobacion interno, necesita todo documentado

Solicitud Visible
Automotriz Andina S.A. (Concesionario Oficial Hyundai)
Importacion de unidades 0km para stock de showroom.
Origen: Puerto de Busan, Corea del Sur
Destino: Puerto de Valparaiso, Chile

Carga: 8 unidades Hyundai Tucson (SUV mediano)
Valor CIF total: USD 320.000

Requisitos:
- Transporte en contenedor de 40'HC adaptado para vehiculos (rampas de seguridad, sistemas de amarre profesional)
- Seguro all-risk por valor total (USD 320.000)
- Gestion aduanera completa en Valparaiso (Homologacion de vehiculos incluida)
- Limpieza y preparacion para entrega en showroom

Necesito entrega en 35 dias maximo para mantener el stock prometido.

Negociacion
FF (Vos):
Automotriz Andina, siempre un placer.

8 unidades Busan-Valparaiso. Les propongo USD 19.500 todo incluido.

Desglose del servicio:
- Contenedor 40'HC con sistema de racks para 8 vehiculos (4 arriba + 4 abajo)
- Sistema de amarre profesional con cinchas certificadas (sin cadenas que dañen la carroceria)
- Flete maritimo Busan-Valparaiso directo (sin transbordos, linea HMM)
- Seguro all-risk por USD 320.000 (sin franquicia, cobertura including general average)
- THC destino Valparaiso
- Gestion aduanera completa con tramite de homologacion ante el Ministerio de Transportes chileno
- Limpieza exterior profesional en puerto
- Delivery con camion portavehiculos a su showroom en Santiago

Transit time: 28-32 dias. Llegan con 3-7 dias de margen sobre su limite de 35.
Cliente:
USD 19.500...

El servicio es completo y la propuesta me cierra en general. Pero debo justificar el gasto ante la casa matriz en Santiago.

Necesito que me ayuden con dos puntos:

1. El seguro all-risk: mi casa matriz exige que la poliza tenga cobertura 'total loss only' como minimo, pero prefieren all-risk. Pueden confirmar que la poliza es all-risk verdadero y no 'all-risk con exclusiones'? Necesito ver la poliza antes del embarque.

2. La homologacion: cuanto tiempo toma? Tenemos experiencias previas donde el tramite demoro 15 dias y tuvimos unidades paradas en puerto generando costos de almacenaje.
FF (Vos):
Ambos puntos los resuelvo ahora mismo.

1. Seguro: es all-risk VERDADERO, no all-risk restringido. Cobertura sin exclusiones por: dano mecanico, ralladuras, inundacion, incendio, robo parcial o total, y general average. Les envio la poliza modelo hoy mismo para revision de su casa matriz. Y agrego: si la casa matriz no aprueba la poliza, la cambiamos sin costo adicional por una que cumpla sus requisitos.

2. Homologacion: tenemos convenio con el Ministerio de Transportes chileno que nos permite tramite preferencial. Tiempo promedio: 5-7 dias habiles. Pero para darles tranquilidad, les ofrezco garantia: si la homologacion demora mas de 10 dias habiles, cubrimos los costos de almacenaje en puerto (USD 45/dia por contenedor).

Como bonus: incluyo un reporte fotografico profesional del estado de cada vehiculo al embarque en Busan y al desembarque en Valparaiso. Fotos HD de cada angulo, interior y exterior. Asi tienen evidencia documentada para cualquier reclamo.
Cliente:
Eso es exactamente lo que necesitaba escuchar.

La garantia de homologacion me da mucha tranquilidad. El reporte fotografico es un valor agregado excellente que no esperaba.

Sin embargo, necesito que me ayuden con el precio. Mi presupuesto aprobado es USD 18.500. Estan USD 1.000 arriba. Pueden acercarse a ese numero?
FF (Vos):
USD 18.500...

Le voy a hacer una propuesta que funcione para ambos.

No puedo llegar a USD 18.500 con todo incluido. Pero puedo ofrecer USD 19.000 si me confirman hoy.

USD 19.000 incluye TODO: racks, amarre profesional, flete directo, all-risk sin franquicia, homologacion con garantia, limpieza, delivery a showroom, y reporte fotografico HD.

Eso es USD 500 por debajo de mi oferta original. Y les recuerdo: el volumen anual que mueven (4 importaciones de 8 unidades = 32 autos/año) nos permite ser agresivos en el precio.

A USD 19.000 por envio, su costo logistico por unidad es USD 2.375. Eso representa el 0.74% del valor CIF. La industria acepta hasta 1.5% como saludable.
Cliente:
USD 19.000 y su argumento del 0.74% son convincentes.

Confirmo el servicio a USD 19.000. Les pido:
- Cotizacion formal con todo detallado
- Poliza modelo hoy
- Confirmacion de booking para la semana entrante
- Contacto directo de su agente en Busan para coordinar el entregado de las unidades

Y como referencia: tenemos 3 importaciones mas este año. Si todo sale bien, les renovamos exclusividad para 2027.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Contenedor 40'HC + racks vehiculos	3.800	-
Flete maritimo Busan-Valparaiso	5.200	-
Seguro all-risk (USD 320.000, 0.15%)	480	-
THC destino Valparaiso	280	-
Homologacion vehiculos	350	-
Desconsolidacion + handling	220	-
Limpieza profesional	120	-
Delivery Valparaiso-Santiago	280	-
Reporte fotografico HD	80	-
TOTAL COSTOS	10.810	-
TOTAL COBRADO AL CLIENTE	-	19.000
MARGEN NETO	-	8.190 (43%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐⭐☆ (precio corporativo, margen excelente)
Servicio	⭐⭐⭐⭐⭐ (racks, amarre profesional, fotos, homologacion)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (all-risk, garantia homologacion)
Relacion	⭐⭐⭐⭐⭐ (exclusividad potencial para 2027)
Margen	⭐⭐⭐⭐⭐ (43% excepcional)

Ejemplo 8: Aereo - Langostinos Patagonicos a Espana
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 12.000
Precio objetivo	USD 9.000
Paciencia	3 rondas
Sensibilidad al precio	Media
Confianza en tu empresa	Media
Urgencia	Alta (temporada de exportacion, ventana corta)
Personalidad	Apasionado por su producto, exigente con la calidad, negociador duro

Solicitud Visible
Patagonia Seafood S.A. (Puerto Madryn, Argentina)
Exportacion de langostinos frescos congelados.
Origen: Puerto Madryn, Chubut, Argentina
Destino: Madrid, Espana

Producto: Langostino argentino (Pleoticus muelleri)
Presentacion: IQF congelado a -18C, cajas de 10kg
Volumen: 2.500 kg (250 cajas)
Valor: USD 62.500

Requisitos criticos:
- Temperatura: -18C ININTERRUMPIDA (no negociable)
- Transit time maximo: 24 horas puerta a puerta
- Cadena de frio certificada HACCP
- Certificado sanitario de exportacion SENASA incluido

Temporada corta: solo exportamos 6 semanas al año. Cada envio cuenta.

Negociacion
FF (Vos):
Patagonia Seafood, un honor trabajar con un producto tan premium.

La ruta Puerto Madryn-Madrid es compleja. No hay vuelos directos. Les propongo una solucion de 2 tramos:

Tramo 1: Puerto Madryn -> Buenos Aires (camion refrigerado a -25C, 18 horas)
Tramo 2: Buenos Aires -> Madrid (Iberia Cargo directo, 12 horas de vuelo)

Total transit time: 22 horas puerta a puerta.

Precio: USD 10.200 todo incluido.

Incluye: camion refrigerado ultra-congelacion, handling en EZE, flete aereo, seguro refrigerado, certificado SENASA, y delivery refrigerado en Madrid.
Cliente:
USD 10.200 es caro.

El ano pasado pague USD 8.800 por una operacion similar. Se que los costos subieron, pero un 16% de aumento me parece excesivo.

Ademas, 22 horas esta bien, pero el tramo terrestre de 18 horas me preocupa. Es mucho tiempo en camion. Que pasa si hay un accidente? Una averia del camion? Un control aduanero en ruta?
FF (Vos):
Entiendo su preocupacion. Y le voy a dar una solucion mejor.

Sobre el precio: el aumento se debe al fuel surcharge que subio 23% este año y a la nueva regulacion de biocombustibles en Argentina que encarecio el transporte refrigerado. Pero no me voy a esconder detras de eso.

Le propongo una alternativa que reduce el riesgo del tramo terrestre y mejora el precio:

NUEVA OPCION:
Tramo 1: Puerto Madryn -> Trelew (camion refrigerado, 1 hora)
Tramo 2: Trelew -> Buenos Aires (Aerolineas Argentinas Cargo, vuelo directo, 2.5 horas)
Tramo 3: Buenos Aires -> Madrid (Iberia Cargo directo, 12 horas)

Transit time total: 18 horas. 4 horas menos.
Precio: USD 9.800.

El vuelo Trelew-Buenos Aires existe los martes y jueves. Reduce drasticamente el tiempo en camion.
Cliente:
Esa opcion es mucho mejor.

18 horas de transit time y solo 1 hora en camion. Eso resuelve mi principal preocupacion.

USD 9.800 es razonable. Pero necesito que me confirmen: el vuelo Trelew-Buenos Aires tiene capacidad refrigerada? No quiero que mi carga viaje en bodega normal con gel packs. Necesito refrigeracion activa.

Y otra cosa: necesito que el certificado SENASA lo gestionen ustedes con 48 horas de anticipacion. No puedo perder tiempo en tramites.
FF (Vos):
Confirmado: el vuelo Trelew-Buenos Aires de Aerolineas Argentinas Cargo tiene contenedores refrigerados activos (RKN). Temperatura controlada a -20C. Monitoreo continuo.

Y sobre SENASA: tenemos convenio directo con la oficina de Puerto Madryn. El certificado sanitario lo tramitamos nosotros con 48hs de anticipacion. Ustedes solo nos entregan la documentacion del producto.

Ademas, agrego sin costo: un data logger por pallet (2 en total) con lectura de temperatura cada 5 minutos. Reciben un informe completo al finalizar el viaje. Si hay una sola desviacion de -18C, lo saben al instante.

USD 9.800, 18 horas de transito, refrigeracion activa en todos los tramos, certificado SENASA incluido, data loggers gratuitos.
Cliente:
Perfecto. Eso es exactamente lo que necesito.

USD 9.800 confirmados. Operacion para el proximo martes (vuelo Trelew-BUE + conexion con Iberia).

Les envio hoy:
- Packing list detallado por caja
- Certificado de origen
- Ficha tecnica del producto

Necesito de ustedes:
- Confirmacion de booking en 24 horas
- Contacto en Madrid para coordinar delivery (nuestro cliente es un distribuidor mayorista en Mercamadrid)

Y les adelanto: si este envio llega perfecto, les confirmo los 3 envios restantes de la temporada.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Camion PMY-Trelew refrigerado	180	-
Flete aereo Trelew-BUE (2.500kg)	890	-
Flete aereo BUE-Madrid (2.500kg)	4.200	-
Contenedores RKN refrigerados (x2)	780	-
Handling TRE + EZE + MAD	420	-
Seguro refrigerado (0.4%)	250	-
Certificado SENASA	120	-
Data loggers IoT (x2)	85	-
Delivery refrigerado Madrid	280	-
TOTAL COSTOS	7.205	-
TOTAL COBRADO AL CLIENTE	-	9.800
MARGEN NETO	-	2.595 (26%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐⭐☆ (competitivo para carga perecedera premium)
Velocidad	⭐⭐⭐⭐⭐ (18 horas, excelente)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (refrigeracion activa, data loggers)
Servicio	⭐⭐⭐⭐⭐ (SENASA incluido, monitoreo)
Volumen	⭐⭐⭐⭐⭐ (3 envios mas potenciales)

Ejemplo 9: Terrestre - Cross-Border Retail Mexico-EE.UU.
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 6.500
Precio objetivo	USD 5.000
Paciencia	4 rondas
Sensibilidad al precio	Alta
Confianza en tu empresa	Baja (primera vez, cliente referido)
Urgencia	Media (reposicion de stock)
Personalidad	Joven, emprendedor, compra online, le gustan las apps y la tecnologia

Solicitud Visible
TrendyShop.mx (E-commerce de moda, Ciudad de Mexico)
Necesitamos enviar mercaderia a nuestro warehouse de fulfillment en EE.UU.
Origen: Almacen principal, Ciudad de Mexico
Destino: Laredo, TX (cross-docking para distribucion a Amazon FBA)

Carga: Ropa y accesorios de moda
Volumen: 15 pallets
Peso: 4.200 kg
Valor: USD 48.000

Complicacion: necesito consolidar en 3 destinos diferentes dentro de Laredo:
- 5 pallets -> Amazon FBA Warehouse Laredo
- 5 pallets -> Warehouse propio (direccion X)
- 5 pallets -> Warehouse de cross-docking (direccion Y)

Necesito tracking en tiempo real y fotos al cruzar la frontera.

Negociacion
FF (Vos):
TrendyShop.mx, encantado de atenderlos.

CDMX-Laredo con consolidacion en 3 destinos. Les propongo USD 5.800.

Incluye:
- Transporte terrestre CDMX-Nuevo Laredo (trailer cerrado 48')
- Cruce fronterizo con despacho aduanero de exportacion MX + importacion US
- Split delivery en 3 destinos dentro de Laredo (camion local con liftgate)
- Tracking GPS en tiempo real (app web, actualizacion cada 5 min)
- Fotos documentales al cruzar la frontera (carga sellada, numero de precinto)
- Seguro de transporte por USD 48.000
- DTD (Door-to-Door) en todos los destinos

Transit time: 4-5 dias.
Cliente:
USD 5.800...

Mire, les voy a ser honesto. Soy un e-commerce joven. Mis margenes son del 15-20%. Cada peso que gasto en logistica es un peso que no invierto en marketing.

Ya me cotizaron USD 5.200 con otra transportista. Pero no me dan tracking en tiempo real ni fotos en la frontera. Ustedes si.

Necesito que se acerquen a USD 5.200. Si me dan el mismo precio con su servicio premium, elijo ustedes sin dudar.
FF (Vos):
Entiendo perfectamente su situacion. Y respeto la transparencia.

USD 5.200 con nuestro servicio premium no es posible. Les explico por que:

El split delivery en 3 destinos dentro de Laredo tiene un costo fijo de USD 380 (3 stops + liftgate en cada uno). Eso solo ya es un diferencial.

Pero le voy a hacer una propuesta diferente, pensada para un e-commerce en crecimiento:

OPCION STARTUP: USD 5.400

Es exactamente el mismo servicio, con una sola diferencia: en lugar de 3 deliveries separados en Laredo, consolidamos en UN warehouse de cross-docking y ustedes se encargan del ultimo tramo (Amazon FBA + warehouse propio desde ahi).

El tracking en tiempo real, las fotos en frontera, el seguro, y todo lo demas se mantiene igual. Ahorramos USD 400 en el split delivery y se lo transferimos a ustedes.
Cliente:
USD 5.400 con consolidacion en un solo punto de Laredo...

Eso me funciona. De hecho, nuestro warehouse de cross-docking ya maneja distribuciones internas. Puedo coordinar desde ahi los otros 2 destinos con un servicio local.

Pero necesito que me confirmen: el warehouse de cross-docking que usan, tiene disponibilidad de recepcion los sabados? Nuestro sistema de inventario se actualiza los fines de semana y necesito que la mercaderia entre en nuestro ERP antes del lunes.
FF (Vos):
Confirmado: el warehouse tiene recepcion los sabados hasta las 14:00hs. Y ademas les ofrezco algo que la otra transportista no les da:

Integracion con su ERP. Tenemos una API que se conecta con Shopify, WooCommerce, y Amazon Seller Central. Cuando la carga cruza la frontera, se actualiza automaticamente su stock en todas las plataformas. Cero trabajo manual.

Y como bonus para un e-commerce en crecimiento: les regalo el primer mes de nuestra plataforma de tracking avanzado (valor USD 49/mes) que incluye: notificaciones automaticas a sus clientes finales, estimacion de entrega, y alertas de retraso.

USD 5.400, todo incluido, tracking en tiempo real, fotos en frontera, recepcion sabados, integracion ERP, y primer mes de plataforma premium gratis.
Cliente:
Eso es LO QUE NECESITO.

La integracion con el ERP me ahorra 3-4 horas semanales de trabajo manual. Y la plataforma de tracking para mis clientes finales es oro puro para el servicio al cliente.

Confirmo a USD 5.400. Opcion Startup.

Les envio hoy:
- Direccion exacta de pickup en CDMX
- Direccion del warehouse de cross-docking en Laredo
- Acceso a nuestra API para la integracion

Necesito de ustedes:
- Booking para el proximo miercoles
- Guia de implementacion de la API

Y les anticipo: si funciona bien, pasamos a 2 envios por semana a partir del mes que viene.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Trailer CDMX-Nuevo Laredo	2.200	-
Despacho aduanero MX + US	480	-
Permiso de cruce fronterizo	120	-
Delivery local Laredo (1 stop)	180	-
Seguro de transporte (0.25%)	120	-
Tracking GPS + plataforma	85	-
Peajes y combustible	520	-
TOTAL COSTOS	3.705	-
TOTAL COBRADO AL CLIENTE	-	5.400
MARGEN NETO	-	1.695 (31%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐⭐⭐☆ (competitivo, opcion startup inteligente)
Servicio Digital	⭐⭐⭐⭐⭐ (API, tracking, integracion ERP)
Gestion de Riesgo	⭐⭐⭐⭐☆ (seguro, fotos en frontera)
Escalabilidad	⭐⭐⭐⭐⭐ (2 envios/semana potenciales)
Margen	⭐⭐⭐⭐⭐ (31% excelente)

Ejemplo 10: Maritimo Project Cargo - Turbina Eolica China-Brasil
Variables Ocultas del Cliente
Variable	Valor
Presupuesto maximo	USD 85.000
Precio objetivo	USD 65.000
Paciencia	5 rondas (proyecto grande, evaluan varios proveedores)
Sensibilidad al precio	Media
Confianza en tu empresa	Media (referidos por un cliente anterior)
Urgencia	Media (proyecto con fecha fija, parque eolico)
Personalidad	Ingeniero, meticuloso, pregunta detalles tecnicos, necesita certificaciones

Solicitud Visible
Energia Limpia do Nordeste S.A. (Recife, Brasil)
Importacion de aerogenerador para Parque Eolico.
Origen: Shanghai, China
Destino: Porto de Suape, Pernambuco, Brasil

Componentes:
- 1 torre de turbina eolica: 35m largo, 4.2m diametro, 45 toneladas
- 3 aspas: 55m largo cada una, 2.8m ancho, 8 toneladas c/u
- 1 nacelle: 12m x 4m x 4m, 28 toneladas

Peso total: 97 toneladas
Volumen: 850 CBM
Valor: USD 2.400.000

Requisitos criticos:
- Transporte en buque breakbulk o heavy lift (no contenedor)
- Certificacion DNV GL para amarre y estiba
- Ingeniero de cargas presente durante embarque y desembarque
- Seguro project cargo por valor total + 20% (cubre demoras y costos indirectos)
- Ruta sin obstaculos de altura (Golden Gate Bridge, Panama, etc.)

Fecha de entrega en Suape: maximo 55 dias desde embarque.

Negociacion
FF (Vos):
Energia Limpia, es un honor participar en un proyecto de esta envergadura.

Project cargo Shanghai-Suape. Les propongo USD 72.000.

El servicio incluye:
- Buque breakbulk con grua heavy lift onboard (capacidad 150 toneladas)
- Ingeniero de cargas certificado DNV GL durante todo el proceso
- Diseno de plan de amarre y estiba personalizado (con certificado DNV GL)
- Embarque en Puerto de Shanghai con supervision 24/7
- Seguro project cargo por USD 2.880.000 (valor + 20%, cobertura ARI)
- Ruta via Estrecho de Malaca -> Oceano Indico -> Cabo de Buena Esperanza -> Atlantico (evitamos Panama por restricciones de altura de las aspas)
- Desembarque en Suape con grua portuaria de 300 toneladas coordinada
- Transporte terrestre final hasta el sitio del parque eolico (coordino)

Transit time: 45-50 dias (ruta por Cabo es 5-7 dias mas larga pero segura para las dimensiones).
Cliente:
USD 72.000...

La propuesta es solida tecnicamente. La ruta por Cabo de Buena Esperanza es inteligente, lo habiamos evaluado internamente.

Sin embargo, tengo dos observaciones:

1. El seguro: su cobertura ARI (All Risks Inland) incluye demoras por clima? Nuestro proyecto tiene penalidades de USD 5.000 por dia de retraso despues del dia 55. Si el buque se retrasa por tormenta en el Atlantico Sur, el seguro cubre esas penalidades?

2. El ingeniero de cargas: es de su plantilla o tercerizado? Necesitamos alguien con experiencia especifica en aerogeneradores, no solo en cargas pesadas genericas.
FF (Vos):
Excelentes preguntas. Y le voy a ser completamente honesto.

1. Seguro: la cobertura ARI cubre dano fisico a la carga y costos de salvamento. Pero NO cubre penalidades contractuales por retraso. Eso es una cobertura separada llamada DSU (Delay in Start-Up).

Le propongo agregar DSU por USD 2.800 adicionales. Cubre hasta USD 150.000 en penalidades por retraso (equivale a 30 dias a USD 5.000/dia). Es lo que usamos en proyectos mineros y de infraestructura.

2. Ingeniero: tenemos ambas opciones. Nuestro ingeniero senior tiene 12 anos de experiencia y ha supervisado 8 proyectos eolicos (Vestas, Gamesa, y Goldwind). Pero si el cliente lo prefiere, podemos contratar un inspector independiente de Bureau Veritas por cuenta nuestra.

Sin costo adicional para ustedes.

Total con DSU: USD 74.800.
Cliente:
USD 74.800 con DSU y Bureau Veritas incluido...

La cobertura DSU es inteligente. USD 150.000 de cobertura por USD 2.800 es un ratio excelente.

Pero necesito que mejoren el precio base. Tenemos un presupuesto aprobado de USD 68.000 para transporte, y la cobertura DSU no estaba contemplada originalmente. Si sumo USD 2.800, necesito que el base baje a USD 65.000 para mantenerme dentro del presupuesto + contingencia.

USD 65.000 base + USD 2.800 DSU = USD 67.800. Eso si lo puedo aprobar.
FF (Vos):
USD 65.000 base + USD 2.800 DSU = USD 67.800...

Le voy a ser directo: USD 65.000 base deja un margen del 8%. Es bajo para un project cargo.

Pero este proyecto nos interesa por tres razones:
1. La exposicion: un parque eolico exitoso genera referencias enormes
2. El volumen futuro: si este funciona, ustedes tienen 12 aerogeneradores mas en pipeline
3. La relacion: Energia Limpia tiene proyectos en toda la region

Acepto USD 67.800 totales con una condicion: carta de intencion firmada para los proximos 3 aerogeneradores del parque, con precio pre-acordado de USD 62.000 base cada uno (escala economica por volumen).

Eso nos da volumen garantizado y a ustedes precio competitivo a futuro.
Cliente:
USD 67.800 con carta de intencion para los proximos 3 a USD 62.000...

Esa es una negociacion inteligente. El precio escalonado por volumen es justo.

Acepto. Pero la carta de intencion es NO vinculante. Es una carta de intencion, no una orden de compra firme. El precio de USD 62.000 se congela por 12 meses desde la entrega exitosa de esta primera turbina.

Confirmo USD 67.800 totales.

Necesito que me envien:
- Plan de amarre preliminar en 72 horas
- Ficha tecnica del buque propuesto
- CV del ingeniero de cargas
- Poliza project cargo + DSU para aprobacion de nuestro banco

Y coordinemos una call tecnica la semana que viene con nuestro ingeniero de obras.
FF (Vos):
Perfecto. USD 67.800 totales.

Resumen: breakbulk Shanghai-Suape, grua 150T, ingeniero DNV GL + Bureau Veritas, seguro project cargo USD 2.880.000, DSU por USD 150.000, ruta por Cabo, transit 45-50 dias.

Carta de intencion no vinculante para 3 unidades adicionales a USD 62.000 base, precio congelado 12 meses.

Les envio todo en 72 horas. La call tecnica la coordinamos por email.

Vamos a cuidar esta turbina como si fuera nuestra. Es el futuro de la energia.
Desglose de Costos del FF
Concepto	Costo (USD)	Cobro (USD)
Flete breakbulk Shanghai-Suape	38.000	-
Grua heavy lift embarque/desembarque	4.500	-
Ingeniero cargas DNV GL (15 dias)	3.200	-
Plan de amarre certificado	1.800	-
Seguro project cargo (USD 2.88M)	5.760	-
DSU (Delay in Start-Up)	1.400	-
Bureau Veritas inspector	2.100	-
Handling Shanghai + Suape	2.800	-
Permisos especiales (OOG)	1.200	-
TOTAL COSTOS	60.760	-
TOTAL COBRADO AL CLIENTE	-	67.800
MARGEN NETO	-	7.040 (10%)

Resumen de la Negociacion
Factor	Resultado
Precio	⭐⭐☆☆☆ (margen ajustado, inversion estrategica)
Servicio Tecnico	⭐⭐⭐⭐⭐ (DNV GL, Bureau Veritas, plan de amarre)
Gestion de Riesgo	⭐⭐⭐⭐⭐ (project cargo + DSU, cobertura total)
Volumen Futuro	⭐⭐⭐⭐⭐ (3 unidades + potencial de 12)
Relacion Estrategica	⭐⭐⭐⭐⭐ (parque eolico, exposicion regional)


FREIGHT FORWARDER
Simulador de Gestion y Negociacion Logistica
10 Ejemplos de Negociaciones  |  Maritimo - Aereo - Terrestre
Junio 2026

# FF — El Escritorio (v1 de testeo)

Prototipo para **probar si la negociación de Freight Forwarder es divertida**, sin el mapa 3D.
Es un solo archivo: `index.html`. No necesita instalar nada.

## Cómo jugar (en la compu)
Doble-click en **`index.html`** → se abre en el navegador. Listo.

## Cómo se juega
- Llega un cliente con una carga. Vos ves **tu costo de flete**.
- Movés 3 palancas: **Precio** (nunca por debajo del 10% de margen), **Plazo de pago**, **Transporte**.
- Podés usar 3 **tácticas**: presión de tiempo, argumento de valor, pago anticipado.
- La barra **"Interés"** te dice qué tan cerca estás de que acepte.
- Enviás cotización → el cliente acepta, contraoferta o se va.
- **Cuidá la caja:** le pagás al proveedor ya, pero al cliente le cobrás según el plazo. Todos los
  meses pagás gastos fijos. Si la caja se va a 0 → **bancarrota**.
- Hay 3 tipos de cliente ocultos (Rata, Apurado, Confiable). Vas viendo **pistas** mientras negociás.

> El truco: contra un **Rata** no le ganás bajando el precio (el piso 10% no te deja). Tenés que
> cerrarlo con plazo de pago, transporte y tácticas. **Ahí está la gracia.**

## Cómo compartirlo con tus amigos
Cualquiera de estas:
1. **Hosting gratis (lo más fácil para celular):** subí `index.html` a [Netlify Drop](https://app.netlify.com/drop)
   o GitHub Pages → te da un link que abrís en cualquier teléfono.
2. **Misma red WiFi:** en la PC corré `python -m http.server 8000` dentro de esta carpeta y desde el
   celular entrá a `http://IP-DE-TU-PC:8000`.
3. **Mandar el archivo:** pasale `index.html` por mail/WhatsApp; lo abren en el navegador del celular.

## Qué mirar cuando lo prueben (esto es lo importante)
- ¿Piden **"una más"** o se aburren?
- ¿Entienden **qué decisión** es interesante, o solo aprietan "Enviar"?
- ¿Se siente **distinto** negociar con un Rata vs. un Apurado?
- ¿La presión de **caja** se siente (cobrar tarde duele)?

Anotá dónde se prenden y dónde se aburren. Con eso ajustamos los números.


<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
<title>Freight Forwarder — El Escritorio (v1 testeo)</title>
<style>
  :root{--bg:#0b1220;--panel:#121c2e;--panel2:#1a2740;--line:#27375a;--txt:#e7eefb;--muted:#8aa0c4;--accent:#ff8a3d;--good:#37d67a;--bad:#ff5d5d;--warn:#ffcf4d;--info:#5db4ff}
  *{box-sizing:border-box;-webkit-tap-highlight-color:transparent}
  body{margin:0;background:var(--bg);color:var(--txt);font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;font-size:15px;line-height:1.4}
  .wrap{max-width:520px;margin:0 auto;padding:10px 12px 40px}
  h1{font-size:17px;margin:6px 0 2px}.sub{color:var(--muted);font-size:12px;margin-bottom:10px}
  .hud{display:flex;gap:8px;margin-bottom:10px}
  .hud .cell{flex:1;background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:8px;text-align:center}
  .hud .lbl{font-size:10px;color:var(--muted);text-transform:uppercase;letter-spacing:.5px}.hud .val{font-size:17px;font-weight:700;margin-top:2px}
  .rent{font-size:11px;text-align:center;color:var(--muted);margin-bottom:10px}
  .card{background:var(--panel);border:1px solid var(--line);border-radius:14px;padding:14px;margin-bottom:12px}
  .client-head{display:flex;align-items:center;gap:10px;margin-bottom:8px}
  .avatar{font-size:34px;width:48px;height:48px;display:flex;align-items:center;justify-content:center;background:var(--panel2);border-radius:12px}
  .client-name{font-weight:700;font-size:16px}.client-tag{font-size:11px;color:var(--muted)}
  .flavor{font-size:12px;border-radius:8px;padding:6px 8px;margin:6px 0}
  .flavor.barata{background:#10301f;color:var(--good)}.flavor.cara{background:#3a1c1c;color:var(--bad)}
  .flavor.volumen{background:#1c2c44;color:var(--info)}
  .req{background:var(--panel2);border-radius:10px;padding:10px;font-size:13px;margin:8px 0}.req b{color:var(--accent)}
  .msg{background:#0e1830;border-left:3px solid var(--accent);border-radius:8px;padding:10px;margin:8px 0;font-style:italic;min-height:42px}
  .bars{display:flex;gap:8px;flex-wrap:wrap;margin:8px 0}.bar-box{flex:1 1 45%}
  .bar-lbl{font-size:10px;color:var(--muted);display:flex;justify-content:space-between;margin-bottom:3px}
  .bar{height:8px;background:#0e1830;border-radius:6px;overflow:hidden}.bar>i{display:block;height:100%;border-radius:6px;transition:width .25s}
  .hints{font-size:12px;color:var(--warn);min-height:16px;margin:4px 0}.hint::before{content:"💡 "}
  .alert{font-size:12px;color:var(--bad);margin:4px 0;font-weight:600}
  .lever{margin:12px 0}.lever .top{display:flex;justify-content:space-between;font-size:13px;margin-bottom:6px}.lever .top b{font-size:14px}
  input[type=range]{width:100%;accent-color:var(--accent)}
  .seg{display:flex;gap:6px;flex-wrap:wrap}
  .seg button{flex:1;min-width:54px;background:var(--panel2);color:var(--txt);border:1px solid var(--line);border-radius:8px;padding:8px 4px;font-size:12px;cursor:pointer}
  .seg button.on{background:var(--accent);color:#1a1206;border-color:var(--accent);font-weight:700}
  .margin-tag{font-size:12px}.margin-ok{color:var(--good)}.margin-floor{color:var(--warn)}
  .tactics{display:flex;gap:6px;margin:10px 0 4px}
  .tactics button{flex:1;background:#152138;border:1px solid var(--line);color:var(--txt);border-radius:8px;padding:8px 4px;font-size:11px;cursor:pointer;line-height:1.2}
  .tactics button:disabled{opacity:.4;cursor:default}
  .actions{display:flex;gap:8px;margin-top:12px}
  .actions button{flex:1;border:none;border-radius:10px;padding:13px 8px;font-size:14px;font-weight:700;cursor:pointer}
  .btn-send{background:var(--accent);color:#1a1206}.btn-accept{background:var(--good);color:#06210f}.btn-walk{background:#2a3550;color:var(--txt);flex:.7}
  .log{font-size:11px;color:var(--muted);margin-top:14px;max-height:130px;overflow:auto}.log div{padding:2px 0;border-bottom:1px solid #182542}
  .center{text-align:center}.big{font-size:22px;font-weight:800;margin:6px 0}.gameover .big{color:var(--bad)}.tip{color:var(--muted);font-size:12px;margin-top:8px}
</style>
</head>
<body>
<div class="wrap">
  <h1>📦 Freight Forwarder — <span style="color:var(--accent)">El Escritorio</span></h1>
  <div class="sub">No hay jugada que siempre gane. Leé al cliente, la carga, el mercado y tu caja. Decidí.</div>
  <div class="hud">
    <div class="cell"><div class="lbl">Caja</div><div class="val" id="hud-cash">$0</div></div>
    <div class="cell"><div class="lbl">Reputación</div><div class="val" id="hud-rep">50</div></div>
    <div class="cell"><div class="lbl">Día</div><div class="val" id="hud-day">1</div></div>
    <div class="cell"><div class="lbl">Ops</div><div class="val" id="hud-ops">0/4</div></div>
  </div>
  <div class="rent" id="rent"></div>
  <div id="game"></div>
  <div class="log" id="log"></div>
</div>

<script>
/* ════════════════════════════════════════════════════════════════════════
   FREIGHT FORWARDER — "El Escritorio"  ·  v1.2
   Objetivo de diseño: que GANAR o PERDER dependa de la ESTRATEGIA comercial,
   no de apretar siempre los mismos botones. Para eso:
     · No todo trato es ganable (el negocio del cliente puede no cerrar).
     · Variedad de situaciones (tarifa barata, temporada cara, volumen).
     · La caja te empuja: el mejor movimiento cambia según tu estado.
     · Ningún botón es gratis (anticipo cuesta margen; tácticas mal usadas dañan).
     · Riesgo después de cerrar (operación puede salir mal / cliente no paga).
   ════════════════════════════════════════════════════════════════════════ */
const $=id=>document.getElementById(id);
const clamp01=x=>Math.max(0,Math.min(1,x));
const rnd=(a,b)=>a+Math.random()*(b-a);
const pick=a=>a[Math.floor(Math.random()*a.length)];
const money=n=>"$"+Math.round(n).toLocaleString("es-AR");

const PERSONAS={
  rata:     {key:"rata",nombre:"El Rata",emoji:"😒",priceSens:90,urgency:20,serviceVal:10,maxRounds:3,concession:0.06,angerThreshold:6,targetMult:1.05,prefTerm:60,hints:["Solo le importa el número.","De los que lloran el precio."]},
  apurado:  {key:"apurado",nombre:"El Apurado",emoji:"⏰",priceSens:30,urgency:90,serviceVal:60,maxRounds:2,concession:0.15,angerThreshold:5,targetMult:1.30,prefTerm:0,hints:["Tiene mucha urgencia.","Cada día de demora le cuesta plata."]},
  confiable:{key:"confiable",nombre:"El Confiable",emoji:"🤝",priceSens:40,urgency:30,serviceVal:75,maxRounds:3,concession:0.10,angerThreshold:6,targetMult:1.18,prefTerm:30,hints:["Parece un cliente serio.","Valora el buen servicio."]},
};
const CARGOS=[
  {co:"Bodega Los Andes",desc:"3x40'HC reefer · Vinos",ruta:"Mendoza → Miami",modo:"maritimo",costo:5200,urgente:false},
  {co:"Minera Cerro Grande",desc:"850kg · Repuestos críticos",ruta:"Santiago → Lima",modo:"aereo",costo:4500,urgente:true},
  {co:"AgroServicios",desc:"Cosechadora 12.5t",ruta:"Córdoba → Río Cuarto",modo:"terrestre",costo:2835,urgente:false},
  {co:"Estudio Varela",desc:"18 CBM · Muebles",ruta:"Milano → Buenos Aires",modo:"maritimo",costo:5800,urgente:false},
  {co:"Flores del Valle",desc:"1.800kg · Rosas (frío)",ruta:"Medellín → Miami",modo:"aereo",costo:2900,urgente:true},
  {co:"TechSur S.A.",desc:"2x40'HC · Electrónica",ruta:"Shanghái → Buenos Aires",modo:"maritimo",costo:5600,urgente:false},
  {co:"Pesquera Austral",desc:"900kg · Langostinos",ruta:"Trelew → Madrid",modo:"aereo",costo:3400,urgente:true},
  {co:"Importadora Norte",desc:"1x40' · Repuestos auto",ruta:"Busan → Valparaíso",modo:"maritimo",costo:2600,urgente:false},
];
const FRASES={
  caro:["Tu precio está muy arriba.","Tengo ofertas más competitivas.","¿Es tu mejor precio?"],
  compara:["Otra empresa me pasó más barato.","Estoy evaluando opciones.","Convenceme."],
  apura:["Necesito respuesta hoy.","La carga sale esta semana.","Cada día me cuesta plata."],
  cerca:["Estamos cerca.","Sólo una pequeña mejora.","Si resolvés esto, avanzamos."],
};
const LOSE={
  competidor:"“Me fui con otro forwarder, me hizo mejor número.”",
  negocio:"“A ese precio no me cierra el negocio. No hago la operación.”",
  paciencia:"“Me cansé de dar vueltas. Lo dejo acá.”",
  enojo:"“Así no se puede negociar. Chau.”",
};

let G;
function newGame(){
  G={cash:6000,rep:50,day:1,deals:0,lost:0,problems:0,nextMonthly:30,monthly:1500,ops:[],capacity:4,over:false,st:null,tierMsg:50};
  log("Arrancás solo con "+money(G.cash)+". Gastos del mes: "+money(G.monthly)+".");
  newClient(); render();
}

// Genera cliente + carga + ESTADO DE MERCADO (esto da la variedad estratégica).
function newClient(){
  const persona=pick(Object.values(PERSONAS));
  const base=pick(CARGOS);
  const scale=1+(G.rep-50)/120;
  let costo=Math.round(base.costo*scale*rnd(0.9,1.15));

  // Estado de mercado: tarifa del proveedor (cambia la forma del trato)
  let market="normal", flavor="";
  const r=Math.random();
  let isVolume=false;
  if(r<0.18){ market="barata"; costo=Math.round(costo*0.78); flavor="🟢 Conseguiste una tarifa baja del proveedor: podés ser barato Y ganar bien."; }
  else if(r<0.40){ market="cara"; costo=Math.round(costo*1.28); flavor="🔴 Temporada alta: el flete está caro, va a costar cerrar."; }
  else if(r<0.55){ isVolume=true; costo=Math.round(costo*2.4); flavor="🔵 Contrato de VOLUMEN: mucha plata, pero el cliente quiere poco margen y te ocupa capacidad."; }

  // Techo comercial del cliente: "a más de esto, mi negocio no cierra".
  // A veces queda por DEBAJO del piso 10% → trato IMPOSIBLE (hay que pasar).
  let viableMult;
  if(isVolume) viableMult=rnd(1.10,1.18);
  else if(market==="cara") viableMult=rnd(1.06,1.22);
  else viableMult=rnd(1.12,1.45);
  if(Math.random()<0.15) viableMult=rnd(1.02,1.11);   // a veces simplemente no le cierra
  const clientMaxViable=Math.round(costo*viableMult);
  const unwinnable=clientMaxViable < Math.round(costo*1.10);

  G.st={
    persona, cargo:{...base,costo,costoBase:costo,clientMaxViable,unwinnable,market,isVolume},
    flavor, price:Math.round(costo*1.18), term:persona.prefTerm===0?0:30, transport:base.modo,
    round:0, anger:0, satisfaction:Math.max(15,Math.min(85,40+Math.round((G.rep-50)*0.4))),
    revealed:[], tactics:{tiempo:false,valor:false,anticipo:false}, tacticBonus:0,
    clientMsg:"“"+base.co+": necesitamos mover esta carga. ¿Qué nos ofrecen?”", counter:null, finished:false,
  };
}

// Aceptación. El techo comercial es una PARED: arriba de eso, el negocio no cierra.
function acceptance(){
  const st=G.st,p=st.persona,costo=st.cargo.costo,target=costo*p.targetMult;
  if(st.price>st.cargo.clientMaxViable) return 0.01;     // no le cierra el negocio
  let acc=0.25;
  if(st.price<=target) acc+=0.30*(1-st.price/target);
  else acc-=(p.priceSens/100)*Math.min((st.price-target)/target,0.5)*1.2;
  acc+=(st.term/60)*0.10;
  acc-=Math.abs(st.term-p.prefTerm)/60*0.12;
  if(st.transport==="aereo") acc+=p.urgency>60?0.16:-0.08;
  acc+=(st.satisfaction/100)*0.15;
  acc-=st.anger*0.04;
  acc+=st.tacticBonus;
  return clamp01(acc);
}
function estimateRisk(st){
  const m=(st.price-st.cargo.costo)/st.cargo.costo;
  let ops=0.08;
  if(m<0.15) ops+=0.18; else if(m<0.22) ops+=0.08;
  ops+=st.persona.key==="rata"?0.12:st.persona.key==="apurado"?0.05:-0.04;
  if(st.cargo.urgente) ops+=0.10;
  ops+=(1-st.satisfaction/100)*0.10;
  ops=clamp01(Math.max(0.02,ops));
  let pay=st.term>0?(st.persona.key==="rata"?0.13:0.04)*(0.5+st.term/60):0;
  return {ops,pay:clamp01(pay),total:clamp01(ops+clamp01(pay))};
}

function sendQuote(){
  const st=G.st; if(st.finished) return;
  st.round++;
  // Pared comercial: el negocio del cliente no cierra → no contraoferta, hay que bajar o pasar.
  if(st.price>st.cargo.clientMaxViable){
    st.counter=null; st.clientMsg=LOSE.negocio;
    revealHint();
    if(st.round>=st.persona.maxRounds) return loseDeal(LOSE.paciencia);
    return render();
  }
  const acc=acceptance();
  if(Math.random()<acc) return closeDeal();
  if(st.round>=st.persona.maxRounds){
    if(Math.random()<acc*0.6) return closeDeal();
    return loseDeal(LOSE.competidor);
  }
  const target=st.cargo.costo*st.persona.targetMult;
  st.counter=Math.round(Math.max(st.cargo.costo*1.10, target, st.price*(1-st.persona.concession)));
  if(st.counter>st.cargo.clientMaxViable) st.counter=null;   // ni su contraoferta le cierra
  let pool=acc>0.45?FRASES.cerca:st.persona.urgency>60?FRASES.apura:FRASES.compara;
  st.clientMsg="“"+pick(pool)+(st.counter?" Te lo tomo a "+money(st.counter)+".":"")+"”";
  revealHint();
  if(st.anger>=st.persona.angerThreshold) return loseDeal(LOSE.enojo);
  render();
}
function acceptCounter(){const st=G.st; if(!st.counter||st.finished) return; st.price=st.counter; closeDeal();}
function useTactic(t){
  const st=G.st,p=st.persona; if(st.finished||st.tactics[t]) return; st.tactics[t]=true;
  if(t==="tiempo"){ if(p.urgency>60){st.tacticBonus+=0.18;flash("Presión de tiempo: le picó la urgencia.");}
                    else {st.tacticBonus-=0.12;st.anger++;flash("Presión de tiempo: no tenía apuro, se MOLESTÓ.");} }
  if(t==="valor"){ if(p.serviceVal>50){st.tacticBonus+=0.15;st.satisfaction=Math.min(100,st.satisfaction+8);flash("Argumento de valor: lo valoró.");}
                   else {flash("Argumento de valor: a este cliente le importa el número, no sirvió.");} }
  if(t==="anticipo"){ const floor=st.cargo.costo*1.10; const nuevo=Math.max(floor,Math.round(st.price*0.92));
                      const bajo=st.price-nuevo; st.price=nuevo; st.term=0; st.tacticBonus+=0.10;
                      flash("Pago anticipado: das "+money(bajo)+" de descuento (menos margen) a cambio de cobrar ya, +interés y sin riesgo de no-pago."); }
  render();
}
function walkAway(){ if(!G.st.finished){ G.lost++; log("Pasaste a "+G.st.cargo.co+"."); nextOrEnd(); } }

function closeDeal(){
  const st=G.st; st.finished=true;
  const m=(st.price-st.cargo.costo)/st.cargo.costo; G.deals++;
  G.cash-=st.cargo.costo;
  const risk=estimateRisk(st);
  G.ops.push({co:st.cargo.co,price:st.price,costo:st.cargo.costo,resolveDay:G.day+Math.max(3,st.term)+Math.round(rnd(0,5)),risk});
  log("✅ Cerraste "+st.cargo.co+" a "+money(st.price)+" (margen "+Math.round(m*100)+"% · riesgo "+Math.round(risk.total*100)+"%).");
  flash("Trato cerrado. Ahora la operación tiene que SALIR BIEN para cobrar.");
  nextOrEnd();
}
function loseDeal(msg){const st=G.st; st.finished=true; G.lost++; G.rep=Math.max(0,G.rep-1); st.clientMsg=msg; log("❌ Perdiste a "+st.cargo.co+"."); render(); setTimeout(nextOrEnd,1100);}

function resolveOps(){
  const due=G.ops.filter(o=>G.day>=o.resolveDay); G.ops=G.ops.filter(o=>G.day<o.resolveDay);
  for(const o of due){
    if(Math.random()<o.risk.pay){ G.problems++; G.rep=Math.max(0,G.rep-4); log("🟥 "+o.co+" NO PAGÓ. Perdés "+money(o.costo)+"."); }
    else if(Math.random()<o.risk.ops){ const ex=Math.round((o.price-o.costo)*rnd(0.5,1.4)); G.cash+=o.price-ex; G.problems++; G.rep=Math.max(0,G.rep-2); log("🟧 Problema en "+o.co+": costo extra "+money(ex)+". Neto "+money(o.price-o.costo-ex)+"."); }
    else { G.cash+=o.price; G.rep=Math.min(100,G.rep+1); log("💰 "+o.co+" pagó "+money(o.price)+". OK."); }
  }
}
function tick(d){ G.day+=d; resolveOps();
  while(G.day>=G.nextMonthly){ G.cash-=G.monthly; G.monthly=Math.round(G.monthly*1.04); log("🧾 Gastos del mes: -"+money(G.monthly/1.04|0)); G.nextMonthly+=30; }
  // sensación de avance (no es victoria final, es crecer)
  if(G.rep>=G.tierMsg+10){ G.tierMsg+=10; log("⭐ Tu reputación creció: llegan clientes y cargas más grandes."); }
}
function nextOrEnd(){ tick(Math.round(rnd(2,5))); if(G.cash<0) return gameOver(); newClient(); render(); }
function advanceTime(){ tick(Math.round(rnd(3,7))); if(G.cash<0) return gameOver(); render(); }

function gameOver(){
  G.over=true;
  $("game").innerHTML=`<div class="card gameover center"><div class="big">🏴 BANCARROTA</div>
    <div>Te quedaste sin caja en el día ${G.day}.</div>
    <div class="tip">Cerraste ${G.deals} · ${G.problems} salieron mal · pasaste ${G.lost} · reputación ${G.rep}.</div>
    <div class="tip">No hay fórmula única: a veces hay que pasar un trato, a veces arriesgar para llegar a fin de mes.</div>
    <div class="actions"><button class="btn-send" onclick="newGame()">Jugar de nuevo</button></div></div>`;
  renderHud();
}

function render(){
  renderHud(); if(G.over) return;
  const st=G.st,p=st.persona,costo=st.cargo.costo;
  if(G.ops.length>=G.capacity){
    $("game").innerHTML=`<div class="card center"><div class="big" style="color:var(--warn)">Sin capacidad</div>
      <div>${G.ops.length}/${G.capacity} operaciones en curso. No podés tomar nuevas.</div>
      <div class="tip">${st.cargo.co} espera. Avanzá el tiempo (los gastos corren igual).</div>
      <div class="actions"><button class="btn-send" onclick="advanceTime()">⏩ Avanzar el tiempo</button></div></div>`; return;
  }
  const floor=Math.round(costo*1.10),cap=Math.round(costo*1.55);
  if(st.price<floor) st.price=floor; if(st.price>cap) st.price=cap;
  const marginPct=Math.round((st.price-costo)/costo*100);
  const acc=acceptance(),risk=estimateRisk(st);
  const mood=st.anger>=4?"😠":acc>0.6?"😀":acc>0.4?"🙂":"😐";
  const flavorCls=st.cargo.market==="barata"?"barata":st.cargo.market==="cara"?"cara":st.cargo.isVolume?"volumen":"";

  $("game").innerHTML=`<div class="card">
    <div class="client-head"><div class="avatar">${mood}</div>
      <div><div class="client-name">${st.cargo.co}</div>
        <div class="client-tag">Ronda ${Math.max(1,st.round)}/${p.maxRounds} · ${st.cargo.ruta} · Ops ${G.ops.length}/${G.capacity}</div></div></div>
    ${st.flavor?`<div class="flavor ${flavorCls}">${st.flavor}</div>`:""}
    <div class="req"><b>${st.cargo.desc}</b> · ${st.cargo.ruta}<br>Tu costo de flete: <b>${money(costo)}</b> · ${st.cargo.urgente?"⚡ URGENTE":"Sin apuro"}</div>
    <div class="msg">${st.clientMsg}</div>
    ${st.cargo.unwinnable?`<div class="alert">⚠ A este cliente no le cierra el negocio ni a tu precio mínimo. Conviene PASAR.</div>`:""}
    <div class="bars">
      ${bar("Satisfacción",st.satisfaction,100,"var(--good)")}
      ${bar("Enojo",st.anger,10,"var(--bad)")}
      ${bar("Interés (¿acepta?)",Math.round(acc*100),100,"var(--accent)")}
      ${bar("⚠ Riesgo de la op",Math.round(risk.total*100),100,"var(--bad)")}
    </div>
    <div class="hints">${st.revealed.map(h=>`<span class="hint">${h}</span>`).join("<br>")}</div>
    <div class="lever"><div class="top"><span>💲 Precio</span><b>${money(st.price)} <span class="${marginPct<=11?'margin-floor':'margin-ok'} margin-tag">(margen ${marginPct}% · ganancia ${money(st.price - costo)})</span></b></div>
      <input type="range" min="${floor}" max="${cap}" value="${st.price}" step="25" oninput="G.st.price=+this.value;render()">
      <div class="bar-lbl"><span>piso 10% (${money(floor)})</span><span>${money(cap)}</span></div></div>
    <div class="lever"><div class="top"><span>📅 Plazo de pago</span><span style="color:var(--muted)">cobrás en ${st.term} días</span></div>
      <div class="seg">${[0,15,30,45,60].map(d=>`<button class="${st.term===d?'on':''}" onclick="G.st.term=${d};render()">${d===0?'Contado':d+'d'}</button>`).join("")}</div></div>
    <div class="lever"><div class="top"><span>🚢 Transporte</span><span style="color:var(--muted)">aéreo = más caro y rápido</span></div>
      <div class="seg">${["maritimo","aereo","terrestre"].map(m=>`<button class="${st.transport===m?'on':''}" onclick="setTransport('${m}')">${m==='maritimo'?'🚢 Marítimo':m==='aereo'?'✈️ Aéreo':'🚚 Terrestre'}</button>`).join("")}</div></div>
    <div class="tactics">
      <button onclick="useTactic('tiempo')" ${st.tactics.tiempo?'disabled':''}>⏱️ Presión de tiempo</button>
      <button onclick="useTactic('valor')" ${st.tactics.valor?'disabled':''}>⭐ Argumento de valor</button>
      <button onclick="useTactic('anticipo')" ${st.tactics.anticipo?'disabled':''}>💵 Pago anticipado</button></div>
    <div class="actions">
      <button class="btn-send" onclick="sendQuote()">Enviar cotización</button>
      ${st.counter?`<button class="btn-accept" onclick="acceptCounter()">Aceptar ${money(st.counter)}</button>`:''}
      <button class="btn-walk" onclick="walkAway()">Pasar</button></div>
  </div>`;
}
function bar(l,v,mx,c){const pct=Math.round(v/mx*100);return `<div class="bar-box"><div class="bar-lbl"><span>${l}</span><span>${pct}%</span></div><div class="bar"><i style="width:${pct}%;background:${c}"></i></div></div>`;}
function renderHud(){$("hud-cash").textContent=money(G.cash);$("hud-cash").style.color=G.cash<G.monthly?"var(--bad)":"var(--txt)";
  $("hud-rep").textContent=G.rep;$("hud-day").textContent=G.day;$("hud-ops").textContent=G.ops.length+"/"+G.capacity;
  const falta=Math.max(0,G.nextMonthly-G.day); $("rent").textContent=`Próximos gastos: ${money(G.monthly)} en ${falta} días`;
  $("rent").style.color = (G.cash<G.monthly && falta<=6)?"var(--bad)":"var(--muted)";}
function setTransport(m){const st=G.st,b=st.cargo.costoBase;st.cargo.costo=m==="aereo"?Math.round(b*2.4):b;
  st.cargo.clientMaxViable=Math.round(st.cargo.clientMaxViable*(st.cargo.costo/(st.cargo._lastCosto||b)));st.cargo._lastCosto=st.cargo.costo;st.transport=m;render();}
function revealHint(){const h=G.st.persona.hints[G.st.revealed.length];if(h)G.st.revealed.push(h);}
function flash(t){log("▶ "+t);}
function log(t){const d=document.createElement("div");d.textContent=t;$("log").prepend(d);}
newGame();
</script>
</body>
</html>

