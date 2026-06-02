import os
import re
from pathlib import Path

BAD_COMMENT_PATTERNS = [
    r"^se ejecuta cuando",
    r"^realiza",
    r"^configura",
    r"^inicializa",
    r"^devuelve",
    r"^indica si",
    r"^se invoca cuando",
    r"^se dispara cuando",
    r"^se activa cuando",
    r"^returns?",
    r"^initiali[sz]e",
    r"^set",
    r"^get",
    r"^makes?",
    r"^creates?",
    r"^adds?",
    r"^removes?",
    r"^is ",
    r"^has ",
    r"^can ",
    r"^should",
    r"^maneja ",
    r"^requires?",
    r"^parent",
    r"^wait",
    r"^note",
    r"^styles",
    r"^standard",
    r"^total width",
    r"^handles?",
    r"^generates?",
    r"^handle",
    r"\bensure\b",
    r"\bnotification(s)?\b",
    r"\bsubscribe\b",
    r"\bevents?\b",
    r"\bpanel\b",
    r"\bui\b",
    r"\bbuild\b",
    r"\bbutton\b",
    r"\bposition\b",
    r"\bcamera\b",
]

COMMON_TOKEN_MAP = {
    "cargo": "cargamento",
    "client": "cliente",
    "clients": "clientes",
    "agent": "agente",
    "agents": "agentes",
    "money": "dinero",
    "rep": "reputación",
    "reputation": "reputación",
    "risk": "riesgo",
    "level": "nivel",
    "player": "jugador",
    "game": "juego",
    "world": "mundo",
    "map": "mapa",
    "gameover": "Game Over",
    "gameOver": "Game Over",
    "over": "terminado",
    "complete": "completo",
    "completed": "completado",
    "accept": "aceptado",
    "accepted": "aceptado",
    "blacklisted": "lista negra",
    "tier": "categoría",
    "route": "ruta",
    "routes": "rutas",
    "day": "día",
    "days": "días",
    "time": "tiempo",
    "scale": "escala",
    "speed": "velocidad",
    "state": "estado",
    "current": "actual",
    "desired": "deseado",
    "initialize": "inicializa",
    "init": "inicializa",
    "destroy": "destruye",
    "create": "crea",
    "build": "construye",
    "permanent": "permanente",
    "follow": "seguimiento",
    "daily": "diario",
    "updates": "actualizaciones",
    "update": "actualiza",
    "expired": "expirado",
    "expire": "expira",
    "delivery": "entrega",
    "ship": "barco",
    "port": "puerto",
    "position": "posición",
    "direction": "dirección",
    "origin": "origen",
    "destination": "destino",
    "region": "región",
    "data": "datos",
    "history": "historial",
    "list": "lista",
    "price": "precio",
    "amount": "cantidad",
    "status": "estado",
    "open": "abre",
    "close": "cierra",
    "show": "muestra",
    "hide": "oculta",
    "select": "selecciona",
    "populate": "llena",
    "send": "envía",
    "receive": "recibe",
    "reset": "reinicia",
    "record": "registra",
    "convert": "convierte",
    "sync": "sincroniza",
    "query": "consulta",
    "request": "solicita",
    "response": "respuesta",
    "message": "mensaje",
    "text": "texto",
    "image": "imagen",
    "button": "botón",
    "layout": "diseño",
    "quality": "calidad",
    "event": "evento",
    "events": "eventos",
    "subscribe": "registra",
    "ui": "UI",
    "load": "carga",
    "save": "guarda",
    "compute": "calcula",
    "calculate": "calcula",
    "is": "indica si",
    "has": "determina si tiene",
    "can": "comprueba si puede",
    "should": "determina si debe",
    "trigger": "dispara",
    "resume": "reanuda",
    "pause": "pausa",
    "notify": "notifica",
    "enable": "habilita",
    "disable": "deshabilita",
    "spawn": "genera",
    "schedule": "programa",
    "handle": "maneja",
    "refresh": "actualiza",
    "state": "estado",
    "current": "actual",
    "desired": "deseado",
    "weather": "clima",
    "grid": "rejilla",
    "ship": "barco",
    "camera": "cámara",
    "collider": "colisionador",
    "raycast": "raycast",
    "detect": "detecta",
    "sun": "sol",
    "controller": "controlador",
    "manager": "gestor",
    "lat": "latitud",
    "lon": "longitud",
    "position": "posición",
    "distance": "distancia",
    "direction": "dirección",
    "region": "región",
    "target": "objetivo",
    "source": "origen",
    "destination": "destino",
    "history": "historial",
    "path": "ruta",
    "order": "orden",
    "report": "informe",
    "status": "estado",
    "value": "valor",
    "price": "precio",
    "quote": "cotización",
    "offer": "oferta",
    "market": "mercado",
    "quality": "calidad",
    "start": "inicio",
    "awake": "inicio",
    "update": "actualiza",
    "fixed": "físico",
    "late": "tardío",
    "enable": "habilita",
    "disable": "deshabilita",
    "payment": "pago",
    "received": "recibido",
    "published": "publicado",
    "triggered": "activado",
    "added": "agregado",
    "expired": "expirado",
    "failed": "fallado",
    "delivered": "entregado",
    "news": "noticias",
    "route": "ruta",
    "routes": "rutas",
    "focus": "enfoque",
    "normalize": "normaliza",
    "angle": "ángulo",
    "lock": "bloqueo",
    "release": "libera",
    "drag": "arrastre",
    "zoom": "zoom",
    "mouse": "ratón",
    "keyboard": "teclado",
    "editor": "editor",
    "place": "ubicación",
    "terrain": "terreno",
    "mask": "máscara",
    "verify": "verifica",
    "clear": "borra",
    "export": "exporta",
    "print": "imprime",
    "label": "etiqueta",
    "shader": "sombreado",
    "cloud": "nube",
    "weather": "clima",
    "cell": "celda",
    "heuristic": "heurística",
    "enqueue": "encola",
    "dequeue": "desencola",
    "key": "clave",
    "width": "ancho",
    "height": "alto",
    "fuel": "combustible",
    "demand": "demanda",
    "month": "mes",
    "tools": "herramientas",
    "btn": "botón",
    "click": "clic",
    "application": "aplicación",
    "quit": "termina",
    "xp": "experiencia",
    "gained": "ganado",
    "result": "resultado",
    "back": "regreso",
    "new": "nuevo",
    "event": "evento",
    "events": "eventos",
    "spline": "trayectoria",
    "sector": "sector",
    "traffic": "tráfico",
    "distance": "distancia",
    "angle": "ángulo",
    "smooth": "suaviza",
    "apply": "aplica",
    "focus": "enfoque",
    "route": "ruta",
    "navigation": "navegación",
    "search": "búsqueda",
    "query": "consulta",
}

EVENT_PHRASE_MAP = {
    "OnMoneyChanged": "Se invoca cuando cambia el dinero.",
    "OnRepChanged": "Se invoca cuando cambia la reputación.",
    "OnLevelUp": "Se invoca cuando el jugador sube de nivel.",
    "OnGameOver": "Se invoca cuando el juego termina.",
    "OnCargoCompleted": "Se invoca cuando un cargamento se completa.",
    "OnCargoAccepted": "Se invoca cuando un cargamento es aceptado.",
    "OnClientBlacklisted": "Se invoca cuando un cliente es puesto en lista negra.",
    "OnClientTierUp": "Se invoca cuando un cliente asciende de categoría.",
    "OnDayPassed": "Se invoca al terminar un día de juego.",
    "OnPlayerLevelUp": "Se invoca cuando el jugador sube de nivel.",
    "OnGamePaused": "Se invoca cuando el juego se pausa.",
    "OnGameResumed": "Se invoca cuando el juego se reanuda.",
    "OnDayStarted": "Se invoca al comenzar un nuevo día.",
    "OnCargoDelivered": "Se invoca cuando un cargamento se entrega.",
    "OnCargoFailed": "Se invoca cuando un cargamento falla.",
    "OnWeatherChanged": "Se invoca cuando cambia el clima.",
    "OnEventTriggered": "Se invoca cuando se activa un evento.",
    "OnApplicationQuit": "Se invoca cuando la aplicación se cierra.",
    "OnXPGained": "Se invoca cuando el jugador gana experiencia.",
    "OnResultBack": "Se invoca cuando regresa el resultado.",
    "OnToolsRouteBtnClick": "Se invoca cuando se pulsa el botón de ruta de herramientas.",
}

METHOD_PREFIX_RESPONSES = {
    "On": "Se invoca cuando",
    "Get": "Obtiene",
    "Set": "Establece",
    "Add": "Agrega",
    "Remove": "Elimina",
    "Create": "Crea",
    "Build": "Construye",
    "Subscribe": "Registra",
    "Ensure": "Asegura",
    "Start": "Inicia",
    "Process": "Procesa",
    "Check": "Verifica",
    "Validate": "Valida",
    "Register": "Registra",
    "Generate": "Genera",
    "Open": "Abre",
    "Close": "Cierra",
    "Show": "Muestra",
    "Hide": "Oculta",
    "Select": "Selecciona",
    "Populate": "Llena",
    "Send": "Envía",
    "Receive": "Recibe",
    "Reset": "Reinicia",
    "Create": "Crea",
    "Build": "Construye",
    "Subscribe": "Registra",
    "Init": "Inicializa",
    "Initialize": "Inicializa",
    "Update": "Actualiza",
    "Compute": "Calcula",
    "Calculate": "Calcula",
    "Load": "Carga",
    "Save": "Guarda",
    "Trigger": "Dispara",
    "Is": "Indica si",
    "Has": "Determina si tiene",
    "Can": "Comprueba si puede",
    "Should": "Determina si debe",
    "Resume": "Reanuda",
    "Pause": "Pausa",
    "Handle": "Gestiona",
    "Refresh": "Actualiza",
    "Toggle": "Alterna",
    "Spawn": "Genera",
}

SKIP_DIRS = {"Library", "Packages", "Temp", "UserSettings", "Logs", ".git"}

CAMEL_SPLIT_RE = re.compile(r"(?<!^)(?=[A-Z0-9])")
DECLARATION_RE = re.compile(
    r"^\s*(?:public|private|protected|internal|static|virtual|override|async|sealed|extern|unsafe|new|partial|readonly|const|volatile|\s)+"
    r"[\w<>,\[\]]+\s+\w+\s*(?:\(|\{)"
)

PROPERTY_RE = re.compile(
    r"^\s*(?:public|private|protected|internal|static|virtual|override|sealed|extern|unsafe|new|partial|readonly|const|volatile|\s)+"
    r"[\w<>,\[\]]+\s+\w+\s*{"
)

CLASS_RE = re.compile(
    r"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|unsafe|new|extern|\s)*(?:class|struct|interface)\s+\w+"
)

COMMENT_CLEAN_RE = re.compile(r"^//\s*")


def list_cs_files(root: Path):
    for path in root.rglob("*.cs"):
        if any(part in SKIP_DIRS for part in path.parts):
            continue
        yield path


def clean_comment(comment: str) -> str:
    return COMMENT_CLEAN_RE.sub("", comment).strip()

COMMENT_TRANSLATION_MAP = {
    **COMMON_TOKEN_MAP,
    "requires": "requiere",
    "require": "requiere",
    "parent": "padre",
    "after": "después de",
    "before": "antes de",
    "placing": "colocar",
    "placed": "colocado",
    "place": "colocar",
    "space": "espacio",
    "in": "en",
    "for": "para",
    "so": "para que",
    "correctly": "correctamente",
    "properly": "correctamente",
    "wait": "espera",
    "waits": "espera",
    "frame": "fotograma",
    "frames": "fotogramas",
    "left": "izquierdo",
    "right": "derecho",
    "bottom": "inferior",
    "top": "superior",
    "can": "puede",
    "will": "se",
    "must": "debe",
    "create": "crea",
    "created": "creado",
    "styles": "estilos",
    "standard": "estándar",
    "day-length": "duración del día",
    "formula": "fórmula",
    "approximation": "aproximación",
    "note": "nota",
    "fires": "se ejecuta",
    "computed": "calculado",
    "astronomical": "astronómico",
    "cached": "almacenado en caché",
    "cache": "caché",
    "textures": "texturas",
    "texture": "textura",
    "button": "botón",
    "buttons": "botones",
    "click": "clic",
    "mouse": "ratón",
    "key": "clave",
    "layout": "diseño",
    "unity": "Unity",
    "world": "mundo",
    "city": "ciudad",
    "route": "ruta",
    "manager": "gestor",
    "event": "evento",
    "events": "eventos",
    "trigger": "activa",
    "triggered": "activado",
    "actual": "actual",
    "start": "Start",
    "during": "durante",
    "their": "su",
    "it": "lo",
    "detect": "detecta",
    "detects": "detecta",
    "detecting": "detectando",
    "collider": "colisionador",
    "raycast": "raycast",
    "background": "fondo",
    "dark": "oscuro",
    "semi-transparent": "semi-transparente",
    "white": "blanco",
    "text": "texto",
    "button": "botón",
    "buttons": "botones",
    "scale": "escala",
    "use": "usa",
    "uses": "usa",
    "during": "durante",
    "here": "aquí",
    "must": "debe",
    "created": "creado",
    "inside": "dentro",
    "their": "su",
    "Start": "Start",
    "OnGUI": "OnGUI",
}

SPECIAL_COMMENT_PHRASES = {
    "can detect it": "puede detectarlo",
    "can detect": "puede detectar",
    "must be created inside ongui": "debe crearse dentro de OnGUI",
    "button layout": "diseño de botones",
    "dark semi-transparent background": "fondo oscuro semi-transparente",
    "white text — bottom left": "texto blanco — esquina inferior izquierda",
    "during their start()": "durante su Start()",
    "scale here so citymarkers can parent correctly during their start()": "escala aquí para que los CityMarker se puedan anexar correctamente durante su Start()",
    "parent after placing in world space so unity converts correctly": "se asigna como padre después de colocarlo en el espacio del mundo para que Unity convierta correctamente",
}

COMMENT_WORD_RE = re.compile(r"\b[A-Za-z0-9']+\b")


def translate_comment_text(comment: str) -> str:
    normalized = comment
    normalized = re.sub(r"\b([A-Za-z_][A-Za-z0-9_]*)'s\b", r"de \1", normalized)
    normalized_lower = normalized.lower()
    for phrase, replacement in SPECIAL_COMMENT_PHRASES.items():
        normalized = re.sub(re.escape(phrase), replacement, normalized, flags=re.IGNORECASE)

    def repl(match: re.Match) -> str:
        word = match.group(0)
        translated = COMMENT_TRANSLATION_MAP.get(word.lower())
        if not translated:
            return word
        if word.isupper():
            return translated.upper()
        if word[0].isupper():
            return translated.capitalize()
        return translated

    return COMMENT_WORD_RE.sub(repl, normalized)


def needs_translation(comment: str) -> bool:
    comment = comment.lower()
    return bool(
        re.search(
            r"\b(?:requires?|parent|after|before|wait|waits|note|styles|standard|total width|handle|handles|handles|handles|generates?|ensure|button|buttons|click|mouse|texture|textures|shader|cached|cache|unit|fires|computed|approximation|formula|triggered|trigger|city|route|manager|application|quit|received|published|result|back|frame)\b",
            comment,
        )
    )


def bad_comment(comment: str) -> bool:
    comment = clean_comment(comment).lower()
    return any(re.search(pattern, comment) for pattern in BAD_COMMENT_PATTERNS)


def split_camel(name: str) -> list[str]:
    parts = re.findall(r"[A-Z](?:[a-z]+|[A-Z]*(?=[A-Z]|$))|[0-9]+|[a-z]+", name)
    return [part for part in parts if part]


def translate_token(token: str) -> str:
    key = token.lower()
    return COMMON_TOKEN_MAP.get(key, token.lower())


def join_tokens(tokens: list[str]) -> str:
    translated = [translate_token(t) for t in tokens]
    return " ".join(translated)


def build_event_comment(method_name: str) -> str:
    if method_name in EVENT_PHRASE_MAP:
        return EVENT_PHRASE_MAP[method_name]
    internal = method_name[2:]
    if internal.endswith("Changed"):
        subject = split_camel(internal[:-7])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando cambia {subject_text}."
    if internal.endswith("Completed"):
        subject = split_camel(internal[:-9])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} se completa."
    if internal.endswith("Accepted"):
        subject = split_camel(internal[:-8])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} es aceptado."
    if internal.endswith("Blacklisted"):
        subject = split_camel(internal[:-11])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} se pone en lista negra."
    if internal.endswith("Passed"):
        subject = split_camel(internal[:-6])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} transcurre."
    if internal.endswith("Up"):
        subject = split_camel(internal[:-2])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} sube de nivel."
    if internal.endswith("Received"):
        subject = split_camel(internal[:-8])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando se recibe {subject_text}."
    if internal.endswith("Published"):
        subject = split_camel(internal[:-9])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} se publica."
    if internal.endswith("Added"):
        subject = split_camel(internal[:-5])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando se agrega {subject_text}."
    if internal.endswith("Expired"):
        subject = split_camel(internal[:-7])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} expira."
    if internal.endswith("Failed"):
        subject = split_camel(internal[:-6])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando {subject_text} falla."
    if internal.endswith("Triggered"):
        subject = split_camel(internal[:-9])
        subject_text = join_tokens(subject)
        return f"Se invoca cuando se activa {subject_text}."
    return f"Se invoca cuando ocurre {join_tokens(split_camel(internal))}."


def build_comment(method_name: str) -> str:
    if method_name in EVENT_PHRASE_MAP:
        return EVENT_PHRASE_MAP[method_name]
    if method_name == "Awake":
        return "Se ejecuta durante Awake al iniciar el componente."
    if method_name == "Start":
        return "Se ejecuta al iniciar el componente."
    if method_name == "Update":
        return "Se ejecuta en cada frame."
    if method_name == "LateUpdate":
        return "Se ejecuta al final de cada frame."
    if method_name == "FixedUpdate":
        return "Se ejecuta en cada ciclo de física."
    if method_name == "OnEnable":
        return "Se invoca cuando el componente se habilita."
    if method_name == "OnDisable":
        return "Se invoca cuando el componente se deshabilita."
    for prefix, phrase in METHOD_PREFIX_RESPONSES.items():
        if method_name.startswith(prefix) and len(method_name) > len(prefix):
            rest = method_name[len(prefix):]
            if prefix == "On":
                return build_event_comment(method_name)
            if prefix in {"Is", "Has", "Can", "Should"}:
                return f"{phrase} {join_tokens(split_camel(rest))}."
            if prefix == "Get" and rest.lower() == "instance":
                return "Obtiene la instancia actual."
            if prefix in {"Set", "Add", "Remove", "Create", "Destroy", "Update", "Compute", "Calculate", "Load", "Save", "Trigger", "Resume", "Pause", "Handle", "Refresh", "Toggle", "Spawn", "Build", "Subscribe", "Generate", "Open", "Close", "Show", "Hide", "Select", "Populate", "Send", "Receive", "Reset", "Register", "Check", "Validate"}:
                return f"{phrase} {join_tokens(split_camel(rest))}."
            if prefix in {"Init", "Initialize"}:
                return f"Inicializa {join_tokens(split_camel(rest))}."
    tokens = split_camel(method_name)
    if tokens:
        first = tokens[0].lower()
        if first in COMMON_TOKEN_MAP:
            verb = translate_token(first).capitalize()
            rest = join_tokens(tokens[1:])
            if rest:
                return f"{verb} {rest}."
            return f"{verb}."
        return f"Gestiona {join_tokens(tokens)}."
    return "Gestiona la operación."


def process_file(path: Path) -> tuple[int, int]:
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    changed = 0
    for i, line in enumerate(lines):
        stripped = line.strip()
        if not stripped.startswith("//"):
            continue
        comment_text = clean_comment(stripped)
        indent = line[: len(line) - len(line.lstrip())]
        j = i + 1
        while j < len(lines) and not lines[j].strip():
            j += 1
        next_line = lines[j].strip() if j < len(lines) else ""
        if bad_comment(stripped) and next_line and not next_line.startswith("//") and (
            DECLARATION_RE.match(next_line)
            or PROPERTY_RE.match(next_line)
            or CLASS_RE.match(next_line)
        ):
            method_name = extract_name_from_declaration(next_line)
            if method_name:
                new_comment = build_comment(method_name)
                if new_comment and comment_text.lower() != new_comment.lower():
                    lines[i] = f"{indent}// {new_comment}"
                    changed += 1
                    continue
        if needs_translation(comment_text):
            translated = translate_comment_text(comment_text)
            if translated and comment_text.lower() != translated.lower():
                lines[i] = f"{indent}// {translated}"
                changed += 1
    if changed:
        path.write_text("\n".join(lines) + ("\n" if text.endswith("\n") else ""), encoding="utf-8")
    return changed, len(lines)


def extract_name_from_declaration(declaration_line: str) -> str | None:
    # method declaration
    method_match = re.search(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(", declaration_line)
    if method_match:
        return method_match.group(1)
    # property declaration
    prop_match = re.search(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*{", declaration_line)
    if prop_match:
        return prop_match.group(1)
    return None


def main() -> None:
    root = Path(__file__).parent
    total_files = 0
    total_changes = 0
    for path in list_cs_files(root):
        updated, _ = process_file(path)
        if updated:
            total_files += 1
            total_changes += updated
            print(f"Updated {updated} comments in {path}")
    print(f"Processed {total_files} files, updated {total_changes} comments.")


if __name__ == "__main__":
    main()
