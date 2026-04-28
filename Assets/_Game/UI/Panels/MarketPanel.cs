using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Models;
using FreightForwarder.Managers;

namespace FreightForwarder.UI.Panels
{
    public class MarketPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _container;
        private ScrollView _cargosScroll;
        private Label _noCargosLabel;
        
        private List<Cargo> _currentCargos;
        
        public event Action<Cargo> OnQuoteRequested;
        
        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            CreateUI();
            Refresh();
        }
        
        private void CreateUI()
        {
            var root = _uiDocument.rootVisualElement;
            
            _container = new VisualElement();
            _container.style.flexGrow = 1;
            _container.style.paddingLeft = 15;
            _container.style.paddingRight = 15;
            _container.style.paddingTop = 15;
            _container.style.paddingBottom = 15;
            
            var title = new Label("📦 MERCADO DE CARGAS");
            title.style.fontSize = 22;
            title.style.color = new Color(0.4f, 0.7f, 1f);
            title.style.marginBottom = 15;
            _container.Add(title);
            
            _cargosScroll = new ScrollView();
            _cargosScroll.style.flexGrow = 1;
            _cargosScroll.style.flexDirection = FlexDirection.Row;
            _cargosScroll.style.flexWrap = Wrap.Wrap;
            _container.Add(_cargosScroll);
            
            _noCargosLabel = new Label("No hay cargas disponibles.");
            _noCargosLabel.style.display = DisplayStyle.None;
            _container.Add(_noCargosLabel);
            
            root.Add(_container);
        }
        
        public void Refresh()
        {
            if (CargoManager.Instance == null) return;
            
            _currentCargos = CargoManager.Instance.GetAvailableCargos();
            _cargosScroll.Clear();
            
            if (_currentCargos == null || _currentCargos.Count == 0)
            {
                _noCargosLabel.style.display = DisplayStyle.Flex;
                return;
            }
            
            _noCargosLabel.style.display = DisplayStyle.None;
            
            foreach (var cargo in _currentCargos)
            {
                var card = CreateCargoCard(cargo);
                _cargosScroll.Add(card);
            }
        }
        
        private VisualElement CreateCargoCard(Cargo cargo)
        {
            var card = new VisualElement();
            card.AddToClassList("market-card");
            
            int daysLeft = cargo.ExpirationDay - TimeManager.Instance.CurrentDay;
            if (daysLeft <= 2)
                card.AddToClassList("market-card-expiring");
            
            var typeLabel = new Label($"{GetCargoEmoji(cargo.CargoType)} {Constants.GetCargoTypeName(cargo.CargoType)}");
            typeLabel.AddToClassList("market-card-title");
            card.Add(typeLabel);
            
            var origin = CityDatabase.GetCity(cargo.OriginCityId);
            var dest = CityDatabase.GetCity(cargo.DestinationCityId);
            var routeLabel = new Label($"📍 {origin?.DisplayName ?? cargo.OriginCityId} → {dest?.DisplayName ?? cargo.DestinationCityId}");
            routeLabel.AddToClassList("market-card-route");
            card.Add(routeLabel);
            
            var clientLabel = new Label($"👤 {cargo.ClientName}");
            clientLabel.AddToClassList("market-card-client");
            card.Add(clientLabel);
            
            var valueLabel = new Label($"💰 ${cargo.DeclaredValue:N0}");
            valueLabel.AddToClassList("market-card-value");
            card.Add(valueLabel);
            
            var specsLabel = new Label($"⚖️ {cargo.Weight:F0} kg | 📦 {cargo.Volume:F0} m³");
            specsLabel.AddToClassList("market-card-specs");
            card.Add(specsLabel);
            
            var expiryLabel = new Label(GetExpiryText(daysLeft));
            expiryLabel.AddToClassList("market-card-expiry");
            if (daysLeft <= 2)
                expiryLabel.style.color = new Color(1f, 0.5f, 0.2f);
            card.Add(expiryLabel);
            
            var preferredLabel = new Label($"🚛 Recomendado: {Constants.GetTransportModeName(cargo.PreferredTransport)}");
            preferredLabel.AddToClassList("market-card-preferred");
            card.Add(preferredLabel);
            
            var quoteBtn = new Button(() => OnQuoteClicked(cargo));
            quoteBtn.text = "💼 Cotizar";
            quoteBtn.AddToClassList("quote-btn");
            card.Add(quoteBtn);
            
            return card;
        }
        
        private string GetCargoEmoji(Constants.CargoType type)
        {
            return type switch
            {
                Constants.CargoType.General => "📦",
                Constants.CargoType.Refrigerated => "🧊",
                Constants.CargoType.Dangerous => "⚠️",
                Constants.CargoType.Urgent => "⚡",
                Constants.CargoType.Valuable => "💎",
                _ => "📦"
            };
        }
        
        private string GetExpiryText(int daysLeft)
        {
            if (daysLeft <= 0) return "⏰ EXPIRÓ!";
            if (daysLeft == 1) return "⏰ Expira MAÑANA";
            return $"⏰ Expira en {daysLeft} días";
        }
        
        private void OnQuoteClicked(Cargo cargo)
        {
            OnQuoteRequested?.Invoke(cargo);
        }
    }
}