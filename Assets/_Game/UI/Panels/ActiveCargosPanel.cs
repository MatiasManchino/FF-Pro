using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Models;
using FreightForwarder.Managers;

namespace FreightForwarder.UI.Panels
{
    public class ActiveCargosPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _container;
        private ScrollView _cargosScroll;
        private Label _noCargosLabel;
        private Button _showHistoryBtn;
        private VisualElement _historyContainer;
        private ListView _historyList;
        
        private bool _showHistory;
        private List<Cargo> _activeCargos;
        private List<Cargo> _cargoHistory;
        
        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            CreateUI();
            Refresh();
            
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoAccepted += _ => Refresh();
                CargoManager.Instance.OnCargoCompleted += _ => Refresh();
                CargoManager.Instance.OnCargoFailed += _ => Refresh();
            }
            
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayPassed += Refresh;
        }
        
        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayPassed -= Refresh;
        }
        
        private void CreateUI()
        {
            var root = _uiDocument.rootVisualElement;
            
            _container = new VisualElement();
            _container.AddToClassList("panel-container");
            
            var title = new Label("🚢 CARGAS ACTIVAS");
            title.AddToClassList("panel-title");
            _container.Add(title);
            
            _cargosScroll = new ScrollView();
            _cargosScroll.AddToClassList("active-cargos-scroll");
            _container.Add(_cargosScroll);
            
            _noCargosLabel = new Label("No hay cargas activas.\nCotiza una carga en el mercado.");
            _noCargosLabel.AddToClassList("empty-label");
            _noCargosLabel.style.display = DisplayStyle.None;
            _cargosScroll.Add(_noCargosLabel);
            
            _showHistoryBtn = new Button(() => ToggleHistory());
            _showHistoryBtn.text = "📜 Ver Historial";
            _showHistoryBtn.AddToClassList("history-btn");
            _container.Add(_showHistoryBtn);
            
            _historyContainer = new VisualElement();
            _historyContainer.AddToClassList("history-container");
            _historyContainer.style.display = DisplayStyle.None;
            
            var historyTitle = new Label("📋 HISTORIAL");
            historyTitle.AddToClassList("history-title");
            _historyContainer.Add(historyTitle);
            
            _historyList = new ListView();
            _historyList.AddToClassList("history-list");
            _historyContainer.Add(_historyList);
            
            _container.Add(_historyContainer);
            root.Add(_container);
        }
        
        public void Refresh()
        {
            if (CargoManager.Instance == null) return;
            
            _activeCargos = CargoManager.Instance.ActiveCargos;
            _cargoHistory = CargoManager.Instance.CompletedCargos.Concat(CargoManager.Instance.FailedCargos).ToList();
            
            RefreshActiveCargos();
            RefreshHistory();
        }
        
        private void RefreshActiveCargos()
        {
            if (_cargosScroll == null) return;
            _cargosScroll.Clear();
            
            if (_activeCargos == null || _activeCargos.Count == 0)
            {
                _noCargosLabel.style.display = DisplayStyle.Flex;
                return;
            }
            
            _noCargosLabel.style.display = DisplayStyle.None;
            
            foreach (var cargo in _activeCargos)
            {
                var card = CreateActiveCard(cargo);
                _cargosScroll.Add(card);
            }
        }
        
        private VisualElement CreateActiveCard(Cargo cargo)
        {
            var card = new VisualElement();
            card.AddToClassList("active-card");
            
            // Clase según modo de transporte
            string modeClass = cargo.TransportMode switch
            {
                Constants.TransportMode.Maritime => "active-card-maritime",
                Constants.TransportMode.Air => "active-card-air",
                _ => "active-card-land"
            };
            card.AddToClassList(modeClass);
            
            // Header
            var headerRow = new VisualElement();
            headerRow.AddToClassList("active-card-header");
            
            var modeEmoji = cargo.TransportMode switch
            {
                Constants.TransportMode.Maritime => "🚢",
                Constants.TransportMode.Air => "✈️",
                _ => "🚛"
            };
            
            var typeLabel = new Label($"{modeEmoji} {Constants.GetCargoTypeName(cargo.CargoType)}");
            typeLabel.AddToClassList("active-card-type");
            headerRow.Add(typeLabel);
            
            var progressPercent = (float)(cargo.TotalTransitDays - cargo.DaysRemaining) / cargo.TotalTransitDays;
            var progressLabel = new Label($"{(progressPercent * 100):F0}%");
            progressLabel.AddToClassList("active-card-progress");
            headerRow.Add(progressLabel);
            
            card.Add(headerRow);
            
            // Ruta
            var origin = CityDatabase.GetCity(cargo.OriginCityId);
            var dest = CityDatabase.GetCity(cargo.DestinationCityId);
            var routeLabel = new Label($"📍 {origin?.DisplayName ?? cargo.OriginCityId} → {dest?.DisplayName ?? cargo.DestinationCityId}");
            routeLabel.AddToClassList("active-card-route");
            card.Add(routeLabel);
            
            // Cliente
            var clientLabel = new Label($"👤 {cargo.ClientName}");
            clientLabel.AddToClassList("active-card-client");
            card.Add(clientLabel);
            
            // Barra de progreso
            var progressBar = new VisualElement();
            progressBar.AddToClassList("progress-bar");
            
            var progressFill = new VisualElement();
            progressFill.AddToClassList("progress-fill");
            progressFill.style.width = new Length(progressPercent * 100, LengthUnit.Percent);
            progressBar.Add(progressFill);
            card.Add(progressBar);
            
            // Detalles
            var detailsRow = new VisualElement();
            detailsRow.AddToClassList("active-card-details");
            
            var daysLeft = cargo.DaysRemaining;
            var daysLabel = new Label(daysLeft <= 1 ? "⏰ Último día" : $"⏳ Faltan {daysLeft} días");
            daysLabel.AddToClassList("active-card-days");
            if (daysLeft <= 2)
                daysLabel.AddToClassList("active-card-days-urgent");
            detailsRow.Add(daysLabel);
            
            var profit = cargo.FinalPrice - cargo.AgentCost;
            var profitLabel = new Label(profit >= 0 ? $"💰 +${profit:N0}" : $"📉 -${-profit:N0}");
            profitLabel.AddToClassList(profit >= 0 ? "active-card-profit-positive" : "active-card-profit-negative");
            detailsRow.Add(profitLabel);
            
            card.Add(detailsRow);
            
            // Agente
            var agent = AgentManager.Instance?.GetAgent(cargo.AgentId);
            if (agent != null)
            {
                var agentLabel = new Label($"🤝 {agent.Name} | Conf: {agent.PlayerTrust}%");
                agentLabel.AddToClassList("active-card-agent");
                card.Add(agentLabel);
            }
            
            return card;
        }
        
        private void RefreshHistory()
        {
            if (_historyList == null) return;
            
            var items = new List<string>();
            foreach (var cargo in _cargoHistory?.Take(20) ?? new List<Cargo>())
            {
                var origin = CityDatabase.GetCity(cargo.OriginCityId);
                var dest = CityDatabase.GetCity(cargo.DestinationCityId);
                string status = cargo.Status == Constants.CargoStatus.Completed ? "✅" : "❌";
                items.Add($"{status} {origin?.DisplayName ?? "?"} → {dest?.DisplayName ?? "?"} | ${cargo.FinalPrice:N0}");
            }
            
            _historyList.itemsSource = items;
            _historyList.makeItem = () => new Label();
            _historyList.bindItem = (element, i) => ((Label)element).text = items[i];
        }
        
        private void ToggleHistory()
        {
            _showHistory = !_showHistory;
            _historyContainer.style.display = _showHistory ? DisplayStyle.Flex : DisplayStyle.None;
            _showHistoryBtn.text = _showHistory ? "📜 Ocultar Historial" : "📜 Ver Historial";
            
            if (_showHistory)
                RefreshHistory();
        }
    }
}