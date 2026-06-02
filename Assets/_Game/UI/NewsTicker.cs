using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Systems.World;
using UnityEngine;
using UnityEngine.UI;

namespace FreightForwarder.UI
{
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
        private float  _x;
        private float  _textWidth;
        private bool   _scrolling;

        private const float SPEED   = 130f;
        private const float BAR_H   = 28f;
        private const float LABEL_W = 90f;

        // UGUI refs
        private RectTransform _textRT;
        private Text          _tickerText;
        private float         _scrollAreaWidth;

        private static Font _fontCache;
// Devuelve el font
        private static Font _font => _fontCache != null
            ? _fontCache : (_fontCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // Se ejecuta durante Awake al iniciar el componente.

        private void Awake() => BuildUI();

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLevelUp           += OnLevelUp;
                EconomyManager.Instance.OnReputationChanged += OnRepChanged;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += OnGameOver;
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted     += OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed        += OnCargoFailed;
                CargoManager.Instance.OnCargoExpired       += OnCargoExpired;
                CargoManager.Instance.OnCargoAddedToMarket += OnCargoAdded;
            }
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered += OnEventTriggered;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
            if (NewsManager.Instance != null)
                NewsManager.Instance.OnNewsPublished += OnNewsPublished;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLevelUp           -= OnLevelUp;
                EconomyManager.Instance.OnReputationChanged -= OnRepChanged;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= OnGameOver;
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted     -= OnCargoCompleted;
                CargoManager.Instance.OnCargoFailed        -= OnCargoFailed;
                CargoManager.Instance.OnCargoExpired       -= OnCargoExpired;
                CargoManager.Instance.OnCargoAddedToMarket -= OnCargoAdded;
            }
            if (EventManager.Instance != null)
                EventManager.Instance.OnEventTriggered -= OnEventTriggered;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
            if (NewsManager.Instance != null)
                NewsManager.Instance.OnNewsPublished -= OnNewsPublished;
        }

// Se invoca cuando noticias se publica.
        private void OnNewsPublished(NewsItem item)
        {
            Color c = item.Category == NewsCategory.Fuel   ? new Color(1f, 0.72f, 0.32f) :
                      item.Category == NewsCategory.Demand ? new Color(0.5f, 0.9f, 1f)   :
                      item.Category == NewsCategory.Risk   ? new Color(1f, 0.55f, 0.4f)  :
                                                             new Color(0.82f, 0.9f, 1f);
            AddMessage(item.Headline, c);
        }

        // Ejecuta las comprobaciones necesarias en cada fotograma del juego.

        private void Update()
        {
            if (!_scrolling || _textRT == null) return;

            _x -= SPEED * Time.deltaTime;
            _textRT.anchoredPosition = new Vector2(_x, 0f);

            if (_x + _textWidth < 0f)
                StartNext();
        }

        // Construye UI.

        private void BuildUI()
        {
            var canvasRT = GetOrCreateCanvas();

            // Full-width bar anchored to bottom of screen
            var bar = MakeRect("TickerBar", canvasRT,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0),
                Vector2.zero, new Vector2(0, BAR_H));
            MakeImage(bar, new Color(0f, 0.03f, 0.07f, 0.92f));

            // Top border line
            var border = MakeRect("TickerBorder", bar,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                Vector2.zero, new Vector2(0, 1f));
            MakeImage(border, new Color(0.25f, 0.55f, 1f, 0.7f));

            // "► FF NEWS" tag on the left
            var tag = MakeRect("TickerTag", bar,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0),
                Vector2.zero, new Vector2(LABEL_W, 0));
            MakeImage(tag, new Color(0.15f, 0.15f, 0.35f, 1f));

            var tagText = MakeText("TagLabel", tag,
                Vector2.zero, Vector2.zero, "► FF NEWS",
                10, FontStyle.Bold, new Color(0.5f, 0.8f, 1f), TextAnchor.MiddleCenter,
                stretch: true);
            tagText.GetComponent<RectTransform>().offsetMin = new Vector2(4, 2);
            tagText.GetComponent<RectTransform>().offsetMax = new Vector2(-4, -2);

            // Vertical separator
            var sep = MakeRect("TickerSep", bar,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0),
                new Vector2(LABEL_W, 0), new Vector2(1, 0));
            MakeImage(sep, new Color(0.25f, 0.55f, 1f, 0.7f));

            // Scroll area — clips text using a Mask
            var scrollArea = MakeRect("TickerScrollArea", bar,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0),
                new Vector2(LABEL_W + 2f, 0), new Vector2(-(LABEL_W + 2f), 0));
            scrollArea.gameObject.AddComponent<RectMask2D>();

            // The text element that moves horizontally
            _textRT = MakeRect("TickerText", scrollArea,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(Screen.width, 0), new Vector2(1200, 0));
            _tickerText = MakeText("Label", _textRT,
                Vector2.zero, Vector2.zero, "",
                12, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft,
                stretch: true);

            _scrollAreaWidth = Screen.width - LABEL_W - 2f;
        }

        // Añade message

        private void AddMessage(string text, Color color)
        {
            _queue.Enqueue(new TickerMsg { Text = text, Color = color });
            if (!_scrolling) StartNext();
        }

// Inicio next.
        private void StartNext()
        {
            if (_queue.Count == 0) { _scrolling = false; return; }

            var msg = _queue.Dequeue();
            _current      = msg.Text;
            _currentColor = msg.Color;

            if (_tickerText != null)
            {
                _tickerText.text  = _current;
                _tickerText.color = _currentColor;
            }

            // Estimate text width: ~7px per character at font size 12
            _textWidth = _current.Length * 7f;
            if (_textRT != null)
                _textRT.sizeDelta = new Vector2(_textWidth + 20f, 0f);

            _x       = _scrollAreaWidth;
            _scrolling = true;

            if (_textRT != null)
                _textRT.anchoredPosition = new Vector2(_x, 0f);
        }

        // Se invoca cuando el jugador sube de nivel.

        private void OnLevelUp(int level)
            => AddMessage($"⭐  ¡Subiste al Nivel {level}!  Bonus: +${level * 100:N0}  —  Seguís creciendo en el mercado global.",
                          new Color(1f, 0.9f, 0.1f));

// Se invoca cuando cambia la reputación.
        private void OnRepChanged(int value)
        {
            if (value <= 20)
                AddMessage($"⚠️  Reputación crítica: {value}/100  —  Tus clientes están perdiendo la confianza. Completá cargas con urgencia.",
                           new Color(1f, 0.35f, 0.2f));
            // Realiza if
            else if (value <= 40)
                AddMessage($"📉  Reputación en baja: {value}/100  —  Evitá más fallos para no perder contratos.",
                           new Color(1f, 0.6f, 0.2f));
        }

// Se invoca cuando el juego termina.
        private void OnGameOver()
            => AddMessage("💀  GAME OVER  —  Tu empresa no pudo sostenerse. El mercado internacional sigue sin vos.",
                          new Color(1f, 0.15f, 0.15f));

// Se invoca cuando un cargamento se completa.
        private void OnCargoCompleted(Cargo cargo)
        {
            int profit = cargo.FinalPrice - cargo.AgentCost;
            AddMessage($"✅  Entrega exitosa: {Route(cargo)}  —  Ganancia neta: +${profit:N0}",
                       new Color(0.2f, 0.95f, 0.45f));
        }

// Se invoca cuando un cargamento falla.
        private void OnCargoFailed(Cargo cargo)
        {
            if (cargo.WasAbandonedByAgent)
                AddMessage($"🚨  El agente abandonó la carga en tránsito: {Route(cargo)}  —  Penalización aplicada.",
                           new Color(1f, 0.3f, 0.1f));
            else
                AddMessage($"❌  Carga fallida: {Route(cargo)}  —  El envío no llegó a destino.",
                           new Color(1f, 0.4f, 0.2f));
        }

// Se invoca cuando cargamento expira.
        private void OnCargoExpired(Cargo cargo)
            => AddMessage($"⏰  Oferta vencida sin cotizar: {Route(cargo)}  —  El cliente buscó otro operador.",
                          new Color(0.8f, 0.6f, 0.2f));

// Se invoca cuando se agrega cargamento.
        private void OnCargoAdded(Cargo cargo)
        {
            string type  = Constants.GetCargoTypeName(cargo.CargoType);
            AddMessage($"📦  Nueva oferta en el mercado: {type}  ·  {Route(cargo)}  —  Valor: ${cargo.DeclaredValue:N0}",
                       new Color(0.6f, 0.8f, 1f));
        }

// Se invoca cuando se activa un evento.
        private void OnEventTriggered(GameEvent evt, Cargo cargo)
            => AddMessage($"⚠️  Evento: {evt.Name}  en ruta {Route(cargo)}  —  {evt.Description}",
                          new Color(1f, 0.65f, 0.1f));

        private int _dayCounter;
// Se invoca al terminar un día de juego.
        private void OnDayPassed()
        {
            _dayCounter++;
            if (_dayCounter % 7 == 0)
                AddMessage($"📅  Semana {_dayCounter / 7} completada  —  Revisá el mercado: nuevas cargas disponibles.",
                           new Color(0.55f, 0.75f, 1f));
            // Realiza if
            else if (_dayCounter == 30)
                AddMessage("🗓️  Primer mes de operaciones completado  —  ¡Buen comienzo como freight forwarder!",
                           new Color(0.6f, 1f, 0.7f));
        }

// Ruta.
        private static string Route(Cargo c)
            => $"{CityDatabase.DisplayNameOf(c.OriginCityId)} → {CityDatabase.DisplayNameOf(c.DestinationCityId)}";

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
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            return rt;
        }

// Gestiona make imagen.
        private static Image MakeImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text MakeText(string name, RectTransform parent,
            Vector2 offset, Vector2 size, string text,
            int fontSize, FontStyle style, Color color, TextAnchor anchor,
            bool stretch = false)
        {
            RectTransform rt;
            if (stretch)
            {
                var go = new GameObject(name);
                rt = go.AddComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.offsetMin = offset;
                rt.offsetMax = size;
            }
            else
            {
                rt = MakeRect(name, parent,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                    offset, size);
            }
            var t = rt.gameObject.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.color     = color;
            t.alignment = anchor;
            t.font      = _font;
            return t;
        }
    }
}