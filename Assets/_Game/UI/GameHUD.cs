using FreightForwarder.Managers;
using FreightForwarder.Models;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.UI
{
    /// <summary>
    /// HUD permanente del juego. Se dibuja en la esquina superior izquierda
    /// sin interferir con la UI del mapa (botones de velocidad al centro, etc.)
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ── Notificaciones ────────────────────────────────────────────────────
        private class Notification
        {
            public string Text;
            public Color  Color;
            public float  TimeLeft;
        }

        private readonly List<Notification> _notifications = new List<Notification>();
        private const float NOTIF_DURATION = 4f;
        private const float NOTIF_H        = 22f;

        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _panel, _money, _stat, _small, _bar, _notifStyle;
        private Texture2D _barBg, _barFill, _barRep, _barXP;
        private bool _ready;

        // ── Layout ────────────────────────────────────────────────────────────
        private const float X  = 68f;   // deja espacio al sidebar izquierdo
        private const float Y  = 46f;   // deja espacio al top bar
        private const float W  = 210f;

        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged     += OnMoneyChanged;
                EconomyManager.Instance.OnReputationChanged += OnRepChanged;
                EconomyManager.Instance.OnLevelUp          += OnLevelUp;
                EconomyManager.Instance.OnGameOver         += OnGameOver;
            }
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted += OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed    += OnCargoFailed;
                CargoManager.Instance.OnCargoExpired   += OnCargoExpired;
            }
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged      -= OnMoneyChanged;
                EconomyManager.Instance.OnReputationChanged -= OnRepChanged;
                EconomyManager.Instance.OnLevelUp           -= OnLevelUp;
                EconomyManager.Instance.OnGameOver          -= OnGameOver;
            }
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted -= OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed    -= OnCargoFailed;
                CargoManager.Instance.OnCargoExpired   -= OnCargoExpired;
            }
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
            }
        }

        private void Update()
        {
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                _notifications[i].TimeLeft -= Time.deltaTime;
                if (_notifications[i].TimeLeft <= 0f)
                    _notifications.Remove(_notifications[i]);
            }
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            if (EconomyManager.Instance == null) return;
            EnsureStyles();
            DrawHUD();
            DrawNotifications();
        }

        private void DrawHUD()
        {
            var eco  = EconomyManager.Instance;
            var time = FFTimeManager.Instance;
            var carg = CargoManager.Instance;

            // Fondo del panel
            float h = 120f;
            GUI.Box(new Rect(X - 2, Y - 2, W + 4, h + 4), GUIContent.none, _panel);

            float y = Y + 4f;

            // ── Dinero ──
            string moneyStr = eco.Money >= 0
                ? $"${eco.Money:N0}"
                : $"-${Mathf.Abs(eco.Money):N0}";
            Color moneyCol = eco.Money >= 0 ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);

            var prevC = GUI.contentColor;
            GUI.contentColor = moneyCol;
            GUI.Label(new Rect(X + 4, y, W - 8, 26f), $"💰  {moneyStr}", _money);
            GUI.contentColor = prevC;
            y += 28f;

            // ── Reputación ──
            GUI.Label(new Rect(X + 4, y, 80f, 16f), $"Rep  {eco.Reputation}/100", _small);
            DrawBar(new Rect(X + 86, y + 2, W - 94, 12f),
                    eco.Reputation / 100f,
                    eco.Reputation > 50 ? new Color(0.2f, 0.8f, 0.3f) :
                    eco.Reputation > 25 ? new Color(0.9f, 0.7f, 0.1f) :
                                          new Color(0.9f, 0.2f, 0.2f));
            y += 18f;

            // ── Nivel + XP ──
            int xpNeeded = eco.GetXPForNextLevel();
            float xpPct  = xpNeeded > 0 ? (float)eco.CurrentXP / xpNeeded : 0f;
            GUI.Label(new Rect(X + 4, y, 80f, 16f), $"Nv.{eco.Level}  XP", _small);
            DrawBar(new Rect(X + 86, y + 2, W - 94, 12f),
                    xpPct, new Color(0.3f, 0.5f, 1f));
            y += 18f;

            // ── Día + cargas activas ──
            int day       = time?.CurrentDay ?? 0;
            string date   = time?.GetFormattedDate() ?? "--/--/----";
            int enTransito = carg?.ActiveCargos.Count ?? 0;
            int enMercado  = carg?.MarketCargos.Count ?? 0;

            GUI.Label(new Rect(X + 4, y, W - 8, 16f),
                      $"Día {day}  ·  {date}", _small);
            y += 18f;

            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(X + 4, y, W - 8, 16f),
                      $"Mercado: {enMercado}   Tránsito: {enTransito}", _small);
            GUI.contentColor = Color.white;
        }

        // ── Barra de progreso ─────────────────────────────────────────────────
        private void DrawBar(Rect r, float fill, Color fillColor)
        {
            // Fondo
            var prev = GUI.color;
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            // Relleno
            float filled = Mathf.Clamp01(fill) * r.width;
            if (filled > 1f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(r.x, r.y, filled, r.height), Texture2D.whiteTexture);
            }
            GUI.color = prev;
        }

        // ── Notificaciones ────────────────────────────────────────────────────
        private void DrawNotifications()
        {
            float startY = Y + 130f;
            for (int i = 0; i < _notifications.Count && i < 5; i++)
            {
                var n     = _notifications[i];
                float alpha = Mathf.Clamp01(n.TimeLeft / 1.2f);
                var prevC = GUI.contentColor;
                GUI.contentColor = new Color(n.Color.r, n.Color.g, n.Color.b, alpha);
                GUI.Label(new Rect(X, startY + i * (NOTIF_H + 2f), W + 80f, NOTIF_H),
                          n.Text, _notifStyle);
                GUI.contentColor = prevC;
            }
        }

        private void AddNotification(string text, Color color)
        {
            _notifications.Insert(0, new Notification
            {
                Text = text, Color = color, TimeLeft = NOTIF_DURATION
            });
            if (_notifications.Count > 5)
                _notifications.RemoveAt(5);
        }

        // ── Callbacks de eventos ──────────────────────────────────────────────
        private void OnMoneyChanged(int value)
        {
            // Solo notificar cambios grandes
        }

        private void OnRepChanged(int value)
        {
            if (value <= 20)
                AddNotification($"⚠️  Reputación crítica: {value}/100", new Color(1f, 0.3f, 0.2f));
        }

        private void OnLevelUp(int level)
        {
            AddNotification($"⭐  ¡Subiste al Nivel {level}!  +${level * 100:N0}", Color.yellow);
        }

        private void OnGameOver()
        {
            AddNotification("💀  GAME OVER", new Color(1f, 0.2f, 0.2f));
        }

        private void OnCargoCompleted(Cargo cargo)
        {
            int profit = cargo.FinalPrice - cargo.AgentCost;
            AddNotification($"✅  {cargo.OriginCityId.Replace('_', ' ')} → {cargo.DestinationCityId.Replace('_', ' ')}  +${profit:N0}",
                            new Color(0.2f, 0.9f, 0.4f));
        }

        private void OnCargoFailed(Cargo cargo)
        {
            AddNotification($"❌  Falló: {cargo.OriginCityId.Replace('_', ' ')} → {cargo.DestinationCityId.Replace('_', ' ')}",
                            new Color(1f, 0.4f, 0.2f));
        }

        private void OnCargoExpired(Cargo cargo)
        {
            AddNotification($"⏰  Expiró sin cotizar: {cargo.OriginCityId.Replace('_', ' ')} → {cargo.DestinationCityId.Replace('_', ' ')}",
                            new Color(0.8f, 0.6f, 0.2f));
        }

        private void OnEventTriggered(GameEvent evt, Cargo cargo)
        {
            string route = $"{cargo.OriginCityId.Replace('_', ' ')} → {cargo.DestinationCityId.Replace('_', ' ')}";
            AddNotification($"⚠️  {evt.Name}  [{route}]", new Color(1f, 0.6f, 0.1f));
        }

        // ── Styles ────────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_ready) return;
            _ready = true;

            _panel = new GUIStyle(GUI.skin.box);
            _panel.normal.background = MakeTex(new Color(0f, 0.04f, 0.08f, 0.88f));

            _money = new GUIStyle(GUI.skin.label)
                { fontSize = 18, fontStyle = FontStyle.Bold };
            _money.normal.textColor = Color.white;

            _stat = new GUIStyle(GUI.skin.label)
                { fontSize = 13, fontStyle = FontStyle.Bold };
            _stat.normal.textColor = Color.white;

            _small = new GUIStyle(GUI.skin.label)
                { fontSize = 11 };
            _small.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            _notifStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 12, fontStyle = FontStyle.Bold };
            _notifStyle.normal.textColor = Color.white;
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
