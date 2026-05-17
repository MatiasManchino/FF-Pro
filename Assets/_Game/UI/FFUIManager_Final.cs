using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Map;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using static FreightForwarder.Models.Constants;
using UnityEngine;

namespace FreightForwarder.UI
{
    /// <summary>
    /// UI principal: TOP BAR siempre visible + sidebar de navegación + paneles.
    /// Layout: TOP BAR (full width) / SIDEBAR (izquierda) / CONTENT (derecha).
    /// </summary>
    public class FFUIManager : Singleton<FFUIManager>
    {
        // ── Paneles ───────────────────────────────────────────────────────────
        private enum Panel { None, Market, ActiveCargos, Agents, Clients, Finances, Offices, Events }
        private Panel   _active = Panel.None;
        private Vector2 _scroll;

        // ── Menú y referencias de mapa ───────────────────────────────────────
        private bool _menuOpen;
        private MapCameraController _camController;

        // ── Log de eventos ────────────────────────────────────────────────────
        private struct EventLog { public string Text; public Color Color; public int Day; }
        private readonly List<EventLog> _eventLog = new List<EventLog>();
        private const int MAX_EVENT_LOG = 30;

        // ── Referencias ───────────────────────────────────────────────────────
        private MarketPanel _marketPanel;

        // ── Layout ────────────────────────────────────────────────────────────
        public  const float SIDEBAR_W = 62f;
        private const float TOP_H     = 38f;
        private const float TICKER_H  = 28f;
        private const float BTN_H     = 48f;
        private const float PANEL_X   = SIDEBAR_W + 6f;
        private const float PANEL_Y   = TOP_H + 8f;
        private const float PANEL_W   = 340f;

        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _navBtn, _navBtnOn, _topBtn, _topBtnOn;
        private GUIStyle _box, _title, _lbl, _small, _logoStyle, _badgeStyle;
        private bool _ready;

        // ── Init ──────────────────────────────────────────────────────────────
        private void Start()
        {
            _marketPanel   = FindAnyObjectByType<MarketPanel>();
            _camController = FindAnyObjectByType<MapCameraController>();

            // Garantizar que todos los managers FF existan (resiliencia si FFInitializer no está en la escena)
            CityDatabase.Initialize();
            var _1  = GameManager.Instance;
            var _2  = FFTimeManager.Instance;
            var _3  = EconomyManager.Instance;
            var _4  = AgentManager.Instance;
            var _5  = ClientManager.Instance;
            var _6  = CargoManager.Instance;
            var _7  = EventManager.Instance;
            var _8  = RouteManager.Instance;
            GameManager.Instance?.StartNewGame();

            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_menuOpen)          _menuOpen = false;
                else if (_active != Panel.None) SetPanel(Panel.None);
            }
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();
            DrawTopBar();
            DrawSidebar();
            DrawCurrentPanel();
        }

        // ══════════════════════════════════════════════════════════════════════
        // TOP BAR — siempre visible
        // ══════════════════════════════════════════════════════════════════════
        private void DrawTopBar()
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0.03f, 0.08f, 0.97f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, TOP_H), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.5f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(0, TOP_H - 1f, Screen.width, 1f), Texture2D.whiteTexture);
            GUI.color = prev;

            float y  = 4f;
            float bH = TOP_H - 8f;
            float x  = SIDEBAR_W + 8f;

            if (EconomyManager.Instance != null)
            {
                var eco = EconomyManager.Instance;

                string moneyStr = eco.Money >= 0 ? $"💰  ${eco.Money:N0}" : $"💰  -${Mathf.Abs(eco.Money):N0}";
                Color moneyCol  = eco.Money >= 0 ? new Color(0.2f, 1f, 0.45f) : new Color(1f, 0.3f, 0.3f);
                DrawTopLabel(ref x, y, bH, moneyStr, moneyCol, 140f);

                DrawVSep(x, y, bH); x += 8f;
                DrawTopLabel(ref x, y, bH, $"⭐  {eco.Reputation}/100", Color.white, 100f);
                DrawVSep(x, y, bH); x += 8f;
            }

            if (FFTimeManager.Instance != null)
            {
                string date = $"📅  Día {FFTimeManager.Instance.CurrentDay}  ·  {FFTimeManager.Instance.GetFormattedDate()}";
                DrawTopLabel(ref x, y, bH, date, new Color(0.75f, 0.75f, 0.75f), 220f);
            }

            // Botones lado derecho: ☰ Menú | FIJAR | PAUSA x1 x10 x100 x1000
            float rx = Screen.width - 8f;

            // ☰ Menú
            rx -= 64f + 3f;
            if (GUI.Button(new Rect(rx, y, 64f, bH), "☰ Menú", _menuOpen ? _topBtnOn : _topBtn))
                _menuOpen = !_menuOpen;
            rx -= 6f; DrawVSep(rx, y, bH); rx -= 6f;

            // FIJAR
            bool locked = _camController != null && _camController.IsManuallyLocked;
            rx -= 52f + 3f;
            if (GUI.Button(new Rect(rx, y, 52f, bH), "FIJAR", locked ? _topBtnOn : _topBtn))
                _camController?.LockToCurrentPosition();
            rx -= 6f; DrawVSep(rx, y, bH); rx -= 6f;

            // Velocidades del mapa
            rx = MapSpeedBtn(rx, y, bH, "x1000", 4);
            rx = MapSpeedBtn(rx, y, bH, "x100",  3);
            rx = MapSpeedBtn(rx, y, bH, "x10",   2);
            rx = MapSpeedBtn(rx, y, bH, "x1",    1);
            rx = MapSpeedBtn(rx, y, bH, "PAUSA", 0);

            if (_menuOpen) DrawMenuPopup();
        }

        private float MapSpeedBtn(float rx, float y, float h, string label, int idx)
        {
            float w = label == "PAUSA" || label == "x1000" ? 48f : 36f;
            rx -= w + 3f;
            bool on = TimeManager.Instance != null && TimeManager.Instance.CurrentSpeedIndex == idx;
            if (GUI.Button(new Rect(rx, y, w, h), label, on ? _topBtnOn : _topBtn))
                TimeManager.Instance?.SetSpeedIndex(idx);
            return rx;
        }

        private void DrawMenuPopup()
        {
            float pw = 210f;
            float bh = 32f;
            float ph = 5 * (bh + 2f) + 8f;
            float px = Screen.width - pw - 8f;
            float py = TOP_H + 2f;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.1f, 0.97f);
            GUI.DrawTexture(new Rect(px - 4f, py, pw + 8f, ph + 4f), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.5f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(px - 4f, py, pw + 8f, 1f), Texture2D.whiteTexture);
            GUI.color = prev;

            float y = py + 4f;

            if (GUI.Button(new Rect(px, y, pw, bh), "▶  Volver a la partida", _topBtn))
                _menuOpen = false;
            y += bh + 2f;

            if (GUI.Button(new Rect(px, y, pw, bh), "💾  Guardar partida", _topBtn))
            {
                _menuOpen = false;
                Debug.Log("[FFUIManager] Guardar partida — pendiente de implementar");
            }
            y += bh + 2f;

            if (GUI.Button(new Rect(px, y, pw, bh), "📂  Cargar Partida", _topBtn))
            {
                _menuOpen = false;
                Debug.Log("[FFUIManager] Cargar partida — pendiente de implementar");
            }
            y += bh + 2f;

            if (GUI.Button(new Rect(px, y, pw, bh), "⚙  Configuración", _topBtn))
            {
                _menuOpen = false;
                Debug.Log("[FFUIManager] Configuración — pendiente de implementar");
            }
            y += bh + 2f;

            if (GUI.Button(new Rect(px, y, pw, bh), "🚪  Salir", _topBtn))
                Application.Quit();
        }

        private void DrawTopLabel(ref float x, float y, float h, string text, Color color, float w)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(new Rect(x, y, w, h), text, _lbl);
            GUI.contentColor = prev;
            x += w + 4f;
        }

        private void DrawVSep(float x, float y, float h)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, 1f, h), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SIDEBAR — navegación
        // ══════════════════════════════════════════════════════════════════════
        private void DrawSidebar()
        {
            float sideH = Screen.height - TOP_H - TICKER_H;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.1f, 0.96f);
            GUI.DrawTexture(new Rect(0, TOP_H, SIDEBAR_W, sideH), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.5f, 1f, 0.4f);
            GUI.DrawTexture(new Rect(SIDEBAR_W - 1f, TOP_H, 1f, sideH), Texture2D.whiteTexture);
            GUI.color = prev;

            float y = TOP_H + 8f;

            // Logo
            GUI.Label(new Rect(0, y, SIDEBAR_W, 12f), "FREIGHT", _logoStyle);
            GUI.Label(new Rect(0, y + 11f, SIDEBAR_W, 12f), "FORWARDER", _logoStyle);

            prev = GUI.color;
            GUI.color = new Color(0.2f, 0.5f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(6f, y + 24f, SIDEBAR_W - 12f, 1f), Texture2D.whiteTexture);
            GUI.color = prev;
            y += 30f;

            // Botones de navegación
            y = NavBtn(y, "📦", "Mercado",    Panel.Market);
            y = NavBtn(y, "🚚", "Cargas",     Panel.ActiveCargos);
            y = NavBtn(y, "👤", "Agentes",    Panel.Agents);
            y = NavBtn(y, "👥", "Clientes",   Panel.Clients);
            y = NavBtn(y, "💰", "Finanzas",   Panel.Finances);
            y = NavBtn(y, "🏢", "Oficinas",   Panel.Offices);
            y = NavBtn(y, "⚡", "Eventos",    Panel.Events);

            // Contador de eventos no vistos (badge) — Eventos es el 7º botón (índice 6)
            if (_eventLog.Count > 0 && _active != Panel.Events)
            {
                float badgeY = TOP_H + 30f + 6 * (BTN_H + 2f) + BTN_H * 0.5f - 8f;
                prev = GUI.color;
                GUI.color = new Color(1f, 0.3f, 0.2f, 0.9f);
                GUI.DrawTexture(new Rect(SIDEBAR_W - 18f, badgeY, 16f, 16f), Texture2D.whiteTexture);
                GUI.color = prev;
                GUI.Label(new Rect(SIDEBAR_W - 18f, badgeY, 16f, 16f),
                          _eventLog.Count > 9 ? "9+" : _eventLog.Count.ToString(), _badgeStyle);
            }
        }

        private float NavBtn(float y, string icon, string label, Panel panel)
        {
            bool on = _active == panel;
            if (GUI.Button(new Rect(2f, y, SIDEBAR_W - 4f, BTN_H),
                           $"{icon}\n{label}", on ? _navBtnOn : _navBtn))
                SetPanel(on ? Panel.None : panel);
            return y + BTN_H + 2f;
        }

        // ── Routing ───────────────────────────────────────────────────────────
        private void SetPanel(Panel panel)
        {
            _active    = panel;
            _scroll    = Vector2.zero;
            _menuOpen  = false;
            _marketPanel?.SetVisible(panel == Panel.Market);
        }

        private void DrawCurrentPanel()
        {
            switch (_active)
            {
                case Panel.ActiveCargos: DrawActiveCargos(); break;
                case Panel.Agents:       DrawAgents();       break;
                case Panel.Clients:      DrawClients();      break;
                case Panel.Finances:     DrawFinances();     break;
                case Panel.Offices:      DrawOffices();      break;
                case Panel.Events:       DrawEvents();       break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL: CARGAS ACTIVAS
        // ══════════════════════════════════════════════════════════════════════
        private void DrawActiveCargos()
        {
            var cargos   = CargoManager.Instance?.ActiveCargos;
            int count    = cargos?.Count ?? 0;
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 0;

            float cardH  = 88f;
            float listH  = Mathf.Min(count * cardH + 8f, Screen.height - PANEL_Y - TICKER_H - 50f);
            float totalH = 36f + (count == 0 ? 28f : listH);

            GUI.Box(new Rect(PANEL_X, PANEL_Y, PANEL_W, totalH), GUIContent.none, _box);

            float x = PANEL_X + 8f;
            float y = PANEL_Y + 6f;
            GUI.Label(new Rect(x, y, PANEL_W - 16, 22f),
                      $"🚚  CARGAS EN TRÁNSITO  ·  {count}", _title);
            y += 28f;

            if (count == 0)
            {
                GUI.Label(new Rect(x, y, PANEL_W - 16, 22f),
                          "No hay cargas en tránsito. Cotizá una desde el Mercado.", _small);
                return;
            }

            _scroll = GUI.BeginScrollView(
                new Rect(PANEL_X + 4, y, PANEL_W - 8, listH), _scroll,
                new Rect(0, 0, PANEL_W - 24, count * cardH));

            for (int i = 0; i < count; i++)
            {
                var c   = cargos[i];
                var card = new Rect(2, i * cardH + 2f, PANEL_W - 28, cardH - 4f);
                DrawRect(card, new Color(0.04f, 0.09f, 0.18f, 0.9f));

                float tx = card.x + 6f, ty = card.y + 4f, tw = card.width - 8f;

                GUI.Label(new Rect(tx, ty, tw, 18f),
                          $"{c.OriginCityId.Replace('_',' ')} → {c.DestinationCityId.Replace('_',' ')}", _lbl);
                ty += 20f;

                GUI.Label(new Rect(tx, ty, tw, 15f),
                          $"{GetCargoTypeName(c.CargoType)}  ·  {GetTransportModeName(c.TransportMode)}  ·  {c.Weight:F0} t  ·  ${c.FinalPrice:N0}",
                          _small);
                ty += 17f;

                float pct      = c.TotalTransitDays > 0
                    ? Mathf.Clamp01((float)Mathf.Max(0, currentDay - c.StartDay) / c.TotalTransitDays) : 0f;
                Color barCol   = pct < 0.5f ? new Color(0.2f, 0.6f, 1f) : new Color(0.2f, 0.9f, 0.4f);

                GUI.Label(new Rect(tx, ty, 110f, 14f), $"Llega en {Mathf.Max(0, c.DaysRemaining)} días", _small);
                DrawBar(new Rect(tx + 114f, ty + 1f, tw - 114f, 11f), pct, barCol);
                ty += 16f;

                GUI.Label(new Rect(tx, ty, tw, 14f),
                          $"Agente: {c.AgentId.Replace('_',' ')}  ·  Margen: {c.Margin*100:F0}%  ·  Riesgo: {RiskLabel(c)}",
                          _small);
            }
            GUI.EndScrollView();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL: AGENTES
        // ══════════════════════════════════════════════════════════════════════
        private void DrawAgents()
        {
            var dict   = AgentManager.Instance?.GetAllAgents();
            var agents = dict?.Values;
            int count  = agents?.Count ?? 0;

            float cardH  = 68f;
            float listH  = Mathf.Min(count * cardH + 8f, Screen.height - PANEL_Y - TICKER_H - 50f);
            float totalH = 36f + (count == 0 ? 28f : listH);

            GUI.Box(new Rect(PANEL_X, PANEL_Y, PANEL_W, totalH), GUIContent.none, _box);

            float x = PANEL_X + 8f, y = PANEL_Y + 6f;
            GUI.Label(new Rect(x, y, PANEL_W - 16, 22f), $"👤  AGENTES  ·  {count}", _title);
            y += 28f;

            if (count == 0) { GUI.Label(new Rect(x, y, PANEL_W - 16, 22f), "Sin agentes.", _small); return; }

            _scroll = GUI.BeginScrollView(
                new Rect(PANEL_X + 4, y, PANEL_W - 8, listH), _scroll,
                new Rect(0, 0, PANEL_W - 24, count * cardH));

            int ai = 0;
            foreach (var a in agents)
            {
                int i = ai++;
                var card = new Rect(2, i * cardH + 2f, PANEL_W - 28, cardH - 4f);
                DrawRect(card, new Color(0.04f, 0.08f, 0.17f, 0.9f));

                float tx = card.x + 6f, ty = card.y + 4f, tw = card.width - 8f;

                GUI.Label(new Rect(tx, ty, tw, 18f),
                          $"{a.GetStateEmoji()}  {a.Name}  —  {GetAgentPersonalityName(a.Personality)}", _lbl);
                ty += 20f;

                float trust = Mathf.Clamp01(a.PlayerTrust / 100f);
                GUI.Label(new Rect(tx, ty, 100f, 14f), $"Confianza: {a.PlayerTrust:F0}", _small);
                DrawBar(new Rect(tx + 104f, ty + 1f, tw - 104f, 11f), trust,
                    trust > 0.6f ? new Color(0.2f, 0.8f, 0.3f) :
                    trust > 0.3f ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.9f, 0.2f, 0.2f));
                ty += 17f;

                GUI.Label(new Rect(tx, ty, tw, 14f),
                          $"Estado: {GetAgentStateName(a.CurrentState)}  ·  Cargas: {a.CurrentCargoIds.Count}  ·  Precio: x{a.GetCurrentPriceMultiplier():F2}",
                          _small);
            }
            GUI.EndScrollView();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL: CLIENTES
        // ══════════════════════════════════════════════════════════════════════
        private void DrawClients()
        {
            var clients = ClientManager.Instance?.ActiveClients;
            int count   = clients?.Count ?? 0;

            float cardH  = 72f;
            float listH  = Mathf.Min(count * cardH + 8f, Screen.height - PANEL_Y - TICKER_H - 80f);
            float totalH = 36f + 40f + (count == 0 ? 28f : listH);

            GUI.Box(new Rect(PANEL_X, PANEL_Y, PANEL_W, totalH), GUIContent.none, _box);

            float x = PANEL_X + 8f, y = PANEL_Y + 6f, w = PANEL_W - 16f;
            GUI.Label(new Rect(x, y, w, 22f), $"👥  CLIENTES  ·  {count} activos", _title);
            y += 28f;

            // Estadísticas rápidas
            int blacklisted = 0, vip = 0;
            if (clients != null)
                foreach (var cl in clients) { if (cl.IsBlacklisted) blacklisted++; if (cl.IsVip) vip++; }

            DrawRect(new Rect(x, y, w, 28f), new Color(0.05f, 0.08f, 0.15f, 0.8f));
            GUI.Label(new Rect(x + 6f, y + 4f, w - 12f, 20f),
                      $"⭐ VIP: {vip}   🚫 Bloqueados: {blacklisted}   📋 Total: {count}", _small);
            y += 34f;

            if (count == 0)
            {
                GUI.Label(new Rect(x, y, w, 22f), "Aún no hay clientes registrados.", _small);
                return;
            }

            _scroll = GUI.BeginScrollView(
                new Rect(PANEL_X + 4, y, PANEL_W - 8, listH), _scroll,
                new Rect(0, 0, PANEL_W - 24, count * cardH));

            for (int i = 0; i < count; i++)
            {
                var cl   = clients[i];
                var card = new Rect(2, i * cardH + 2f, PANEL_W - 28, cardH - 4f);
                Color bgCol = cl.IsBlacklisted ? new Color(0.15f, 0.03f, 0.03f, 0.9f) :
                              cl.IsVip         ? new Color(0.05f, 0.12f, 0.05f, 0.9f) :
                                                 new Color(0.04f, 0.08f, 0.16f, 0.9f);
                DrawRect(card, bgCol);

                float tx = card.x + 6f, ty = card.y + 4f, tw = card.width - 8f;

                string badge = cl.IsBlacklisted ? " 🚫" : cl.IsVip ? " ⭐" : "";
                GUI.Label(new Rect(tx, ty, tw, 18f),
                          $"{cl.CompanyName}{badge}", _lbl);
                ty += 20f;

                GUI.Label(new Rect(tx, ty, tw, 15f),
                          $"Tipo: {GetClientTypeName(cl.ClientType)}  ·  Enojo: {cl.AngerLevel}/5  ·  Entregas: {cl.TotalDeliveries}",
                          _small);
                ty += 17f;

                GUI.Label(new Rect(tx, ty, tw, 14f),
                          $"Relación: {cl.RelationshipLevel:F0}  ·  Exitosas: {cl.SuccessfulDeliveries}  ·  Fallidas: {cl.FailedDeliveries}",
                          _small);
            }
            GUI.EndScrollView();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL: FINANZAS
        // ══════════════════════════════════════════════════════════════════════
        private void DrawFinances()
        {
            float totalH = 286f;
            GUI.Box(new Rect(PANEL_X, PANEL_Y, PANEL_W, totalH), GUIContent.none, _box);

            float x = PANEL_X + 12f, y = PANEL_Y + 8f, w = PANEL_W - 24f;

            GUI.Label(new Rect(x, y, w, 22f), "💰  FINANZAS", _title);
            y += 28f;

            if (EconomyManager.Instance == null) return;
            var eco = EconomyManager.Instance;

            Color moneyCol  = eco.Money >= 0 ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
            string moneyStr = eco.Money >= 0 ? $"${eco.Money:N0}" : $"-${Mathf.Abs(eco.Money):N0}";
            DrawKV(x, ref y, w, "Liquidez",   moneyStr, moneyCol);
            DrawKV(x, ref y, w, "Reputación", $"{eco.Reputation}/100", Color.white);
            DrawBar(new Rect(x, y, w, 10f), eco.Reputation / 100f,
                eco.Reputation > 50 ? new Color(0.2f, 0.8f, 0.3f) :
                eco.Reputation > 25 ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.9f, 0.2f, 0.2f));
            y += 14f;

            int   xpNeeded = eco.GetXPForNextLevel();
            float xpPct    = xpNeeded > 0 ? (float)eco.CurrentXP / xpNeeded : 0f;
            DrawKV(x, ref y, w, "Nivel", $"{eco.Level}  (XP {eco.CurrentXP}/{xpNeeded})", Color.white);
            DrawBar(new Rect(x, y, w, 10f), xpPct, new Color(0.3f, 0.5f, 1f));
            y += 18f;

            var carg = CargoManager.Instance;
            if (carg != null)
            {
                DrawSep(x, y, w); y += 10f;
                DrawKV(x, ref y, w, "Completadas",   $"{carg.CompletedCargos.Count}", new Color(0.2f, 0.9f, 0.4f));
                DrawKV(x, ref y, w, "Fallidas",      $"{carg.FailedCargos.Count}",    new Color(1f, 0.4f, 0.2f));
                DrawKV(x, ref y, w, "En tránsito",   $"{carg.ActiveCargos.Count}",    new Color(0.4f, 0.7f, 1f));
                DrawKV(x, ref y, w, "En mercado",    $"{carg.MarketCargos.Count}",    Color.white);

                float rate = carg.GetSuccessRate();
                DrawKV(x, ref y, w, "Tasa de éxito", $"{rate * 100:F0}%",
                    rate > 0.7f ? new Color(0.2f, 0.9f, 0.4f) :
                    rate > 0.4f ? new Color(0.9f, 0.7f, 0.1f) : new Color(1f, 0.3f, 0.2f));
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL: OFICINAS (ciudades desbloqueables)
        // ══════════════════════════════════════════════════════════════════════
        private void DrawOffices()
        {
            var allCities = CityDatabase.AllCities;
            if (allCities == null) return;

            var locked   = new List<WorldCity>();
            var unlocked = new List<WorldCity>();
            foreach (var c in allCities.Values)
                (c.IsUnlocked ? unlocked : locked).Add(c);

            float cardH  = 54f;
            int   count  = locked.Count;
            float listH  = Mathf.Min(count * cardH + 8f, Screen.height - PANEL_Y - TICKER_H - 80f);
            float totalH = 36f + 34f + (count == 0 ? 28f : listH);

            GUI.Box(new Rect(PANEL_X, PANEL_Y, PANEL_W, totalH), GUIContent.none, _box);

            float x = PANEL_X + 8f, y = PANEL_Y + 6f, w = PANEL_W - 16f;
            GUI.Label(new Rect(x, y, w, 22f), "🏢  OFICINAS", _title);
            y += 28f;

            DrawRect(new Rect(x, y, w, 26f), new Color(0.05f, 0.1f, 0.18f, 0.8f));
            GUI.Label(new Rect(x + 6f, y + 4f, w - 12f, 18f),
                      $"Activas: {unlocked.Count}  ·  Por desbloquear: {locked.Count}", _small);
            y += 32f;

            if (count == 0)
            {
                GUI.Label(new Rect(x, y, w, 22f), "✅  Todas las ciudades están desbloqueadas.", _small);
                return;
            }

            _scroll = GUI.BeginScrollView(
                new Rect(PANEL_X + 4, y, PANEL_W - 8, listH), _scroll,
                new Rect(0, 0, PANEL_W - 24, count * cardH));

            int currentMoney = EconomyManager.Instance?.Money ?? 0;

            for (int i = 0; i < count; i++)
            {
                var city = locked[i];
                var card = new Rect(2, i * cardH + 2f, PANEL_W - 28, cardH - 4f);
                DrawRect(card, new Color(0.04f, 0.08f, 0.15f, 0.9f));

                float tx = card.x + 6f, ty = card.y + 4f, tw = card.width - 80f;

                GUI.Label(new Rect(tx, ty, tw, 18f), $"📍  {city.DisplayName}  ·  {city.Country}", _lbl);
                ty += 20f;

                string infra = "";
                if (city.HasPort)    infra += "⚓ Puerto  ";
                if (city.HasAirport) infra += "✈ Aeropuerto  ";
                if (city.IsLandHub)  infra += "🚛 Tierra";
                GUI.Label(new Rect(tx, ty, tw, 14f), infra.Length > 0 ? infra : "Sin infraestructura especial", _small);

                // Botón desbloquear
                bool canAfford  = currentMoney >= city.UnlockCost;
                string btnLabel = $"${city.UnlockCost:N0}\n🔓 Abrir";
                var btnRect     = new Rect(card.xMax - 74f, card.y + 8f, 70f, 36f);

                var prevC = GUI.contentColor;
                GUI.contentColor = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                GUI.enabled = canAfford;
                if (GUI.Button(btnRect, btnLabel))
                {
                    EconomyManager.Instance?.SubtractMoney(city.UnlockCost);
                    CargoManager.Instance?.UnlockCity(city.Id);
                    _scroll = Vector2.zero;
                }
                GUI.enabled = true;
                GUI.contentColor = prevC;
            }
            GUI.EndScrollView();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL: EVENTOS
        // ══════════════════════════════════════════════════════════════════════
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
        }

        private void DrawEvents()
        {
            int count    = _eventLog.Count;
            float cardH  = 56f;
            float listH  = Mathf.Min(count * cardH + 8f, Screen.height - PANEL_Y - TICKER_H - 50f);
            float totalH = 36f + (count == 0 ? 36f : listH);

            GUI.Box(new Rect(PANEL_X, PANEL_Y, PANEL_W, totalH), GUIContent.none, _box);

            float x = PANEL_X + 8f, y = PANEL_Y + 6f, w = PANEL_W - 16f;
            GUI.Label(new Rect(x, y, w, 22f), $"⚡  EVENTOS  ·  {count} registrados", _title);
            y += 28f;

            if (count == 0)
            {
                GUI.Label(new Rect(x, y, w, 28f),
                          "Sin eventos recientes. Los eventos aparecen durante el tránsito de cargas.", _small);
                return;
            }

            _scroll = GUI.BeginScrollView(
                new Rect(PANEL_X + 4, y, PANEL_W - 8, listH), _scroll,
                new Rect(0, 0, PANEL_W - 24, count * cardH));

            for (int i = 0; i < count; i++)
            {
                var ev   = _eventLog[i];
                var card = new Rect(2, i * cardH + 2f, PANEL_W - 28, cardH - 4f);
                DrawRect(card, new Color(0.1f, 0.06f, 0.02f, 0.9f));

                float tx = card.x + 6f, ty = card.y + 4f, tw = card.width - 60f;

                var prevC = GUI.contentColor;
                GUI.contentColor = ev.Color;
                GUI.Label(new Rect(tx, ty, tw, 34f), ev.Text, _small);
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(card.xMax - 54f, ty, 50f, 16f), $"Día {ev.Day}", _small);
                GUI.contentColor = prevC;
            }
            GUI.EndScrollView();
        }

        // ── Helpers de dibujo ─────────────────────────────────────────────────
        private static string RiskLabel(Cargo c)
        {
            int risk = c.EventsEncountered?.Count ?? 0;
            return risk == 0 ? "🟢 Bajo" : risk == 1 ? "🟡 Medio" : "🔴 Alto";
        }

        private void DrawKV(float x, ref float y, float w, string key, string val, Color valColor)
        {
            GUI.Label(new Rect(x, y, w * 0.48f, 18f), key, _small);
            var prev = GUI.contentColor;
            GUI.contentColor = valColor;
            GUI.Label(new Rect(x + w * 0.48f, y, w * 0.52f, 18f), val, _lbl);
            GUI.contentColor = prev;
            y += 20f;
        }

        private void DrawBar(Rect r, float fill, Color color)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            float filled = Mathf.Clamp01(fill) * r.width;
            if (filled > 0f) { GUI.color = color; GUI.DrawTexture(new Rect(r.x, r.y, filled, r.height), Texture2D.whiteTexture); }
            GUI.color = prev;
        }

        private void DrawRect(Rect r, Color c)
        {
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void DrawSep(float x, float y, float w)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            GUI.DrawTexture(new Rect(x, y, w, 1f), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // ── Styles ────────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_ready) return;
            _ready = true;

            _navBtn = new GUIStyle(GUI.skin.button)
                { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _navBtn.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            _navBtnOn = new GUIStyle(_navBtn);
            _navBtnOn.normal.background = MakeTex(new Color(0.1f, 0.35f, 0.8f, 0.95f));
            _navBtnOn.normal.textColor  = Color.white;

            _topBtn = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
            _topBtn.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _topBtnOn = new GUIStyle(_topBtn);
            _topBtnOn.normal.background = MakeTex(new Color(0.15f, 0.4f, 0.85f));
            _topBtnOn.normal.textColor  = Color.white;

            _box = new GUIStyle(GUI.skin.box);
            _box.normal.background = MakeTex(new Color(0f, 0.04f, 0.1f, 0.93f));

            _title = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            _title.normal.textColor = Color.white;

            _lbl = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            _lbl.normal.textColor = Color.white;

            _small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            _small.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            _logoStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 8, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _logoStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);

            _badgeStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _badgeStyle.normal.textColor = Color.white;
        }

        private static Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new[] { col, col, col, col });
            t.Apply();
            return t;
        }
    }
}
