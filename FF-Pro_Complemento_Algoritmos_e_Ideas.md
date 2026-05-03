# FREIGHT FORWARDER PRO — DOCUMENTO COMPLEMENTARIO
# Algoritmos Faltantes de la Versión Godot + Ideas Frescas para Unity

> **Nota:** Este documento complementa el archivo `FF-Pro_Documentacion_Completa.md` que ya documenta los 31 archivos C# actuales. Aquí NO se repite nada de lo que ya está documentado. Se cubren:
>
> 1. **Algoritmos y sistemas de la versión Godot que NO están implementados en la versión Unity actual**
> 2. **Sistemas planificados en PROJECT.md que aún no existen en el código**
> 3. **Ideas frescas y nuevas para hacer el juego más inmersivo y atrapante**

---

## TABLA DE CONTENIDOS

1. [Sistemas Faltantes del Godot Original](#1-sistemas-faltantes-del-godot-original)
   - 1.1 [Sistema de Redes de Transporte (AirNetwork + SeaNetwork)](#11-sistema-de-redes-de-transporte-airnetwork--seanetwork)
   - 1.2 [Pathfinding A* sobre Mapa Real (MapData)](#12-pathfinding-a-sobre-mapa-real-mapdata)
   - 1.3 [Eventos Mundiales por Calendario](#13-eventos-mundiales-por-calendario)
   - 1.4 [Sistema de Noticias (NewsManager)](#14-sistema-de-noticias-newsmanager)
   - 1.5 [Sistema de Llamadas a Clientes](#15-sistema-de-llamadas-a-clientes)
   - 1.6 [Desbloqueo de Features por Nivel](#16-desbloqueo-de-features-por-nivel)
   - 1.7 [Sistema de Fallo Diario de Cargas](#17-sistema-de-fallo-diario-de-cargas)
   - 1.8 [Sistema de Pagos Pendientes (Payment Delay)](#18-sistema-de-pagos-pendientes-payment-delay)
   - 1.9 [Experiencia de Agentes por Ruta](#19-experiencia-de-agentes-por-ruta)
   - 1.10 [Degradación de Satisfacción de Clientes](#110-degradacion-de-satisfaccion-de-clientes)
   - 1.11 [Zonas Terrestres (Land Zones)](#111-zonas-terrestres-land-zones)
   - 1.12 [Hub Tiers para Aeropuertos y Puertos](#112-hub-tiers-para-aeropuertos-y-puertos)
   - 1.13 [Sistema de Tiers de Ciudades (Desbloqueo Progresivo)](#113-sistema-de-tiers-de-ciudades-desbloqueo-progresivo)
   - 1.14 [Noticias de Fondo (Background News)](#114-noticias-de-fondo-background-news)
   - 1.15 [Noticias Reales desde GNews API](#115-noticias-reales-desde-gnews-api)
   - 1.16 [Negociación en Rondas con Contraoferta del Jugador](#116-negociacion-en-rondas-con-contraoferta-del-jugador)
   - 1.17 [Efectos de Noticias con Modificadores Temporales](#117-efectos-de-noticias-con-modificadores-temporales)
   - 1.18 [Índice de Precio de Combustible Dinámico](#118-indice-de-precio-de-combustible-dinamico)
   - 1.19 [Panel de Clientes con Filtros Avanzados](#119-panel-de-clientes-con-filtros-avanzados)
   - 1.20 [Ticker de Noticias Animado](#120-ticker-de-noticias-animado)
2. [Sistemas Planificados (PROJECT.md) Aún No Implementados](#2-sistemas-planificados-projectmd-aun-no-implementados)
3. [Ideas Frescas para la Versión Unity Steam](#3-ideas-frescas-para-la-version-unity-steam)

---

# 1. SISTEMAS FALTANTES DEL GODOT ORIGINAL

Estos son algoritmos y mecánicas que **existían y funcionaban en la versión Godot (GDScript)** pero que **no están presentes en el código Unity C# actual** (los 31 archivos documentados anteriormente).

---

## 1.1 Sistema de Redes de Transporte (AirNetwork + SeaNetwork)

### Qué es
En Godot existían dos scripts independientes (`AirNetwork.gd` y `SeaNetwork.gd`) que modelaban las redes reales de transporte aéreo y marítimo con grafos de nodos interconectados. Estos scripts pre-calculaban las rutas óptimas entre todas las ciudades al inicio del juego.

### Algoritmo de AirNetwork (Godot)

```gdscript
# Constantes
const AIR_SPEED_KM_PER_DAY: float = 19200.0  # 800 km/h * 24h
const NORM_TO_KM: float = 20000.0

# 50 aeropuertos con coordenadas normalizadas (0-1), región, y si es hub
var airports := {
    "buenos_aires": {"pos": Vector2(0.338, 0.692), "region": "south_america", "is_hub": true},
    "shanghai":     {"pos": Vector2(0.837, 0.319), "region": "east_asia",     "is_hub": true},
    "dubai":        {"pos": Vector2(0.653, 0.361), "region": "middle_east",   "is_hub": true},
    # ... 50 aeropuertos en total
}
```

El sistema construía un **grafo ponderado** donde:
- Cada aeropuerto es un nodo
- Los hubs se conectan a TODOS los demás aeropuertos
- Los no-hubs solo se conectan a los hubs de su región + regiones vecinas
- El peso de cada arista es la distancia Haversine en kilómetros

```gdscript
# Lógica de conexión de hubs
func _build_hub_graph():
    var hub_list: Array = []
    var non_hub_list: Array = []
    for id in airports:
        if airports[id]["is_hub"]:
            hub_list.append(id)
        else:
            non_hub_list.append(id)
    
    # Hub a hub: conexión completa (mesh)
    for i in range(hub_list.size()):
        for j in range(i + 1, hub_list.size()):
            var dist = _haversine_km(airports[hub_list[i]]["pos"], airports[hub_list[j]]["pos"])
            _add_edge(hub_list[i], hub_list[j], dist)
    
    # No-hub a hubs de su región y regiones adyacentes
    for nh in non_hub_list:
        var region = airports[nh]["region"]
        var adj_regions = _get_adjacent_regions(region)
        for h in hub_list:
            if airports[h]["region"] == region or airports[h]["region"] in adj_regions:
                var dist = _haversine_km(airports[nh]["pos"], airports[h]["pos"])
                _add_edge(nh, h, dist)
```

### Algoritmo de búsqueda de ruta (Dijkstra)

```gdscript
func find_best_route(origin: String, destination: String) -> Dictionary:
    # Dijkstra sobre el grafo de aeropuertos
    var dist := {}
    var prev := {}
    var queue := []
    
    for id in airports:
        dist[id] = INF
        prev[id] = ""
    dist[origin] = 0
    queue.append(origin)
    
    while not queue.is_empty():
        queue.sort_custom(func(a, b): return dist[a] < dist[b])
        var current = queue.pop_front()
        
        if current == destination:
            break
        
        for neighbor in _graph[current]:
            var alt = dist[current] + _graph[current][neighbor]
            if alt < dist[neighbor]:
                dist[neighbor] = alt
                prev[neighbor] = current
                if neighbor not in queue:
                    queue.append(neighbor)
    
    # Reconstruir ruta
    var path := []
    var node = destination
    while node != "":
        path.push_front(node)
        node = prev[node]
    
    var total_km = dist[destination]
    var days = max(1, int(round(total_km / AIR_SPEED_KM_PER_DAY)))
    
    return {
        "route": path,        # ej: ["buenos_aires", "miami", "london"]
        "total_km": total_km,
        "days": days,
        "stops": path.slice(1, -1),  # escalas intermedias
        "leg_days": _calculate_leg_days(path)  # días por tramo
    }
```

### Algoritmo de SeaNetwork
Similar al aéreo pero con:
- `SEA_SPEED_KM_PER_DAY = 600.0` (20 nudos)
- Usaba `MapData.find_route()` para calcular rutas marítimas reales que bordeaban los continentes
- Pre-calculaba TODAS las distancias entre pares de puertos al inicio (cache en `_direct_days`)

```gdscript
func _precompute_all_distances():
    var port_list = ports.keys()
    for i in range(port_list.size()):
        for j in range(i + 1, port_list.size()):
            var a = port_list[i]
            var b = port_list[j]
            var dist = MapData.get_real_route_distance(a, b)  # Ruta marítima real con A*
            var days = max(1, int(round(dist / SEA_SPEED_KM_PER_DAY)))
            _direct_days[a + "_" + b] = days
            _direct_days[b + "_" + a] = days
```

### Qué falta en Unity
- **Todo.** La versión Unity calcula la distancia como una simple línea recta Haversine entre dos puntos. No hay grafos de rutas, no hay escalas intermedias, no hay diferencia entre rutas marítimas y aéreas. No hay cálculo de días por tramo.

### Cómo implementarlo en Unity C#
Crear `AirRouteNetwork.cs` y `SeaRouteNetwork.cs` en `Assets/_Game/Map/`:
- Usar un `Dictionary<string, Dictionary<string, float>>` para el grafo
- Implementar Dijkstra con `PriorityQueue<string, float>` (disponible en .NET 6+)
- Pre-calcular en `Awake()` o en una coroutine al iniciar
- Para rutas marítimas, usar datos geográficos reales o waypoints manuales

---

## 1.2 Pathfinding A* sobre Mapa Real (MapData)

### Qué es
En Godot existía un sistema completo de pathfinding A* sobre una grilla de navegación de 180×90 celdas que representaba la superficie de la Tierra. El sistema diferenciaba tierra de agua para que los barcos navegaran por el océano y los camiones por tierra.

### Algoritmo (Godot)

```gdscript
# map_data.gd
const NAV_W: int = 180
const NAV_H: int = 90
const POLAR_N: int = 7    # Excluir polos del grafo marítimo
const POLAR_S: int = 82

var nav_grid: Array = []   # nav_grid[y][x] = true si es tierra
var astar_water: WrappingAStar2D  # A* para rutas marítimas (envuelve en X)
var astar_land: WrappingAStar2D   # A* para rutas terrestres

func _build_nav_grid():
    # Cargar "land mask" de una textura real de la Tierra
    # Cada pixel de la textura indica si es tierra (verde/marrón) o agua (azul)
    var image = load("res://assets/map/land_mask.png").get_image()
    for y in range(NAV_H):
        nav_grid.append([])
        for x in range(NAV_W):
            var px = int(float(x) / NAV_W * image.get_width())
            var py = int(float(y) / NAV_H * image.get_height())
            var color = image.get_pixel(px, py)
            nav_grid[y].append(color.g > 0.3)  # Verde = tierra

func _build_astar_graphs():
    # Crear nodos A* para tierra y agua por separado
    for y in range(NAV_H):
        for x in range(NAV_W):
            var id = y * NAV_W + x
            var pos = Vector2(float(x), float(y))
            if nav_grid[y][x]:
                astar_land.add_point(id, pos)
            else:
                if y >= POLAR_N and y <= POLAR_S:
                    astar_water.add_point(id, pos)
    
    # Conectar nodos adyacentes (8 direcciones)
    # NOTA: WrappingAStar2D envuelve en X para manejar el dateline
    for y in range(NAV_H):
        for x in range(NAV_W):
            var id = y * NAV_W + x
            var is_land = nav_grid[y][x]
            for dx in [-1, 0, 1]:
                for dy in [-1, 0, 1]:
                    if dx == 0 and dy == 0:
                        continue
                    var nx = (x + dx) % NAV_W  # Wrap horizontal
                    var ny = y + dy
                    if ny < 0 or ny >= NAV_H:
                        continue
                    var nid = ny * NAV_W + nx
                    var astar = astar_land if is_land else astar_water
                    if astar.has_point(nid):
                        astar.connect_points(id, nid)

func find_route(origin: Vector2, dest: Vector2, want_water: bool) -> PackedVector2Array:
    var from_nav = _world_to_nav(origin)
    var to_nav = _world_to_nav(dest)
    var from_id = from_nav.y * NAV_W + from_nav.x
    var to_id = to_nav.y * NAV_W + to_nav.x
    var astar = astar_water if want_water else astar_land
    
    if not astar.has_point(from_id) or not astar.has_point(to_id):
        return PackedVector2Array([origin, dest])  # Fallback: línea recta
    
    var path = astar.get_point_path(from_id, to_id)
    if path.is_empty():
        return PackedVector2Array([origin, dest])
    
    # Convertir de grilla a coordenadas del mundo
    var result = PackedVector2Array()
    for point in path:
        result.append(_nav_to_world(point))
    return result
```

### Datos Geográficos (continent_data.json)
El Godot original incluía un archivo `continent_data.json` de **56,427 caracteres** con datos reales de contornos de continentes para la detección de tierra/agua.

### Qué falta en Unity
- **No hay pathfinding A*.** Las rutas son arcos simples entre dos puntos.
- **No hay distinción tierra/agua.** Un barco y un avión siguen la misma curva visual.
- **No hay datos geográficos reales.** No hay land mask ni grilla de navegación.

### Cómo implementarlo en Unity C#
- Crear `NavigationGrid.cs` en `Assets/_Game/Map/`
- Usar una textura de "land mask" de NASA (gratis, dominio público)
- Implementar A* con la clase nativa `AStar2D` o crear uno custom
- Para performance, correr el pre-cálculo en un hilo secundario con `Task.Run()`

---

## 1.3 Eventos Mundiales por Calendario

### Qué es
En Godot existía un sistema completo de **eventos mundiales programados por fecha real** que afectaban la economía del juego según la época del año. Estos eventos tienen fechas fijas de inicio y fin y aplican modificadores a precios, oferta, riesgo y tolerancia de clientes.

### Datos Completos del Godot Original

```gdscript
enum WorldEventCategory {
    LOGISTICS_PARALYSIS,   # Cierre masivo de operaciones
    HIGH_DEMAND,           # Alta demanda estacional
    OPERATIONAL_RISK       # Riesgo operacional por feriados
}

const WORLD_EVENTS: Array = [
    # ═══════════════ GLOBALES ═══════════════
    {
        "id": "christmas_newyear",
        "name": "Navidad y Ano Nuevo",
        "category": LOGISTICS_PARALYSIS,
        "description": "Cierre masivo de oficinas, puertos y aduanas.",
        "affected_regions": ["Europe", "North America", "South America", "Oceania"],
        "start_month": 12, "start_day": 22,
        "end_month": 1, "end_day": 5,
        "modifiers": {
            "price_multiplier": 1.15,
            "offer_rate": 0.4,
            "event_risk": 1.8,
            "client_tolerance": -0.1,
            "delay_risk": 0.35
        }
    },
    {
        "id": "european_summer",
        "name": "Temporada Alta Europa",
        "category": HIGH_DEMAND,
        "description": "Verano europeo. Vuelos saturados, poco espacio. Tarifas aereas altisimas.",
        "affected_regions": ["Europe"],
        "start_month": 7, "start_day": 1,
        "end_month": 8, "end_day": 31,
        "modifiers": {
            "price_multiplier": 1.5,
            "offer_rate": 0.7,
            "event_risk": 1.6,
            "client_tolerance": -0.25,
            "delay_risk": 0.3
        }
    },
    
    # ═══════════════ ASIA ═══════════════
    {
        "id": "chinese_new_year",
        "name": "Ano Nuevo Chino",
        "category": LOGISTICS_PARALYSIS,
        "description": "Fabricas y puertos chinos cerrados 2-4 semanas.",
        "affected_countries": ["China"],
        "affected_cities": ["shanghai", "hong_kong"],
        "start_month": 1, "start_day": 20,
        "end_month": 2, "end_day": 15,
        "modifiers": {
            "price_multiplier": 1.6,
            "offer_rate": 0.15,
            "event_risk": 2.0,
            "client_tolerance": -0.2,
            "delay_risk": 0.5
        }
    },
    {
        "id": "golden_week_china",
        "name": "Golden Week (China)",
        "category": HIGH_DEMAND,
        "affected_cities": ["shanghai", "hong_kong"],
        "start_month": 10, "start_day": 1,
        "end_month": 10, "end_day": 7,
        "modifiers": {
            "price_multiplier": 1.4,
            "offer_rate": 0.5,
            "event_risk": 1.5,
            "client_tolerance": -0.15,
            "delay_risk": 0.3
        }
    },
    {
        "id": "golden_week_japan",
        "name": "Golden Week (Japon)",
        "category": HIGH_DEMAND,
        "affected_cities": ["tokyo"],
        "start_month": 4, "start_day": 29,
        "end_month": 5, "end_day": 5,
        "modifiers": {
            "price_multiplier": 1.25,
            "offer_rate": 0.6,
            "event_risk": 1.3,
            "client_tolerance": -0.1,
            "delay_risk": 0.2
        }
    },
    
    # ═══════════════ NORTEAMERICA ═══════════════
    {
        "id": "thanksgiving",
        "name": "Thanksgiving (EEUU)",
        "category": HIGH_DEMAND,
        "affected_cities": ["miami", "new_york", "los_angeles"],
        "start_month": 11, "start_day": 24,
        "end_month": 11, "end_day": 30,
        "modifiers": {
            "price_multiplier": 1.3,
            "offer_rate": 0.6,
            "event_risk": 1.4,
            "client_tolerance": -0.1,
            "delay_risk": 0.25
        }
    },
    {
        "id": "july_4th",
        "name": "Dia de la Independencia (EEUU)",
        "category": OPERATIONAL_RISK,
        "affected_cities": ["miami", "new_york", "los_angeles"],
        "start_month": 7, "start_day": 3,
        "end_month": 7, "end_day": 5,
        "modifiers": {
            "price_multiplier": 1.1,
            "offer_rate": 0.7,
            "event_risk": 1.3,
            "client_tolerance": 0.0,
            "delay_risk": 0.15
        }
    },
    
    # ═══════════════ SUDAMERICA ═══════════════
    {
        "id": "arg_fiestas_patrias",
        "name": "Fiestas Patrias (Argentina)",
        "category": OPERATIONAL_RISK,
        "affected_cities": ["buenos_aires"],
        "start_month": 5, "start_day": 25,
        "end_month": 5, "end_day": 26,
        "modifiers": {
            "price_multiplier": 1.05,
            "offer_rate": 0.7,
            "event_risk": 1.3,
            "delay_risk": 0.15
        }
    },
    {
        "id": "brazil_independencia",
        "name": "Dia de la Independencia (Brasil)",
        "category": OPERATIONAL_RISK,
        "affected_cities": ["sao_paulo"],
        "start_month": 9, "start_day": 7,
        "end_month": 9, "end_day": 8,
        "modifiers": {
            "price_multiplier": 1.05,
            "offer_rate": 0.7,
            "event_risk": 1.3,
            "delay_risk": 0.15
        }
    },
    {
        "id": "chile_fiestas_patrias",
        "name": "Fiestas Patrias (Chile)",
        "category": OPERATIONAL_RISK,
        "affected_cities": ["valparaiso"],
        "start_month": 9, "start_day": 18,
        "end_month": 9, "end_day": 20,
        "modifiers": {
            "price_multiplier": 1.1,
            "offer_rate": 0.6,
            "event_risk": 1.4,
            "delay_risk": 0.2
        }
    },
    
    # ═══════════════ MEDIO ORIENTE ═══════════════
    {
        "id": "ramadan",
        "name": "Ramadan",
        "category": OPERATIONAL_RISK,
        "affected_cities": ["dubai", "jeddah"],
        "start_month": 3, "start_day": 10,
        "end_month": 4, "end_day": 10,
        "modifiers": {
            "price_multiplier": 1.15,
            "offer_rate": 0.5,
            "event_risk": 1.4,
            "client_tolerance": -0.1,
            "delay_risk": 0.25
        }
    },
    
    # ═══════════════ INDIA ═══════════════
    {
        "id": "diwali",
        "name": "Diwali (India)",
        "category": HIGH_DEMAND,
        "affected_cities": ["mumbai"],
        "start_month": 10, "start_day": 20,
        "end_month": 11, "end_day": 5,
        "modifiers": {
            "price_multiplier": 1.3,
            "offer_rate": 0.5,
            "event_risk": 1.5,
            "client_tolerance": -0.15,
            "delay_risk": 0.25
        }
    }
]
```

### Algoritmo de Activacion de Eventos Mundiales (Godot)

```gdscript
func check_world_events(day: int, month: int) -> void:
    var previous_events := active_world_events.duplicate()
    active_world_events.clear()
    
    for event in Constants.WORLD_EVENTS:
        if _is_event_active(event, day, month):
            active_world_events.append(event)
    
    # Detectar eventos que empezaron
    for event in active_world_events:
        var was_active := false
        for prev in previous_events:
            if prev["id"] == event["id"]:
                was_active = true
                break
        if not was_active:
            world_event_started.emit(event)
            news_flash.emit("🌍 " + event["name"], event["description"])
            _apply_world_event_delays(event)
    
    # Detectar eventos que terminaron
    for prev in previous_events:
        var still_active := false
        for event in active_world_events:
            if event["id"] == prev["id"]:
                still_active = true
                break
        if not still_active:
            world_event_ended.emit(prev)
            news_flash.emit("Evento finalizado: %s" % prev["name"], "{}")

func _is_event_active(event: Dictionary, day: int, month: int) -> bool:
    var sm = event["start_month"]
    var sd = event["start_day"]
    var em = event["end_month"]
    var ed = event["end_day"]
    
    # Manejar eventos que cruzan el ano (ej: Navidad dic22 -> ene5)
    if sm > em:
        return (month > sm or (month == sm and day >= sd) or
                month < em or (month == em and day <= ed))
    else:
        return ((month > sm or (month == sm and day >= sd)) and
                (month < em or (month == em and day <= ed)))

func _get_world_event_risk_for_cargo(cargo: Dictionary) -> float:
    var risk: float = 0.0
    for event in active_world_events:
        var affected_cities = event.get("affected_cities", [])
        if cargo["origin_city"] in affected_cities or cargo["destination_city"] in affected_cities:
            risk += event["modifiers"].get("delay_risk", 0.0)
    return risk
```

### Qué falta en Unity
- **El enum `WorldEventCategory` no existe.**
- **Los datos de los 12+ eventos mundiales no existen.** La version Unity tiene 20 event types para transito (CustomsDelay, Weather, etc) pero NO tiene eventos mundiales por calendario.
- **El algoritmo de deteccion de eventos activos por fecha no existe.**
- **Los modificadores globales (price_multiplier, offer_rate, event_risk, delay_risk) no se aplican.**

---

## 1.4 Sistema de Noticias (NewsManager)

### Qué es
Un manager dedicado a mostrar noticias en un ticker animado. Tres tipos de noticias:

#### Tipo 1: Noticias con efectos mecanicos
```gdscript
# Se generan con 20% de probabilidad diaria
# 5 categorias: LOGISTICS, ECONOMY, EVENT, CLIENT, WEATHER
# Cada una aplica efectos temporales

const NEWS_WITH_EFFECTS: Dictionary = {
    NewsCategory.LOGISTICS: [
        {
            "text": "Congestion record en puertos del sudeste asiatico",
            "effects": {
                "price_multiplier": 1.2,
                "delay_risk": 0.15,
                "duration": 5  # dias
            }
        },
        {
            "text": "Nueva terminal portuaria en Rotterdam duplica capacidad",
            "effects": {
                "price_multiplier": 0.9,
                "delay_risk": -0.05,
                "duration": 7
            }
        },
        # ... ~100 noticias con efectos
    ],
    NewsCategory.ECONOMY: [
        {
            "text": "El precio del petroleo sube un 12% esta semana",
            "effects": {
                "fuel_multiplier": 1.15,
                "price_multiplier": 1.08,
                "duration": 4
            }
        },
        # ... ~80 noticias
    ],
    # etc.
}
```

#### Tipo 2: Noticias de fondo (ambientacion, sin efectos)
```gdscript
const BACKGROUND_NEWS: Array = [
    {"text": "Un cafe con medialunas en Buenos Aires: la pausa perfecta"},
    {"text": "Record de turistas en Barcelona este verano"},
    {"text": "El metro de Tokio transporto 8.7 millones de personas hoy"},
    {"text": "Un gato se perdio en un almacen de Dubai y aparecio 3 dias despues"},
    {"text": "Noche de tango en La Boca: lleno total"},
    {"text": "El Big Ben suena a las 12 en punto. Como siempre."},
    # ... ~200 frases de ambientacion
]
```

#### Tipo 3: Noticias reales desde GNews API
```gdscript
# Cada 5 dias se consulta la API de GNews
func fetch_real_news():
    var url = "https://gnews.io/api/v4/search?" \
        + "q=economia%20OR%20logistica%20OR%20comercio%20OR%20transporte" \
        + "&lang=es&country=ar&max=10" \
        + "&apikey=" + _gnews_api_key
    _news_http_request.request(url)

func _on_real_news_received(result, response_code, headers, body):
    if response_code != 200:
        return
    var json = JSON.new()
    json.parse(body.get_string_from_utf8())
    var articles = json.data.get("articles", [])
    for article in articles:
        var title = article.get("title", "")
        if title.length() > 0:
            _real_news_queue.append(title)
```

### Qué falta en Unity
- **No existe NewsManager.cs.** No hay sistema de noticias.
- **No hay ticker animado** de noticias desplazandose por la pantalla.
- **No hay noticias con efectos mecanicos.** Los eventos del EventManager.cs actual generan eventos de transito pero no "noticias del mercado" que afecten globalmente.
- **No hay noticias de fondo/ambientacion.**
- **No hay integracion con API de noticias reales.**

---

## 1.5 Sistema de Llamadas a Clientes

### Qué es
El jugador podia llamar por telefono a los clientes para mejorar relaciones, reducir enojo, o generar nuevas oportunidades de carga.

### Algoritmo (Godot)

```gdscript
func _on_call_client_pressed(client_name: String, strategy: int = 0):
    # Reproduce sonido de telefono
    call_sound.play()
    
    # Probabilidad de que el cliente atienda (5% a 35%)
    var anger = client_anger.get(client_name, 0)
    var satisfaction = _get_client_satisfaction(client_name)
    var answer_chance = 0.20 + (satisfaction * 0.001) - (anger * 0.03)
    answer_chance = clamp(answer_chance, 0.05, 0.35)
    
    if randf() > answer_chance:
        notification_posted.emit("📞 %s no contesto..." % client_name, "warning")
        return
    
    # ESTRATEGIAS DE LLAMADA:
    # 0 = Normal, 1 = Alagar, 2 = Chupar medias
    match strategy:
        0:  # Normal
            var roll = randf()
            if roll < 0.5:
                # Efecto positivo leve
                _modify_satisfaction(client_name, +2)
                notification_posted.emit("📞 Buena charla con %s. +2 satisfaccion." % client_name, "success")
            elif roll < 0.8:
                # Neutro
                notification_posted.emit("📞 Charla cordial con %s." % client_name, "info")
            else:
                # Efecto negativo leve
                _modify_satisfaction(client_name, -1)
                notification_posted.emit("📞 %s parecia apurado..." % client_name, "warning")
        
        1:  # Alagar (Flatter)
            var roll = randf()
            if roll < 0.4:
                _modify_satisfaction(client_name, +5)
                _modify_anger(client_name, -1)
                notification_posted.emit("📞 %s agradecio tus palabras! +5 satisfaccion." % client_name, "success")
            else:
                _modify_satisfaction(client_name, -2)
                notification_posted.emit("📞 %s noto que estabas exagerando..." % client_name, "warning")
        
        2:  # Chupar medias (Suck up) — alto riesgo, alta recompensa
            var roll = randf()
            if roll < 0.25:
                _modify_satisfaction(client_name, +10)
                _modify_anger(client_name, -2)
                # Puede generar una carga nueva
                if randf() < 0.3:
                    _generate_cargo_from_client(client_name)
                    notification_posted.emit("📞 %s esta encantado y tiene una carga nueva!" % client_name, "success")
                else:
                    notification_posted.emit("📞 %s esta muy contento! +10 satisfaccion." % client_name, "success")
            else:
                _modify_satisfaction(client_name, -5)
                _modify_anger(client_name, +1)
                notification_posted.emit("📞 %s se ofendio por tu actitud..." % client_name, "danger")
    
    # Bonus de lealtad: cada 10 llamadas exitosas -> +1 satisfaccion permanente
    client_successful_calls[client_name] = client_successful_calls.get(client_name, 0) + 1
    if client_successful_calls[client_name] % 10 == 0:
        _modify_satisfaction(client_name, +1)
```

### Qué falta en Unity
- **No existe sistema de llamadas.** No hay boton de llamar, no hay estrategias de llamada, no hay probabilidad de que atienda, no hay efectos de llamada.
- **No hay sistema de satisfaccion de clientes** separado del `RelationshipLevel`. En Godot la satisfaccion (0-100) y el anger (0-5) eran metricas independientes.

---

## 1.6 Desbloqueo de Features por Nivel

### Qué es
En Godot, subir de nivel desbloqueaba nuevas mecanicas de juego. La version Unity tiene XP y niveles pero NO desbloquea nada.

### Datos Completos (Godot)

```gdscript
func _check_level_unlocks(new_level: int) -> void:
    match new_level:
        2:
            unlocked_features.append("priority_cargos")
            # Cargas prioritarias: mejor paga, deadline mas ajustado
        3:
            unlocked_features.append("insurance")
            # Seguro de carga: proteccion ante eventos de dano
        4:
            unlocked_features.append("bulk_contracts")
            # Contratos en lote: multiples cargas del mismo cliente
        5:
            unlocked_features.append("dangerous_goods")
            # Cargas peligrosas: mayor rentabilidad, mayor riesgo
        7:
            unlocked_features.append("vip_clients")
            # Clientes VIP: contratos premium, pagos al contado
        10:
            unlocked_features.append("global_network")
            # Red global: bonus en todas las rutas
        15:
            unlocked_features.append("logistics_mogul")
            # Magnate de la logistica: bonus permanente +10 reputacion
```

### Qué falta en Unity
- `EconomyManager.cs` tiene `AddXP()` y `_xpPerLevel` pero el nivel solo sube un numero. No hay `unlocked_features` list ni `_check_level_unlocks()`. Subir de nivel no hace nada mecanicamente.

---

## 1.7 Sistema de Fallo Diario de Cargas

### Qué es
En Godot, cada carga activa tenia una probabilidad diaria de fallo independiente de los eventos aleatorios. Era un sistema de "desgaste" que aumentaba el riesgo con el tiempo.

### Algoritmo (Godot)

```gdscript
const CARGO_FAILURE_BASE_CHANCE: float = 0.008  # 0.8% base diaria
const CARGO_MAX_EXTRA_DELAY_DAYS: int = 15       # Limite de demora

func _check_cargo_failure(cargo: Dictionary) -> Dictionary:
    var result = {"failed": false}
    var fail_chance = CARGO_FAILURE_BASE_CHANCE
    
    # Mas eventos previos = mas riesgo
    var events_count = cargo.get("events_encountered", []).size()
    fail_chance += events_count * 0.005
    
    # Cargas peligrosas = mas riesgo
    if cargo["cargo_type"] == CargoType.DANGEROUS:
        fail_chance += 0.008
    elif cargo["cargo_type"] == CargoType.VALUABLE:
        fail_chance += 0.005
    
    # Eventos mundiales activos = mas riesgo
    var risk_modifier = event_manager._get_world_event_risk_for_cargo(cargo)
    fail_chance += risk_modifier
    
    # CANCELACION AUTOMATICA si la demora acumulada supera 15 dias
    var extra_delay = cargo.get("extra_delay_days", 0)
    if extra_delay >= CARGO_MAX_EXTRA_DELAY_DAYS:
        result["failed"] = true
        result["type"] = "excessive_delay"
        result["reason"] = "La carga acumulo %d dias de demora. El cliente cancelo." % extra_delay
        return result
    
    # Roll de fallo
    if randf() < fail_chance:
        # No falla catastroficamente, sino que agrega demoras
        var extra_days = randi_range(1, 4)
        cargo["days_remaining"] += extra_days
        cargo["extra_delay_days"] = extra_delay + extra_days
    
    return result
```

### Qué falta en Unity
- No hay `_check_cargo_failure()`. Las cargas solo fallan por eventos del `EventManager.cs`.
- No hay `extra_delay_days` ni acumulacion de demoras.
- No hay `CARGO_MAX_EXTRA_DELAY_DAYS` ni cancelacion automatica por exceso de demora.
- No hay `CARGO_FAILURE_BASE_CHANCE`.

---

## 1.8 Sistema de Pagos Pendientes (Payment Delay)

### Qué es
En Godot, los clientes NO siempre pagaban al contado. Segun el tipo de cliente, el pago se recibia inmediatamente o se postergaba como un "pago pendiente".

### Algoritmo (Godot)

```gdscript
const CLIENT_PAYMENT_DELAY: Dictionary = {
    ClientType.GOOD_PAYER: 0,          # Paga al contado
    ClientType.URGENT_CLIENT: -7,      # Paga por adelantado (7 dias antes)
    ClientType.CREDIT_CLIENT: 30,      # Paga a 30 dias
    ClientType.CONTRACT_CLIENT: 15,    # 50% al contado + 50% a 15 dias
    ClientType.BAD_PAYER: 5,           # Retrasa 5 dias
    ClientType.VERY_BAD_CLIENT: 10,    # Retrasa 10 dias
}

func _complete_cargo(cargo: Dictionary):
    var final_price = cargo["final_price"]
    var client_type = cargo["client_type"]
    var payment_delay = CLIENT_PAYMENT_DELAY.get(client_type, 0)
    
    match payment_delay:
        0:  # Pago al contado
            economy.earn_money(final_price, "Pago: %s" % cargo["id"])
        -7:  # Pago anticipado
            economy.earn_money(final_price, "Pago anticipado: %s" % cargo["id"])
        _:  # Pago pendiente
            match client_type:
                ClientType.CONTRACT_CLIENT:
                    # 50% al contado, 50% en 15 dias
                    var half = final_price / 2
                    economy.earn_money(half, "Anticipo contrato: %s" % cargo["id"])
                    pending_payments.append({
                        "cargo_id": cargo["id"],
                        "client_name": cargo["client_name"],
                        "amount": final_price - half,
                        "days_remaining": payment_delay,
                        "is_contract": true
                    })
                _:
                    # Todo pendiente
                    pending_payments.append({
                        "cargo_id": cargo["id"],
                        "client_name": cargo["client_name"],
                        "amount": final_price,
                        "days_remaining": payment_delay
                    })

func _process_pending_payments():
    # Se llama cada dia
    var completed_payments = []
    for payment in pending_payments:
        payment["days_remaining"] -= 1
        if payment["days_remaining"] <= 0:
            economy.earn_money(payment["amount"], "Pago recibido: %s" % payment["client_name"])
            completed_payments.append(payment)
    for p in completed_payments:
        pending_payments.erase(p)
```

### Qué falta en Unity
- **No hay `pending_payments`.** En la version Unity, el cargo se completa y se paga inmediatamente. No hay demora de pago.
- **No hay `CLIENT_PAYMENT_DELAY`.** El enum `ClientType` existe pero no tiene delays asociados.
- **No hay `_process_pending_payments()`.** No se procesan pagos dia a dia.

---

## 1.9 Experiencia de Agentes por Ruta

### Qué es
En Godot, los agentes ganaban experiencia en rutas especificas. A medida que un agente transportaba mas cargas por la misma ruta, se volvia mas eficiente (menor costo, mayor velocidad) en ese trayecto.

### Algoritmo (Godot)

```gdscript
# Estructura: agent_experience[agent_id][route_key] = count
var agent_experience: Dictionary = {}

func _complete_cargo_with_agent(cargo: Dictionary):
    var agent_id = cargo["agent_id"]
    var route_key = "%s_%s" % [cargo["origin_city"], cargo["destination_city"]]
    
    if not agent_experience.has(agent_id):
        agent_experience[agent_id] = {}
    
    agent_experience[agent_id][route_key] = agent_experience[agent_id].get(route_key, 0) + 1
    
    # Bonus: cada 3 entregas en la misma ruta reduce el costo 5% y la velocidad 10%
    var exp_count = agent_experience[agent_id][route_key]
    var cost_reduction = min(0.25, exp_count / 3 * 0.05)    # Maximo 25% descuento
    var speed_bonus = min(0.30, exp_count / 3 * 0.10)       # Maximo 30% mas rapido

# Se mostraba en el panel de agentes:
# "🗺 Shanghai → Rotterdam: 5 entregas (⬇15% costo, ⬆20% velocidad)"
```

### Qué falta en Unity
- **No hay experiencia por ruta.** El `Agent.cs` tiene `_trustLevel` y `_routeExperience` como diccionario pero SOLO registra la cantidad; no aplica ningun bonus mecanico de costo/velocidad por experiencia acumulada.

---

## 1.10 Degradacion de Satisfaccion de Clientes

### Qué es
Los clientes se olvidaban del jugador si no habia actividad. Cada 5 dias sin interaccion, la satisfaccion bajaba. Y cada 60 dias, el enojo se reducia lentamente (pero nunca a estado "feliz").

### Algoritmo (Godot)

```gdscript
var _days_since_last_degrade: int = 0
var _days_since_last_calm: int = 0

func _on_day_advanced():
    _days_since_last_degrade += 1
    _days_since_last_calm += 1
    
    # Cada 5 dias: degradar satisfaccion de clientes inactivos
    if _days_since_last_degrade >= 5:
        _days_since_last_degrade = 0
        _degrade_client_mood()
    
    # Cada 60 dias: calmar clientes enojados
    if _days_since_last_calm >= 60:
        _days_since_last_calm = 0
        _calm_clients()

func _degrade_client_mood():
    for client_name in client_memory:
        var mem = client_memory[client_name]
        var last_interaction = mem.get("last_seen", 0)
        var days_inactive = total_days - last_interaction
        
        if days_inactive >= 5:
            var current_sat = mem.get("satisfaction", 50)
            mem["satisfaction"] = max(0, current_sat - 3)

func _calm_clients():
    for client_name in client_anger:
        if client_anger[client_name] > 0:
            client_anger[client_name] -= 1  # Reduce 1 nivel de enojo cada 60 dias
```

### Qué falta en Unity
- **No hay degradacion pasiva de satisfaccion.** Los clientes del Unity mantienen su `RelationshipLevel` indefinidamente sin decaer por inactividad.
- **No hay mecanismo de "calma" del enojo** con el tiempo.

---

## 1.11 Zonas Terrestres (Land Zones)

### Qué es
En Godot, las ciudades pertenecian a "zonas terrestres" que determinaban si dos ciudades podian conectarse por transporte terrestre. Solo ciudades de la misma zona podian conectarse por tierra.

### Datos Completos (Godot)

```gdscript
const LAND_ZONES: Dictionary = {
    # Sudamerica
    "buenos_aires": "south_america",
    "sao_paulo": "south_america",
    "lima": "south_america",
    "bogota": "south_america",
    "valparaiso": "south_america",
    "santiago": "south_america",
    "santos": "south_america",
    "cartagena": "south_america",
    
    # Norteamerica
    "miami": "north_america",
    "new_york": "north_america",
    "los_angeles": "north_america",
    "houston": "north_america",
    "vancouver": "north_america",
    "mexico_city": "north_america",
    
    # Europa
    "london": "europe",
    "rotterdam": "europe",
    "hamburg": "europe",
    "antwerp": "europe",
    "barcelona": "europe",
    "madrid": "europe",
    "marseille": "europe",
    "paris": "europe",
    "frankfurt": "europe",
    "athens": "europe",
    "istanbul": "europe",  # Conectada a Europa por tierra
    
    # Asia Oriental
    "shanghai": "east_asia",
    "tokyo": "east_asia",  # Isla, pero conectada via tunnel/ferry
    "hong_kong": "east_asia",
    "busan": "east_asia",
    "taipei": "east_asia",
    "ho_chi_minh": "east_asia",
    "manila": "east_asia",
    "bangkok": "east_asia",
    
    # Sur de Asia
    "mumbai": "south_asia",
    "colombo": "south_asia",
    "karachi": "south_asia",
    
    # Africa
    "cape_town": "africa",
    "johannesburg": "africa",
    "mombasa": "africa",
    "casablanca": "africa",
    "port_said": "africa",
    
    # Medio Oriente
    "dubai": "middle_east",
    "jeddah": "middle_east",
    
    # Oceania (sin conexion terrestre a otros continentes)
    "sydney": "oceania",
    "auckland": "oceania",
}

func can_use_land_transport(origin_id: String, dest_id: String) -> bool:
    var origin_zone = LAND_ZONES.get(origin_id, "")
    var dest_zone = LAND_ZONES.get(dest_id, "")
    return origin_zone != "" and origin_zone == dest_zone
```

### Qué falta en Unity
- **No hay zonas terrestres.** La version Unity permite seleccionar `TransportMode.Land` entre cualquier par de ciudades, incluso Buenos Aires a Tokyo.

---

## 1.12 Hub Tiers para Aeropuertos y Puertos

### Qué es
En Godot existia un sistema de clasificacion de hubs que afectaba el costo y la eficiencia del transporte. Los hubs se clasificaban en Tier S, A y B.

### Datos (Godot)

```gdscript
enum HubTier { S, A, B }

const HUB_TIERS := {
    "air": {
        "miami": HubTier.S, "los_angeles": HubTier.S, "new_york": HubTier.S,
        "london": HubTier.S, "shanghai": HubTier.S, "dubai": HubTier.S,
        "singapore": HubTier.S,
        "sao_paulo": HubTier.A, "mexico_city": HubTier.A, "bogota": HubTier.A,
        "madrid": HubTier.A, "frankfurt": HubTier.A, "tokyo": HubTier.A,
        "hong_kong": HubTier.A, "sydney": HubTier.A, "mumbai": HubTier.A,
        "buenos_aires": HubTier.B, "santiago": HubTier.B, "lima": HubTier.B,
        "panama": HubTier.B, "paris": HubTier.B, "istanbul": HubTier.B,
        # ... todos los demas
    },
    "maritime": {
        "shanghai": HubTier.S, "singapore": HubTier.S, "rotterdam": HubTier.S,
        "hong_kong": HubTier.S, "los_angeles": HubTier.S,
        "santos": HubTier.A, "buenos_aires": HubTier.A, "miami": HubTier.A,
        "hamburg": HubTier.A, "antwerp": HubTier.A, "dubai": HubTier.A,
        "tokyo": HubTier.A, "mumbai": HubTier.A,
        # ... etc
    }
}
```

### Qué falta en Unity
- **No hay Hub Tiers.** Todas las ciudades son equivalentes para calculo de rutas.

---

## 1.13 Sistema de Tiers de Ciudades (Desbloqueo Progresivo)

### Qué es
Las ciudades se desbloqueaban progresivamente segun la cantidad de oficinas que tenia el jugador.

### Datos Completos (Godot)

```gdscript
const CITY_UNLOCK_TIERS: Array = [
    # Tier 0: Arranque (0 oficinas)
    {"offices_required": 0,  "cities": ["buenos_aires"]},
    
    # Tier 1: 1 oficina
    {"offices_required": 1,  "cities": ["sao_paulo", "miami", "lima"]},
    
    # Tier 2: 3 oficinas
    {"offices_required": 3,  "cities": ["shanghai", "rotterdam", "tokyo", "houston", "panama"]},
    
    # Tier 3: 5 oficinas
    {"offices_required": 5,  "cities": ["new_york", "london", "dubai", "valparaiso",
                                         "singapore", "hamburg", "cape_town", "madrid", "bogota"]},
    
    # Tier 4: 8 oficinas
    {"offices_required": 8,  "cities": ["los_angeles", "hong_kong", "mumbai", "istanbul",
                                         "barcelona", "bangkok", "vancouver", "casablanca",
                                         "paris", "mexico_city", "santiago"]},
    
    # Tier 5: 12 oficinas
    {"offices_required": 12, "cities": ["antwerp", "busan", "johannesburg", "sydney",
                                         "marseille", "athens", "karachi", "colombo",
                                         "jeddah", "frankfurt", "santos", "cartagena"]},
    
    # Tier 6: 18 oficinas (dominio total)
    {"offices_required": 18, "cities": ["port_said", "mombasa", "ho_chi_minh", "manila",
                                         "taipei", "vladivostok", "auckland"]}
]

func get_unlockable_cities(office_count: int) -> Array:
    var result = []
    for tier in CITY_UNLOCK_TIERS:
        if office_count >= tier["offices_required"]:
            result.append_array(tier["cities"])
    return result
```

### Qué falta en Unity
- La version Unity tiene `OfficesPanel.cs` y un concepto basico de ciudades pero **no tiene el sistema de tiers de desbloqueo**. Las 10 ciudades actuales estan hardcodeadas en `CityDatabase.cs` sin sistema de desbloqueo progresivo.
- No hay `get_unlockable_cities()` ni logica de oficinas requeridas.

---

## 1.14 Noticias de Fondo (Background News)

### Qué es
Mas de 200 frases cortas de ambientacion que aparecian en el ticker sin ningun efecto mecanico. Servian para dar vida al mundo.

### Ejemplos del Godot Original

```gdscript
const BACKGROUND_NEWS: Array = [
    {"text": "Un cafe con medialunas en Buenos Aires: la pausa perfecta"},
    {"text": "Record de turistas en Barcelona este verano"},
    {"text": "El metro de Tokio transporto 8.7 millones de personas hoy"},
    {"text": "Un gato se perdio en un almacen de Dubai y aparecio 3 dias despues"},
    {"text": "Noche de tango en La Boca: lleno total"},
    {"text": "El Big Ben suena a las 12 en punto. Como siempre."},
    {"text": "Inauguracion del nuevo muelle en Rotterdam"},
    {"text": "Festival de luces en Shanghai ilumina el Bund"},
    {"text": "Temperaturas record en Mumbai esta semana"},
    {"text": "Patagonia: temporada de avistamiento de ballenas"},
    {"text": "La torre Eiffel cumple un nuevo aniversario"},
    {"text": "Concierto al aire libre en Central Park"},
    {"text": "Nueva linea de metro en Estambul conecta Europa y Asia"},
    {"text": "Cosecha record de soja en la pampa argentina"},
    {"text": "Los cerezos florecen en Tokio: hanami season"},
    {"text": "Nuevo restaurante peruano abre en Miami"},
    {"text": "Lluvias monzonicas llegan antes a Mumbai este ano"},
    {"text": "Maratón de Ciudad del Cabo: 30.000 corredores"},
    # ... ~200 mas
]
```

### Qué falta en Unity
- **No hay noticias de fondo.** No hay array de textos de ambientacion ni ticker que las muestre.

---

## 1.15 Noticias Reales desde GNews API

### Algoritmo (Godot)

```gdscript
# NewsManager.gd
var _gnews_api_key: String = ""

func _load_api_key():
    # Lee la API key desde un archivo de configuracion local
    var path = "user://config.json"
    if not FileAccess.file_exists(path):
        return
    var f = FileAccess.open(path, FileAccess.READ)
    var json = JSON.new()
    if json.parse(f.get_as_text()) == OK and json.data is Dictionary:
        _gnews_api_key = json.data.get("gnews_api_key", "")

func fetch_real_news():
    if _gnews_api_key.is_empty():
        return
    var url = "https://gnews.io/api/v4/search?" \
        + "q=economia+OR+logistica+OR+comercio+OR+transporte+OR+exportacion+OR+importacion" \
        + "&lang=es&country=ar&max=10" \
        + "&apikey=" + _gnews_api_key
    _news_http_request.request(url)

# Se llamaba cada 5 dias de juego
```

### Qué falta en Unity
- **No hay integracion con GNews API** ni con ninguna fuente de noticias reales.

---

## 1.16 Negociacion en Rondas con Contraoferta del Jugador

### Qué es
En Godot, la negociacion tenia multiples rondas donde el JUGADOR tambien podia hacer contraofertas al cliente (no solo aceptar o rechazar la contraoferta del cliente).

### Algoritmo (Godot)

```gdscript
# En game_screen.gd
var _current_negotiation_round: int = 0
var _current_max_rounds: int = 2
var _can_counter_offer: bool = false

func _setup_negotiation_round_ui():
    # SpinBox para que el jugador ingrese su contraoferta
    _counter_spinbox = SpinBox.new()
    _counter_spinbox.min_value = _current_counter_offer + 10
    _counter_spinbox.max_value = _current_counter_offer * 3
    _counter_spinbox.value = int(_current_counter_offer * 1.15)
    
    # Boton "Enviar Contraoferta"
    _counter_btn = Button.new()
    _counter_btn.text = "Enviar Contraoferta"
    
    # Boton "Rechazar Definitivamente"
    _walkaway_btn = Button.new()
    _walkaway_btn.text = "Rechazar Definitivamente"

func _on_send_counter_offer():
    var player_offer = int(_counter_spinbox.value)
    var result = GameState.player_counter_offer(_current_cargo_id, player_offer)
    
    if result.get("accepted", false):
        # Cliente acepto la contraoferta del jugador
        notification_posted.emit("El cliente acepto tu oferta de $%d!" % player_offer, "success")
    elif result.get("new_counter", 0) > 0:
        # El cliente hace una nueva contraoferta
        _current_counter_offer = result["new_counter"]
        _current_negotiation_round += 1
        _update_negotiation_round_ui()
    else:
        # Negociacion terminada sin acuerdo
        notification_posted.emit("El cliente rechazo definitivamente.", "danger")
```

### Qué falta en Unity
- La version Unity tiene contraoferta del CLIENTE pero **el jugador no puede hacer una contra-contraoferta**. Solo puede aceptar o rechazar. No hay rondas multiples con SpinBox para ingresar un nuevo precio.

---

## 1.17 Efectos de Noticias con Modificadores Temporales

### Algoritmo (Godot)

```gdscript
# EventManager.gd
var _active_news_effects: Dictionary = {}
var _news_effect_duration: Dictionary = {}

func _apply_news_effects(effects: Dictionary, duration: int):
    if effects.has("price_multiplier"):
        _active_news_effects["price_multiplier"] = effects["price_multiplier"]
        _news_effect_duration["price_multiplier"] = duration
    if effects.has("delay_risk"):
        _active_news_effects["delay_risk"] = effects["delay_risk"]
        _news_effect_duration["delay_risk"] = duration
    if effects.has("fuel_multiplier"):
        fuel_price_index *= effects["fuel_multiplier"]
        _news_effect_duration["fuel"] = duration

func _process_daily_effects():
    # Cada dia reducir la duracion de los efectos activos
    var expired = []
    for key in _news_effect_duration:
        _news_effect_duration[key] -= 1
        if _news_effect_duration[key] <= 0:
            expired.append(key)
    for key in expired:
        _active_news_effects.erase(key)
        _news_effect_duration.erase(key)

func get_current_price_multiplier() -> float:
    return _active_news_effects.get("price_multiplier", 1.0)
```

### Qué falta en Unity
- **No hay modificadores temporales de noticias.** Los eventos de `EventManager.cs` afectan cargas individuales pero no hay efectos globales temporales que modifiquen precios, riesgos, etc.

---

## 1.18 Indice de Precio de Combustible Dinamico

### Algoritmo (Godot)

```gdscript
# En EventManager.gd
var fuel_price_index: float = 1.0

# El indice cambia por:
# 1. Noticias de economia (ej: "El petroleo sube 12%")
# 2. Eventos mundiales (ej: Ramadan reduce oferta de crudo)
# 3. Fluctuacion aleatoria cada 7 dias

# Se usa para modificar costos de agentes:
# costo_final = costo_base * fuel_price_index
```

### Qué falta en Unity
- **No hay `fuel_price_index`.** Los costos de transporte son fijos basados en la tarifa del agente sin fluctuacion de combustible.

---

## 1.19 Panel de Clientes con Filtros Avanzados

### Algoritmo (Godot)

```gdscript
# clients_panel.gd
func _apply_filter():
    _filtered_clients = []
    match _current_filter:
        0:  # Todos
            _filtered_clients = _all_clients.duplicate()
        1:  # Activos (tienen cargas)
            for client in _all_clients:
                if client["active"] > 0:
                    _filtered_clients.append(client)
        2:  # Enojados (anger > 0)
            for client in _all_clients:
                if client["anger_level"] > 0:
                    _filtered_clients.append(client)
        3:  # Muy enojados (anger >= 3)
            for client in _all_clients:
                if client["anger_level"] >= 3:
                    _filtered_clients.append(client)
```

La UI mostraba por cada cliente:
- Nombre, Tipo, Estado emocional (emoji), Total cargas, Completadas, Fallidas, Activas, Pendientes, Profit, Satisfaccion (estrellas)
- Botones: "Info" (abre detalle con rutas favoritas, historial, primera/ultima interaccion) y "Llamar"

### Qué falta en Unity
- La version Unity **no tiene panel de clientes.** No hay `ClientsPanel.cs`. Los clientes solo aparecen en el contexto de las cargas del mercado.

---

## 1.20 Ticker de Noticias Animado

### Algoritmo (Godot)

```gdscript
# game_screen.gd
var _ticker_position: float = 0.0
var _ticker_speed: float = 120.0       # pixeles por segundo
var _ticker_texts: Array[String] = []
var _current_ticker_text: String = ""
var _ticker_width: float = 0.0

func add_news(text: String, type: String):
    var icon = match type:
        "event":   "⚡ "
        "success": "✅ "
        "warning": "⚠️ "
        _:         "📰 "
    _ticker_texts.append(icon + text + " ")
    # Maximo 6 noticias en cola
    while _ticker_texts.size() > 6:
        _ticker_texts.pop_front()

func _process(delta):
    if not _current_ticker_text.is_empty():
        _ticker_position -= _ticker_speed * delta
        news_label.position.x = _ticker_position
        # Cuando desaparece -> siguiente noticia
        if _ticker_position + _ticker_width < 0:
            _start_next_ticker()

func _start_next_ticker():
    if _ticker_texts.is_empty():
        _current_ticker_text = ""
        return
    _current_ticker_text = _ticker_texts.pop_front()
    news_label.text = _current_ticker_text
    _ticker_width = news_label.get_combined_minimum_size().x
    _ticker_position = container_width  # Empieza fuera de la pantalla por la derecha
```

### Qué falta en Unity
- **No hay ticker animado.** La UI de la version Unity tiene paneles estaticos sin barra de noticias que se desplace en la parte inferior de la pantalla.

---

# 2. SISTEMAS PLANIFICADOS (PROJECT.md) AUN NO IMPLEMENTADOS

Estos son sistemas que el `PROJECT.md` del usuario describe como parte del plan pero que **no estan ni en el codigo Godot ni en el codigo Unity actual**:

### 2.1 Competidores AI
- 3-5 empresas rivales que tambien toman cargas del mercado
- Si el jugador no cotiza en 3 dias, un rival la toma (60-80% chance)
- Los rivales tienen reputacion que crece/baja
- El mercado muestra "[TOMADA por RivalCo]" en cargas tomadas

### 2.2 Sistema de Divisas
- Cargas en distintas monedas (USD, EUR, CNY, BRL, ARS)
- Tipo de cambio fluctua ±2% por semana
- Pantalla "Finanzas > Divisas" con tipos actuales

### 2.3 Freight Rate Index (FRI)
- Indice que refleja demanda global de transporte
- Sube/baja ±5% por semana
- Afecta precios de agentes y pagos de clientes
- Grafico de linea en panel de Finanzas

### 2.4 Contratos a Largo Plazo
- Clientes ofrecen contratos: "3 cargas mensuales por 6 meses"
- Precio fijo (5-10% menor al spot)
- Penalidad si el jugador no cumple

### 2.5 Fuel Futures (Cobertura de Combustible)
- El jugador puede "bloquear" precios de combustible comprando futuros
- Mecanica financiera avanzada

### 2.6 ServiceLocator Pattern
- Acceso centralizado a managers via interfaces
- `ServiceLocator.Get<ICargoManager>()`
- Permite testing con mocks

### 2.7 ScriptableObjects para Datos
- Ciudades, agentes, eventos como assets editables sin recompilar
- `CityDatabase.asset`, `AgentDatabase.asset`, `EventDatabase.asset`

### 2.8 Sistema de Audio con FMOD
- Musica ambiental, efectos de sonido
- Sonido de llamada telefonica, alertas radiales, tecleo mecanico

### 2.9 Tutorial Interactivo
- Overlay oscuro con spotlight en elementos
- 5 pasos guiados para los primeros minutos
- Se puede saltar y reveer

### 2.10 Sistema de Logros Steam (15 achievements)
- FIRST_CARGO, TEN_CARGOS, FIFTY_CARGOS, FIRST_MILLION, GLOBAL_NETWORK, PERFECT_MONTH, NEGOTIATOR, INSURANCE_PAYS, REPUTATION_MAX, SOUTH_AMERICA, DANGEROUS_EXPERT, SPEED_RUN, BETRAYAL, SURVIVOR, FIVE_YEARS

### 2.11 Steam Cloud Save + Rich Presence
- Sincronizar save con Steam Cloud
- Mostrar estado del jugador en Steam

### 2.12 Arbol de Habilidades (Licencias/Certificaciones)
- Certificacion IATA: reduce costos aereos
- Operador Economico Autorizado (OEA): reduce inspecciones de aduana
- Especialista en Seguros: recupera 50% en accidentes

### 2.13 Carga de Proyecto (Breakbulk)
- Maquinaria gigante que no cabe en contenedores
- Requiere barcos especiales y planificacion manual

### 2.14 Modelo de "Director de Juego" (estilo RimWorld)
- Un AI Director que lanza eventos para equilibrar dificultad
- Micro-eventos (pinchazo de rueda), Macro-eventos (Canal de Suez bloqueado), Oportunidades (cliente desesperado)

### 2.15 Transporte Multimodal
- `TransportMode.Multimodal` — combinar camion + barco + avion en un solo envio
- Requiere planificacion de ruta multi-tramo

### 2.16 50+ Ciudades Progresivas
- El Godot tenia 20 ciudades. El Unity actual tiene 10. El plan dice 50+.

---

# 3. IDEAS FRESCAS PARA LA VERSION UNITY STEAM

Estas son ideas nuevas que NO existian en ninguna version anterior ni en el PROJECT.md. Estan pensadas para hacer el juego mas inmersivo, atrapante, y visualmente espectacular en Unity 3D.

---

## 3.1 MUNDO VIVO EN 3D

### 3.1.1 Estaciones del Ano Visibles en el Globo
El mapa 3D debe mostrar las estaciones del ano de forma visual y afectar el gameplay:

```
IMPLEMENTACION SUGERIDA:
- 12 texturas de la Tierra (una por mes) o shader que interpole entre verano/invierno
- Hemisferio norte: nieve en dic-feb, verde en jun-ago
- Hemisferio sur: verde en dic-feb, colores otoñales en mar-may
- Cada estacion afecta:
  * Invierno: +15% probabilidad de eventos Weather, +10% costos maritimos
  * Verano: +20% demanda de carga refrigerada, -5% costos aereos
  * Monzon (jun-sep en India): +25% eventos Weather en rutas a Mumbai
  * Tifones (jul-nov en Asia): +30% eventos en rutas Shanghai/Tokyo/Manila
```

### 3.1.2 Vehiculos Visibles Recorriendo el Mundo
Cargueros, aviones y camiones visibles en el globo 3D moviéndose por las rutas:

```
IMPLEMENTACION SUGERIDA:
- Prefab de barco container (low-poly) que navega por la superficie del globo
- Prefab de avion cargo que vuela en arco sobre el globo (elevado sobre la superficie)
- Prefab de camion que sigue las carreteras (pegado a la superficie terrestre)
- Prefab de tren que sigue vias ferroviarias
- Cada cargo activa muestra su vehiculo en la posicion correcta segun el progreso
- Al hacer click en un vehiculo: popup con informacion de la carga
- Estela visual: los barcos dejan estela en el agua, los aviones dejan trail
- Velocidad visual: ajustada al TimeScale del juego (x1, x2, x3)
```

### 3.1.3 Ciclo Dia/Noche en el Globo con Ciudades Iluminadas
```
IMPLEMENTACION SUGERIDA:
- Shader que muestra sombra de la Tierra segun la hora del dia
- Las ciudades con oficinas brillan de noche (emision en el shader)
- Nubes volumetricas ligeras que se mueven sobre el globo
- Aurora boreal visible en latitudes altas en invierno
```

### 3.1.4 Clima Visible en el Mapa
```
IMPLEMENTACION SUGERIDA:
- Particulas de tormenta sobre zonas con eventos Weather activos
- Iconos de nieve/lluvia/sol sobre ciudades segun la estacion
- Huracanes visibles como espirales que se mueven por el oceano
- Cuando hay un evento mundial activo (ej: Canal de Suez bloqueado),
  se marca visualmente la zona afectada en rojo
```

---

## 3.2 MECANICAS NUEVAS DE GAMEPLAY

### 3.2.1 Sistema de Seguros Detallado
```
TIPOS DE SEGURO:
- Seguro Basico ($300): cubre 50% del valor en caso de dano
- Seguro Premium ($800): cubre 80% del valor + 50% del retraso
- Seguro Total ($1500): cubre 100% del valor + 100% del retraso
- Seguro de Guerra ($2000): cubre pirateria, robos, zonas de conflicto
- Sin seguro: el jugador asume todo el riesgo

MECANICA:
- Al cotizar, el jugador elige si incluir seguro y que tipo
- El costo del seguro se suma al costo de transporte
- Si ocurre un evento de dano:
  * Con seguro: se activa automaticamente, el jugador recupera segun cobertura
  * Sin seguro: el jugador pierde el valor completo
  * Logro "INSURANCE_PAYS" cuando el seguro salva una carga
```

### 3.2.2 Sistema de Documentacion y Aduanas
```
NUEVA MECANICA:
- Para cada envio, hay un "nivel de documentacion" (1-5)
- Nivel 1 (basico): alto riesgo de DocumentationError (+15%)
- Nivel 3 (completo): riesgo normal
- Nivel 5 (premium): reduce riesgo a 1%
- Preparar documentacion cuesta tiempo (1-3 dias) y dinero ($100-$500)
- Agentes con alta confianza pueden gestionar documentacion automaticamente
- Certificacion OEA reduce costos de documentacion en 30%
```

### 3.2.3 Sistema de Almacenes (Warehousing)
```
NUEVA MECANICA:
- El jugador puede alquilar almacenes en ciudades con oficina
- Los almacenes permiten:
  * Consolidar cargas (juntar varias cargas pequeñas para enviar como una grande = descuento)
  * Desconsolidar cargas (recibir un container y distribuir a destinos finales)
  * Almacenar temporalmente carga cuando hay huelga/evento = evitar perdida
- Costo mensual del almacen segun ciudad y tamano
- Desbloquear en nivel 4+
```

### 3.2.4 Sistema de Subastas de Carga
```
NUEVA MECANICA:
- Una vez por semana aparece una "subasta" especial con cargas premium
- El jugador compite contra AI competitors ofertando un precio
- La subasta dura 1 dia de juego (los rivales van subiendo ofertas)
- Ganar la subasta da mucho XP y reputacion pero bajo margen
- Perder no tiene penalidad pero pierde la oportunidad
```

### 3.2.5 Sistema de Empleados
```
NUEVA MECANICA:
- Contratar empleados para automatizar tareas:
  * Coordinador de ruta: reduce tiempo de transito en 10%
  * Negociador: +5% probabilidad de aceptacion de cotizacion
  * Documentalista: reduce riesgo de errores de documentacion
  * Analista de mercado: muestra informacion adicional de cargas (margen estimado, riesgo)
- Cada empleado tiene un salario mensual
- Se pueden asignar a oficinas especificas
- Desbloquear en nivel 5+
```

### 3.2.6 Sistema de Reputacion por Region
```
NUEVA MECANICA:
- En lugar de una sola reputacion global, tener reputacion por region:
  * Sudamerica: 50 (base)
  * Europa: 0 (desconocido)
  * Asia: 0 (desconocido)
  * etc.
- Completar cargas en una region sube la reputacion de esa region
- Los clientes de una region prefieren forwarders con alta reputacion en su zona
- Permite especializarse: "ser el rey de la ruta Asia-Sudamerica"
```

---

## 3.3 VISUAL Y EXPERIENCIA

### 3.3.1 Interfaz "Centro de Operaciones" 3D
```
IDEA:
- En lugar de solo paneles 2D, el juego tiene una vista de "oficina" en 3D
- El jugador ve su escritorio con:
  * Monitor con el dashboard del juego
  * Telefono (para el sistema de llamadas)
  * Mapa del mundo en la pared (que se puede ampliar al mapa 3D)
  * Bandeja de documentos (cargas pendientes)
  * Marcos con fotos de ciudades donde tiene oficinas
- Al subir de nivel de oficina, la oficina se ve mas grande y lujosa
- Es una capa visual encima de los paneles, no reemplaza la funcionalidad
```

### 3.3.2 Cinematic de Entrega
```
IDEA:
- Cuando una carga se completa exitosamente (especialmente si fue dificil),
  mostrar una mini-cinematica de 3-5 segundos:
  * El barco llegando al puerto
  * El avion aterrizando
  * El camion entrando al almacen
- Usar Cinemachine para transiciones suaves de camara
- Solo para entregas importantes (>$50,000 o primera entrega a una ciudad nueva)
- Se puede desactivar en opciones
```

### 3.3.3 Sistema de Radio/Musica Adaptativa
```
IDEA:
- Musica que cambia segun el estado del juego:
  * Calma: musica ambiental de oficina (lo-fi, jazz suave)
  * Tension: cuando hay un evento critico o multiples cargas en riesgo
  * Exito: fanfarria breve cuando se completa una carga importante
  * Crisis: musica tensa cuando el dinero esta bajo o la reputacion cae
- FMOD permite transiciones suaves entre estados musicales
- Efecto de radio: las noticias del ticker se "escuchan" como un noticiero de fondo
```

### 3.3.4 Clima Dinamico en la Oficina
```
IDEA:
- La vista de la oficina muestra el clima real de Buenos Aires (o la ciudad sede)
- Si es de noche en el juego, la ventana de la oficina muestra la ciudad de noche
- Si llueve en el juego (evento Weather en Buenos Aires), se ve lluvia por la ventana
```

---

## 3.4 MECANICAS DE ENGAGEMENT A LARGO PLAZO

### 3.4.1 Desafios Semanales
```
IDEA:
- Cada semana de juego (210 dias reales = 7 dias * 30 seg = 3.5 min):
  * "Completa 5 envios esta semana" -> bonus $2000
  * "Sin fallos esta semana" -> bonus reputacion +5
  * "Usa 3 modos de transporte distintos" -> bonus XP x2
- Muestra progreso en el HUD
- Opcional: se pueden ignorar sin penalidad
```

### 3.4.2 Logbook / Diario del Forwarder
```
IDEA:
- Un registro automatico de eventos importantes:
  * "Dia 15: Mi primera entrega exitosa a Shanghai"
  * "Dia 45: Tormenta tropical detuvo 3 cargas simultaneamente"
  * "Dia 100: Alcance $100,000 en revenue total"
- Se puede leer desde el menu de pausa
- Al final de la partida (game over o victoria), se muestra como un "resumen de carrera"
- Genera screenshots automaticos en momentos clave
```

### 3.4.3 Sistema de Crisis Globales (Macro-Eventos)
```
IDEA (ampliacion del Director de Juego):
- Eventos que afectan TODO el mundo por semanas:
  * "Pandemia Global": +50% costos, -70% oferta, duracion 30-60 dias
  * "Canal de Suez Bloqueado": rutas Asia-Europa +300% costo maritimo, 15 dias
  * "Huelga Global de Pilotos": transporte aereo deshabilitado por 7 dias
  * "Boom Economico": +100% demanda, +30% precios, 20 dias
  * "Guerra Comercial EEUU-China": aranceles +40% en rutas China-EEUU
- El jugador debe adaptarse: cambiar rutas, modos de transporte, renegociar contratos
- Es lo que hace al juego "atrapante" — nunca sabes que va a pasar
```

### 3.4.4 Modo Historia / Campana
```
IDEA:
- En lugar de solo sandbox, un modo con objetivos narrativos:
  * Capitulo 1: "De Buenos Aires al Mundo" — completa 10 envios internacionales
  * Capitulo 2: "Crisis en Asia" — maneja una crisis de suministro por Ano Nuevo Chino
  * Capitulo 3: "El Gran Contrato" — gana la licitacion de un cliente VIP
  * Capitulo 4: "Expansion Europea" — abre 3 oficinas en Europa
  * Capitulo 5: "El Magnate" — alcanza 50 ciudades y $1M en revenue
- Cada capitulo tiene dialogos con clientes (visual novel style)
- Desbloquea personajes, oficinas tematicas, vehiculos especiales
```

### 3.4.5 Vehiculos Personalizables
```
IDEA:
- El jugador puede "marcar" sus vehiculos con el logo de la empresa
- Comprar vehiculos propios (en lugar de solo usar agentes):
  * Camion propio: costo inicial alto, pero 0 costo de agente
  * Contenedor propio: reduce costos maritimos
  * Avion charter: para cargas urgentes premium
- Los vehiculos se ven en el mapa 3D con el branding del jugador
- Requiere nivel alto y mucha inversion
```

### 3.4.6 Sistema de Espionaje Comercial
```
IDEA:
- Investigar a los competidores AI:
  * "Espiar precios": ver a cuanto cotizan los rivales ($500)
  * "Estudiar rutas": ver que rutas usan mas ($1000)
  * "Sabotear": reducir reputacion del rival (-5 rep al rival, riesgo: si te descubren, tu reputacion baja -15)
- Los rivales tambien pueden espiarte
- Agrega una capa de estrategia tipo "juego de mesa" sobre la logistica
```

---

## 3.5 MEJORAS TECNICAS PARA STEAM

### 3.5.1 Steam Workshop: Mods de Ciudades
```
IDEA:
- Los jugadores pueden crear ciudades custom con coordenadas, infraestructura, y nombre
- Se comparten via Steam Workshop
- Usar ScriptableObjects para que los modders puedan crear assets sin programar
- Template de mod: un .unitypackage con la estructura necesaria
```

### 3.5.2 Modo Hardcore
```
IDEA:
- Sin autosave
- Penalidades 2x
- Sin opcion de rechazar eventos (debes resolver todo)
- Cargas expiran en 3 dias (en vez de 7)
- Sin tutoriales
- Logro especial: "IRON FORWARDER" — sobrevive 365 dias en modo hardcore
```

### 3.5.3 Estadisticas y Graficos
```
IDEA:
- Graficos de linea de:
  * Revenue vs Gastos (mensual)
  * Reputacion a lo largo del tiempo
  * Cantidad de cargas activas por dia
  * Margen promedio por ruta
  * Porcentaje de exito de cotizaciones
- Comparacion con estadisticas globales de otros jugadores (Steam leaderboards)
- Exportar estadisticas como CSV para los nerds de datos
```

### 3.5.4 Multijugador Asincronico
```
IDEA:
- No multijugador en tiempo real, sino asincronico:
  * Comparar tu rendimiento semanal con amigos de Steam
  * "Freight Forwarder of the Week" — ranking global
  * Desafios semanales compartidos: "Esta semana, la ruta mas rentable es Shanghai-Rotterdam. Quien obtiene el mayor margen?"
- Leaderboards por:
  * Mayor revenue en 30 dias
  * Mayor racha sin fallos
  * Mayor cantidad de ciudades desbloqueadas
```

---

## 3.6 TABLA COMPARATIVA: GODOT vs UNITY ACTUAL vs PLAN

| Feature | Godot (Existia) | Unity Actual (31 archivos) | Plan (PROJECT.md) | Ideas Nuevas |
|---|---|---|---|---|
| Ciudades | 20 | 10 | 50+ | Con estaciones visibles |
| Modos transporte | 3 (Mar/Air/Land) | 5 (enums, sin Rail logica) | 5 + Multimodal | + vehiculos propios |
| Pathfinding A* | Si (grilla 180x90) | No | Mencionado | Con rutas animadas 3D |
| AirNetwork/SeaNetwork | Si (Dijkstra) | No | Mencionado | Con hub tiers |
| Eventos mundiales | 12+ por calendario | No | Mencionado | + Crisis globales |
| Noticias ticker | Si (3 tipos) | No | Mencionado | + Radio adaptativa |
| GNews API | Si | No | No | Mantener |
| Llamadas a clientes | Si (3 estrategias) | No | No | Agregar |
| Feature unlocks | 7 niveles | No (XP sin efecto) | Mencionado | + Arbol de habilidades |
| Fallo diario cargo | Si (0.8% base) | No | No | Agregar |
| Pagos pendientes | Si (delay por tipo) | No | Mencionado | + Factoring |
| Agente experiencia ruta | Si (bonus) | Parcial (tracking sin bonus) | Mencionado | + Especializacion |
| Degradacion clientes | Si (cada 5 dias) | No | No | Agregar |
| Calma de enojo | Si (cada 60 dias) | No | No | Agregar |
| Land zones | Si | No | No | Agregar |
| Hub tiers | Si (S/A/B) | No | No | Agregar |
| City unlock tiers | Si (7 tiers) | Parcial (sin logica) | Mencionado | Con 50+ ciudades |
| Panel de clientes | Si (filtros+detalle) | No | Mencionado | + CRM completo |
| Negociacion en rondas | Si (counter-counter) | No (solo accept/reject) | Mencionado | + Tacticas |
| Modificadores temporales | Si (noticias) | No | No | + Clima + estaciones |
| Fuel price index | Si | No | Mencionado | + Fuel futures |
| Competidores AI | No | No | Si (3-5 rivales) | + Espionaje |
| Divisas | No | No | Si | Agregar |
| FRI (Freight Rate Index) | No | No | Si | Con grafico |
| Contratos largo plazo | No | No | Si | Agregar |
| Seguro detallado | Basico | HasInsurance bool | Si | 4 niveles |
| Tutorial | No | No | Si | Interactivo |
| Steam achievements | No | No | Si (15) | Agregar |
| Audio/FMOD | Basico | No | Si | Musica adaptativa |
| Almacenes | No | No | No | NUEVO |
| Subastas | No | No | No | NUEVO |
| Empleados | No | No | No | NUEVO |
| Reputacion por region | No | No | No | NUEVO |
| Oficina 3D | No | No | No | NUEVO |
| Cinematicas entrega | No | No | No | NUEVO |
| Logbook | No | No | No | NUEVO |
| Modo campana | No | No | No | NUEVO |
| Desafios semanales | No | No | No | NUEVO |
| Vehiculos propios | No | No | No | NUEVO |
| Steam Workshop mods | No | No | Mencionado | Agregar |
| Modo Hardcore | No | No | Mencionado | Agregar |
| Leaderboards | No | No | Mencionado | Agregar |

---

## 3.7 PRIORIDAD DE IMPLEMENTACION SUGERIDA

### Fase Inmediata (Critica — sin esto el juego no funciona bien)
1. **Land Zones** — Sin esto, se puede enviar un camion de Buenos Aires a Tokyo
2. **City Unlock Tiers** — Sin esto, las 10 ciudades estan siempre disponibles
3. **Pagos Pendientes** — Sin esto, todos los clientes pagan al contado (irreal)
4. **Fallo Diario de Cargas** — Sin esto, las cargas solo fallan por eventos explicitos
5. **Feature Unlocks por Nivel** — Sin esto, subir de nivel no tiene sentido

### Fase Corta (Mejora significativa del gameplay)
6. **Eventos Mundiales por Calendario** — Hace el juego dinamico y estacional
7. **NewsManager + Ticker** — Da vida y ambientacion al mundo
8. **Panel de Clientes** — El jugador necesita ver sus relaciones
9. **Degradacion de satisfaccion** — Sin esto, los clientes nunca se olvidan de vos
10. **Negociacion en rondas** — Hace la negociacion mas profunda

### Fase Media (Diferenciacion para Steam)
11. **AirNetwork + SeaNetwork** — Rutas realistas con escalas
12. **Hub Tiers** — Diferenciacion entre ciudades grandes y pequeñas
13. **Fuel Price Index** — Dinamismo economico
14. **Llamadas a Clientes** — Mecanica unica y divertida
15. **Vehiculos visibles en el globo** — Visual WOW factor
16. **Estaciones del ano** — Visual WOW factor

### Fase Avanzada (Para destacar en Steam)
17. **Competidores AI** — Tension y competencia
18. **Sistema de Seguros detallado** — Profundidad estrategica
19. **Documentacion y Aduanas** — Realismo
20. **Almacenes** — Logistica avanzada
21. **Empleados** — Gestion de personal
22. **Arbol de Habilidades** — Progresion significativa

### Fase Premium (Post-Launch / DLC)
23. **Modo Campana/Historia**
24. **Subastas de carga**
25. **Reputacion por region**
26. **Vehiculos propios**
27. **Espionaje comercial**
28. **Crisis globales dramaticas**
29. **Steam Workshop**
30. **Multijugador asincronico**

---

*Documento generado como complemento a FF-Pro_Documentacion_Completa.md*
*Analisis basado en: consolidated_code_FF_apto_IA.txt (Godot 4.3), PROJECT.md (Plan Unity), Devolveme+el+proyecto+funcionando.txt (GDD), FreightForwarder.txt (Reconstruccion), Termina+esto.txt (Master Plan)*
*Fecha: Mayo 2026*
