using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Map;
using FreightForwarder.Models;
using FreightForwarder.Systems.Maritime;
using FreightForwarder.Utils;
using static FreightForwarder.Models.Constants;
using UnityEngine;
using UnityEngine.UI;

namespace FreightForwarder.UI
{
    public class FFUIManager : Singleton<FFUIManager>
    {
// Gestiona panel.
        private enum Panel { None = 0, Market, ActiveCargos, Agents, Clients, Finances, Offices, Events }
        private Panel _active = Panel.None;
        private bool  _menuOpen;

        private MapCameraController _camController;
        private MarketPanel         _marketPanel;

// Evento log.
        private struct EventLog { public string Text; public Color Color; public int Day; }
        private readonly List<EventLog> _eventLog = new List<EventLog>();
        private const int MAX_EVENT_LOG = 30;

        // ── Layout ────────────────────────────────────────────────────────────
        public  const float SIDEBAR_W  = 62f;
        private const float TOP_H      = 38f;
        private const float TICKER_H   = 28f;
        private const float BTN_H      = 48f;
        private const float PANEL_X    = SIDEBAR_W + 6f;
        private const float PANEL_Y    = TOP_H + 8f;
        private const float PANEL_W    = 340f;
        private const float PANEL_MAX_H = 620f;

        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color C_BG_DARK  = new Color(0f, 0.03f, 0.08f, 0.97f);
        private static readonly Color C_BG_PANEL = new Color(0f, 0.04f, 0.10f, 0.96f);
        private static readonly Color C_BTN_OFF  = new Color(0.10f, 0.12f, 0.15f, 0.92f);
        private static readonly Color C_BTN_ON   = new Color(0.10f, 0.35f, 0.80f, 0.95f);
        private static readonly Color C_ACCENT   = new Color(0.20f, 0.50f, 1.00f, 0.50f);
        private static readonly Color C_GREY     = new Color(0.75f, 0.75f, 0.75f, 1.00f);

        // ── UGUI refs ─────────────────────────────────────────────────────────
        private Text    _moneyText, _repText, _dateText;
        private Image[] _speedBgs;
        private Image   _menuBg, _lockBg;
        private Text    _menuStatus;
        private GameObject _menuPopupGO;

        private Image[]    _navBgs;
        private Text       _badgeText;
        private GameObject _badgeGO;

        private static readonly Panel[] SIDE_PANELS = {
            Panel.Market, Panel.ActiveCargos, Panel.Agents,
            Panel.Clients, Panel.Finances, Panel.Offices, Panel.Events
        };
        private static readonly Panel[] CONTENT_PANELS = {
            Panel.ActiveCargos, Panel.Agents, Panel.Clients,
            Panel.Finances, Panel.Offices, Panel.Events
        };
        private GameObject[]    _panelGOs;
        private RectTransform[] _scrollContents;
        private Text[]          _panelHeaders;

        // Finances fixed refs
        private Text  _finMoney, _finRep, _finLevel, _finStats, _finRecvHeader, _finRecv;
        private Image _finRepFill, _finXpFill;

        private static Font _fontCache;
// Font
        private static Font _font => _fontCache != null ? _fontCache : (_fontCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // ── TopBar dirty-check caché ──────────────────────────────────────────
        private int  _tbMoney    = int.MinValue;
        private int  _tbRep      = int.MinValue;
        private int  _tbDay      = -1;
        private int  _tbSpeed    = -1;
        private bool _tbLocked   = false;
        private bool _tbMenu     = false;
        private int  _lastDayMoney;
        private int  _tbMinute   = -1;

        // Stored delegates so lambdas can be unsubscribed from EconomyManager events
        private Action<int> _onMoneyChanged;
        private Action<int> _onRepChanged;

        // Se ejecuta durante Awake al iniciar el componente.

        protected override void OnAwake() { BuildUI(); }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            _marketPanel   = FindAnyObjectByType<MarketPanel>();
            _camController = FindAnyObjectByType<MapCameraController>();

            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnMinuteChanged += OnGameMinuteChanged;   // reloj en vivo (no depende de Update)
            if (CargoManager.Instance != null)
                CargoManager.Instance.OnCargoAccepted += OnCargoAcceptedFocus; // cámara → origen al aceptar
            if (EconomyManager.Instance != null)
            {
                _onMoneyChanged = _ => { RefreshTopBar(); RefreshFinancesIfVisible(); };
                _onRepChanged   = _ => { RefreshTopBar(); RefreshFinancesIfVisible(); };
                EconomyManager.Instance.OnMoneyChanged      += _onMoneyChanged;
                EconomyManager.Instance.OnReputationChanged += _onRepChanged;
            }
            _lastDayMoney = EconomyManager.Instance?.Money ?? 0;
            RefreshTopBar();   // pintura inicial — no depende de que corra Update
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnMinuteChanged -= OnGameMinuteChanged;
            if (CargoManager.Instance != null)
                CargoManager.Instance.OnCargoAccepted -= OnCargoAcceptedFocus;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged      -= _onMoneyChanged;
                EconomyManager.Instance.OnReputationChanged -= _onRepChanged;
            }
        }

        // La cámara va al ORIGEN del transporte cuando se cierra (acepta) una carga.
        private void OnCargoAcceptedFocus(Cargo c)
        {
            if (_camController == null || c == null) return;
            var city = CityDatabase.GetCity(c.OriginCityId);
            if (city != null) _camController.FocusOnCity(city.Latitude, city.Longitude);
        }

        // Al tocar una carga EN TRÁNSITO, la cámara se va al vehículo y lo sigue en el mapa.
        private void FocusActiveCargoVehicle(Cargo c)
        {
            if (_camController == null || c == null) return;
            Transform veh = null;
            if (c.TransportMode == TransportMode.Maritime)
            {
                if (ShipMarker.TryGetMarker(c.Id, out var sm) && sm != null) veh = sm.transform;
            }
            else if (TransportMarkerManager.Instance != null &&
                     TransportMarkerManager.Instance.TryGetMarker(c.Id, out var tm) && tm != null)
            {
                veh = tm.transform;
            }

            if (veh != null)
            {
                _camController.FollowTransform(veh);
            }
            else
            {
                // El vehículo aún no existe en el mapa: enfocar destino/origen como respaldo.
                var city = CityDatabase.GetCity(c.DestinationCityId) ?? CityDatabase.GetCity(c.OriginCityId);
                if (city != null) _camController.FocusOnCity(city.Latitude, city.Longitude);
            }
        }

        // Reloj en vivo del top bar, disparado por TimeManager cada minuto de juego.
        private void OnGameMinuteChanged() => RefreshTopBar();

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_menuOpen) ToggleMenu();
                else if (_active != Panel.None) SetPanel(Panel.None);
            }
            RefreshTopBar();
        }

        // Construye UI.

        private void BuildUI()
        {
            var canvas = GetOrCreateCanvas();
            BuildTopBar(canvas);
            BuildSidebar(canvas);
            BuildPanels(canvas);
            BuildMenuPopup(canvas);
        }

// Construye top bar.
        private void BuildTopBar(RectTransform c)
        {
            var bar = MakeRect("TopBar", c,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), Vector2.zero, new Vector2(0, TOP_H));
            MakeImg(bar, C_BG_DARK);
            var acc = MakeRect("TopAccent", bar,
                new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), Vector2.zero, new Vector2(0, 1f));
            MakeImg(acc, C_ACCENT);

            float lh = TOP_H - 8f;
            float ly = -((TOP_H - lh) * 0.5f);   // centrado vertical dentro del marco (no pegado al borde)
            _moneyText = MakeTxtPos("Money", bar, new Vector2(SIDEBAR_W+8f, ly), new Vector2(150f, lh),
                "$0", 12, FontStyle.Bold, new Color(0.2f,1f,0.4f), TextAnchor.MiddleLeft);
            _repText   = MakeTxtPos("Rep", bar, new Vector2(SIDEBAR_W+164f, ly), new Vector2(110f, lh),
                "⭐ 50", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            _dateText  = MakeTxtPos("Date", bar, new Vector2(SIDEBAR_W+280f, ly), new Vector2(220f, lh),
                "Día 0", 11, FontStyle.Normal, C_GREY, TextAnchor.MiddleLeft);
            float bh = TOP_H - 8f;
            float by = -((TOP_H - bh) * 0.5f);   // centrado vertical de los botones
            float rx = -8f;

            (_menuBg, _) = MakeTopBtn("MenuBtn", bar, rx, by, 64f, bh, "☰ Menú", ToggleMenu);
            rx -= 70f;
            MakeVSep(bar, rx, by, bh); rx -= 8f;
            (_lockBg, _) = MakeTopBtn("LockBtn", bar, rx, by, 52f, bh, "FIJAR",
                () => _camController?.LockToCurrentPosition());
            rx -= 58f;
            MakeVSep(bar, rx, by, bh); rx -= 8f;

            string[] spLbls = { "PAUSA","x1","x10","x100","x1000" };
            float[]  spW    = { 48f, 36f, 36f, 42f, 48f };
            _speedBgs = new Image[5];
            for (int i = 4; i >= 0; i--)
            {
                int idx = i;
                (Image bg, _) = MakeTopBtn($"Speed{i}", bar, rx, by, spW[i], bh, spLbls[i],
                    () => TimeManager.Instance?.SetSpeedIndex(idx));
                _speedBgs[i] = bg;
                rx -= spW[i] + 3f;
            }
        }

// Construye sidebar.
        private void BuildSidebar(RectTransform c)
        {
            var sb = new GameObject("Sidebar").AddComponent<RectTransform>();
            sb.SetParent(c, false);
            sb.anchorMin = new Vector2(0,0); sb.anchorMax = new Vector2(0,1);
            sb.pivot = new Vector2(0,0);
            sb.offsetMin = new Vector2(0, TICKER_H);
            sb.offsetMax = new Vector2(SIDEBAR_W, -TOP_H);
            MakeImg(sb, C_BG_PANEL);

            var border = MakeRect("SBBorder", sb,
                new Vector2(1,0), new Vector2(1,1), new Vector2(1,0), Vector2.zero, new Vector2(1,0));
            MakeImg(border, C_ACCENT);

            float y = -8f;
            MakeTxtPos("Logo1", sb, new Vector2(0,y), new Vector2(SIDEBAR_W,12),
                "FREIGHT", 8, FontStyle.Bold, new Color(0.4f,0.7f,1f), TextAnchor.MiddleCenter); y -= 12f;
            MakeTxtPos("Logo2", sb, new Vector2(0,y), new Vector2(SIDEBAR_W,12),
                "FORWARDER", 8, FontStyle.Bold, new Color(0.4f,0.7f,1f), TextAnchor.MiddleCenter); y -= 12f;
            var div = MakeRect("Div", sb, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(6,y), new Vector2(SIDEBAR_W-12,1));
            MakeImg(div, C_ACCENT); y -= 6f;

            string[] icons = {"📦","🚚","👤","👥","💰","🏢","⚡"};
            string[] lbls  = {"Mercado","Cargas","Agentes","Clientes","Finanzas","Oficinas","Eventos"};
            _navBgs = new Image[7];

            for (int i = 0; i < 7; i++)
            {
                Panel p = SIDE_PANELS[i];
                var rt = MakeRect($"Nav{i}", sb, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                    new Vector2(2f, y), new Vector2(SIDEBAR_W-4f, BTN_H));
                var img = MakeImg(rt, C_BTN_OFF);
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                SetBtnColors(btn);
                btn.onClick.AddListener(() => SetPanel(_active == p ? Panel.None : p));
                MakeTxtStretch($"NavLbl{i}", rt, $"{icons[i]}\n{lbls[i]}", 10, FontStyle.Bold,
                    C_GREY, TextAnchor.MiddleCenter);
                _navBgs[i] = img;

                if (i == 6)
                {
                    var bgGO = new GameObject("Badge");
                    var bgRT = bgGO.AddComponent<RectTransform>();
                    bgRT.SetParent(rt, false);
                    bgRT.anchorMin = bgRT.anchorMax = new Vector2(1f,1f);
                    bgRT.pivot = new Vector2(1f,1f);
                    bgRT.anchoredPosition = new Vector2(-1f,-1f);
                    bgRT.sizeDelta = new Vector2(16f,16f);
                    bgGO.AddComponent<Image>().color = new Color(1f,0.3f,0.2f,0.9f);
                    _badgeText = MakeTxtStretch("BadgeTxt", bgRT, "0", 9, FontStyle.Bold,
                        Color.white, TextAnchor.MiddleCenter);
                    _badgeGO = bgGO;
                    _badgeGO.SetActive(false);
                }
                y -= BTN_H + 2f;
            }
        }

// Construye panels.
        private void BuildPanels(RectTransform c)
        {
            _panelGOs       = new GameObject[CONTENT_PANELS.Length];
            _scrollContents = new RectTransform[CONTENT_PANELS.Length];
            _panelHeaders   = new Text[CONTENT_PANELS.Length];
            string[] titles = {
                "🚚  CARGAS EN TRÁNSITO", "👤  AGENTES", "👥  CLIENTES",
                "💰  FINANZAS", "🏢  OFICINAS", "⚡  EVENTOS"
            };

            for (int i = 0; i < CONTENT_PANELS.Length; i++)
            {
                var rt = MakeRect($"Panel{CONTENT_PANELS[i]}", c,
                    new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                    new Vector2(PANEL_X, -PANEL_Y), new Vector2(PANEL_W, PANEL_MAX_H));
                MakeImg(rt, C_BG_PANEL);
                _panelGOs[i] = rt.gameObject;
                _panelGOs[i].SetActive(false);

                _panelHeaders[i] = MakeTxtPos("PanelHeader", rt, new Vector2(8,-6),
                    new Vector2(PANEL_W-16, 24), titles[i], 13, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

                if (CONTENT_PANELS[i] == Panel.Finances)
                {
                    BuildFinancesContent(rt);
                    continue;
                }

                var scrollHost = new GameObject("ScrollHost").AddComponent<RectTransform>();
                scrollHost.SetParent(rt, false);
                scrollHost.anchorMin = Vector2.zero; scrollHost.anchorMax = Vector2.one;
                scrollHost.pivot = new Vector2(0.5f,0.5f);
                scrollHost.offsetMin = new Vector2(4, 4);
                scrollHost.offsetMax = new Vector2(-4, -36);

                var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
                viewport.SetParent(scrollHost, false);
                viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f,0.5f);
                viewport.offsetMin = viewport.offsetMax = Vector2.zero;
                viewport.gameObject.AddComponent<RectMask2D>();

                var content = new GameObject("Content").AddComponent<RectTransform>();
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1);
                content.pivot = new Vector2(0.5f,1f);
                content.offsetMin = content.offsetMax = Vector2.zero;
                content.sizeDelta = new Vector2(0, 400f);
                _scrollContents[i] = content;

                var sr = scrollHost.gameObject.AddComponent<ScrollRect>();
                sr.viewport = viewport; sr.content = content;
                sr.horizontal = false; sr.scrollSensitivity = 20f;
                sr.movementType = ScrollRect.MovementType.Clamped;
            }
        }

// Construye finances content.
        private void BuildFinancesContent(RectTransform panel)
        {
            float x = 12f, w = PANEL_W - 24f, y = -36f;

            _finMoney = MakeTxtPos("FinMoney", panel, new Vector2(x,y), new Vector2(w,18),
                "Liquidez: $0", 12, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            _finMoney.supportRichText = true; y -= 22f;

            _finRep = MakeTxtPos("FinRep", panel, new Vector2(x,y), new Vector2(w,18),
                "Reputación: 0/100", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft); y -= 20f;
            _finRepFill = MakeBarRow(panel, x, ref y, w);

            _finLevel = MakeTxtPos("FinLevel", panel, new Vector2(x,y), new Vector2(w,18),
                "Nivel 1 — XP 0/100", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft); y -= 20f;
            _finXpFill = MakeBarRow(panel, x, ref y, w);

            var sep = MakeRect("FinSep", panel, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(x,y), new Vector2(w,1));
            MakeImg(sep, new Color(0.3f,0.3f,0.3f,0.5f)); y -= 10f;

            _finStats = MakeTxtPos("FinStats", panel, new Vector2(x,y), new Vector2(w,116),
                "", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
            y -= 120f;

            var sep2 = MakeRect("FinSep2", panel, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(x,y), new Vector2(w,1));
            MakeImg(sep2, new Color(0.3f,0.3f,0.3f,0.5f)); y -= 10f;

            _finRecvHeader = MakeTxtPos("FinRecvHeader", panel, new Vector2(x,y), new Vector2(w,18),
                "💰 Cuentas por cobrar", 12, FontStyle.Bold, new Color(0.4f,0.85f,1f), TextAnchor.UpperLeft);
            y -= 22f;

            _finRecv = MakeTxtPos("FinRecv", panel, new Vector2(x,y), new Vector2(w,260),
                "", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
        }

// Construye menu popup.
        private void BuildMenuPopup(RectTransform c)
        {
            float pw = 220f, bh = 32f, ph = 5*(bh+2f)+12f+30f;
            var popup = MakeRect("MenuPopup", c,
                new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                new Vector2(-8f, -(TOP_H+2f)), new Vector2(pw, ph));
            MakeImg(popup, new Color(0f,0.04f,0.10f,0.97f));
            var top = MakeRect("PopupBorder", popup,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), Vector2.zero, new Vector2(0,1));
            MakeImg(top, new Color(0.2f,0.5f,1f,0.6f));

            float y = -6f;
// Realiza btn
            void Btn(string lbl, Action a) => AddMenuBtn(popup, lbl, ref y, pw, bh, a);
            Btn("▶  Volver a la partida",  ToggleMenu);
            Btn("💾  Guardar partida",      SaveGame);
            Btn("📂  Cargar partida",       LoadGame);
            Btn("⚙  Controles",            ShowControlsInfo);
            Btn("🚪  Salir",                Application.Quit);

            _menuStatus = MakeTxtPos("MenuStatus", popup, new Vector2(8, y-2), new Vector2(pw-16, 26),
                "", 10, FontStyle.Italic, new Color(0.6f,0.85f,1f), TextAnchor.UpperLeft);

            _menuPopupGO = popup.gameObject;
            _menuPopupGO.SetActive(false);
        }

// Añade menu btn
        private void AddMenuBtn(RectTransform parent, string label, ref float y, float w, float h, Action onClick)
        {
            var rt = MakeRect("MBtn", parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(0,y), new Vector2(w,h));
            var img = MakeImg(rt, C_BTN_OFF);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img; SetBtnColors(btn);
            btn.onClick.AddListener(onClick.Invoke);
            MakeTxtPos("Lbl", rt, new Vector2(8, -(h*0.5f)), new Vector2(w-12,h),
                label, 11, FontStyle.Bold, C_GREY, TextAnchor.MiddleLeft);
            y -= h + 2f;
        }

        // Establece panel.

        private void SetPanel(Panel p)
        {
            _active = p;
            _menuOpen = false;
            _menuPopupGO?.SetActive(false);

            for (int i = 0; i < CONTENT_PANELS.Length; i++)
                _panelGOs[i].SetActive(CONTENT_PANELS[i] == p);

            _marketPanel?.SetVisible(p == Panel.Market);
            UpdateNavColors();
            PopulateActivePanel();
        }

// Actualiza nav colors
        private void UpdateNavColors()
        {
            for (int i = 0; i < _navBgs.Length; i++)
                _navBgs[i].color = (_active == SIDE_PANELS[i]) ? C_BTN_ON : C_BTN_OFF;

            bool hasBadge = _eventLog.Count > 0 && _active != Panel.Events;
            _badgeGO?.SetActive(hasBadge);
            if (hasBadge && _badgeText != null)
                _badgeText.text = _eventLog.Count > 9 ? "9+" : _eventLog.Count.ToString();
        }

// Llena active panel.
        private void PopulateActivePanel()
        {
            switch (_active)
            {
                case Panel.ActiveCargos: PopulateActiveCargos(); break;
                case Panel.Agents:       PopulateAgents();       break;
                case Panel.Clients:      PopulateClients();      break;
                case Panel.Finances:     RefreshFinances();      break;
                case Panel.Offices:      PopulateOffices();      break;
                case Panel.Events:       PopulateEvents();       break;
            }
        }

        // ── Populate: Active Cargos ───────────────────────────────────────────

        private void PopulateActiveCargos()
        {
            var cargos   = CargoManager.Instance?.ActiveCargos;
            int count    = cargos?.Count ?? 0;
            int pi       = PanelIndex(Panel.ActiveCargos);
            _panelHeaders[pi].text = $"🚚  CARGAS EN TRÁNSITO  ·  {count}";
            var content  = _scrollContents[pi];
            ClearChildren(content);
            const float CH = 106f;

            if (count == 0)
            {
                content.sizeDelta = new Vector2(0, 60f);
                MakeTxtPos("None", content, new Vector2(8,-8), new Vector2(PANEL_W-32, 40),
                    "No hay cargas en tránsito.", 12, FontStyle.Italic, C_GREY, TextAnchor.UpperLeft);
                return;
            }

            content.sizeDelta = new Vector2(0, count * CH + 8f);
            int day = FFTimeManager.Instance?.CurrentDay ?? 0;

            for (int i = 0; i < count; i++)
            {
                var c    = cargos[i];
                var card = MakeCard(content, i, CH, new Color(0.04f,0.09f,0.18f,0.9f));
                // Tocar la tarjeta → la cámara va al vehículo en tránsito.
                var cargoRef = c;
                var cardBtn  = card.gameObject.AddComponent<Button>();
                cardBtn.transition = Selectable.Transition.None;
                cardBtn.onClick.AddListener(() => FocusActiveCargoVehicle(cargoRef));

                var ship = c.TransportMode == TransportMode.Maritime
                    ? MaritimeSimulationManager.Instance?.GetShipment(c.Id) : null;

                // Progreso: del barco si es marítimo, si no por días de tránsito de la carga.
                float pct = ship != null
                    ? ship.ProgressPercent / 100f
                    : (c.TotalTransitDays > 0
                        ? Mathf.Clamp01((float)Mathf.Max(0, day - c.StartDay) / c.TotalTransitDays) : 0f);
                Color barCol = pct < 0.5f ? new Color(0.2f,0.6f,1f) : new Color(0.2f,0.9f,0.4f);
                int daysLeft = ship != null ? ship.DaysRemaining : Mathf.Max(0, c.DaysRemaining);

                string agentPart = string.IsNullOrEmpty(c.AgentId)
                    ? "" : $"Agente: {c.AgentId.Replace('_',' ')}  ·  ";
                MakeTxtPos("Info", card, new Vector2(6,-4), new Vector2(card.sizeDelta.x-8, 62),
                    $"{CityDatabase.DisplayNameOf(c.OriginCityId)} → {CityDatabase.DisplayNameOf(c.DestinationCityId)}\n" +
                    $"{GetCargoTypeName(c.CargoType)}  ·  {GetTransportModeName(c.TransportMode)}  ·  {c.Weight:F0}t  ·  ${c.FinalPrice:N0}\n" +
                    $"{agentPart}Riesgo: {RiskLabel(c)}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);

                // Puertos del recorrido real: origen (0) + escalas + destino (1).
                List<float> portFractions = null;
                if (ship != null)
                {
                    portFractions = new List<float> { 0f };
// Foreach
                    foreach (var stop in ship.IntermediateStops)
                        portFractions.Add(Mathf.Clamp01(stop.fraction));
                    portFractions.Add(1f);

                    Color statusCol =
                        ship.Status == ShipStatus.Storm ? new Color(1f,0.4f,0.2f) :
                        ship.Status == ShipStatus.AtSea ? new Color(0.3f,0.9f,1f) :
                                                          new Color(1f,0.85f,0.2f);
                    string emoji =
                        ship.Status == ShipStatus.Storm ? "⚡" :
                        ship.Status == ShipStatus.AtSea ? "🚢" : "⚓";
                    MakeTxtPos("MarStatus", card, new Vector2(6,-62), new Vector2(card.sizeDelta.x-8,14),
                        $"{emoji}  {ship.StatusText}  ·  {portFractions.Count} puertos",
                        10, FontStyle.Bold, statusCol, TextAnchor.UpperLeft);
                }

                MakeTxtPos("Days", card, new Vector2(6,-84), new Vector2(114,14),
                    $"Llega en {daysLeft} días",
                    10, FontStyle.Normal, new Color(0.65f,0.65f,0.65f), TextAnchor.UpperLeft);
                MakeRouteBar(card, new Vector2(124f,-84f), new Vector2(card.sizeDelta.x-130f, 11f),
                    pct, barCol, portFractions);
            }
        }

        // Llena agentes.

        private void PopulateAgents()
        {
            var dict  = AgentManager.Instance?.GetAllAgents();
            int count = dict?.Count ?? 0;
            int pi    = PanelIndex(Panel.Agents);
            _panelHeaders[pi].text = $"👤  AGENTES  ·  {count}";
            var content = _scrollContents[pi];
            ClearChildren(content);
            const float CH = 68f;
            content.sizeDelta = new Vector2(0, count * CH + 8f);

            int i = 0;
            if (dict == null) return;
// Foreach
            foreach (var a in dict.Values)
            {
                var card = MakeCard(content, i++, CH, new Color(0.04f,0.08f,0.17f,0.9f));
                float trust = Mathf.Clamp01(a.PlayerTrust / 100f);
                Color trustCol = trust > 0.6f ? new Color(0.2f,0.8f,0.3f) :
                                 trust > 0.3f ? new Color(0.9f,0.7f,0.1f) : new Color(0.9f,0.2f,0.2f);

                MakeTxtPos("Info", card, new Vector2(6,-4), new Vector2(card.sizeDelta.x-8, 40),
                    $"{a.GetStateEmoji()}  {a.Name}  —  {GetAgentPersonalityName(a.Personality)}\n" +
                    $"Estado: {GetAgentStateName(a.CurrentState)}  ·  Cargas: {a.CurrentCargoIds.Count}  ·  Precio: x{a.GetCurrentPriceMultiplier():F2}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);

                MakeTxtPos("Trust", card, new Vector2(6,-46), new Vector2(104,14),
                    $"Confianza: {a.PlayerTrust:F0}", 10, FontStyle.Normal, new Color(0.65f,0.65f,0.65f), TextAnchor.UpperLeft);
                MakeBarH(card, new Vector2(114f,-46f), new Vector2(card.sizeDelta.x-120f, 11f), trust, trustCol);
            }
        }

        // Llena clientes.

        private void PopulateClients()
        {
            var clients = ClientManager.Instance?.ActiveClients;
            int pi      = PanelIndex(Panel.Clients);

            int diamante = 0, vip = 0, blacklisted = 0;
            var list = new List<Client>();
            if (clients != null)
// Foreach
                foreach (var cl in clients)
                {
                    list.Add(cl);
                    if (cl.IsBlacklisted) blacklisted++;
                    if (cl.Tier == ClientTier.Diamante) diamante++;
                    else if (cl.Tier == ClientTier.VIP || cl.IsVip) vip++;
                }
            int count = list.Count;

            // Orden por importancia: bloqueados al final; luego más rentables y con más cargas arriba.
            list.Sort((a, b) =>
            {
                if (a.IsBlacklisted != b.IsBlacklisted) return a.IsBlacklisted ? 1 : -1;
                int imp = ClientImportance(b).CompareTo(ClientImportance(a));
                if (imp != 0) return imp;
                return b.RelationshipLevel.CompareTo(a.RelationshipLevel);
            });

            _panelHeaders[pi].text = $"👥  CLIENTES  ·  {count}   💎{diamante}  ⭐{vip}  🚫{blacklisted}";
            var content = _scrollContents[pi];
            ClearChildren(content);
            const float CH = 98f;
            content.sizeDelta = new Vector2(0, count * CH + 8f);

            for (int i = 0; i < count; i++)
            {
                var cl = list[i];
                Color bgCol = cl.IsBlacklisted                  ? new Color(0.15f,0.03f,0.03f,0.9f)  :
                              cl.Tier == ClientTier.Diamante     ? new Color(0.04f,0.10f,0.16f,0.92f) :
                              cl.IsVip || cl.Tier == ClientTier.VIP ? new Color(0.10f,0.09f,0.02f,0.92f) :
                              cl.Tier == ClientTier.Frecuente    ? new Color(0.04f,0.11f,0.06f,0.9f)  :
                                                                   new Color(0.04f,0.08f,0.16f,0.9f);
                var card = MakeCard(content, i, CH, bgCol);
                float tw = card.sizeDelta.x - 12;

                Color tierCol = cl.IsBlacklisted                ? new Color(1f,0.5f,0.5f)    :
                                cl.Tier == ClientTier.Diamante  ? new Color(0.6f,0.9f,1f)    :
                                cl.Tier == ClientTier.VIP       ? new Color(1f,0.85f,0.2f)   :
                                cl.Tier == ClientTier.Frecuente ? new Color(0.4f,0.85f,0.5f) : Color.white;

                MakeTxtPos("Name", card, new Vector2(6,-4), new Vector2(tw-24,18),
                    $"{cl.GetTierBadge()}  ·  {cl.CompanyName}{(cl.IsBlacklisted ? "  🚫" : "")}",
                    12, FontStyle.Bold, tierCol, TextAnchor.UpperLeft);

                // Ícono de teléfono arriba a la derecha (sin acción por ahora: futuro seguimiento del cliente).
                MakeTxtPos("Phone", card, new Vector2(card.sizeDelta.x-26,-4), new Vector2(22,18),
                    "☎", 15, FontStyle.Bold, new Color(0.55f,0.8f,1f), TextAnchor.UpperRight);

                MakeTxtPos("Info", card, new Vector2(6,-24), new Vector2(tw,16),
                    $"{GetClientTypeName(cl.ClientType)}  ·  Entregas {cl.SuccessfulDeliveries}/{cl.TotalDeliveries}  ·  Lucro ${cl.TotalProfit:N0}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);

                MakeTxtPos("Route", card, new Vector2(6,-40), new Vector2(tw,14),
                    $"Ruta: {FavoriteRouteLabel(cl)}",
                    10, FontStyle.Normal, new Color(0.6f,0.75f,0.95f), TextAnchor.UpperLeft);

                MakeTxtPos("RelLbl", card, new Vector2(6,-58), new Vector2(58,14),
                    "Relación", 10, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
                float relLvl = cl.RelationshipLevel;
                Color relCol = RelationshipBarColor(relLvl);
                // Barra que se llena con la relación; color por tramo (rojo→naranja→amarillo→lima→verde).
                MakeBarH(card, new Vector2(64,-58), new Vector2(tw-92, 11f), relLvl/100f, relCol);
                // Carita en la punta de la barra (provisional con texto; a futuro, imágenes).
                MakeTxtPos("Face", card, new Vector2(tw-24,-60), new Vector2(26,18),
                    RelationshipFace(relLvl), 13, FontStyle.Bold, relCol, TextAnchor.MiddleLeft);

                Color angerCol = cl.AngerLevel >= 3 ? new Color(1f,0.4f,0.3f) :
                                 cl.AngerLevel >= 1 ? new Color(0.95f,0.75f,0.2f) : C_GREY;
                MakeTxtPos("Mood", card, new Vector2(6,-76), new Vector2(tw,14),
                    $"{RelationshipWord(cl.RelationshipLevel)}  ·  Enojo {cl.AngerLevel}/5",
                    10, FontStyle.Normal, angerCol, TextAnchor.UpperLeft);
            }
        }

// Gestiona favorite ruta etiqueta.
        private static string FavoriteRouteLabel(Client cl)
        {
            if (cl.FavoriteRoutes == null || cl.FavoriteRoutes.Count == 0) return "sin ruta habitual";
            string raw = cl.FavoriteRoutes[cl.FavoriteRoutes.Count - 1];
            var parts = raw.Split('→');
            return parts.Length == 2
                ? $"{CityDatabase.DisplayNameOf(parts[0])} → {CityDatabase.DisplayNameOf(parts[1])}"
                : raw;
        }

        // Importancia del cliente: rentabilidad acumulada + volumen de cargas.
        private static int ClientImportance(Client cl)
            => cl.TotalProfit + (cl.SuccessfulDeliveries + cl.PendingOffers) * 300;

        // Color de la barra de relación por tramo: rojo → naranja → amarillo → verde lima → verde.
        private static Color RelationshipBarColor(float level)
        {
            if (level < 20f) return new Color(0.90f, 0.20f, 0.20f); // rojo
            if (level < 40f) return new Color(1.00f, 0.50f, 0.10f); // naranja
            if (level < 60f) return new Color(0.95f, 0.85f, 0.20f); // amarillo
            if (level < 80f) return new Color(0.60f, 0.90f, 0.30f); // verde lima
            return new Color(0.20f, 0.85f, 0.35f);                  // verde
        }

        // Carita de ánimo (provisional en texto hasta tener imágenes).
        private static string RelationshipFace(float level)
        {
            if (level < 35f) return ":(";
            if (level < 65f) return ":|";
            return ":)";
        }

// Gestiona relationship word.
        private static string RelationshipWord(float level)
        {
            if (level >= 90) return "Excelente";
            if (level >= 70) return "Muy buena";
            if (level >= 50) return "Buena";
            if (level >= 30) return "Regular";
            if (level >= 10) return "Mala";
            return "Pésima";
        }

        // Llena offices.

        private void PopulateOffices()
        {
            var allCities = CityDatabase.AllCities;
            int pi        = PanelIndex(Panel.Offices);
            if (allCities == null) return;

            var locked = new List<WorldCity>();
            var unlocked = new List<WorldCity>();
// Foreach
            foreach (var c in allCities.Values)
                (c.IsUnlocked ? unlocked : locked).Add(c);

            _panelHeaders[pi].text = $"🏢  OFICINAS  ·  {unlocked.Count} activas, {locked.Count} por desbloquear";
            var content  = _scrollContents[pi];
            ClearChildren(content);

            const float CH = 54f;
            content.sizeDelta = new Vector2(0, locked.Count * CH + 8f);
            int money = EconomyManager.Instance?.Money ?? 0;

            for (int i = 0; i < locked.Count; i++)
            {
                var city = locked[i];
                var card = MakeCard(content, i, CH, new Color(0.04f,0.08f,0.15f,0.9f));
                float tw = card.sizeDelta.x - 84f;
                string infra = (city.HasPort ? "⚓ " : "") + (city.HasAirport ? "✈ " : "") + (city.IsLandHub ? "🚛" : "");

                MakeTxtPos("Info", card, new Vector2(6,-4), new Vector2(tw,44),
                    $"📍 {city.DisplayName}  ·  {city.Country}\n" +
                    $"{(infra.Length > 0 ? infra : "Sin infraestructura especial")}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);

                bool canAfford = money >= city.UnlockCost;
                var btnRT = MakeRect("UnlockBtn", card,
                    new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                    new Vector2(-4,-8), new Vector2(76,38));
                var btnImg = MakeImg(btnRT, canAfford
                    ? new Color(0.1f,0.4f,0.15f,0.95f) : new Color(0.3f,0.3f,0.3f,0.7f));
                MakeTxtStretch("BtnLbl", btnRT,
                    $"${city.UnlockCost:N0}\n🔓 Abrir", 10, FontStyle.Bold,
                    canAfford ? Color.white : new Color(0.5f,0.5f,0.5f), TextAnchor.MiddleCenter);

                if (canAfford)
                {
                    string cid = city.Id; int cost = city.UnlockCost;
                    var btn = btnRT.gameObject.AddComponent<Button>();
                    btn.targetGraphic = btnImg; SetBtnColors(btn);
                    btn.onClick.AddListener(() =>
                    {
                        EconomyManager.Instance?.SubtractMoney(cost);
                        CargoManager.Instance?.UnlockCity(cid);
                        PopulateOffices();
                    });
                }
            }
        }

        // Llena eventos.

        private void PopulateEvents()
        {
            int count = _eventLog.Count;
            int pi    = PanelIndex(Panel.Events);
            _panelHeaders[pi].text = $"⚡  EVENTOS  ·  {count} registrados";
            var content = _scrollContents[pi];
            ClearChildren(content);
            const float CH = 56f;
            content.sizeDelta = new Vector2(0, count * CH + 8f);

            for (int i = 0; i < count; i++)
            {
                var ev   = _eventLog[i];
                var card = MakeCard(content, i, CH, new Color(0.10f,0.06f,0.02f,0.9f));
                MakeTxtPos("EvTxt", card, new Vector2(6,-4), new Vector2(card.sizeDelta.x-60,46),
                    ev.Text, 11, FontStyle.Normal, ev.Color, TextAnchor.UpperLeft);
                MakeTxtPos("Day", card,
                    new Vector2(card.sizeDelta.x-52f,-4), new Vector2(48,14),
                    $"Día {ev.Day}", 10, FontStyle.Normal, new Color(0.5f,0.5f,0.5f), TextAnchor.UpperLeft);
            }
        }

        // Refresca finances

        private void RefreshFinances()
        {
            var eco  = EconomyManager.Instance;
            var carg = CargoManager.Instance;
            if (eco == null || _finMoney == null) return;

            string moneyStr = eco.Money >= 0 ? $"${eco.Money:N0}" : $"-${Mathf.Abs(eco.Money):N0}";
            _finMoney.text  = $"Liquidez:  {moneyStr}";
            _finMoney.color = eco.Money >= 0 ? new Color(0.2f,1f,0.4f) : new Color(1f,0.3f,0.3f);

            _finRep.text   = $"Reputación:  {eco.Reputation}/100";
            SetBarFill(_finRepFill, eco.Reputation / 100f,
                eco.Reputation > 50 ? new Color(0.2f,0.8f,0.3f) :
                eco.Reputation > 25 ? new Color(0.9f,0.7f,0.1f) : new Color(0.9f,0.2f,0.2f));

            int   xpNeeded = eco.GetXPForNextLevel();
            float xpPct    = xpNeeded > 0 ? (float)eco.CurrentXP / xpNeeded : 0f;
            _finLevel.text = $"Nivel {eco.Level}  —  XP {eco.CurrentXP}/{xpNeeded}";
            SetBarFill(_finXpFill, xpPct, new Color(0.3f,0.5f,1f));

            if (carg != null)
            {
                float rate = carg.GetSuccessRate();
                int day = FFTimeManager.Instance?.CurrentDay ?? 0;
                int cities = 0;
                if (CityDatabase.AllCities != null)
                    foreach (var cc in CityDatabase.AllCities.Values) if (cc.IsUnlocked) cities++;
                int net = eco.GetNetProfit();
                string netStr = net >= 0 ? $"${net:N0}" : $"-${Mathf.Abs(net):N0}";
                string netCol = net >= 0 ? "#5FE08A" : "#FF6B6B";
                _finStats.color = new Color(0.85f,0.89f,0.97f);
                _finStats.text =
                    $"<color=#5FE08A>Ingresos</color>  ${eco.TotalRevenue:N0}      <color=#FF8A7A>Egresos</color>  ${eco.TotalCosts:N0}\n" +
                    $"<color=#BFE6FF>Ganancia neta</color>   <color={netCol}><b>{netStr}</b></color>\n" +
                    $"<color=#FFC766>Costo mensual oficinas</color>   ${eco.MonthlyOfficeCosts:N0}\n" +
                    $"<color=#BFE6FF>Día {day}</color>     ·     <color=#BFE6FF>Ciudades activas: {cities}</color>\n" +
                    $"<color=#9BE89B>Completadas: {carg.CompletedCargos.Count}</color>     <color=#FF9B8A>Fallidas: {carg.FailedCargos.Count}</color>\n" +
                    $"En tránsito: {carg.ActiveCargos.Count}      En mercado: {carg.MarketCargos.Count}\n" +
                    $"Tasa de éxito: <b>{rate * 100:F0}%</b>";
            }

            RefreshReceivables();
        }

// Refresca receivables
        private void RefreshReceivables()
        {
            if (_finRecv == null) return;
            var pay = PaymentManager.Instance;
            if (pay == null || pay.PendingCount == 0)
            {
                if (_finRecvHeader != null) _finRecvHeader.text = "💰 Cuentas por cobrar";
                _finRecv.text  = "Sin cobros pendientes.";
                _finRecv.color = C_GREY;
                return;
            }

            int day = FFTimeManager.Instance?.CurrentDay ?? 0;
            if (_finRecvHeader != null)
                _finRecvHeader.text = $"💰 Cuentas por cobrar  ·  ${pay.TotalReceivable:N0}";

            var list = new List<PendingPayment>(pay.Pending);
            list.Sort((a, b) => a.DueDay.CompareTo(b.DueDay));

            var sb = new System.Text.StringBuilder();
            int shown = 0;
// Foreach
            foreach (var p in list)
            {
                if (shown >= 7) { sb.Append($"… y {list.Count - 7} cobro(s) más"); break; }
                shown++;
                string tag = p.Timing == PaymentTiming.Late  ? "  ⚠ atrasado"
                           : p.Timing == PaymentTiming.Early ? "  ⚡ anticipado" : "";
                int d = p.DaysRemaining(day);
                string when = d <= 0 ? "hoy" : $"en {d}d";
                sb.AppendLine($"{p.ClientName}:  ${p.Amount:N0}  —  {when}{tag}");
            }
            _finRecv.text  = sb.ToString();
            _finRecv.color = C_GREY;
        }

// Refresca finances if visible
        private void RefreshFinancesIfVisible()
        {
            if (_active == Panel.Finances) RefreshFinances();
        }

        // ── Top bar refresh ───────────────────────────────────────────────────

        private void RefreshTopBar()
        {
            if (_moneyText == null) return;
            var eco = EconomyManager.Instance;
            var tm  = TimeManager.Instance;
            int day = FFTimeManager.Instance?.CurrentDay ?? 0;

            // Fecha + hora EN VIVO (se refresca cada minuto de juego).
            // Guarda: si el reloj aún no inicializó (fecha por defecto), CurrentLocalTime tira excepción.
            if (_dateText != null && tm != null && tm.CurrentUtcTime.Year > 1)
            {
                var lt = tm.CurrentLocalTime;
                int stamp = day * 1440 + lt.Hour * 60 + lt.Minute;
                if (stamp != _tbMinute)
                {
                    _tbMinute = stamp;
                    _dateText.text = $"📅  Día {day}    {lt:dd/MM/yyyy}    {lt:HH:mm}";
                }
            }

            // Dinero + reputación.
            int money = eco?.Money ?? 0;
            int rep   = eco?.Reputation ?? 0;
            if (money != _tbMoney || rep != _tbRep)
            {
                int delta = money - _lastDayMoney;
                string deltaStr = delta != 0 ? $"  {FormatDelta(delta)}/d" : "";
                _moneyText.text  = money >= 0
                    ? $"💰  {FormatCompact(money)}{deltaStr}"
                    : $"💰  -{FormatCompact(-money)}{deltaStr}";
                _moneyText.color = money >= 0 ? new Color(0.2f,1f,0.4f) : new Color(1f,0.3f,0.3f);
                if (_repText != null) _repText.text = $"⭐  {rep}/100";
                _tbMoney = money; _tbRep = rep;
            }

            // Velocidad.
            int speedIdx = tm?.CurrentSpeedIndex ?? 1;
            if (speedIdx != _tbSpeed)
            {
                for (int i = 0; i < _speedBgs.Length; i++)
                    _speedBgs[i].color = (i == speedIdx) ? C_BTN_ON : C_BTN_OFF;
                _tbSpeed = speedIdx;
            }

            // Fijar / Menú.
            bool locked = _camController != null && _camController.IsManuallyLocked;
            if (locked != _tbLocked && _lockBg != null) { _lockBg.color = locked ? C_BTN_ON : C_BTN_OFF; _tbLocked = locked; }
            bool menuOpen = _menuOpen;
            if (menuOpen != _tbMenu && _menuBg != null) { _menuBg.color = menuOpen ? C_BTN_ON : C_BTN_OFF; _tbMenu = menuOpen; }
        }

// Gestiona format compact.
        private static string FormatCompact(int n)
        {
            int abs = System.Math.Abs(n);
            if (abs >= 1_000_000) return $"${n / 1_000_000f:F1}M";
            if (abs >= 10_000)    return $"${n / 1_000f:F0}k";
            if (abs >= 1_000)     return $"${n / 1_000f:F1}k";
            return $"${n}";
        }

// Gestiona format delta.
        private static string FormatDelta(int delta)
        {
            int  abs  = System.Math.Abs(delta);
            char sign = delta >= 0 ? '+' : '-';
            if (abs >= 1_000_000) return $"{sign}{abs/1_000_000f:F1}M";
            if (abs >= 1_000)     return $"{sign}{abs/1_000f:F0}k";
            return $"{sign}{abs}";
        }

        // Se invoca cuando se activa un evento.

        private void OnEventTriggered(GameEvent evt, Cargo cargo)
        {
            string route = $"{CityDatabase.DisplayNameOf(cargo.OriginCityId)} → {CityDatabase.DisplayNameOf(cargo.DestinationCityId)}";
            _eventLog.Insert(0, new EventLog
            {
                Text  = $"⚡  {evt.Name}  [{route}]\n{evt.Description}",
                Color = new Color(1f, 0.65f, 0.1f),
                Day   = FFTimeManager.Instance?.CurrentDay ?? 0
            });
            if (_eventLog.Count > MAX_EVENT_LOG) _eventLog.RemoveAt(_eventLog.Count - 1);
            UpdateNavColors();
            if (_active == Panel.Events) PopulateEvents();
        }

// Se invoca al terminar un día de juego.
        private void OnDayPassed()
        {
            _lastDayMoney = EconomyManager.Instance?.Money ?? _lastDayMoney;
            RefreshTopBar();   // actualiza fecha/plata aunque no corra Update
            if (_active != Panel.None && _active != Panel.Market)
                PopulateActivePanel();
        }

        // Alterna menu

        private void ToggleMenu()
        {
            _menuOpen = !_menuOpen;
            _menuPopupGO?.SetActive(_menuOpen);
        }

        // ── Guardar / Cargar (estado central vía PlayerPrefs) ─────────────────
        // Nota: persiste dinero, reputación, nivel, XP, estadísticas y ciudades.
        // Las cargas en tránsito todavía no se guardan (próxima iteración).
        private void SaveGame()
        {
            var eco = EconomyManager.Instance;
            if (eco == null) return;
            PlayerPrefs.SetInt("ff_money",  eco.Money);
            PlayerPrefs.SetInt("ff_rep",    eco.Reputation);
            PlayerPrefs.SetInt("ff_level",  eco.Level);
            PlayerPrefs.SetInt("ff_xp",     eco.CurrentXP);
            PlayerPrefs.SetInt("ff_done",   eco.TotalCargosCompleted);
            PlayerPrefs.SetInt("ff_failed", eco.TotalCargosFailed);
            PlayerPrefs.SetInt("ff_rev",    eco.TotalRevenue);
            PlayerPrefs.SetInt("ff_cost",   eco.TotalCosts);

            var ids = new List<string>();
            if (CityDatabase.AllCities != null)
                foreach (var cc in CityDatabase.AllCities.Values) if (cc.IsUnlocked) ids.Add(cc.Id);
            PlayerPrefs.SetString("ff_cities",   string.Join(",", ids));
            PlayerPrefs.SetString("ff_savedate", System.DateTime.Now.ToString("dd/MM HH:mm"));
            PlayerPrefs.SetInt("ff_has_save", 1);
            PlayerPrefs.Save();

            if (_menuStatus != null) _menuStatus.text = $"💾 Guardado · ${eco.Money:N0}";
        }

// Carga juego
        private void LoadGame()
        {
            if (PlayerPrefs.GetInt("ff_has_save", 0) == 0)
            {
                if (_menuStatus != null) _menuStatus.text = "No hay partida guardada.";
                return;
            }
            EconomyManager.Instance?.RestoreState(
                PlayerPrefs.GetInt("ff_money",  Constants.INITIAL_MONEY),
                PlayerPrefs.GetInt("ff_rep",    Constants.INITIAL_REPUTATION),
                PlayerPrefs.GetInt("ff_level",  1),
                PlayerPrefs.GetInt("ff_xp",     0),
                PlayerPrefs.GetInt("ff_done",   0),
                PlayerPrefs.GetInt("ff_failed", 0),
                PlayerPrefs.GetInt("ff_rev",    0),
                PlayerPrefs.GetInt("ff_cost",   0));

            string cities = PlayerPrefs.GetString("ff_cities", "");
            if (!string.IsNullOrEmpty(cities))
// Foreach
                foreach (var id in cities.Split(','))
                    if (!string.IsNullOrEmpty(id)) CargoManager.Instance?.UnlockCity(id);

            RefreshTopBar();
            if (_active != Panel.None && _active != Panel.Market) PopulateActivePanel();

            if (_menuStatus != null)
                _menuStatus.text = $"📂 Cargado ({PlayerPrefs.GetString("ff_savedate", "")})";
        }

// Muestra controls info
        private void ShowControlsInfo()
        {
            if (_menuStatus != null)
                _menuStatus.text = "Rueda: zoom · Arrastrar: girar · FIJAR: bloquear · W/S: ciudades";
        }

// Riesgo etiqueta.
        private static string RiskLabel(Cargo c)
        {
            int r = c.EventsEncountered?.Count ?? 0;
            return r == 0 ? "🟢 Bajo" : r == 1 ? "🟡 Medio" : "🔴 Alto";
        }

// Gestiona panel index.
        private int PanelIndex(Panel p)
        {
            for (int i = 0; i < CONTENT_PANELS.Length; i++)
                if (CONTENT_PANELS[i] == p) return i;
            return -1;
        }

// Borra children.
        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

// Establece bar fill.
        private static void SetBarFill(Image fill, float amount, Color col)
        {
            if (fill == null) return;
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            fill.color = col;
        }

        // Obtiene or create canvas

        private static RectTransform GetOrCreateCanvas()
        {
            // EventSystem check DEBE come antes de any Canvas lookup —
            // the Canvas may already exist in the scene but EventSystem may not.
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Canvas found = FindAnyObjectByType<Canvas>();
            if (found != null)
            {
                // Patch existing canvas — may have been created with wrong settings
                if (found.GetComponent<GraphicRaycaster>() == null)
                    found.gameObject.AddComponent<GraphicRaycaster>();
                var existCs = found.GetComponent<CanvasScaler>();
                if (existCs != null)
                {
                    existCs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    existCs.referenceResolution = new Vector2(1280, 720);
                    existCs.matchWidthOrHeight  = 0.5f;
                }
                found.sortingOrder = 10;
                return found.GetComponent<RectTransform>();
            }

            var go = new GameObject("UICanvas");
            var c  = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 10;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280, 720);
            cs.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform MakeRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name); var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

// Gestiona make img.
        private static Image MakeImg(RectTransform rt, Color col)
        {
            var img = rt.gameObject.AddComponent<Image>(); img.color = col; return img;
        }

        private Text MakeTxtPos(string name, RectTransform parent, Vector2 pos, Vector2 size,
            string text, int fontSize, FontStyle style, Color color, TextAnchor anchor)
        {
            var rt = MakeRect(name, parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), pos, size);
            var t  = rt.gameObject.AddComponent<Text>();
            t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.color = color;
            t.alignment = anchor; t.font = _font;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

        private Text MakeTxtStretch(string name, RectTransform parent, string text,
            int fontSize, FontStyle style, Color color, TextAnchor anchor, Vector2 margin = default)
        {
            var go = new GameObject(name); var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f,0.5f);
            rt.offsetMin = margin; rt.offsetMax = -margin;
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.color = color;
            t.alignment = anchor; t.font = _font;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

// Gestiona make card.
        private RectTransform MakeCard(RectTransform content, int idx, float cardH, Color bg)
        {
            float w = content.sizeDelta.x > 0 ? content.sizeDelta.x - 8f : PANEL_W - 8f;
            var card = MakeRect($"Card{idx}", content,
                new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(4f, -(idx * cardH + 4f)), new Vector2(w, cardH - 4f));
            MakeImg(card, bg);
            return card;
        }

// Gestiona make bar h.
        private void MakeBarH(RectTransform parent, Vector2 pos, Vector2 size, float fill, Color col)
        {
            var bg = MakeRect("BarBg", parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), pos, size);
            bg.gameObject.AddComponent<Image>().color = new Color(0.12f,0.12f,0.12f,0.9f);
            // El relleno se dimensiona por anclas (Image.Type.Filled no funciona sin sprite asignado).
            float f = Mathf.Clamp01(fill);
            var fillRT = new GameObject("BarFill").AddComponent<RectTransform>();
            fillRT.SetParent(bg, false);
            fillRT.anchorMin = new Vector2(0,0); fillRT.anchorMax = new Vector2(f,1);
            fillRT.pivot = new Vector2(0,0.5f); fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            fillRT.gameObject.AddComponent<Image>().color = col;
        }

        // Barra de progreso de recorrido: línea + relleno + marcadores de puerto + punto del buque.
        private void MakeRouteBar(RectTransform parent, Vector2 pos, Vector2 size, float fill, Color col,
                                  IList<float> portFractions)
        {
            float f = Mathf.Clamp01(fill);

            var bg = MakeRect("RouteBg", parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), pos, size);
            MakeImg(bg, new Color(0.12f,0.12f,0.12f,0.9f));

            var fillRT = new GameObject("RouteFill").AddComponent<RectTransform>();
            fillRT.SetParent(bg, false);
            fillRT.anchorMin = new Vector2(0,0); fillRT.anchorMax = new Vector2(f,1);
            fillRT.pivot = new Vector2(0,0.5f); fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            fillRT.gameObject.AddComponent<Image>().color = col;

            // Marcadores de puerto (escalas) en su posición real del recorrido.
            if (portFractions != null)
            {
// Foreach
                foreach (float pf in portFractions)
                {
                    float cf = Mathf.Clamp01(pf);
                    bool passed = cf <= f + 0.0001f;
                    var tick = MakeRect("Port", bg, new Vector2(cf,0.5f), new Vector2(cf,0.5f),
                        new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(3f, size.y + 6f));
                    MakeImg(tick, passed ? new Color(0.55f,0.85f,1f) : new Color(0.5f,0.5f,0.55f));
                }
            }

            // Punto que representa el buque, en la posición de avance actual.
            float dotD = size.y + 5f;
            var dot = MakeRect("ShipDot", bg, new Vector2(f,0.5f), new Vector2(f,0.5f),
                new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(dotD, dotD));
            var dotImg = MakeImg(dot, Color.white);
            var circle = CircleSprite();
            if (circle != null) dotImg.sprite = circle;
        }

        private static Sprite _circleSprite;
        private static bool    _circleSpriteTried;
// Gestiona circle sprite.
        private static Sprite CircleSprite()
        {
            if (!_circleSpriteTried)
            {
                _circleSpriteTried = true;
                // Círculo generado por código (evita el error de UI/Skin/Knob.psd, que no existe en esta versión).
                const int S = 32;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                float c = (S - 1) * 0.5f;
                var clear = new Color(1f, 1f, 1f, 0f);
                for (int yy = 0; yy < S; yy++)
                    for (int xx = 0; xx < S; xx++)
                    {
                        float d = Mathf.Sqrt((xx - c) * (xx - c) + (yy - c) * (yy - c));
                        tex.SetPixel(xx, yy, d <= c ? Color.white : clear);
                    }
                tex.Apply();
                _circleSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            }
            return _circleSprite;
        }

// Gestiona make bar row.
        private Image MakeBarRow(RectTransform parent, float x, ref float y, float w)
        {
            var bg = MakeRect("Bar", parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(x,y), new Vector2(w,10));
            bg.gameObject.AddComponent<Image>().color = new Color(0.12f,0.12f,0.12f,0.9f);
            var fillRT = new GameObject("Fill").AddComponent<RectTransform>();
            fillRT.SetParent(bg, false);
            fillRT.anchorMin = new Vector2(0,0); fillRT.anchorMax = new Vector2(0,1);
            fillRT.pivot = new Vector2(0,0.5f); fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var img = fillRT.gameObject.AddComponent<Image>();
            y -= 14f;
            return img;
        }

        private (Image bg, Button btn) MakeTopBtn(string name, RectTransform c,
            float rx, float ry, float w, float h, string lbl, Action onClick)
        {
            var rt  = MakeRect(name, c, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                new Vector2(rx,ry), new Vector2(w,h));
            var img = MakeImg(rt, C_BTN_OFF);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img; SetBtnColors(btn);
            btn.onClick.AddListener(onClick.Invoke);
            MakeTxtStretch("Lbl", rt, lbl, 11, FontStyle.Bold, C_GREY, TextAnchor.MiddleCenter);
            return (img, btn);
        }

// Gestiona make v sep.
        private void MakeVSep(RectTransform bar, float rx, float ry, float h)
        {
            var sep = MakeRect("VSep", bar, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                new Vector2(rx,ry), new Vector2(1,h));
            MakeImg(sep, new Color(0.3f,0.3f,0.3f,0.6f));
        }

// Establece btn colors.
        private static void SetBtnColors(Button btn)
        {
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.2f,0.4f,0.9f,1f);
            cb.pressedColor     = new Color(0.1f,0.25f,0.7f,1f);
            btn.colors = cb;
        }
    }
}