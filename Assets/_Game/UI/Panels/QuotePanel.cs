using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Models;
using FreightForwarder.Managers;

namespace FreightForwarder.UI.Panels
{
    public class QuotePanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _container;
        private Label _cargoInfoLabel;
        private Label _clientInfoLabel;
        private Label _costLabel;
        private Label _profitLabel;
        private Label _resultLabel;
        
        private DropdownField _transportModeDropdown;
        private DropdownField _agentDropdown;
        private IntegerField _priceField;
        private Button _submitBtn;
        private Button _cancelBtn;
        
        private Cargo _currentCargo;
        private List<Agent> _availableAgents;
        private int _currentTransportCost;
        
        public event Action OnClosed;
        
        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            CreateUI();
            Hide();
        }
        
        private void CreateUI()
        {
            var root = _uiDocument.rootVisualElement;
            
            _container = new VisualElement();
            _container.style.position = Position.Absolute;
            _container.style.top = 0;
            _container.style.left = 0;
            _container.style.right = 0;
            _container.style.bottom = 0;
            _container.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _container.style.alignItems = Align.Center;
            _container.style.justifyContent = Justify.Center;
            _container.style.display = DisplayStyle.None;
            
            var panel = new VisualElement();
            panel.AddToClassList("quote-panel");
            
            var title = new Label("📋 COTIZACIÓN");
            title.style.fontSize = 20;
            title.style.color = new Color(0.4f, 0.75f, 1f);
            title.style.marginBottom = 15;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(title);
            
            var columns = new VisualElement();
            columns.style.flexDirection = FlexDirection.Row;
            columns.style.marginBottom = 15;
            
            var leftColumn = new VisualElement();
            leftColumn.style.flexGrow = 1;
            leftColumn.style.marginRight = 15;
            
            _cargoInfoLabel = new Label("Carga: ...");
            _cargoInfoLabel.style.marginBottom = 8;
            leftColumn.Add(_cargoInfoLabel);
            
            _clientInfoLabel = new Label("Cliente: ...");
            _clientInfoLabel.style.marginBottom = 8;
            leftColumn.Add(_clientInfoLabel);
            
            columns.Add(leftColumn);
            
            var rightColumn = new VisualElement();
            rightColumn.style.flexGrow = 1;
            
            var transportLabel = new Label("🚛 Modo de transporte:");
            transportLabel.style.marginBottom = 4;
            rightColumn.Add(transportLabel);
            
            _transportModeDropdown = new DropdownField();
            _transportModeDropdown.style.marginBottom = 10;
            _transportModeDropdown.RegisterValueChangedCallback(ev => OnTransportModeChanged());
            rightColumn.Add(_transportModeDropdown);
            
            var agentLabel = new Label("🤝 Agente:");
            agentLabel.style.marginBottom = 4;
            rightColumn.Add(agentLabel);
            
            _agentDropdown = new DropdownField();
            _agentDropdown.style.marginBottom = 10;
            _agentDropdown.RegisterValueChangedCallback(ev => OnAgentChanged());
            rightColumn.Add(_agentDropdown);
            
            columns.Add(rightColumn);
            panel.Add(columns);
            
            _costLabel = new Label("💰 Costo transporte: $0");
            _costLabel.style.marginBottom = 5;
            panel.Add(_costLabel);
            
            _profitLabel = new Label("📈 Ganancia: $0");
            _profitLabel.style.marginBottom = 15;
            panel.Add(_profitLabel);
            
            _priceField = new IntegerField("Precio (USD)");
            _priceField.style.marginBottom = 10;
            _priceField.RegisterValueChangedCallback(ev => OnPriceChanged());
            panel.Add(_priceField);
            
            _resultLabel = new Label();
            _resultLabel.style.marginTop = 10;
            _resultLabel.style.marginBottom = 10;
            panel.Add(_resultLabel);
            
            var buttonsRow = new VisualElement();
            buttonsRow.style.flexDirection = FlexDirection.Row;
            buttonsRow.style.justifyContent = Justify.Center;
            
            _submitBtn = new Button(() => OnSubmit());
            _submitBtn.text = "Enviar Cotización";
            _submitBtn.style.backgroundColor = new Color(0.2f, 0.6f, 0.3f);
            _submitBtn.style.marginRight = 10;
            _submitBtn.style.paddingLeft = 20;
            _submitBtn.style.paddingRight = 20;
            
            _cancelBtn = new Button(() => Hide());
            _cancelBtn.text = "Cancelar";
            _cancelBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            
            buttonsRow.Add(_submitBtn);
            buttonsRow.Add(_cancelBtn);
            panel.Add(buttonsRow);
            
            _container.Add(panel);
            root.Add(_container);
        }
        
        public void Show(Cargo cargo)
        {
            _currentCargo = cargo;
            _container.style.display = DisplayStyle.Flex;
            Refresh();
        }
        
        public void Hide()
        {
            _container.style.display = DisplayStyle.None;
            OnClosed?.Invoke();
        }
        
        private void Refresh()
        {
            var origin = CityDatabase.GetCity(_currentCargo.OriginCityId);
            var dest = CityDatabase.GetCity(_currentCargo.DestinationCityId);
            _cargoInfoLabel.text = $"📦 {Constants.GetCargoTypeName(_currentCargo.CargoType)}\n📍 {origin?.DisplayName} → {dest?.DisplayName}\n💰 ${_currentCargo.DeclaredValue:N0}";
            _clientInfoLabel.text = $"👤 {_currentCargo.ClientName}\n🏷️ {Constants.GetClientTypeName(_currentCargo.ClientType)}";
            
            UpdateTransportModes();
            _priceField.value = 1000;
        }
        
        private void UpdateTransportModes()
        {
            var origin = CityDatabase.GetCity(_currentCargo.OriginCityId);
            var dest = CityDatabase.GetCity(_currentCargo.DestinationCityId);
            
            _transportModeDropdown.choices.Clear();
            if (origin.HasPort && dest.HasPort) _transportModeDropdown.choices.Add("Marítimo");
            if (origin.HasAirport && dest.HasAirport) _transportModeDropdown.choices.Add("Aéreo");
            if (origin.IsLandHub && dest.IsLandHub) _transportModeDropdown.choices.Add("Terrestre");
            
            if (_transportModeDropdown.choices.Count > 0)
                _transportModeDropdown.value = _transportModeDropdown.choices[0];
            
            UpdateAgents();
        }
        
        private void OnTransportModeChanged() => UpdateAgents();
        
        private void UpdateAgents()
        {
            _availableAgents = AgentManager.Instance?.GetAvailableAgents() ?? new List<Agent>();
            _agentDropdown.choices.Clear();
            foreach (var agent in _availableAgents)
                _agentDropdown.choices.Add(agent.Name);
            
            if (_agentDropdown.choices.Count > 0)
                _agentDropdown.value = _agentDropdown.choices[0];
            
            UpdateCost();
        }
        
        private void OnAgentChanged() => UpdateCost();
        
        private void UpdateCost()
        {
            _currentTransportCost = 500;
            _costLabel.text = $"💰 Costo transporte: ${_currentTransportCost:N0}";
            UpdateProfit();
        }
        
        private void UpdateProfit()
        {
            int profit = _priceField.value - _currentTransportCost;
            _profitLabel.text = profit >= 0 ? $"📈 Ganancia: +${profit:N0}" : $"📉 Pérdida: -${-profit:N0}";
        }
        
        private void OnPriceChanged() => UpdateProfit();
        
        private async void OnSubmit()
        {
            int price = _priceField.value;
            if (price <= _currentTransportCost)
            {
                ShowResult("El precio debe ser mayor al costo", false);
                return;
            }
            
            ShowResult("✅ ¡COTIZACIÓN ENVIADA! (Demo)", true);
            await System.Threading.Tasks.Task.Delay(1500);
            Hide();
        }
        
        private void ShowResult(string message, bool isSuccess)
        {
            _resultLabel.text = message;
            _resultLabel.style.color = isSuccess ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
        }
    }
}