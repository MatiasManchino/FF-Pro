using UnityEngine;
using System.Collections.Generic;

public class GameUIPanel : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    private static GameUIPanel _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindAnyObjectByType<GameUIPanel>() != null) return;
        var go = new GameObject("GameUIPanel");
        go.AddComponent<GameUIPanel>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Message log ───────────────────────────────────────────────────────────
    private static readonly Queue<string> msgLog = new Queue<string>();
    private const int MAX_MSGS = 8;

    public static void Log(string msg)
    {
        string t = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentDateString() : "--";
        msgLog.Enqueue($"[{t}]  {msg}");
        while (msgLog.Count > MAX_MSGS) msgLog.Dequeue();
    }

    // ── Ticker ────────────────────────────────────────────────────────────────
    private float tickerX   = 0f;    // starts recalculated on first frame
    private bool  tickerInit = false;
    private const float TICKER_SPEED   = 90f;  // px/s
    private const float CHAR_WIDTH_EST = 7.5f;

    private string TickerString()
    {
        if (msgLog.Count == 0) return "Sin eventos recientes.";
        return string.Join("    ·    ", msgLog.ToArray());
    }

    // ── Section nav ───────────────────────────────────────────────────────────
    private enum Section { None, Mercado, Activas, Finanzas, Clientes, Agentes, Oficinas }
    private Section activeSection = Section.None;
    private Vector2 scrollMercado, scrollActivas, scrollClientes, scrollAgentes;

    // ── In-game pause menu ───────────────────────────────────────────────────
    private bool showInGameMenu = false;

    // ── Styles ────────────────────────────────────────────────────────────────
    private bool   stylesReady;
    private GUIStyle styleTopBg, styleSideBg, styleSectionBg, styleLogBg;
    private GUIStyle styleBold, styleLabel, styleSmall, styleSectionTitle;
    private GUIStyle styleSideBtn, styleSideBtnOn;
    private GUIStyle styleSpeedBtn, styleSpeedBtnOn;
    private GUIStyle styleMenuBtn;
    private GUIStyle styleLogEntry, styleLogLabel;
    private GUIStyle styleRow, styleRowAlt, styleHeaderRow;
    private GUIStyle styleOverlayPanel, styleOverlayTitle, styleOverlayBtn, styleOverlayBtnRed;

    // ── Layout ────────────────────────────────────────────────────────────────
    private const float TOP_H   = 54f;
    private const float SIDE_W  = 158f;
    private const float LOG_H   = 40f;
    private const float PANEL_W = 470f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        if (EventManager.Instance != null)
            EventManager.Instance.OnEventTriggered += (evt, cargo) =>
                Log($"Evento: {evt.Name} — {cargo.Name}");

        if (CargoManager.Instance != null)
        {
            CargoManager.Instance.OnCargoCompleted      += c => Log($"Carga completada: {c.Name}  +${c.FinalPrice:N0}");
            CargoManager.Instance.OnCargoFailed         += c => Log($"Carga fallida: {c.Name}");
            CargoManager.Instance.OnCargoAddedToMarket  += c =>
                Log($"Nueva cotización: {c.OriginCity?.Name ?? "?"} → {c.DestinationCity?.Name ?? "?"}  ${c.CargoValue:N0}");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewGameStarted += () => { msgLog.Clear(); Log("Nueva partida iniciada."); };
            GameManager.Instance.OnGamePaused     += () => Log("Juego pausado.");
            GameManager.Instance.OnGameResumed    += () => Log("Juego reanudado.");
        }
    }

    private void Update()
    {
        if (!IsVisible) return;

        string s    = TickerString();
        float  full = s.Length * CHAR_WIDTH_EST;
        float  area = Screen.width - SIDE_W - 90f;

        if (!tickerInit)
        {
            tickerX   = area;
            tickerInit = true;
        }

        tickerX -= TICKER_SPEED * Time.unscaledDeltaTime;
        if (tickerX < -full) tickerX = area;
    }

    private bool IsVisible =>
        GameManager.Instance != null &&
        (GameManager.Instance.CurrentState == GameState.Playing ||
         GameManager.Instance.CurrentState == GameState.Paused);

    // ── OnGUI ─────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        if (!IsVisible) return;
        if (!stylesReady) BuildStyles();

        DrawTopBar();
        DrawSideNav();
        DrawLog();
        if (activeSection != Section.None) DrawSectionPanel();
        if (showInGameMenu)                DrawInGameMenu();
    }

    // ── Top bar ───────────────────────────────────────────────────────────────
    private void DrawTopBar()
    {
        GUI.Box(new Rect(0, 0, Screen.width, TOP_H), GUIContent.none, styleTopBg);

        float x = 12f;
        GUI.Label(new Rect(x, 0, 220f, TOP_H), "Freight Forwarder Inc.", styleBold); x += 228f;
        Sep(x, 8f, TOP_H - 16f); x += 14f;

        string date = TimeManager.Instance?.GetCurrentDateString() ?? "--/--/----";
        GUI.Label(new Rect(x, 0, 178f, TOP_H), date, styleLabel); x += 185f;
        Sep(x, 8f, TOP_H - 16f); x += 14f;

        float money = EconomyManager.Instance?.CurrentMoney ?? 0f;
        GUI.Label(new Rect(x, 0, 145f, TOP_H), $"${money:N0}", styleLabel); x += 152f;
        Sep(x, 8f, TOP_H - 16f); x += 14f;

        float rep = EconomyManager.Instance?.CurrentReputation ?? 0f;
        GUI.Label(new Rect(x, 0, 105f, TOP_H), $"Rep {rep:0}%", styleLabel); x += 112f;
        Sep(x, 8f, TOP_H - 16f); x += 14f;

        int lvl = GetLevel(rep, EconomyManager.Instance?.CompletedCargos ?? 0);
        GUI.Label(new Rect(x, 0, 80f, TOP_H), $"Nivel {lvl}", styleLabel);

        // Controles velocidad (derecha)
        bool paused = GameManager.Instance?.IsPaused ?? false;
        float ts    = TimeManager.Instance?.TimeScale ?? 1f;
        float rx    = Screen.width - 12f;

        rx -= 76f;
        if (GUI.Button(new Rect(rx, 9f, 72f, TOP_H - 18f), "Menú", styleMenuBtn))
            showInGameMenu = !showInGameMenu;
        rx -= 14f; Sep(rx, 8f, TOP_H - 16f); rx -= 8f;

        foreach (var (speed, lbl) in new[] { (10f, "x10"), (5f, "x5"), (1f, "x1") })
        {
            bool on = !paused && Mathf.Approximately(ts, speed);
            rx -= 48f;
            if (GUI.Button(new Rect(rx, 9f, 44f, TOP_H - 18f), lbl, on ? styleSpeedBtnOn : styleSpeedBtn))
                SetSpeed(speed);
            rx -= 4f;
        }

        rx -= 46f;
        if (GUI.Button(new Rect(rx, 9f, 42f, TOP_H - 18f), "⏸", paused ? styleSpeedBtnOn : styleSpeedBtn))
        {
            if (paused) GameManager.Instance.ResumeGame();
            else        GameManager.Instance.PauseGame();
        }
    }

    private void Sep(float x, float y, float h)
    {
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        GUI.DrawTexture(new Rect(x, y, 2f, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ── Side nav ──────────────────────────────────────────────────────────────
    private void DrawSideNav()
    {
        float navH = Screen.height - TOP_H - LOG_H;
        GUI.Box(new Rect(0, TOP_H, SIDE_W, navH), GUIContent.none, styleSideBg);

        string[]  names    = { "Mercado", "Activas", "Finanzas", "Clientes", "Agentes", "Oficinas" };
        Section[] sections = { Section.Mercado, Section.Activas, Section.Finanzas,
                                Section.Clientes, Section.Agentes, Section.Oficinas };

        float y   = TOP_H + 10f;
        float btnH = 46f;
        for (int i = 0; i < names.Length; i++)
        {
            bool on = activeSection == sections[i];
            if (GUI.Button(new Rect(8f, y, SIDE_W - 16f, btnH), names[i], on ? styleSideBtnOn : styleSideBtn))
                activeSection = on ? Section.None : sections[i];
            y += btnH + 5f;
        }
    }

    // ── Ticker log ────────────────────────────────────────────────────────────
    private void DrawLog()
    {
        float y = Screen.height - LOG_H;
        GUI.Box(new Rect(0, y, Screen.width, LOG_H), GUIContent.none, styleLogBg);

        float labelW = 85f;
        float padX   = 6f;
        GUI.Label(new Rect(SIDE_W + padX, y + (LOG_H - 18f) / 2f, labelW, 18f), "Registro:", styleLogLabel);

        float clipX = SIDE_W + labelW + padX * 2f;
        float clipW = Screen.width - clipX - 8f;

        string s = TickerString();
        float  textW = s.Length * CHAR_WIDTH_EST + 60f;

        GUI.BeginClip(new Rect(clipX, y + 2f, clipW, LOG_H - 4f));
        GUI.Label(new Rect(tickerX, (LOG_H - 20f) / 2f, textW, 20f), s, styleLogEntry);
        GUI.EndClip();
    }

    // ── Section panel ─────────────────────────────────────────────────────────
    private void DrawSectionPanel()
    {
        float ph = Screen.height - TOP_H - LOG_H;
        GUI.Box(new Rect(SIDE_W, TOP_H, PANEL_W, ph), GUIContent.none, styleSectionBg);
        GUILayout.BeginArea(new Rect(SIDE_W + 8f, TOP_H + 8f, PANEL_W - 16f, ph - 16f));
        switch (activeSection)
        {
            case Section.Mercado:  DrawMercado();  break;
            case Section.Activas:  DrawActivas();  break;
            case Section.Finanzas: DrawFinanzas(); break;
            case Section.Clientes: DrawClientes(); break;
            case Section.Agentes:  DrawAgentes();  break;
            case Section.Oficinas: DrawOficinas(); break;
        }
        GUILayout.EndArea();
    }

    // ── In-game menu overlay ──────────────────────────────────────────────────
    private void DrawInGameMenu()
    {
        // Fondo oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 320f;
        float ph = 260f;
        float px = (Screen.width  - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, styleOverlayPanel);
        GUILayout.BeginArea(new Rect(px + 16f, py + 16f, pw - 32f, ph - 32f));

        GUILayout.Label("Menú de Juego", styleOverlayTitle);
        GUILayout.Space(12f);

        if (GUILayout.Button("Guardar Partida", styleOverlayBtn, GUILayout.Height(44f)))
        {
            GameManager.Instance?.SaveCurrentGame();
            Log("Partida guardada.");
            showInGameMenu = false;
        }
        GUILayout.Space(6f);

        if (GUILayout.Button("Volver al Menú Principal", styleOverlayBtnRed, GUILayout.Height(44f)))
        {
            showInGameMenu = false;
            GameManager.Instance?.LoadMainMenu();
        }
        GUILayout.Space(6f);

        if (GUILayout.Button("Cancelar", styleOverlayBtn, GUILayout.Height(38f)))
            showInGameMenu = false;

        GUILayout.EndArea();
    }

    // ── Mercado ───────────────────────────────────────────────────────────────
    private void DrawMercado()
    {
        GUILayout.Label("Mercado de Cargas", styleSectionTitle);
        GUILayout.Space(4f);
        if (CargoManager.Instance == null) { GUILayout.Label("No disponible.", styleLabel); return; }
        var list = CargoManager.Instance.MarketCargos;
        if (list.Count == 0) { GUILayout.Label("Sin cargas disponibles.", styleSmall); return; }

        GUILayout.BeginHorizontal(styleHeaderRow);
        GUILayout.Label("Ruta", styleSmall, GUILayout.Width(165f));
        GUILayout.Label("Tipo", styleSmall, GUILayout.Width(90f));
        GUILayout.Label("Valor", styleSmall, GUILayout.Width(75f));
        GUILayout.Label("Vence", styleSmall, GUILayout.Width(55f));
        GUILayout.EndHorizontal();

        int today = TimeManager.Instance?.GetTotalDays() ?? 0;
        scrollMercado = GUILayout.BeginScrollView(scrollMercado);
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            GUILayout.BeginHorizontal(i % 2 == 0 ? styleRow : styleRowAlt);
            GUILayout.Label($"{c.OriginCity?.Name ?? "?"} → {c.DestinationCity?.Name ?? "?"}", styleSmall, GUILayout.Width(165f));
            GUILayout.Label(c.CargoType.ToString(), styleSmall, GUILayout.Width(90f));
            GUILayout.Label($"${c.CargoValue:N0}", styleSmall, GUILayout.Width(75f));
            GUILayout.Label($"{c.ExpiryDay - today}d", styleSmall, GUILayout.Width(55f));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    // ── Activas ───────────────────────────────────────────────────────────────
    private void DrawActivas()
    {
        GUILayout.Label("Cargas en Tránsito", styleSectionTitle);
        GUILayout.Space(4f);
        if (CargoManager.Instance == null) { GUILayout.Label("No disponible.", styleLabel); return; }
        var list = CargoManager.Instance.ActiveCargos;
        if (list.Count == 0) { GUILayout.Label("Sin cargas activas.", styleSmall); return; }

        GUILayout.BeginHorizontal(styleHeaderRow);
        GUILayout.Label("Carga",       styleSmall, GUILayout.Width(120f));
        GUILayout.Label("Ruta",        styleSmall, GUILayout.Width(165f));
        GUILayout.Label("Transporte",  styleSmall, GUILayout.Width(85f));
        GUILayout.Label("Estado",      styleSmall, GUILayout.Width(75f));
        GUILayout.EndHorizontal();

        scrollActivas = GUILayout.BeginScrollView(scrollActivas);
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            GUILayout.BeginHorizontal(i % 2 == 0 ? styleRow : styleRowAlt);
            GUILayout.Label(c.Name, styleSmall, GUILayout.Width(120f));
            GUILayout.Label($"{c.OriginCity?.Name ?? "?"} → {c.DestinationCity?.Name ?? "?"}", styleSmall, GUILayout.Width(165f));
            GUILayout.Label(c.TransportMode.ToString(), styleSmall, GUILayout.Width(85f));
            GUILayout.Label(c.Status.ToString(), styleSmall, GUILayout.Width(75f));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    // ── Finanzas ──────────────────────────────────────────────────────────────
    private void DrawFinanzas()
    {
        GUILayout.Label("Finanzas", styleSectionTitle);
        GUILayout.Space(10f);
        if (EconomyManager.Instance == null) { GUILayout.Label("No disponible.", styleLabel); return; }
        var e = EconomyManager.Instance;
        StatRow("Capital actual:",      $"${e.CurrentMoney:N0}");
        StatRow("Total ingresos:",       $"${e.TotalEarned:N0}");
        StatRow("Total gastos:",         $"${e.TotalSpent:N0}");
        StatRow("Ganancia neta:",        $"${e.TotalEarned - e.TotalSpent:N0}");
        GUILayout.Space(8f);
        StatRow("Reputación:",           $"{e.CurrentReputation:0} / 100");
        StatRow("Cargas completadas:",   e.CompletedCargos.ToString());
        StatRow("Cargas fallidas:",      e.FailedCargos.ToString());
        int tot = e.CompletedCargos + e.FailedCargos;
        StatRow("Tasa de éxito:",        tot > 0 ? $"{(float)e.CompletedCargos / tot * 100f:0}%" : "—");
        GUILayout.Space(8f);
        StatRow("Nivel empresa:",        GetLevel(e.CurrentReputation, e.CompletedCargos).ToString());
    }

    private void StatRow(string lbl, string val)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(lbl, styleSmall, GUILayout.Width(175f));
        GUILayout.Label(val, styleBold,  GUILayout.Width(160f));
        GUILayout.EndHorizontal();
        GUILayout.Space(2f);
    }

    // ── Clientes ──────────────────────────────────────────────────────────────
    private void DrawClientes()
    {
        GUILayout.Label("Clientes", styleSectionTitle);
        GUILayout.Space(4f);
        if (ClientManager.Instance == null) { GUILayout.Label("No disponible.", styleLabel); return; }
        var list = ClientManager.Instance.AllClients;
        if (list.Count == 0) { GUILayout.Label("Sin clientes todavía.", styleSmall); return; }

        GUILayout.BeginHorizontal(styleHeaderRow);
        GUILayout.Label("Empresa",    styleSmall, GUILayout.Width(145f));
        GUILayout.Label("Tipo",       styleSmall, GUILayout.Width(95f));
        GUILayout.Label("Satisf.",    styleSmall, GUILayout.Width(60f));
        GUILayout.Label("Contratos",  styleSmall, GUILayout.Width(70f));
        GUILayout.EndHorizontal();

        scrollClientes = GUILayout.BeginScrollView(scrollClientes);
        for (int i = 0; i < list.Count; i++)
        {
            var cl = list[i];
            GUILayout.BeginHorizontal(i % 2 == 0 ? styleRow : styleRowAlt);
            GUILayout.Label(cl.CompanyName ?? cl.Name, styleSmall, GUILayout.Width(145f));
            GUILayout.Label(cl.ClientType.ToString(), styleSmall, GUILayout.Width(95f));
            GUILayout.Label($"{cl.CurrentSatisfaction * 100f:0}%", styleSmall, GUILayout.Width(60f));
            GUILayout.Label(cl.TotalContracts.ToString(), styleSmall, GUILayout.Width(70f));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    // ── Agentes ───────────────────────────────────────────────────────────────
    private void DrawAgentes()
    {
        GUILayout.Label("Agentes", styleSectionTitle);
        GUILayout.Space(4f);
        if (AgentManager.Instance == null) { GUILayout.Label("No disponible.", styleLabel); return; }
        var list = AgentManager.Instance.AllAgents;
        if (list.Count == 0) { GUILayout.Label("Sin agentes todavía.", styleSmall); return; }

        GUILayout.BeginHorizontal(styleHeaderRow);
        GUILayout.Label("Agente",      styleSmall, GUILayout.Width(130f));
        GUILayout.Label("Especialidad",styleSmall, GUILayout.Width(90f));
        GUILayout.Label("Rating",      styleSmall, GUILayout.Width(70f));
        GUILayout.Label("Confiab.",    styleSmall, GUILayout.Width(60f));
        GUILayout.Label("Estado",      styleSmall, GUILayout.Width(60f));
        GUILayout.EndHorizontal();

        scrollAgentes = GUILayout.BeginScrollView(scrollAgentes);
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            GUILayout.BeginHorizontal(i % 2 == 0 ? styleRow : styleRowAlt);
            GUILayout.Label(a.Name, styleSmall, GUILayout.Width(130f));
            GUILayout.Label(a.Specialization.ToString(), styleSmall, GUILayout.Width(90f));
            GUILayout.Label(a.Rating.ToString(), styleSmall, GUILayout.Width(70f));
            GUILayout.Label($"{a.Reliability * 100f:0}%", styleSmall, GUILayout.Width(60f));
            GUILayout.Label(a.IsAvailable ? "Libre" : "Ocupado", styleSmall, GUILayout.Width(60f));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    // ── Oficinas ──────────────────────────────────────────────────────────────
    private void DrawOficinas()
    {
        GUILayout.Label("Oficinas", styleSectionTitle);
        GUILayout.Space(12f);
        GUILayout.Label("Oficina Central — Buenos Aires", styleLabel);
        GUILayout.Space(6f);
        GUILayout.Label("Expansión de oficinas disponible próximamente.", styleSmall);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetSpeed(float speed)
    {
        if (GameManager.Instance == null || TimeManager.Instance == null) return;
        if (GameManager.Instance.IsPaused) GameManager.Instance.ResumeGame();
        TimeManager.Instance.SetTimeScale(speed);
    }

    private int GetLevel(float rep, int cargos)
    {
        if (cargos >= 100 || rep >= 90f) return 5;
        if (cargos >= 50  || rep >= 75f) return 4;
        if (cargos >= 20  || rep >= 55f) return 3;
        if (cargos >= 5   || rep >= 35f) return 2;
        return 1;
    }

    // ── Styles ────────────────────────────────────────────────────────────────
    private void BuildStyles()
    {
        stylesReady = true;
        Texture2D T(Color c) { var t = new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }

        styleTopBg     = new GUIStyle { normal = { background = T(new Color(0.09f,0.11f,0.16f,0.97f)) } };
        styleSideBg    = new GUIStyle { normal = { background = T(new Color(0.07f,0.09f,0.13f,0.97f)) } };
        styleSectionBg = new GUIStyle { normal = { background = T(new Color(0.08f,0.10f,0.15f,0.96f)) } };
        styleLogBg     = new GUIStyle { normal = { background = T(new Color(0.05f,0.07f,0.11f,0.97f)) } };

        styleBold = new GUIStyle(GUI.skin.label) {
            fontSize = 14, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }, alignment = TextAnchor.MiddleLeft };
        styleLabel = new GUIStyle(GUI.skin.label) {
            fontSize = 14,
            normal = { textColor = new Color(0.85f,0.92f,1.00f) }, alignment = TextAnchor.MiddleLeft };
        styleSmall = new GUIStyle(GUI.skin.label) {
            fontSize = 12,
            normal = { textColor = new Color(0.78f,0.85f,0.96f) }, alignment = TextAnchor.MiddleLeft };
        styleSectionTitle = new GUIStyle(GUI.skin.label) {
            fontSize = 16, fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.30f,0.70f,1.00f) }, alignment = TextAnchor.MiddleLeft };

        styleLogLabel = new GUIStyle(GUI.skin.label) {
            fontSize = 13, fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.70f,0.80f,1.00f) }, alignment = TextAnchor.MiddleLeft };
        styleLogEntry = new GUIStyle(GUI.skin.label) {
            fontSize = 12,
            normal = { textColor = new Color(0.70f,0.92f,0.70f) }, alignment = TextAnchor.MiddleLeft };

        Color sNorm = new Color(0.16f,0.20f,0.28f,0.95f);
        Color sOn   = new Color(0.22f,0.50f,0.88f,1.00f);
        styleSideBtn = new GUIStyle(GUI.skin.button) {
            fontSize = 14, alignment = TextAnchor.MiddleLeft,
            normal  = { background = T(sNorm), textColor = new Color(0.80f,0.88f,1.00f) },
            hover   = { background = T(new Color(0.22f,0.27f,0.38f)), textColor = Color.white },
            padding = new RectOffset(14,4,0,0) };
        styleSideBtnOn = new GUIStyle(styleSideBtn) {
            fontStyle = FontStyle.Bold,
            normal = { background = T(sOn), textColor = Color.white } };

        Color spNorm = new Color(0.18f,0.22f,0.32f,0.95f);
        Color spOn   = new Color(0.20f,0.52f,0.90f,1.00f);
        styleSpeedBtn = new GUIStyle(GUI.skin.button) {
            fontSize = 13,
            normal = { background = T(spNorm), textColor = new Color(0.80f,0.90f,1.00f) },
            hover  = { background = T(new Color(0.25f,0.30f,0.42f)), textColor = Color.white } };
        styleSpeedBtnOn = new GUIStyle(styleSpeedBtn) {
            fontStyle = FontStyle.Bold,
            normal = { background = T(spOn), textColor = Color.white } };

        styleMenuBtn = new GUIStyle(GUI.skin.button) {
            fontSize = 13,
            normal = { background = T(new Color(0.22f,0.27f,0.38f)), textColor = Color.white },
            hover  = { background = T(new Color(0.30f,0.36f,0.50f)), textColor = Color.white } };

        styleRow = new GUIStyle {
            normal  = { background = T(new Color(0.14f,0.17f,0.24f,0.90f)) },
            padding = new RectOffset(6,4,3,3) };
        styleRowAlt = new GUIStyle {
            normal  = { background = T(new Color(0.10f,0.13f,0.19f,0.90f)) },
            padding = new RectOffset(6,4,3,3) };
        styleHeaderRow = new GUIStyle {
            normal  = { background = T(new Color(0.18f,0.22f,0.32f,1.00f)) },
            padding = new RectOffset(6,4,4,4) };

        // Overlay (in-game menu)
        styleOverlayPanel = new GUIStyle {
            normal = { background = T(new Color(0.08f,0.10f,0.16f,0.98f)) } };
        styleOverlayTitle = new GUIStyle(GUI.skin.label) {
            fontSize = 18, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
        styleOverlayBtn = new GUIStyle(GUI.skin.button) {
            fontSize = 15,
            normal = { background = T(new Color(0.18f,0.22f,0.32f)), textColor = Color.white },
            hover  = { background = T(new Color(0.28f,0.34f,0.48f)), textColor = Color.white } };
        styleOverlayBtnRed = new GUIStyle(styleOverlayBtn) {
            normal = { background = T(new Color(0.52f,0.13f,0.13f)), textColor = Color.white },
            hover  = { background = T(new Color(0.70f,0.18f,0.18f)), textColor = Color.white } };
    }
}
