using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Map;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using static FreightForwarder.Models.Constants;
using UnityEngine;
using UnityEngine.UI;

namespace FreightForwarder.UI
{
    public class FFUIManager : Singleton<FFUIManager>
    {
        private enum Panel { None = 0, Market, ActiveCargos, Agents, Clients, Finances, Offices, Events }
        private Panel _active = Panel.None;
        private bool  _menuOpen;

        private MapCameraController _camController;
        private MarketPanel         _marketPanel;

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
        private Text  _finMoney, _finRep, _finLevel, _finStats;
        private Image _finRepFill, _finXpFill;

        private static Font _fontCache;
        private static Font _font => _fontCache != null ? _fontCache : (_fontCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // ── TopBar dirty-check cache ──────────────────────────────────────────
        private int  _tbMoney    = int.MinValue;
        private int  _tbRep      = int.MinValue;
        private int  _tbDay      = -1;
        private int  _tbSpeed    = -1;
        private bool _tbLocked   = false;
        private bool _tbMenu     = false;

        // Stored delegates so lambdas can be unsubscribed from EconomyManager events
        private Action<int> _onMoneyChanged;
        private Action<int> _onRepChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnAwake() { BuildUI(); }

        private void Start()
        {
            _marketPanel   = FindAnyObjectByType<MarketPanel>();
            _camController = FindAnyObjectByType<MapCameraController>();

            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
            if (EconomyManager.Instance != null)
            {
                _onMoneyChanged = _ => RefreshFinancesIfVisible();
                _onRepChanged   = _ => RefreshFinancesIfVisible();
                EconomyManager.Instance.OnMoneyChanged      += _onMoneyChanged;
                EconomyManager.Instance.OnReputationChanged += _onRepChanged;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged      -= _onMoneyChanged;
                EconomyManager.Instance.OnReputationChanged -= _onRepChanged;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_menuOpen) ToggleMenu();
                else if (_active != Panel.None) SetPanel(Panel.None);
            }
            RefreshTopBar();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvas = GetOrCreateCanvas();
            BuildTopBar(canvas);
            BuildSidebar(canvas);
            BuildPanels(canvas);
            BuildMenuPopup(canvas);
        }

        private void BuildTopBar(RectTransform c)
        {
            var bar = MakeRect("TopBar", c,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), Vector2.zero, new Vector2(0, TOP_H));
            MakeImg(bar, C_BG_DARK);
            var acc = MakeRect("TopAccent", bar,
                new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), Vector2.zero, new Vector2(0, 1f));
            MakeImg(acc, C_ACCENT);

            float ly = -(TOP_H * 0.5f);
            float lh = TOP_H - 8f;
            _moneyText = MakeTxtPos("Money", bar, new Vector2(SIDEBAR_W+8f, ly), new Vector2(150f, lh),
                "$0", 12, FontStyle.Bold, new Color(0.2f,1f,0.4f), TextAnchor.MiddleLeft);
            _repText   = MakeTxtPos("Rep", bar, new Vector2(SIDEBAR_W+164f, ly), new Vector2(110f, lh),
                "⭐ 50", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            _dateText  = MakeTxtPos("Date", bar, new Vector2(SIDEBAR_W+280f, ly), new Vector2(240f, lh),
                "Día 0", 11, FontStyle.Normal, C_GREY, TextAnchor.MiddleLeft);

            float bh = TOP_H - 8f;
            float by = -(TOP_H * 0.5f);
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

            _finStats = MakeTxtPos("FinStats", panel, new Vector2(x,y), new Vector2(w,120),
                "", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
        }

        private void BuildMenuPopup(RectTransform c)
        {
            float pw = 210f, bh = 32f, ph = 5*(bh+2f)+12f;
            var popup = MakeRect("MenuPopup", c,
                new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                new Vector2(-8f, -(TOP_H+2f)), new Vector2(pw, ph));
            MakeImg(popup, new Color(0f,0.04f,0.10f,0.97f));
            var top = MakeRect("PopupBorder", popup,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,1), Vector2.zero, new Vector2(0,1));
            MakeImg(top, new Color(0.2f,0.5f,1f,0.6f));

            float y = -6f;
            void Btn(string lbl, Action a) => AddMenuBtn(popup, lbl, ref y, pw, bh, a);
            Btn("▶  Volver a la partida",  ToggleMenu);
            Btn("💾  Guardar partida",      () => { ToggleMenu(); Debug.Log("[UI] Guardar — pendiente"); });
            Btn("📂  Cargar Partida",       () => { ToggleMenu(); Debug.Log("[UI] Cargar — pendiente"); });
            Btn("⚙  Configuración",        () => { ToggleMenu(); Debug.Log("[UI] Config — pendiente"); });
            Btn("🚪  Salir",                Application.Quit);

            _menuPopupGO = popup.gameObject;
            _menuPopupGO.SetActive(false);
        }

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

        // ── Panel switching ───────────────────────────────────────────────────

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

        private void UpdateNavColors()
        {
            for (int i = 0; i < _navBgs.Length; i++)
                _navBgs[i].color = (_active == SIDE_PANELS[i]) ? C_BTN_ON : C_BTN_OFF;

            bool hasBadge = _eventLog.Count > 0 && _active != Panel.Events;
            _badgeGO?.SetActive(hasBadge);
            if (hasBadge && _badgeText != null)
                _badgeText.text = _eventLog.Count > 9 ? "9+" : _eventLog.Count.ToString();
        }

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
            const float CH = 88f;
            content.sizeDelta = new Vector2(0, count * CH + 8f);
            int day = FFTimeManager.Instance?.CurrentDay ?? 0;

            for (int i = 0; i < count; i++)
            {
                var c    = cargos[i];
                var card = MakeCard(content, i, CH, new Color(0.04f,0.09f,0.18f,0.9f));
                float pct = c.TotalTransitDays > 0
                    ? Mathf.Clamp01((float)Mathf.Max(0, day - c.StartDay) / c.TotalTransitDays) : 0f;
                Color barCol = pct < 0.5f ? new Color(0.2f,0.6f,1f) : new Color(0.2f,0.9f,0.4f);

                MakeTxtPos("Info", card, new Vector2(6,-4), new Vector2(card.sizeDelta.x-8, 62),
                    $"{c.OriginCityId.Replace('_',' ')} → {c.DestinationCityId.Replace('_',' ')}\n" +
                    $"{GetCargoTypeName(c.CargoType)}  ·  {GetTransportModeName(c.TransportMode)}  ·  {c.Weight:F0}t  ·  ${c.FinalPrice:N0}\n" +
                    $"Agente: {c.AgentId.Replace('_',' ')}  ·  Riesgo: {RiskLabel(c)}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);

                MakeTxtPos("Days", card, new Vector2(6,-66), new Vector2(114,14),
                    $"Llega en {Mathf.Max(0, c.DaysRemaining)} días",
                    10, FontStyle.Normal, new Color(0.65f,0.65f,0.65f), TextAnchor.UpperLeft);
                MakeBarH(card, new Vector2(124f,-66f), new Vector2(card.sizeDelta.x-130f, 11f), pct, barCol);
            }
        }

        // ── Populate: Agents ──────────────────────────────────────────────────

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

        // ── Populate: Clients ─────────────────────────────────────────────────

        private void PopulateClients()
        {
            var clients = ClientManager.Instance?.ActiveClients;
            int count   = clients?.Count ?? 0;
            int pi      = PanelIndex(Panel.Clients);

            int blacklisted = 0, vip = 0;
            if (clients != null)
                foreach (var cl in clients) { if (cl.IsBlacklisted) blacklisted++; if (cl.IsVip) vip++; }

            _panelHeaders[pi].text = $"👥  CLIENTES  ·  {count}  ⭐{vip}  🚫{blacklisted}";
            var content = _scrollContents[pi];
            ClearChildren(content);
            const float CH = 72f;
            content.sizeDelta = new Vector2(0, count * CH + 8f);

            for (int i = 0; i < count; i++)
            {
                var cl = clients[i];
                Color bgCol = cl.IsBlacklisted ? new Color(0.15f,0.03f,0.03f,0.9f) :
                              cl.IsVip         ? new Color(0.05f,0.12f,0.05f,0.9f) :
                                                 new Color(0.04f,0.08f,0.16f,0.9f);
                var card  = MakeCard(content, i, CH, bgCol);
                string badge = cl.IsBlacklisted ? " 🚫" : cl.IsVip ? " ⭐" : "";

                MakeTxtPos("Info", card, new Vector2(6,-4), new Vector2(card.sizeDelta.x-8, 62),
                    $"{cl.CompanyName}{badge}\n" +
                    $"Tipo: {GetClientTypeName(cl.ClientType)}  ·  Enojo: {cl.AngerLevel}/5  ·  Entregas: {cl.TotalDeliveries}\n" +
                    $"Relación: {cl.RelationshipLevel:F0}  ·  Exitosas: {cl.SuccessfulDeliveries}  ·  Fallidas: {cl.FailedDeliveries}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
            }
        }

        // ── Populate: Offices ─────────────────────────────────────────────────

        private void PopulateOffices()
        {
            var allCities = CityDatabase.AllCities;
            int pi        = PanelIndex(Panel.Offices);
            if (allCities == null) return;

            var locked = new List<WorldCity>();
            var unlocked = new List<WorldCity>();
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

        // ── Populate: Events ──────────────────────────────────────────────────

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

        // ── Finances ──────────────────────────────────────────────────────────

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
                _finStats.text =
                    $"Completadas:  {carg.CompletedCargos.Count}\n" +
                    $"Fallidas:  {carg.FailedCargos.Count}\n" +
                    $"En tránsito:  {carg.ActiveCargos.Count}\n" +
                    $"En mercado:  {carg.MarketCargos.Count}\n" +
                    $"Tasa de éxito:  {rate * 100:F0}%";
            }
        }

        private void RefreshFinancesIfVisible()
        {
            if (_active == Panel.Finances) RefreshFinances();
        }

        // ── Top bar refresh ───────────────────────────────────────────────────

        private void RefreshTopBar()
        {
            if (_moneyText == null) return;
            var eco  = EconomyManager.Instance;
            var time = FFTimeManager.Instance;

            int  money    = eco?.Money ?? 0;
            int  rep      = eco?.Reputation ?? 0;
            int  day      = time?.CurrentDay ?? 0;
            int  speedIdx = TimeManager.Instance?.CurrentSpeedIndex ?? 1;
            bool locked   = _camController != null && _camController.IsManuallyLocked;
            bool menuOpen = _menuOpen;

            if (money == _tbMoney && rep == _tbRep && day == _tbDay &&
                speedIdx == _tbSpeed && locked == _tbLocked && menuOpen == _tbMenu)
                return;

            if (money != _tbMoney || rep != _tbRep)
            {
                _moneyText.text  = money >= 0 ? $"💰  ${money:N0}" : $"💰  -${Mathf.Abs(money):N0}";
                _moneyText.color = money >= 0 ? new Color(0.2f,1f,0.4f) : new Color(1f,0.3f,0.3f);
                _repText.text    = $"⭐  {rep}/100";
            }
            if (day != _tbDay)
                _dateText.text = $"📅  Día {day}  ·  {time?.GetFormattedDate() ?? "--/--/----"}";
            if (speedIdx != _tbSpeed)
                for (int i = 0; i < _speedBgs.Length; i++)
                    _speedBgs[i].color = (i == speedIdx) ? C_BTN_ON : C_BTN_OFF;
            if (locked != _tbLocked && _lockBg != null)
                _lockBg.color = locked ? C_BTN_ON : C_BTN_OFF;
            if (menuOpen != _tbMenu && _menuBg != null)
                _menuBg.color = menuOpen ? C_BTN_ON : C_BTN_OFF;

            _tbMoney  = money;
            _tbRep    = rep;
            _tbDay    = day;
            _tbSpeed  = speedIdx;
            _tbLocked = locked;
            _tbMenu   = menuOpen;
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void OnEventTriggered(GameEvent evt, Cargo cargo)
        {
            string route = $"{cargo.OriginCityId.Replace('_',' ')} → {cargo.DestinationCityId.Replace('_',' ')}";
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

        private void OnDayPassed()
        {
            if (_active != Panel.None && _active != Panel.Market)
                PopulateActivePanel();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ToggleMenu()
        {
            _menuOpen = !_menuOpen;
            _menuPopupGO?.SetActive(_menuOpen);
        }

        private static string RiskLabel(Cargo c)
        {
            int r = c.EventsEncountered?.Count ?? 0;
            return r == 0 ? "🟢 Bajo" : r == 1 ? "🟡 Medio" : "🔴 Alto";
        }

        private int PanelIndex(Panel p)
        {
            for (int i = 0; i < CONTENT_PANELS.Length; i++)
                if (CONTENT_PANELS[i] == p) return i;
            return -1;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

        private static void SetBarFill(Image fill, float amount, Color col)
        {
            if (fill == null) return;
            fill.fillAmount = Mathf.Clamp01(amount);
            fill.color = col;
        }

        // ── UGUI factory ──────────────────────────────────────────────────────

        private static RectTransform GetOrCreateCanvas()
        {
            // EventSystem check MUST come before any Canvas lookup —
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

        private RectTransform MakeCard(RectTransform content, int idx, float cardH, Color bg)
        {
            float w = content.sizeDelta.x > 0 ? content.sizeDelta.x - 8f : PANEL_W - 8f;
            var card = MakeRect($"Card{idx}", content,
                new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(4f, -(idx * cardH + 4f)), new Vector2(w, cardH - 4f));
            MakeImg(card, bg);
            return card;
        }

        private void MakeBarH(RectTransform parent, Vector2 pos, Vector2 size, float fill, Color col)
        {
            var bg = MakeRect("BarBg", parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), pos, size);
            bg.gameObject.AddComponent<Image>().color = new Color(0.12f,0.12f,0.12f,0.9f);
            var fillRT = new GameObject("BarFill").AddComponent<RectTransform>();
            fillRT.SetParent(bg, false);
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.pivot = new Vector2(0,0.5f); fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var img = fillRT.gameObject.AddComponent<Image>();
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0; img.fillAmount = Mathf.Clamp01(fill); img.color = col;
        }

        private Image MakeBarRow(RectTransform parent, float x, ref float y, float w)
        {
            var bg = MakeRect("Bar", parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(x,y), new Vector2(w,10));
            bg.gameObject.AddComponent<Image>().color = new Color(0.12f,0.12f,0.12f,0.9f);
            var fillRT = new GameObject("Fill").AddComponent<RectTransform>();
            fillRT.SetParent(bg, false);
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.pivot = new Vector2(0,0.5f); fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var img = fillRT.gameObject.AddComponent<Image>();
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0; img.fillAmount = 0f;
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

        private void MakeVSep(RectTransform bar, float rx, float ry, float h)
        {
            var sep = MakeRect("VSep", bar, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                new Vector2(rx,ry), new Vector2(1,h));
            MakeImg(sep, new Color(0.3f,0.3f,0.3f,0.6f));
        }

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
