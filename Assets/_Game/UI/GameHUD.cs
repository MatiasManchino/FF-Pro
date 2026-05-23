using FreightForwarder.Managers;
using FreightForwarder.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FreightForwarder.UI
{
    public class GameHUD : MonoBehaviour
    {
        private class Notification
        {
            public string Text;
            public Color  Color;
            public float  TimeLeft;
        }

        private readonly List<Notification> _notifications = new List<Notification>();
        private const float NOTIF_DURATION = 4f;
        private const int   MAX_NOTIFS     = 5;

        // UGUI refs
        private Text  _moneyText;
        private Text  _repLabel;
        private Image _repFill;
        private Text  _xpLabel;
        private Image _xpFill;
        private Text  _dateText;
        private Text  _cargoText;
        private readonly List<Text> _notifTexts = new List<Text>();
        private bool _dirty = true;
        private int  _lastDay = -1;

        private static Font _fontCache;
        private static Font _font => _fontCache != null
            ? _fontCache : (_fontCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // Layout constants matching old IMGUI positions
        private const float PANEL_X = 68f;
        private const float PANEL_Y = 46f;
        private const float PANEL_W = 214f;
        private const float PANEL_H = 110f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()  => BuildUI();
        private void Start()  => SubscribeEvents();

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged      -= OnMoneyChanged;
                EconomyManager.Instance.OnReputationChanged -= OnRepChanged;
                EconomyManager.Instance.OnLevelUp           -= OnLevelUp;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= OnGameOver;
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted -= OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed    -= OnCargoFailed;
                CargoManager.Instance.OnCargoExpired   -= OnCargoExpired;
            }
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
        }

        private void Update()
        {
            bool anyAlive = false;
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                _notifications[i].TimeLeft -= Time.deltaTime;
                if (_notifications[i].TimeLeft <= 0f) _notifications.RemoveAt(i);
                else anyAlive = true;
            }

            int today = FFTimeManager.Instance?.CurrentDay ?? 0;
            if (today != _lastDay) { _lastDay = today; _dirty = true; }

            if (_dirty || anyAlive)
            {
                RefreshDisplay();
                _dirty = false;
            }
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasRT = GetOrCreateCanvas();

            // HUD panel — top-left, matching old IMGUI offset
            var panel = MakeRect("HUDPanel", canvasRT,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(PANEL_X, -PANEL_Y), new Vector2(PANEL_W, PANEL_H));
            MakeImage(panel, new Color(0f, 0.04f, 0.08f, 0.88f));

            float y = -4f;

            _moneyText = MakeText("Money", panel, new Vector2(4, y), new Vector2(PANEL_W - 8, 26),
                "$0", 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            y -= 28f;

            _repLabel = MakeText("RepLabel", panel, new Vector2(4, y), new Vector2(80, 16),
                "Rep 0/100", 11, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleLeft);
            _repFill = MakeBar("RepBar", panel, new Vector2(86, y - 2), new Vector2(PANEL_W - 94, 12));
            y -= 18f;

            _xpLabel = MakeText("XPLabel", panel, new Vector2(4, y), new Vector2(80, 16),
                "Nv.1  XP", 11, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleLeft);
            _xpFill = MakeBar("XPBar", panel, new Vector2(86, y - 2), new Vector2(PANEL_W - 94, 12));
            y -= 18f;

            _dateText = MakeText("Date", panel, new Vector2(4, y), new Vector2(PANEL_W - 8, 16),
                "Día 0  ·  --/--/----", 11, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleLeft);
            y -= 18f;

            _cargoText = MakeText("CargoStats", panel, new Vector2(4, y), new Vector2(PANEL_W - 8, 16),
                "Mercado: 0   Tránsito: 0", 11, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleLeft);

            // Notification container directly below the panel
            var notifRT = MakeRect("HUDNotifications", canvasRT,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(PANEL_X, -(PANEL_Y + PANEL_H + 4f)),
                new Vector2(PANEL_W + 80f, MAX_NOTIFS * 24f));

            for (int i = 0; i < MAX_NOTIFS; i++)
            {
                var nt = MakeText($"Notif_{i}", notifRT,
                    new Vector2(0, -i * 24f), new Vector2(PANEL_W + 80f, 22f),
                    "", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                _notifTexts.Add(nt);
            }
        }

        private void RefreshDisplay()
        {
            if (_moneyText == null) return;

            var eco  = EconomyManager.Instance;
            var time = FFTimeManager.Instance;
            var carg = CargoManager.Instance;

            if (eco != null)
            {
                _moneyText.text  = eco.Money >= 0
                    ? $"💰  ${eco.Money:N0}"
                    : $"💰  -${Mathf.Abs(eco.Money):N0}";
                _moneyText.color = eco.Money >= 0
                    ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);

                _repLabel.text = $"Rep  {eco.Reputation}/100";
                SetBar(_repFill, eco.Reputation / 100f,
                    eco.Reputation > 50 ? new Color(0.2f, 0.8f, 0.3f) :
                    eco.Reputation > 25 ? new Color(0.9f, 0.7f, 0.1f) :
                                          new Color(0.9f, 0.2f, 0.2f));

                int   xpNeeded = eco.GetXPForNextLevel();
                float xpPct    = xpNeeded > 0 ? (float)eco.CurrentXP / xpNeeded : 0f;
                _xpLabel.text  = $"Nv.{eco.Level}  XP";
                SetBar(_xpFill, xpPct, new Color(0.3f, 0.5f, 1f));
            }

            _dateText.text  = $"Día {time?.CurrentDay ?? 0}  ·  {time?.GetFormattedDate() ?? "--/--/----"}";
            _cargoText.text = $"Mercado: {carg?.MarketCargos.Count ?? 0}   Tránsito: {carg?.ActiveCargos.Count ?? 0}";

            for (int i = 0; i < MAX_NOTIFS; i++)
            {
                if (i < _notifications.Count)
                {
                    var n     = _notifications[i];
                    float a   = Mathf.Clamp01(n.TimeLeft / 1.2f);
                    _notifTexts[i].text  = n.Text;
                    _notifTexts[i].color = new Color(n.Color.r, n.Color.g, n.Color.b, a);
                }
                else
                {
                    _notifTexts[i].text = "";
                }
            }
        }

        // ── Notifications ─────────────────────────────────────────────────────

        private void AddNotification(string text, Color color)
        {
            _notifications.Insert(0, new Notification { Text = text, Color = color, TimeLeft = NOTIF_DURATION });
            if (_notifications.Count > MAX_NOTIFS) _notifications.RemoveAt(MAX_NOTIFS);
        }

        // ── Event subscriptions ───────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged      += OnMoneyChanged;
                EconomyManager.Instance.OnReputationChanged += OnRepChanged;
                EconomyManager.Instance.OnLevelUp           += OnLevelUp;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += OnGameOver;
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted += OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed    += OnCargoFailed;
                CargoManager.Instance.OnCargoExpired   += OnCargoExpired;
            }
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
        }

        private void OnMoneyChanged(int v) { _dirty = true; }
        private void OnRepChanged(int v)   { _dirty = true; if (v <= 20) AddNotification($"⚠️  Reputación crítica: {v}/100", new Color(1f, 0.3f, 0.2f)); }
        private void OnLevelUp(int l)      { _dirty = true; AddNotification($"⭐  ¡Subiste al Nivel {l}!  +${l * 100:N0}", Color.yellow); }
        private void OnGameOver()          { AddNotification("💀  GAME OVER", new Color(1f, 0.2f, 0.2f)); }

        private void OnCargoCompleted(Cargo c)
            { _dirty = true; AddNotification($"✅  {Route(c)}  +${c.FinalPrice - c.AgentCost:N0}", new Color(0.2f, 0.9f, 0.4f)); }
        private void OnCargoFailed(Cargo c)
            { _dirty = true; AddNotification($"❌  Falló: {Route(c)}", new Color(1f, 0.4f, 0.2f)); }
        private void OnCargoExpired(Cargo c)
            { _dirty = true; AddNotification($"⏰  Expiró: {Route(c)}", new Color(0.8f, 0.6f, 0.2f)); }
        private void OnEventTriggered(GameEvent e, Cargo c)
            => AddNotification($"⚠️  {e.Name}  [{Route(c)}]", new Color(1f, 0.6f, 0.1f));

        private static string Route(Cargo c)
            => $"{c.OriginCityId.Replace('_', ' ')} → {c.DestinationCityId.Replace('_', ' ')}";

        // ── UGUI factory helpers ──────────────────────────────────────────────

        private static RectTransform GetOrCreateCanvas()
        {
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var existing = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (existing != null)
            {
                if (existing.GetComponent<GraphicRaycaster>() == null)
                    existing.gameObject.AddComponent<GraphicRaycaster>();
                var existCs = existing.GetComponent<CanvasScaler>();
                if (existCs != null)
                {
                    existCs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    existCs.referenceResolution = new Vector2(1280, 720);
                    existCs.matchWidthOrHeight  = 0.5f;
                }
                existing.sortingOrder = 10;
                return existing.GetComponent<RectTransform>();
            }

            var cgo = new GameObject("UICanvas");
            var c   = cgo.AddComponent<Canvas>();
            c.renderMode   = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;

            var cs = cgo.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280, 720);
            cs.matchWidthOrHeight  = 0.5f;

            cgo.AddComponent<GraphicRaycaster>();
            return cgo.GetComponent<RectTransform>();
        }

        private static RectTransform MakeRect(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            return rt;
        }

        private static Image MakeImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text MakeText(string name, RectTransform parent,
            Vector2 offset, Vector2 size, string text,
            int fontSize, FontStyle style, Color color, TextAnchor anchor)
        {
            var rt = MakeRect(name, parent,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                offset, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.color     = color;
            t.alignment = anchor;
            t.font      = _font;
            return t;
        }

        private static Image MakeBar(string name, RectTransform parent, Vector2 offset, Vector2 size)
        {
            // Background
            var bgRT = MakeRect(name + "_BG", parent,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                offset, size);
            bgRT.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // Fill — uses Image.Filled so fillAmount drives the bar width
            var fillRT = MakeRect(name + "_Fill", bgRT,
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero);
            var fill = fillRT.gameObject.AddComponent<Image>();
            fill.type        = Image.Type.Filled;
            fill.fillMethod  = Image.FillMethod.Horizontal;
            fill.fillOrigin  = 0;
            fill.fillAmount  = 0f;
            return fill;
        }

        private static void SetBar(Image fill, float amount, Color color)
        {
            fill.fillAmount = Mathf.Clamp01(amount);
            fill.color      = color;
        }
    }
}
