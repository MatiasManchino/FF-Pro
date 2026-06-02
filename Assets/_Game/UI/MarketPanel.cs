using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Systems.Maritime;
using UnityEngine;
using UnityEngine.UI;

namespace FreightForwarder.UI
{
    public class MarketPanel : MonoBehaviour
    {
        // Estado.
        private enum State { Market, Quoting, Result }
        private State _state = State.Market;

        private Cargo  _selectedCargo;
        private bool   _visible;

        // ── Quote form state ──────────────────────────────────────────────────
        private Constants.TransportMode   _mode;
        private string                    _agentId    = "";
        private string                    _priceInput = "";
        private int                       _agentCost;
        private float                     _margin;
        private float                     _currentDist;
        private Constants.TransportMode[] _availableModes;
        private List<Agent>               _availableAgents = new List<Agent>();
        private Text                      _agentCostLabel;

        // ── Maritime ruta selection estado ────────────────────────────────────
        private bool                   _isMaritime;
        private List<ShipmentOption>   _maritimeOptions = new List<ShipmentOption>();
        private int                    _selectedOptionIdx;
        private RectTransform          _routeOptsContent;
        private Image[]                _routeOptBtnBgs;

        // ── Resultado estado ──────────────────────────────────────────────────────
        private string _resultMsg = "";
        private bool   _resultOk;
        private Quote  _lastQuote;

        // ── Layout ────────────────────────────────────────────────────────────
        private const float PX = FFUIManager.SIDEBAR_W + 6f;
        private const float PY = 46f;
        private const float PW = 320f;

        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color C_BG     = new Color(0f, 0.05f, 0.10f, 0.93f);
        private static readonly Color C_BTN    = new Color(0.10f, 0.12f, 0.15f, 0.92f);
        private static readonly Color C_BTN_ON = new Color(0.10f, 0.35f, 0.80f, 0.95f);
        private static readonly Color C_GREEN  = new Color(0.10f, 0.45f, 0.15f, 0.95f);
        private static readonly Color C_RED    = new Color(0.45f, 0.10f, 0.10f, 0.95f);
        private static readonly Color C_GREY   = new Color(0.75f, 0.75f, 0.75f, 1.00f);

        private static Font _fontCache;
// Font
        private static Font _font => _fontCache != null ? _fontCache : (_fontCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // ── UGUI refs — root ──────────────────────────────────────────────────
        private RectTransform _rootRT;

        // ── Market panel refs ─────────────────────────────────────────────────
        private GameObject     _marketGO;
        private Text           _marketHeader;
        private RectTransform  _marketContent;

        // ── Quoting panel refs ────────────────────────────────────────────────
        private GameObject     _quotingGO;
        private Text           _quotingCargoInfo;
        private Text           _quotingDistText;
        private RectTransform  _modeButtonsRT;
        private RectTransform  _agentContent;
        private GameObject     _agentSectionLabelGO;
        private GameObject     _agentScrollHostGO;
        private InputField     _priceInputField;
        private Text           _marginText;
        private Text           _clientTipText;
        private Button         _submitBtn;
        private Image          _submitBtnImg;
        private Text           _submitHintText;
        private Image[]        _modeBtnBgs;

        // ── Resultado panel refs ─────────────────────────────────────────────────
        private GameObject _resultGO;
        private Text       _resultMsgText;
        private GameObject _counterOfferGO;
        private Text       _counterAcceptLbl;
        private Text       _backBtnLbl;

        // Establece visible.
        public void SetVisible(bool v)
        {
            _visible = v;
            if (_rootRT != null) _rootRT.gameObject.SetActive(v);
            if (!v) { _state = State.Market; ShowState(); }
            else ShowMarket();
        }
// Indica si visible
        public bool IsVisible => _visible;

        // Se ejecuta durante Awake al iniciar el componente.

        private void Awake()
        {
            BuildUI();
        }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        private void OnDestroy()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
        }

// Se invoca al terminar un día de juego.
        private void OnDayPassed()
        {
            if (_visible && _state == State.Market) PopulateMarket();
        }

        // Construye UI.

        private void BuildUI()
        {
            var canvas = GetOrCreateCanvas();

            _rootRT = new GameObject("MarketPanel").AddComponent<RectTransform>();
            _rootRT.SetParent(canvas, false);
            _rootRT.anchorMin = new Vector2(0,1); _rootRT.anchorMax = new Vector2(0,1);
            _rootRT.pivot = new Vector2(0,1);
            _rootRT.anchoredPosition = new Vector2(PX, -PY);
            _rootRT.sizeDelta = new Vector2(PW, 560f);
            _rootRT.gameObject.SetActive(false);

            BuildMarketPanel();
            BuildQuotingPanel();
            BuildResultPanel();
        }

// Construye mercado panel.
        private void BuildMarketPanel()
        {
            var go = new GameObject("MarketState");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_rootRT, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = C_BG;
            _marketGO = go;

            _marketHeader = MakeTxtPos("MktHeader", rt, new Vector2(8,-4), new Vector2(PW-16,28),
                "MERCADO", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

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
            _marketContent = content;

            var sr = scrollHost.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport; sr.content = content;
            sr.horizontal = false; sr.scrollSensitivity = 20f;
            sr.movementType = ScrollRect.MovementType.Clamped;
        }

// Construye quoting panel.
        private void BuildQuotingPanel()
        {
            var go = new GameObject("QuoteState");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_rootRT, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = C_BG;
            _quotingGO = go;

            float y = -6f;

            // Regreso + title
            MakeBtn("BackBtn", rt, new Vector2(8,y), new Vector2(72,22), "← Volver",
                () => ShowMarket()); y -= 28f;
            MakeTxtPos("QTitle", rt, new Vector2(84,-6), new Vector2(PW-92, 22),
                "COTIZAR CARGA", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

            // Cargo info
            _quotingCargoInfo = MakeTxtPos("CargoInfo", rt, new Vector2(8,y), new Vector2(PW-16,38),
                "", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
            y -= 42f;
            _quotingDistText = MakeTxtPos("Dist", rt, new Vector2(8,y), new Vector2(PW-16,16),
                "", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
            y -= 22f;

            // Mode botones
            MakeSectionLabel("MODO DE TRANSPORTE", rt, ref y);
            _modeButtonsRT = MakeRect("ModeBtns", rt, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(8,y), new Vector2(PW-16, 26));
            y -= 32f;

            // Agent scroll
            _agentSectionLabelGO = MakeSectionLabel("AGENTE", rt, ref y);
            float agentH = 120f;
            var agentScrollHost = new GameObject("AgentScrollHost").AddComponent<RectTransform>();
            _agentScrollHostGO = agentScrollHost.gameObject;
            agentScrollHost.SetParent(rt, false);
            agentScrollHost.anchorMin = new Vector2(0,1); agentScrollHost.anchorMax = new Vector2(0,1);
            agentScrollHost.pivot = new Vector2(0,1);
            agentScrollHost.anchoredPosition = new Vector2(8,y);
            agentScrollHost.sizeDelta = new Vector2(PW-16, agentH);

            var agentVP = new GameObject("VP").AddComponent<RectTransform>();
            agentVP.SetParent(agentScrollHost, false);
            agentVP.anchorMin = Vector2.zero; agentVP.anchorMax = Vector2.one;
            agentVP.pivot = new Vector2(0.5f,0.5f);
            agentVP.offsetMin = agentVP.offsetMax = Vector2.zero;
            agentVP.gameObject.AddComponent<RectMask2D>();

            var agentContent = new GameObject("AgentContent").AddComponent<RectTransform>();
            agentContent.SetParent(agentVP, false);
            agentContent.anchorMin = new Vector2(0,1); agentContent.anchorMax = new Vector2(1,1);
            agentContent.pivot = new Vector2(0.5f,1f);
            agentContent.offsetMin = agentContent.offsetMax = Vector2.zero;
            agentContent.sizeDelta = new Vector2(0, 200f);
            _agentContent = agentContent;

            var asr = agentScrollHost.gameObject.AddComponent<ScrollRect>();
            asr.viewport = agentVP; asr.content = agentContent;
            asr.horizontal = false; asr.scrollSensitivity = 20f;
            asr.movementType = ScrollRect.MovementType.Clamped;
            y -= agentH + 6f;

            // Price section
            MakeSectionLabel("TU PRECIO (USD)", rt, ref y);
            _agentCostLabel = MakeTxtPos("AgentCostLbl", rt, new Vector2(8,y), new Vector2(PW-16,18),
                "Costo agente: $0", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
            y -= 20f;

            _priceInputField = MakeInputField("PriceInput", rt, new Vector2(8,y), new Vector2(PW-76,28)); y -= 4f;
            _priceInputField.onValueChanged.AddListener(s => { _priceInput = s; RecalcCost(); RefreshQuoteForm(); });

            _marginText = MakeTxtPos("Margin", rt, new Vector2(PW-66,y+4), new Vector2(58,28),
                "0%", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            y -= 34f;

            _clientTipText = MakeTxtPos("ClientTip", rt, new Vector2(8,y), new Vector2(PW-16,32),
                "", 10, FontStyle.Italic, new Color(0.6f,0.8f,1f), TextAnchor.UpperLeft);
            y -= 38f;

            _submitBtn    = MakeBtn("SubmitBtn", rt, new Vector2(8,y), new Vector2(PW-16,32),
                "Enviar Cotización →", SendQuote);
            _submitBtnImg = _submitBtn.GetComponent<Image>();
            _submitBtnImg.color = C_GREEN;
            y -= 36f;

            _submitHintText = MakeTxtPos("SubmitHint", rt, new Vector2(8,y), new Vector2(PW-16,16),
                "", 10, FontStyle.Italic, new Color(0.7f,0.5f,0.3f), TextAnchor.UpperLeft);
        }

// Construye resultado panel.
        private void BuildResultPanel()
        {
            var go = new GameObject("ResultState");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_rootRT, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = C_BG;
            _resultGO = go;

            float y = -8f;
            MakeTxtPos("ResTitle", rt, new Vector2(8,y), new Vector2(PW-16,22),
                "RESPUESTA DEL CLIENTE", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            y -= 28f;

            _resultMsgText = MakeTxtPos("ResMsg", rt, new Vector2(8,y), new Vector2(PW-16,68),
                "", 12, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            _resultMsgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            y -= 74f;

            // Counter-oferta botones (shown/hidden)
            var coGO = new GameObject("CounterOfferBtns");
            var coRT = coGO.AddComponent<RectTransform>();
            coRT.SetParent(rt, false);
            coRT.anchorMin = new Vector2(0,1); coRT.anchorMax = new Vector2(0,1);
            coRT.pivot = new Vector2(0,1);
            coRT.anchoredPosition = new Vector2(8,y);
            coRT.sizeDelta = new Vector2(PW-16, 32);
            _counterOfferGO = coGO;

            float hw = (PW-18f) * 0.5f;
            var acceptBtn = MakeBtn("AcceptCO", coRT, new Vector2(0,0), new Vector2(hw,32), "", AcceptCounterOffer);
            acceptBtn.GetComponent<Image>().color = C_GREEN;
            _counterAcceptLbl = acceptBtn.GetComponentInChildren<Text>();

            MakeBtn("RejectCO", coRT, new Vector2(hw+2f,0), new Vector2(hw,32), "Rechazar",
                RejectCounterOffer).GetComponent<Image>().color = C_RED;
            y -= 38f;

            var backBtn = MakeBtn("BackBtn", rt, new Vector2(8,y), new Vector2(PW-16,30), "← Al Mercado",
                OnResultBack);
            _backBtnLbl = backBtn.GetComponentInChildren<Text>();
        }

        // Muestra estado

        private void ShowState()
        {
            if (_marketGO  != null) _marketGO.SetActive(_state == State.Market);
            if (_quotingGO != null) _quotingGO.SetActive(_state == State.Quoting);
            if (_resultGO  != null) _resultGO.SetActive(_state == State.Result);
        }

// Muestra market
        private void ShowMarket()
        {
            _state = State.Market;
            ShowState();
            PopulateMarket();
        }

// Muestra quoting
        private void ShowQuoting()
        {
            _state = State.Quoting;
            ShowState();
            PopulateModeButtons();
            PopulateAgents();
            RefreshQuoteForm();
        }

// Muestra resultado
        private void ShowResult()
        {
            _state = State.Result;
            ShowState();

            _resultMsgText.text  = _resultMsg;
            _resultMsgText.color = _resultOk ? new Color(0.2f,0.95f,0.4f) :
                (_lastQuote != null && _lastQuote.HasCounterOffer) ? new Color(0.6f,0.8f,1f) :
                new Color(1f,0.4f,0.2f);

            bool hasOffer = _lastQuote != null && _lastQuote.HasCounterOffer;
            _counterOfferGO.SetActive(hasOffer);
            if (hasOffer && _counterAcceptLbl != null)
                _counterAcceptLbl.text = $"Aceptar ${_lastQuote.CounterOfferPrice:N0}";

            if (_backBtnLbl != null)
                _backBtnLbl.text = _resultOk ? "← Al Mercado" : "← Intentar de nuevo";
        }

        // Llena mercado.

        private void PopulateMarket()
        {
            var cargos = CargoManager.Instance?.MarketCargos;
            int count  = cargos?.Count ?? 0;
            int day    = FFTimeManager.Instance?.CurrentDay ?? 0;
            int money  = EconomyManager.Instance?.Money ?? 0;

            if (_marketHeader != null)
                _marketHeader.text = $"MERCADO  ·  Día {day}  ·  ${money:N0}  [{count} cargas]";

            ClearChildren(_marketContent);
            const float CH = 120f;
            _marketContent.sizeDelta = new Vector2(0, count * CH + 8f);

            for (int i = 0; i < count; i++)
            {
                var c    = cargos[i];
                var card = MakeCard(_marketContent, i, CH, CargoCardBg(c.CargoType));
                float tw = card.sizeDelta.x - 72f;
                int daysLeft = c.ExpirationDay - (FFTimeManager.Instance?.CurrentDay ?? 0);
                Color daysCol = daysLeft <= 2 ? new Color(1f,0.4f,0.2f) : C_GREY;

                MakeTxtPos("Info", card, new Vector2(6,-4), new Vector2(tw,18),
                    $"{CargoIcon(c.CargoType)} {CityDatabase.DisplayNameOf(c.OriginCityId)} → {CityDatabase.DisplayNameOf(c.DestinationCityId)}",
                    12, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
                MakeTxtPos("Type", card, new Vector2(6,-24), new Vector2(tw,16),
                    $"{Constants.GetCargoTypeName(c.CargoType)}  |  {c.Weight:F0}t  |  ${c.DeclaredValue:N0}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
                MakeTxtPos("Client", card, new Vector2(6,-42), new Vector2(tw,16),
                    $"Cliente: {Constants.GetClientTypeName(c.ClientType)}",
                    11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
                MakeTxtPos("Expiry", card, new Vector2(6,-60), new Vector2(tw,16),
                    $"Vence en {daysLeft} días", 11, FontStyle.Normal, daysCol, TextAnchor.UpperLeft);

                var bRT = MakeRect("QuoteBtn", card, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
                    new Vector2(-4,-CH*0.5f+22f), new Vector2(60,42));
                var bImg = bRT.gameObject.AddComponent<Image>();
                bImg.color = C_GREEN;
                var btn = bRT.gameObject.AddComponent<Button>();
                btn.targetGraphic = bImg; SetBtnColors(btn);
                Cargo cargo = c;
                btn.onClick.AddListener(() => OpenQuote(cargo));
                MakeTxtStretch("Lbl", bRT, "Cotizar\n→", 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            }
        }

        // Abre cotización.

        private void OpenQuote(Cargo cargo)
        {
            _selectedCargo  = cargo;
            _mode           = cargo.PreferredTransport;
            _agentId        = "";
            _priceInput     = "";
            _availableModes = GetAvailableModes();
            if (_priceInputField != null) _priceInputField.text = "";

            // Detect maritime cargo (has port names stored in RouteWaypoints by GenerateCargo)
            _isMaritime = cargo.RouteWaypoints != null && cargo.RouteWaypoints.Count >= 2
                          && MaritimeSimulationManager.Instance != null;
            // Distancia real siempre (el aéreo la usa para costo/días; el marítimo no la mira).
            _currentDist = CityDatabase.GetDistance(cargo.OriginCityId, cargo.DestinationCityId);

            if (_isMaritime)
            {
                _maritimeOptions = MaritimeSimulationManager.Instance.GetRouteOptions(
                    cargo.RouteWaypoints[0], cargo.RouteWaypoints[1]);
                _selectedOptionIdx = 0;
                // Pre-fill price from the selected (Direct) option cost with a margin
                if (_maritimeOptions.Count > 0)
                {
                    int suggestedCost = _maritimeOptions[_selectedOptionIdx].EstimatedCostUSD;
                    _priceInput = Mathf.RoundToInt(suggestedCost * 1.3f).ToString();
                    if (_priceInputField != null) _priceInputField.text = _priceInput;
                    _agentCost = suggestedCost;
                }
            }
            else
            {
                RefreshAgents();
            }

            ShowQuoting();
        }

// Refresca agents
        private void RefreshAgents()
        {
            if (AgentManager.Instance == null) return;
            _availableAgents = AgentManager.Instance.GetAvailableAgents(_mode);
            if (_availableAgents.Count > 0 && string.IsNullOrEmpty(_agentId))
            {
                _agentId = _availableAgents[0].Id;
                RecalcCost();
            }
        }

// Gestiona recalc cost.
        private void RecalcCost()
        {
            if (string.IsNullOrEmpty(_agentId) || _selectedCargo == null) return;
            var agent = AgentManager.Instance?.GetAgent(_agentId);
            if (agent == null) return;
            float dist = _currentDist;
            _agentCost = agent.CalculateCost(_selectedCargo, dist);
            // El aéreo es más caro que el marítimo: rango $1.200–$30.000 (los bajos son más frecuentes por distancia).
            if (_mode == Constants.TransportMode.Air)
                _agentCost = Mathf.Clamp(_agentCost, 1200, 30000);
            if (string.IsNullOrEmpty(_priceInput))
            {
                _priceInput = Mathf.RoundToInt(_agentCost * 1.35f).ToString();
                if (_priceInputField != null) _priceInputField.text = _priceInput;
            }
            if (int.TryParse(_priceInput, out int price) && price > 0)
                _margin = (float)(price - _agentCost) / price;
            else
                _margin = 0f;
        }

        private const float MODE_ROW_H = 26f;

// Llena mode botones.
        private void PopulateModeButtons()
        {
            if (_modeButtonsRT == null) return;
            ClearChildren(_modeButtonsRT);
            _routeOptsContent = null;

            var modes = _availableModes ?? GetAvailableModes();
            bool maritimeSel = MaritimeSelected;

            // El espacio de abajo (sección agente, oculta en cargas marítimas) absorbe la altura extra.
            _modeButtonsRT.sizeDelta = new Vector2(_modeButtonsRT.sizeDelta.x, maritimeSel ? 160f : 26f);

            // Fila de botones de modo: SIEMPRE visible, arriba.
            _modeBtnBgs = new Image[modes.Length];
            float bw = (PW - 18f) / Mathf.Max(1, modes.Length);
            for (int i = 0; i < modes.Length; i++)
            {
                var m   = modes[i];
                var rt  = MakeRect($"Mode{i}", _modeButtonsRT,
                    new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                    new Vector2(i * (bw + 2f), 0f), new Vector2(bw, 22f));
                var img = rt.gameObject.AddComponent<Image>();
                img.color = _mode == m ? C_BTN_ON : C_BTN;
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = img; SetBtnColors(btn);
                Constants.TransportMode mc = m;
                btn.onClick.AddListener(() =>
                {
                    _mode = mc;
                    _agentId = ""; _priceInput = "";
                    if (_priceInputField != null) _priceInputField.text = "";
                    RefreshAgents(); PopulateModeButtons(); PopulateAgents(); RefreshQuoteForm();
                });
                MakeTxtStretch("Lbl", rt, Constants.GetTransportModeName(m), 9, FontStyle.Bold,
                    Color.white, TextAnchor.MiddleCenter);
                _modeBtnBgs[i] = img;
            }

            if (maritimeSel)
                PopulateRouteOptions();
            else if (AirReady)
            { /* Aéreo operativo: los agentes se muestran en su propia sección (PopulateAgents). */ }
            else if (_mode == Constants.TransportMode.Air)
                BuildModeNotice("✈  Aéreo: origen o destino sin aeropuerto.");
            else
                BuildModeNotice($"🚧 {Constants.GetTransportModeName(_mode)}: en desarrollo.");
        }

// Construye mode notice.
        private void BuildModeNotice(string msg)
        {
            MakeTxtPos("ModeNotice", _modeButtonsRT, new Vector2(4, -(MODE_ROW_H + 2f)),
                new Vector2(PW - 24f, 46f),
                msg, 10, FontStyle.Italic, new Color(0.95f, 0.8f, 0.3f), TextAnchor.UpperLeft);
        }

// Llena ruta options.
        private void PopulateRouteOptions()
        {
            // Expand the mode botones RT to fit 3 option cards
            if (_modeButtonsRT != null)
                _modeButtonsRT.sizeDelta = new Vector2(_modeButtonsRT.sizeDelta.x, 160f);

            if (_routeOptsContent == null)
            {
                _routeOptsContent = MakeRect("RouteOptsContent", _modeButtonsRT,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                _routeOptsContent.offsetMin = Vector2.zero;
                _routeOptsContent.offsetMax = new Vector2(0f, -MODE_ROW_H); // debajo de la fila de modos
            }
            ClearChildren(_routeOptsContent);

            _routeOptBtnBgs = new Image[_maritimeOptions.Count];
            const float OH = 44f;
            for (int i = 0; i < _maritimeOptions.Count; i++)
            {
                var opt   = _maritimeOptions[i];
                bool sel  = i == _selectedOptionIdx;
                string costTag = opt.Type == ShipmentOption.OptionType.Direct ? "$$$"
                               : opt.Type == ShipmentOption.OptionType.MixedA ? "$$" : "$";
                string ports = opt.PortSequence.Count > 0
                    ? string.Join(" → ", opt.PortSequence)
                    : opt.DisplayLabel;

                var card = MakeRect($"Opt{i}", _routeOptsContent,
                    new Vector2(0,1), new Vector2(1,1), new Vector2(0,1),
                    new Vector2(0, -(i * OH)), new Vector2(0, OH - 2f));
                var img = card.gameObject.AddComponent<Image>();
                img.color = sel ? C_BTN_ON : C_BTN;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = img; SetBtnColors(btn);
                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    _selectedOptionIdx = idx;
                    _agentCost = _maritimeOptions[idx].EstimatedCostUSD;
                    RecalcCost();
                    PopulateRouteOptions();
                    RefreshQuoteForm();
                });
                _routeOptBtnBgs[i] = img;

                // Cards are stretch-anchored (sizeDelta.x = 0); use PW minus margins for text width.
                float tw = PW - 30f;
                // First line: label + TT + cost tag
                MakeTxtPos($"L1_{i}", card, new Vector2(6,-3), new Vector2(tw, 15),
                    $"{opt.DisplayLabel}  |  {opt.TotalTTDays} días  |  {costTag}  ~${opt.EstimatedCostUSD:N0}",
                    10, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
                // Second line: port sequence (truncated)
                string ps = ports.Length > 48 ? ports.Substring(0, 45) + "…" : ports;
                MakeTxtPos($"L2_{i}", card, new Vector2(6,-17), new Vector2(tw, 13),
                    ps, 9, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
                // Third line: port days breakdown
                int portCount = opt.PortSequence.Count;
                MakeTxtPos($"L3_{i}", card, new Vector2(6,-30), new Vector2(tw, 13),
                    $"Base {opt.BaseTTDays:F1}d + {portCount}×2d puertos = {opt.TotalTTDays}d",
                    9, FontStyle.Italic, new Color(0.6f,0.8f,1f), TextAnchor.UpperLeft);
            }
        }

// Llena agentes.
        private void PopulateAgents()
        {
            if (_agentContent == null) return;
            ClearChildren(_agentContent);

            bool showAgents = AirReady;   // hoy solo el aéreo usa el flujo de agentes
            if (_agentSectionLabelGO != null) _agentSectionLabelGO.SetActive(showAgents);
            if (_agentScrollHostGO   != null) _agentScrollHostGO.SetActive(showAgents);

            if (!showAgents)
            {
                _agentContent.sizeDelta = new Vector2(0, 0f);
                return;
            }

            if (_availableAgents.Count == 0)
            {
                _agentContent.sizeDelta = new Vector2(0, 40f);
                MakeTxtPos("NoAgent", _agentContent, new Vector2(4,-4), new Vector2(PW-32,32),
                    "No hay agentes disponibles para este modo.", 11, FontStyle.Normal, C_GREY, TextAnchor.UpperLeft);
                return;
            }

            float dist = _currentDist;
            const float AH = 40f;
            _agentContent.sizeDelta = new Vector2(0, _availableAgents.Count * AH);

            for (int i = 0; i < _availableAgents.Count; i++)
            {
                var agent = _availableAgents[i];
                bool on   = _agentId == agent.Id;
                int  cost = _selectedCargo != null ? agent.CalculateCost(_selectedCargo, dist) : 0;
                int  est  = EstimateDays(_mode, dist, agent.GetCurrentSpeedMultiplier());

                var rt  = MakeRect($"Agent{i}", _agentContent,
                    new Vector2(0,1), new Vector2(1,1), new Vector2(0,1),
                    new Vector2(0, -(i * AH)), new Vector2(0, AH-2));
                var img = rt.gameObject.AddComponent<Image>();
                img.color = on ? C_BTN_ON : C_BTN;
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = img; SetBtnColors(btn);
                string aid = agent.Id;
                btn.onClick.AddListener(() => { _agentId = aid; RecalcCost(); PopulateAgents(); RefreshQuoteForm(); });
                MakeTxtStretch("Lbl", rt,
                    $"{agent.GetStateEmoji()} {agent.Name}  ${cost:N0}  ·  ~{est} días",
                    10, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
                    new Vector2(8, 0));
            }
        }

// Refresca quote form
        private void RefreshQuoteForm()
        {
            if (_quotingCargoInfo == null || _selectedCargo == null) return;
            var c = _selectedCargo;

            if (MaritimeSelected && c.RouteWaypoints?.Count >= 2)
            {
                _quotingCargoInfo.text =
                    $"{CargoIcon(c.CargoType)}  {c.RouteWaypoints[0]} → {c.RouteWaypoints[1]}\n" +
                    $"{Constants.GetCargoTypeName(c.CargoType)}  |  {c.Weight:F0}t  |  ${c.DeclaredValue:N0}";
                var selOpt = _maritimeOptions.Count > _selectedOptionIdx ? _maritimeOptions[_selectedOptionIdx] : null;
                _quotingDistText.text = selOpt != null
                    ? $"TT: {selOpt.TotalTTDays} días  |  Costo operación: ${selOpt.EstimatedCostUSD:N0}"
                    : "Ruta marítima";
                if (_agentCostLabel != null)
                    _agentCostLabel.text = selOpt != null
                        ? $"Costo logístico: ${selOpt.EstimatedCostUSD:N0}"
                        : "Costo logístico: $0";
                _agentCost = selOpt?.EstimatedCostUSD ?? 0;
            }
            else
            {
                _quotingCargoInfo.text = $"{CargoIcon(c.CargoType)}  {CityDatabase.DisplayNameOf(c.OriginCityId)} → {CityDatabase.DisplayNameOf(c.DestinationCityId)}\n" +
                    $"{Constants.GetCargoTypeName(c.CargoType)}  |  {c.Weight:F0}t  |  ${c.DeclaredValue:N0}";
                _quotingDistText.text  = $"Distancia: {_currentDist:N0} km";
                if (_agentCostLabel != null) _agentCostLabel.text = $"Costo agente: ${_agentCost:N0}";
            }
            if (_clientTipText != null) _clientTipText.text = ClientTip(c.ClientType);

            // Margin display
            Color marginCol = _margin < 0.05f ? new Color(1f,0.3f,0.3f) :
                              _margin > 0.35f ? new Color(1f,0.75f,0f) : new Color(0.3f,1f,0.5f);
            if (_marginText != null)
            {
                _marginText.text  = $"{_margin * 100:F0}%";
                _marginText.color = marginCol;
            }

            // Estado del botón Enviar. En cargas marítimas solo se cotiza el modo Marítimo;
            // los demás modos se muestran pero todavía no están desarrollados.
            bool modeReady = MaritimeSelected || (AirReady && !string.IsNullOrEmpty(_agentId));
            bool canSend   = modeReady && int.TryParse(_priceInput, out int p) && p > _agentCost;
            if (_submitBtn != null)
            {
                _submitBtn.interactable = canSend;
                _submitBtnImg.color = canSend ? C_GREEN : new Color(0.3f,0.3f,0.3f,0.8f);
            }
            if (_submitHintText != null)
            {
                if (!canSend && modeReady)
                    _submitHintText.text = "El precio debe ser mayor al costo logístico.";
                else if (AirNoAirport)
                    _submitHintText.text = "✈  Aéreo: origen o destino sin aeropuerto.";
                else if (!modeReady && _mode != Constants.TransportMode.Maritime)
                    _submitHintText.text = "🚧 Este modo todavía no está disponible.";
                else
                    _submitHintText.text = "";
            }
        }

        // Envía cotización.

        private void SendQuote()
        {
            if (_selectedCargo == null || !int.TryParse(_priceInput, out int price)) return;
            // Solo se puede enviar en Marítimo (con ruta) o Aéreo (con aeropuertos y agente).
            if (!MaritimeSelected && !(AirReady && !string.IsNullOrEmpty(_agentId))) return;
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 0;

            // ── Maritime flow ─────────────────────────────────────────────
            if (MaritimeSelected && _maritimeOptions.Count > 0)
            {
                var opt    = _maritimeOptions[_selectedOptionIdx];
                var client = ClientManager.Instance?.GetClientById(_selectedCargo.ClientId);

                // Build a synthetic quote for client evaluation
                _lastQuote = new Quote(
                    _selectedCargo.Id, _selectedCargo.ClientId, _selectedCargo.ClientName,
                    price, opt.EstimatedCostUSD, Constants.TransportMode.Maritime,
                    "maritime", "Marítimo", opt.TotalTTDays, currentDay);

                Quote.NegotiationResult result = client != null
                    ? ClientManager.Instance.EvaluateQuote(_lastQuote, client, _selectedCargo)
                    : Quote.NegotiationResult.Acceptance("Trato cerrado.", 0.5f);

                if (result.Accepted)
                {
                    _lastQuote.Accept();
                    CargoManager.Instance.AcceptMaritimeOption(_selectedCargo, opt, price, currentDay);
                    _resultOk  = true;
                    float m = price > 0 ? (float)(price - opt.EstimatedCostUSD) / price : 0f;
                    _resultMsg = $"✅ {result.ClientMessage}\n\n" +
                        $"Ruta: {opt.DisplayLabel}  |  TT: {opt.TotalTTDays} días\n" +
                        $"Precio: ${price:N0}  |  Margen: {m*100:F0}%";
                }
                // Realiza if
                else if (result.HasCounterOffer)
                {
                    _lastQuote.SetCounterOffer(result.CounterOfferPrice, result.ClientMessage);
                    _resultOk  = false;
                    _resultMsg = $"🔄 {result.ClientMessage}\n\nContraoferta: ${result.CounterOfferPrice:N0}";
                }
                else
                {
                    _lastQuote.Reject(result.ClientMessage);
                    _resultOk  = false;
                    _resultMsg = $"❌ {result.ClientMessage}";
                }
                ShowResult();
                return;
            }

            // ── Estándar (non-maritime) flow ──────────────────────────────
            var agentObj = AgentManager.Instance?.GetAgent(_agentId);
            if (agentObj == null) return;

            float dist    = _currentDist;
            int   estDays = EstimateDays(_mode, dist, agentObj.GetCurrentSpeedMultiplier());

            _lastQuote = new Quote(
                _selectedCargo.Id, _selectedCargo.ClientId, _selectedCargo.ClientName,
                price, _agentCost, _mode, _agentId, agentObj.Name, estDays, currentDay);

            var clientStd = ClientManager.Instance?.GetClientById(_selectedCargo.ClientId);
            Quote.NegotiationResult resultStd = clientStd != null
                ? ClientManager.Instance.EvaluateQuote(_lastQuote, clientStd, _selectedCargo)
                : Quote.NegotiationResult.Acceptance("Trato cerrado.", 0.5f);

            if (resultStd.Accepted)
            {
                _lastQuote.Accept();
                CargoManager.Instance.AcceptQuote(_selectedCargo, _lastQuote, currentDay);
                _resultOk  = true;
                _resultMsg = $"✅ {resultStd.ClientMessage}\n\nPrecio: ${price:N0}  |  Margen: {_margin*100:F0}%";
            }
            // Realiza if
            else if (resultStd.HasCounterOffer)
            {
                _lastQuote.SetCounterOffer(resultStd.CounterOfferPrice, resultStd.ClientMessage);
                _resultOk  = false;
                _resultMsg = $"🔄 {resultStd.ClientMessage}\n\nContraoferta: ${resultStd.CounterOfferPrice:N0}";
            }
            else
            {
                _lastQuote.Reject(resultStd.ClientMessage);
                _resultOk  = false;
                _resultMsg = $"❌ {resultStd.ClientMessage}";
            }
            ShowResult();
        }

// Aceptado counter oferta.
        private void AcceptCounterOffer()
        {
            _lastQuote.AcceptCounterOffer();
            int day = FFTimeManager.Instance?.CurrentDay ?? 0;

            // Marítimo (ruta) solo si el modo elegido es marítimo; si no (aéreo), va por agente.
            if (MaritimeSelected && _maritimeOptions.Count > 0)
            {
                var opt = _maritimeOptions[_selectedOptionIdx];
                CargoManager.Instance.AcceptMaritimeOption(_selectedCargo, opt, _lastQuote.FinalPrice, day);
            }
            else
            {
                CargoManager.Instance.AcceptQuote(_selectedCargo, _lastQuote, day);
            }

            _resultMsg = $"✅ Trato cerrado por ${_lastQuote.FinalPrice:N0}";
            _resultOk  = true;
            _lastQuote.HasCounterOffer = false;
            ShowResult();
        }

// Gestiona reject counter oferta.
        private void RejectCounterOffer()
        {
            _lastQuote.RejectCounterOffer();
            _resultMsg = "❌ Rechazaste la contraoferta.";
            _resultOk  = false;
            _lastQuote.HasCounterOffer = false;
            ShowResult();
        }

// Se invoca cuando regresa el resultado.
        private void OnResultBack()
        {
            if (!_resultOk && _selectedCargo != null &&
                _selectedCargo.Status == Constants.CargoStatus.Available)
                ShowQuoting();
            else
                ShowMarket();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Constants.TransportMode[] GetAvailableModes()
        {
            // Mostramos siempre los 4 modos. Hoy solo el marítimo está desarrollado;
            // los demás quedan visibles para implementarlos más adelante.
            return new[]
            {
                Constants.TransportMode.Maritime,
                Constants.TransportMode.Air,
                Constants.TransportMode.Land,
                Constants.TransportMode.Rail,
            };
        }

        // El marítimo es el único modo operativo hoy; el resto se muestra pero no se cotiza.
        private bool MaritimeSelected => _isMaritime && _mode == Constants.TransportMode.Maritime;

        // Aéreo operativo: modo Aéreo con aeropuerto en origen y destino (vuelo directo vía agentes).
        private bool AirReady
        {
            get
            {
                if (_mode != Constants.TransportMode.Air || _selectedCargo == null) return false;
                var o = CityDatabase.GetCity(_selectedCargo.OriginCityId);
                var d = CityDatabase.GetCity(_selectedCargo.DestinationCityId);
                return o != null && d != null && o.HasAirport && d.HasAirport;
            }
        }
        private bool AirNoAirport => _mode == Constants.TransportMode.Air && !AirReady;

// Gestiona estimate días.
        private static int EstimateDays(Constants.TransportMode mode, float dist, float speedMult)
        {
            float kmPerDay = mode == Constants.TransportMode.Air  ? 15000f :
                             mode == Constants.TransportMode.Land ? 600f   :
                             mode == Constants.TransportMode.Rail ? 800f   : 2000f;
            return Mathf.Max(1, Mathf.CeilToInt(dist / (kmPerDay * speedMult)));
        }

// Cliente tip.
        private static string ClientTip(Constants.ClientType t)
        {
            switch (t)
            {
                case Constants.ClientType.UrgentClient:   return "⚡ Urgente: acepta precios altos pero exige rapidez.";
                case Constants.ClientType.GoodPayer:      return "👍 Buen Pagador: confiable, tolera márgenes razonables.";
                case Constants.ClientType.VeryBadClient:  return "⚠️ Muy Difícil: margen máximo 10%, no negocia.";
                case Constants.ClientType.BadPayer:       return "⚠️ Mal Pagador: margen máximo 15%, suele rechazar.";
                case Constants.ClientType.ContractClient: return "📋 Contrato: busca precio estable, descuentos esperados.";
                case Constants.ClientType.CreditClient:   return "📅 Crédito: pagará en 45 días. Margen máximo 20%.";
                default:                                  return "";
            }
        }

// Cargamento card bg.
        private static Color CargoCardBg(Constants.CargoType t)
        {
            Color c;
            switch (t)
            {
                case Constants.CargoType.Refrigerated: c = Color.cyan;                    break;
                case Constants.CargoType.Dangerous:    c = Color.red;                     break;
                case Constants.CargoType.Urgent:       c = Color.yellow;                  break;
                case Constants.CargoType.Valuable:     c = new Color(1f,0.8f,0f);         break;
                default:                               c = Color.white;                   break;
            }
            return new Color(c.r, c.g, c.b, 0.10f);
        }

// Cargamento icon.
        private static string CargoIcon(Constants.CargoType t)
        {
            switch (t)
            {
                case Constants.CargoType.Refrigerated: return "🔵";
                case Constants.CargoType.Dangerous:    return "🔴";
                case Constants.CargoType.Urgent:       return "⚡";
                case Constants.CargoType.Valuable:     return "⭐";
                default:                               return "🟡";
            }
        }

// Borra children.
        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

        // Obtiene or create canvas

        private static RectTransform GetOrCreateCanvas()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var ex = FindAnyObjectByType<Canvas>();
            if (ex != null)
            {
                if (ex.GetComponent<GraphicRaycaster>() == null)
                    ex.gameObject.AddComponent<GraphicRaycaster>();
                var existCs = ex.GetComponent<CanvasScaler>();
                if (existCs != null)
                {
                    existCs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    existCs.referenceResolution = new Vector2(1280, 720);
                    existCs.matchWidthOrHeight  = 0.5f;
                }
                ex.sortingOrder = 10;
                return ex.GetComponent<RectTransform>();
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
            float w = PW - 10f;
            var card = MakeRect($"Card{idx}", content,
                new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(4f, -(idx * cardH + 4f)), new Vector2(w, cardH - 4f));
            card.gameObject.AddComponent<Image>().color = bg;
            return card;
        }

        private Button MakeBtn(string name, RectTransform parent, Vector2 pos, Vector2 size,
            string label, Action onClick)
        {
            var rt  = MakeRect(name, parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), pos, size);
            var img = rt.gameObject.AddComponent<Image>(); img.color = C_BTN;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img; SetBtnColors(btn);
            btn.onClick.AddListener(onClick.Invoke);
            MakeTxtStretch("Lbl", rt, label, 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            return btn;
        }

// Gestiona make input field.
        private InputField MakeInputField(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var rt = MakeRect(name, parent, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), pos, size);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

            var textGO = new GameObject("Text"); var textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(rt, false);
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(6, 4); textRT.offsetMax = new Vector2(-6, -4);
            var textC = textGO.AddComponent<Text>();
            textC.font = _font; textC.fontSize = 13; textC.color = Color.white;
            textC.alignment = TextAnchor.MiddleLeft;

            var phGO = new GameObject("Placeholder"); var phRT = phGO.AddComponent<RectTransform>();
            phRT.SetParent(rt, false);
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(6, 4); phRT.offsetMax = new Vector2(-6, -4);
            var phC = phGO.AddComponent<Text>();
            phC.font = _font; phC.fontSize = 13; phC.fontStyle = FontStyle.Italic;
            phC.color = new Color(0.4f,0.4f,0.4f); phC.text = "0";
            phC.alignment = TextAnchor.MiddleLeft;

            var input = rt.gameObject.AddComponent<InputField>();
            input.targetGraphic = bg;
            input.textComponent = textC;
            input.placeholder   = phC;
            input.contentType   = InputField.ContentType.IntegerNumber;
            return input;
        }

// Gestiona make section etiqueta.
        private GameObject MakeSectionLabel(string text, RectTransform parent, ref float y)
        {
            var t = MakeTxtPos("SecLabel", parent, new Vector2(8, y), new Vector2(PW-16, 16),
                text, 10, FontStyle.Bold, new Color(0.6f,0.8f,1f), TextAnchor.UpperLeft);
            y -= 18f;
            return t.gameObject;
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