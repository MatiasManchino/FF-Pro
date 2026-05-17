using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using UnityEngine;

namespace FreightForwarder.UI
{
    /// <summary>
    /// Cinta de noticias en la parte inferior de la pantalla.
    /// Los mensajes entran por la derecha y salen por la izquierda.
    /// Agregá este componente al GameObject [FF System].
    /// </summary>
    public class NewsTicker : MonoBehaviour
    {
        private struct TickerMsg
        {
            public string Text;
            public Color  Color;
        }

        private readonly Queue<TickerMsg> _queue = new Queue<TickerMsg>();

        private string _current      = "";
        private Color  _currentColor = Color.white;
        private float  _x;           // posición horizontal del texto
        private float  _textWidth    = 0f;
        private bool   _scrolling;
        private bool   _widthReady;

        private const float SPEED    = 130f;   // píxeles por segundo
        private const float BAR_H    = 28f;
        private const float LABEL_W  = 90f;    // ancho del tag "► FF NEWS"

        private GUIStyle _tickerStyle;
        private GUIStyle _tagStyle;
        private bool     _ready;

        // ── Ciclo de vida ─────────────────────────────────────────────────────
        private void Start()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLevelUp           += OnLevelUp;
                EconomyManager.Instance.OnReputationChanged += OnRepChanged;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += OnGameOver;
            }
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted     += OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed        += OnCargoFailed;
                CargoManager.Instance.OnCargoExpired       += OnCargoExpired;
                CargoManager.Instance.OnCargoAddedToMarket += OnCargoAdded;
            }
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
            }
            if (FFTimeManager.Instance != null)
            {
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLevelUp           -= OnLevelUp;
                EconomyManager.Instance.OnReputationChanged -= OnRepChanged;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= OnGameOver;
            }
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted     -= OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed        -= OnCargoFailed;
                CargoManager.Instance.OnCargoExpired       -= OnCargoExpired;
                CargoManager.Instance.OnCargoAddedToMarket -= OnCargoAdded;
            }
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
            }
            if (FFTimeManager.Instance != null)
            {
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
            }
        }

        // ── Update: avanza el scroll ──────────────────────────────────────────
        private void Update()
        {
            if (!_scrolling) return;

            _x -= SPEED * Time.deltaTime;

            // Solo avanza al siguiente cuando ya se midió el ancho real del texto
            if (_widthReady && _x + _textWidth < 0f)
                StartNext();
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();

            float barY = Screen.height - BAR_H;

            // Fondo
            var prev = GUI.color;
            GUI.color = new Color(0f, 0.03f, 0.07f, 0.92f);
            GUI.DrawTexture(new Rect(0, barY, Screen.width, BAR_H), Texture2D.whiteTexture);

            // Línea superior
            GUI.color = new Color(0.25f, 0.55f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(0, barY, Screen.width, 1f), Texture2D.whiteTexture);

            // Tag izquierdo "► FF NEWS"
            GUI.color = new Color(0.15f, 0.15f, 0.35f, 1f);
            GUI.DrawTexture(new Rect(0, barY, LABEL_W, BAR_H), Texture2D.whiteTexture);
            GUI.color = prev;

            GUI.Label(new Rect(4f, barY + 2f, LABEL_W - 4f, BAR_H - 2f), "► FF NEWS", _tagStyle);

            // Separador vertical
            GUI.color = new Color(0.25f, 0.55f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(LABEL_W, barY, 1f, BAR_H), Texture2D.whiteTexture);
            GUI.color = prev;

            if (!_scrolling) return;

            // Medir ancho real la primera vez que se renderiza este mensaje
            if (!_widthReady)
            {
                _textWidth = _tickerStyle.CalcSize(new GUIContent(_current)).x;
                _widthReady = true;
            }

            // Texto con clipping al área de la cinta (a la derecha del tag)
            GUI.BeginGroup(new Rect(LABEL_W + 2f, barY, Screen.width - LABEL_W - 2f, BAR_H));
            var prevC = GUI.contentColor;
            GUI.contentColor = _currentColor;
            GUI.Label(new Rect(_x, 2f, _textWidth + 4f, BAR_H - 2f), _current, _tickerStyle);
            GUI.contentColor = prevC;
            GUI.EndGroup();
        }

        // ── Control de cola ───────────────────────────────────────────────────
        private void AddMessage(string text, Color color)
        {
            _queue.Enqueue(new TickerMsg { Text = text, Color = color });
            if (!_scrolling) StartNext();
        }

        private void StartNext()
        {
            if (_queue.Count == 0) { _scrolling = false; return; }

            var msg        = _queue.Dequeue();
            _current       = msg.Text;
            _currentColor  = msg.Color;
            _x             = Screen.width - LABEL_W;   // empieza en el borde derecho visible
            _textWidth     = Screen.width;              // estimado seguro hasta que OnGUI lo mida
            _widthReady    = false;
            _scrolling     = true;
        }

        // ── Callbacks ─────────────────────────────────────────────────────────
        private void OnLevelUp(int level)
            => AddMessage($"⭐  ¡Subiste al Nivel {level}!  Bonus: +${level * 100:N0}  —  Seguís creciendo en el mercado global.",
                          new Color(1f, 0.9f, 0.1f));

        private void OnRepChanged(int value)
        {
            if (value <= 20)
                AddMessage($"⚠️  Reputación crítica: {value}/100  —  Tus clientes están perdiendo la confianza. Completá cargas con urgencia.",
                           new Color(1f, 0.35f, 0.2f));
            else if (value <= 40)
                AddMessage($"📉  Reputación en baja: {value}/100  —  Evitá más fallos para no perder contratos.",
                           new Color(1f, 0.6f, 0.2f));
        }

        private void OnGameOver()
            => AddMessage("💀  GAME OVER  —  Tu empresa no pudo sostenerse. El mercado internacional sigue sin vos.",
                          new Color(1f, 0.15f, 0.15f));

        private void OnCargoCompleted(Cargo cargo)
        {
            int profit = cargo.FinalPrice - cargo.AgentCost;
            string route = Route(cargo);
            AddMessage($"✅  Entrega exitosa: {route}  —  Ganancia neta: +${profit:N0}",
                       new Color(0.2f, 0.95f, 0.45f));
        }

        private void OnCargoFailed(Cargo cargo)
        {
            string route = Route(cargo);
            if (cargo.WasAbandonedByAgent)
                AddMessage($"🚨  El agente abandonó la carga en tránsito: {route}  —  Penalización aplicada.",
                           new Color(1f, 0.3f, 0.1f));
            else
                AddMessage($"❌  Carga fallida: {route}  —  El envío no llegó a destino.",
                           new Color(1f, 0.4f, 0.2f));
        }

        private void OnCargoExpired(Cargo cargo)
            => AddMessage($"⏰  Oferta vencida sin cotizar: {Route(cargo)}  —  El cliente buscó otro operador.",
                          new Color(0.8f, 0.6f, 0.2f));

        private void OnCargoAdded(Cargo cargo)
        {
            string type  = Constants.GetCargoTypeName(cargo.CargoType);
            string route = Route(cargo);
            AddMessage($"📦  Nueva oferta en el mercado: {type}  ·  {route}  —  Valor: ${cargo.DeclaredValue:N0}",
                       new Color(0.6f, 0.8f, 1f));
        }

        private void OnEventTriggered(GameEvent evt, Cargo cargo)
            => AddMessage($"⚠️  Evento: {evt.Name}  en ruta {Route(cargo)}  —  {evt.Description}",
                          new Color(1f, 0.65f, 0.1f));

        private int _dayCounter;
        private void OnDayPassed()
        {
            _dayCounter++;
            if (_dayCounter % 7 == 0)
                AddMessage($"📅  Semana {_dayCounter / 7} completada  —  Revisá el mercado: nuevas cargas disponibles.",
                           new Color(0.55f, 0.75f, 1f));
            else if (_dayCounter == 30)
                AddMessage("🗓️  Primer mes de operaciones completado  —  ¡Buen comienzo como freight forwarder!",
                           new Color(0.6f, 1f, 0.7f));
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string Route(Cargo c)
            => $"{c.OriginCityId.Replace('_', ' ')} → {c.DestinationCityId.Replace('_', ' ')}";

        // ── Styles ────────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_ready) return;
            _ready = true;

            _tickerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                wordWrap  = false,
            };
            _tickerStyle.normal.textColor = Color.white;

            _tagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _tagStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
        }
    }
}
