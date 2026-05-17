using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using UnityEngine;

namespace FreightForwarder.UI
{
    public class MarketPanel : MonoBehaviour
    {
        // ── Estado ────────────────────────────────────────────────────────────
        private enum State { Market, Quoting, Result }
        private State _state = State.Market;

        private Cargo  _selectedCargo;
        private Vector2 _scrollMarket;
        private Vector2 _scrollAgents;

        // ── Quote form ────────────────────────────────────────────────────────
        private Constants.TransportMode _mode;
        private string   _agentId        = "";
        private string   _priceInput     = "";
        private int      _agentCost;
        private float    _margin;
        private List<Agent> _availableAgents = new List<Agent>();

        // ── Resultado ─────────────────────────────────────────────────────────
        private string _resultMsg   = "";
        private bool   _resultOk;
        private Quote  _lastQuote;

        // ── Layout ────────────────────────────────────────────────────────────
        private const float PX    = 68f;   // deja espacio al sidebar izquierdo
        private const float PY    = 46f;   // deja espacio al top bar
        private const float PW    = 320f;
        private bool  _visible    = false;

        public void SetVisible(bool v) { _visible = v; if (!v) _state = State.Market; }
        public bool IsVisible => _visible;

        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _box, _title, _lbl, _small, _btn, _btnOn,
                         _btnGreen, _btnRed, _btnClose, _tag;
        private bool _ready;

        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (CargoManager.Instance == null) return;
            EnsureStyles();
            // Botón propio solo si FFUIManager no está coordinando
            if (FFUIManager.Instance == null) DrawToggle();
            if (!_visible) return;

            switch (_state)
            {
                case State.Market:  DrawMarket();  break;
                case State.Quoting: DrawQuoting(); break;
                case State.Result:  DrawResult();  break;
            }
        }

        // ── Botón flotante para mostrar/ocultar ───────────────────────────────
        private void DrawToggle()
        {
            string label = _visible ? "◀ Mercado" : "▶ Mercado";
            int count    = CargoManager.Instance?.MarketCargos.Count ?? 0;
            if (!_visible) label = $"▶ Mercado [{count}]";
            if (GUI.Button(new Rect(PX, PY - 28f, 140f, 24f), label, _btn))
                _visible = !_visible;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL DE MERCADO
        // ══════════════════════════════════════════════════════════════════════
        private void DrawMarket()
        {
            var cargos = CargoManager.Instance.MarketCargos;
            float cardH  = 120f;
            float listH  = Mathf.Min(cargos.Count * cardH + 8f, 480f);
            float totalH = 40f + listH;

            var panel = new Rect(PX, PY, PW, totalH);
            GUI.Box(panel, GUIContent.none, _box);

            // Cabecera
            int day = FFTimeManager.Instance?.CurrentDay ?? 0;
            int money = EconomyManager.Instance?.Money ?? 0;
            GUI.Label(new Rect(PX + 8, PY + 4, PW - 16, 22f),
                      $"MERCADO  ·  Día {day}  ·  ${money:N0}", _title);

            if (cargos.Count == 0)
            {
                GUI.Label(new Rect(PX + 8, PY + 34, PW - 16, 24f),
                          "No hay cargas disponibles.", _small);
                return;
            }

            // Lista scrollable
            var scrollRect  = new Rect(PX + 4, PY + 36, PW - 8, listH);
            var contentRect = new Rect(0, 0, PW - 24, cargos.Count * cardH);
            _scrollMarket = GUI.BeginScrollView(scrollRect, _scrollMarket, contentRect);

            for (int i = 0; i < cargos.Count; i++)
                DrawCargoCard(cargos[i], i, cardH);

            GUI.EndScrollView();
        }

        private void DrawCargoCard(Cargo c, int idx, float h)
        {
            float y = idx * h;
            var cardRect = new Rect(2, y + 2, PW - 28, h - 4);

            // Fondo coloreado según tipo
            Color bg = CargoColor(c.CargoType);
            bg.a = 0.12f;
            DrawColoredBox(cardRect, bg);

            float tx = cardRect.x + 6;
            float ty = cardRect.y + 4;
            float tw = cardRect.width - 70;

            // Tipo + ruta
            GUI.Label(new Rect(tx, ty, tw, 18f),
                      $"{CargoIcon(c.CargoType)} {c.OriginCityId.Replace('_',' ')} → {c.DestinationCityId.Replace('_',' ')}",
                      _lbl);
            ty += 20f;

            // Info
            GUI.Label(new Rect(tx, ty, tw, 16f),
                      $"{Constants.GetCargoTypeName(c.CargoType)}  |  {c.Weight:F0} t  |  ${c.DeclaredValue:N0}",
                      _small);
            ty += 18f;

            GUI.Label(new Rect(tx, ty, tw, 16f),
                      $"Cliente: {Constants.GetClientTypeName(c.ClientType)}",
                      _small);
            ty += 18f;

            int days = c.ExpirationDay - (FFTimeManager.Instance?.CurrentDay ?? 0);
            Color daysCol = days <= 2 ? new Color(1f, 0.4f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            var prevColor = GUI.contentColor;
            GUI.contentColor = daysCol;
            GUI.Label(new Rect(tx, ty, tw, 16f), $"Vence en {days} días", _small);
            GUI.contentColor = prevColor;

            // Botón cotizar
            var btnRect = new Rect(cardRect.xMax - 62, cardRect.y + (h / 2f) - 18f, 58f, 36f);
            if (GUI.Button(btnRect, "Cotizar\n→", _btnGreen))
                OpenQuote(c);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL DE COTIZACIÓN
        // ══════════════════════════════════════════════════════════════════════
        private void OpenQuote(Cargo cargo)
        {
            _selectedCargo = cargo;
            _mode          = cargo.PreferredTransport;
            _agentId       = "";
            _priceInput    = "";
            RefreshAgents();
            _state = State.Quoting;
        }

        private void RefreshAgents()
        {
            if (AgentManager.Instance == null) return;
            _availableAgents = AgentManager.Instance.GetAvailableAgents(_mode);
            if (_availableAgents.Count > 0 && string.IsNullOrEmpty(_agentId))
                SelectAgent(_availableAgents[0].Id);
        }

        private void SelectAgent(string id)
        {
            _agentId = id;
            RecalcCost();
        }

        private void RecalcCost()
        {
            if (string.IsNullOrEmpty(_agentId) || _selectedCargo == null) return;
            Agent agent = AgentManager.Instance?.GetAgent(_agentId);
            if (agent == null) return;

            float dist  = CityDatabase.GetDistance(_selectedCargo.OriginCityId,
                                                    _selectedCargo.DestinationCityId);
            _agentCost  = agent.CalculateCost(_selectedCargo, dist);

            if (string.IsNullOrEmpty(_priceInput))
                _priceInput = Mathf.RoundToInt(_agentCost * 1.35f).ToString();

            if (int.TryParse(_priceInput, out int price) && price > 0)
                _margin = (float)(price - _agentCost) / price;
            else
                _margin = 0f;
        }

        private void DrawQuoting()
        {
            float panelH = 440f;
            var panel = new Rect(PX, PY, PW, panelH);
            GUI.Box(panel, GUIContent.none, _box);

            float x = PX + 8;
            float y = PY + 6;

            // ── Cabecera ──
            if (GUI.Button(new Rect(x, y, 60f, 20f), "← Volver", _btn))
                _state = State.Market;

            GUI.Label(new Rect(x + 68, y, PW - 80, 20f), "COTIZAR CARGA", _title);
            y += 26f;

            // ── Info de la carga ──
            var c = _selectedCargo;
            GUI.Label(new Rect(x, y, PW - 16, 18f),
                      $"{CargoIcon(c.CargoType)}  {c.OriginCityId.Replace('_',' ')} → {c.DestinationCityId.Replace('_',' ')}",
                      _lbl);
            y += 20f;
            GUI.Label(new Rect(x, y, PW - 16, 16f),
                      $"{Constants.GetCargoTypeName(c.CargoType)}  |  {c.Weight:F0} t  |  ${c.DeclaredValue:N0}",
                      _small);
            y += 20f;

            float dist = CityDatabase.GetDistance(c.OriginCityId, c.DestinationCityId);
            GUI.Label(new Rect(x, y, PW - 16, 16f), $"Distancia: {dist:N0} km", _small);
            y += 22f;

            // ── Modo de transporte ──
            DrawSectionLabel(x, ref y, "MODO DE TRANSPORTE");
            float bw = (PW - 20f) / 3f;
            Constants.TransportMode[] modes = GetAvailableModes();
            for (int i = 0; i < modes.Length; i++)
            {
                var m    = modes[i];
                var rect = new Rect(x + i * bw, y, bw - 2, 24f);
                bool on  = _mode == m;
                if (GUI.Button(rect, Constants.GetTransportModeName(m), on ? _btnOn : _btn))
                {
                    _mode     = m;
                    _agentId  = "";
                    _priceInput = "";
                    RefreshAgents();
                }
            }
            y += 28f;

            // ── Selector de agente ──
            DrawSectionLabel(x, ref y, "AGENTE");
            if (_availableAgents.Count == 0)
            {
                GUI.Label(new Rect(x, y, PW - 16, 18f), "No hay agentes disponibles para este modo.", _small);
                y += 22f;
            }
            else
            {
                float agentListH = Mathf.Min(_availableAgents.Count * 40f, 120f);
                var scrollRect   = new Rect(x, y, PW - 18, agentListH);
                var contentRect  = new Rect(0, 0, PW - 34, _availableAgents.Count * 40f);
                _scrollAgents = GUI.BeginScrollView(scrollRect, _scrollAgents, contentRect);

                for (int i = 0; i < _availableAgents.Count; i++)
                {
                    var agent   = _availableAgents[i];
                    bool on     = _agentId == agent.Id;
                    float dist2 = dist;
                    int cost    = agent.CalculateCost(c, dist2);
                    int estDays = EstimateDays(_mode, dist2, agent.GetCurrentSpeedMultiplier());
                    string lbl  = $"{agent.GetStateEmoji()} {agent.Name}\n${cost:N0}  ·  ~{estDays} días";
                    if (GUI.Button(new Rect(0, i * 40f, PW - 34, 38f), lbl, on ? _btnOn : _btn))
                        SelectAgent(agent.Id);
                }
                GUI.EndScrollView();
                y += agentListH + 4f;
            }

            // ── Precio ──
            DrawSectionLabel(x, ref y, "TU PRECIO (USD)");
            RecalcCost();

            GUI.Label(new Rect(x, y, 160f, 18f), $"Costo agente: ${_agentCost:N0}", _small);
            y += 20f;

            string prev = _priceInput;
            _priceInput = GUI.TextField(new Rect(x, y, PW - 70, 26f), _priceInput, _btn);
            if (_priceInput != prev) RecalcCost();

            // Margen en tiempo real
            Color marginCol = _margin < 0.05f ? Color.red :
                              _margin > 0.35f ? new Color(1f, 0.7f, 0f) : Color.green;
            var prevC = GUI.contentColor;
            GUI.contentColor = marginCol;
            GUI.Label(new Rect(x + PW - 64, y, 60f, 26f),
                      $"{_margin * 100:F0}%", _lbl);
            GUI.contentColor = prevC;
            y += 30f;

            // Tip de cliente
            GUI.Label(new Rect(x, y, PW - 16, 16f),
                      ClientTip(c.ClientType), _small);
            y += 24f;

            // ── Botón enviar ──
            bool canSend = !string.IsNullOrEmpty(_agentId) &&
                           int.TryParse(_priceInput, out int p) && p > _agentCost;

            GUI.enabled = canSend;
            if (GUI.Button(new Rect(x, y, PW - 16, 32f), "Enviar Cotización →", _btnGreen))
                SendQuote();
            GUI.enabled = true;

            if (!canSend && !string.IsNullOrEmpty(_agentId))
                GUI.Label(new Rect(x, y + 34f, PW - 16, 14f),
                          "El precio debe ser mayor al costo del agente.", _small);
        }

        private void SendQuote()
        {
            if (_selectedCargo == null || !int.TryParse(_priceInput, out int price)) return;

            Agent agent = AgentManager.Instance?.GetAgent(_agentId);
            if (agent == null) return;

            float dist    = CityDatabase.GetDistance(_selectedCargo.OriginCityId,
                                                     _selectedCargo.DestinationCityId);
            int estDays   = EstimateDays(_mode, dist, agent.GetCurrentSpeedMultiplier());
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 0;

            _lastQuote = new Quote(
                _selectedCargo.Id, _selectedCargo.ClientId, _selectedCargo.ClientName,
                price, _agentCost, _mode, _agentId, agent.Name,
                estDays, currentDay);

            Client client = ClientManager.Instance?.GetClientById(_selectedCargo.ClientId);
            Quote.NegotiationResult result;

            if (client != null)
                result = ClientManager.Instance.EvaluateQuote(_lastQuote, client, _selectedCargo);
            else
                result = Quote.NegotiationResult.Acceptance("Trato cerrado.", 0.5f);

            if (result.Accepted)
            {
                _lastQuote.Accept();
                CargoManager.Instance.AcceptQuote(_selectedCargo, _lastQuote, currentDay);
                _resultOk  = true;
                _resultMsg = $"✅ {result.ClientMessage}\n\nPrecio: ${price:N0}  |  Margen: {_margin * 100:F0}%";
            }
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

            _state = State.Result;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PANEL DE RESULTADO
        // ══════════════════════════════════════════════════════════════════════
        private void DrawResult()
        {
            float h = _lastQuote.HasCounterOffer ? 220f : 170f;
            var panel = new Rect(PX, PY, PW, h);
            GUI.Box(panel, GUIContent.none, _box);

            float x = PX + 8;
            float y = PY + 8;

            GUI.Label(new Rect(x, y, PW - 16, 20f), "RESPUESTA DEL CLIENTE", _title);
            y += 26f;

            GUI.Label(new Rect(x, y, PW - 16, 60f), _resultMsg, _lbl);
            y += 66f;

            // Si hay contraoferta, mostrar botones de aceptar/rechazar
            if (_lastQuote.HasCounterOffer)
            {
                if (GUI.Button(new Rect(x, y, (PW - 20f) / 2f, 30f),
                               $"Aceptar ${_lastQuote.CounterOfferPrice:N0}", _btnGreen))
                {
                    _lastQuote.AcceptCounterOffer();
                    int currentDay = FFTimeManager.Instance?.CurrentDay ?? 0;
                    CargoManager.Instance.AcceptQuote(_selectedCargo, _lastQuote, currentDay);
                    _resultMsg = $"✅ Trato cerrado por ${_lastQuote.FinalPrice:N0}";
                    _resultOk  = true;
                    _lastQuote.HasCounterOffer = false;
                }
                if (GUI.Button(new Rect(x + (PW - 16f) / 2f, y, (PW - 20f) / 2f, 30f),
                               "Rechazar", _btnRed))
                {
                    _lastQuote.RejectCounterOffer();
                    _resultMsg = "❌ Rechazaste la contraoferta.";
                    _resultOk  = false;
                    _lastQuote.HasCounterOffer = false;
                }
                y += 34f;
            }

            // Volver
            string backLabel = _resultOk ? "← Al Mercado" : "← Intentar de nuevo";
            if (GUI.Button(new Rect(x, y, PW - 16, 28f), backLabel, _btn))
            {
                if (!_resultOk && _selectedCargo != null &&
                    _selectedCargo.Status == Constants.CargoStatus.Available)
                    _state = State.Quoting;
                else
                    _state = State.Market;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Constants.TransportMode[] GetAvailableModes()
        {
            if (_selectedCargo == null)
                return new[] { Constants.TransportMode.Maritime };

            var modes = new List<Constants.TransportMode>();
            WorldCity origin = CityDatabase.GetCity(_selectedCargo.OriginCityId);
            WorldCity dest   = CityDatabase.GetCity(_selectedCargo.DestinationCityId);
            if (origin == null || dest == null)
                return new[] { Constants.TransportMode.Maritime };

            if (origin.HasPort && dest.HasPort)
                modes.Add(Constants.TransportMode.Maritime);
            if (origin.HasAirport && dest.HasAirport)
                modes.Add(Constants.TransportMode.Air);
            if (origin.CanLandTransportTo(dest) && origin.IsLandHub && dest.IsLandHub)
                modes.Add(Constants.TransportMode.Land);

            if (modes.Count == 0) modes.Add(Constants.TransportMode.Maritime);
            return modes.ToArray();
        }

        private int EstimateDays(Constants.TransportMode mode, float distKm, float speedMult)
        {
            float kmPerDay = mode == Constants.TransportMode.Air   ? 15000f :
                             mode == Constants.TransportMode.Land  ? 600f   :
                             mode == Constants.TransportMode.Rail  ? 800f   : 2000f;
            return Mathf.Max(1, Mathf.CeilToInt(distKm / (kmPerDay * speedMult)));
        }

        private string ClientTip(Constants.ClientType t)
        {
            switch (t)
            {
                case Constants.ClientType.UrgentClient:   return "⚡ Urgente: acepta precios altos pero exige rapidez.";
                case Constants.ClientType.GoodPayer:      return "👍 Buen Pagador: confiable, tolera márgenes razonables.";
                case Constants.ClientType.VeryBadClient:  return "⚠️ Muy Difícil: margen máximo 10%, no negocia.";
                case Constants.ClientType.BadPayer:       return "⚠️ Mal Pagador: margen máximo 15%, suele rechazar.";
                case Constants.ClientType.ContractClient: return "📋 Contrato: busca precio estable, descuentos esperados.";
                case Constants.ClientType.CreditClient:   return "📅 Crédito: pagará en 45 días. Margen máximo 20%.";
                default: return "";
            }
        }

        private void DrawSectionLabel(float x, ref float y, string text)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = new Color(0.6f, 0.8f, 1f);
            GUI.Label(new Rect(x, y, PW - 16, 16f), text, _small);
            GUI.contentColor = prev;
            y += 18f;
        }

        private static Color CargoColor(Constants.CargoType t)
        {
            switch (t)
            {
                case Constants.CargoType.Refrigerated: return Color.cyan;
                case Constants.CargoType.Dangerous:    return Color.red;
                case Constants.CargoType.Urgent:       return Color.yellow;
                case Constants.CargoType.Valuable:     return new Color(1f, 0.8f, 0f);
                default:                               return Color.white;
            }
        }

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

        private void DrawColoredBox(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // ── Styles ────────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_ready) return;
            _ready = true;

            _box = new GUIStyle(GUI.skin.box);
            _box.normal.background = MakeTex(new Color(0f, 0.05f, 0.1f, 0.92f));

            _title = new GUIStyle(GUI.skin.label)
                { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _title.normal.textColor = Color.white;

            _lbl = new GUIStyle(GUI.skin.label)
                { fontSize = 12, fontStyle = FontStyle.Bold };
            _lbl.normal.textColor = Color.white;

            _small = new GUIStyle(GUI.skin.label)
                { fontSize = 11, wordWrap = true };
            _small.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            _btn = new GUIStyle(GUI.skin.button)
                { fontSize = 11, fontStyle = FontStyle.Bold };
            _btn.normal.textColor = Color.white;

            _btnOn = new GUIStyle(_btn);
            _btnOn.normal.background = MakeTex(new Color(0.1f, 0.4f, 0.8f, 0.95f));
            _btnOn.normal.textColor  = Color.white;

            _btnGreen = new GUIStyle(_btn);
            _btnGreen.normal.background = MakeTex(new Color(0.1f, 0.5f, 0.15f, 0.95f));
            _btnGreen.normal.textColor  = Color.white;

            _btnRed = new GUIStyle(_btn);
            _btnRed.normal.background = MakeTex(new Color(0.5f, 0.1f, 0.1f, 0.95f));
            _btnRed.normal.textColor  = Color.white;
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
