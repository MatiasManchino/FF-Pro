using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Models;
using FreightForwarder.Managers;

namespace FreightForwarder.UI.Panels
{
    public class OfficesPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _container;
        private ScrollView _officesScroll;
        
        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            CreateUI();
            Refresh();
            
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnMoneyChanged += _ => Refresh();
        }
        
        private void CreateUI()
        {
            var root = _uiDocument.rootVisualElement;
            
            _container = new VisualElement();
            _container.AddToClassList("panel-container");
            
            var title = new Label("🏢 OFICINAS");
            title.AddToClassList("panel-title");
            _container.Add(title);
            
            _officesScroll = new ScrollView();
            _officesScroll.AddToClassList("offices-scroll");
            _container.Add(_officesScroll);
            
            root.Add(_container);
        }
        
        public void Refresh()
        {
            if (_officesScroll == null) return;
            _officesScroll.Clear();
            
            var allCities = CityDatabase.AllCities.Values;
            var unlockedCities = allCities.Where(c => c.IsUnlocked).ToList();
            var lockedCities = allCities.Where(c => !c.IsUnlocked).ToList();
            
            int officeCount = unlockedCities.Count;
            int totalCities = allCities.Count();
            
            var progressLabel = new Label($"🌍 Progreso: {officeCount}/{totalCities} ciudades");
            progressLabel.AddToClassList("offices-progress-label");
            _officesScroll.Add(progressLabel);
            
            var progressBar = new VisualElement();
            progressBar.AddToClassList("offices-progress-bar");
            
            var progressFill = new VisualElement();
            progressFill.AddToClassList("offices-progress-fill");
            progressFill.style.width = new Length((float)officeCount / totalCities * 100, LengthUnit.Percent);
            progressBar.Add(progressFill);
            _officesScroll.Add(progressBar);
            
            // Ciudades desbloqueadas
            var unlockedTitle = new Label($"✅ DESBLOQUEADAS ({unlockedCities.Count})");
            unlockedTitle.AddToClassList("offices-section-title");
            unlockedTitle.AddToClassList("offices-title-unlocked");
            _officesScroll.Add(unlockedTitle);
            
            foreach (var city in unlockedCities)
            {
                var card = CreateOfficeCard(city, true);
                _officesScroll.Add(card);
            }
            
            // Ciudades bloqueadas
            var lockedTitle = new Label($"🔒 BLOQUEADAS ({lockedCities.Count})");
            lockedTitle.AddToClassList("offices-section-title");
            lockedTitle.AddToClassList("offices-title-locked");
            _officesScroll.Add(lockedTitle);
            
            foreach (var city in lockedCities.Take(10))
            {
                var card = CreateOfficeCard(city, false);
                _officesScroll.Add(card);
            }
        }
        
        private VisualElement CreateOfficeCard(WorldCity city, bool isUnlocked)
        {
            var card = new VisualElement();
            card.AddToClassList(isUnlocked ? "office-card-unlocked" : "office-card-locked");
            
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("office-info");
            
            var nameLabel = new Label($"🏙️ {city.DisplayName}, {city.Country}");
            nameLabel.AddToClassList("office-name");
            infoContainer.Add(nameLabel);
            
            var infraLabel = new Label($"{(city.HasPort ? "🚢 " : "")}{(city.HasAirport ? "✈️ " : "")}{(city.IsLandHub ? "🚛 " : "")}");
            infraLabel.AddToClassList("office-infra");
            infoContainer.Add(infraLabel);
            
            card.Add(infoContainer);
            
            if (isUnlocked)
            {
                var level = 1;
                var levelLabel = new Label($"⭐ Nivel {level}");
                levelLabel.AddToClassList("office-level");
                card.Add(levelLabel);
                
                if (level < 5)
                {
                    int cost = 5000 * level;
                    var upgradeBtn = new Button(() => OnUpgradeClicked(city.Id, cost));
                    upgradeBtn.text = $"⬆️ ${cost:N0}";
                    upgradeBtn.AddToClassList("office-btn");
                    upgradeBtn.AddToClassList("office-btn-upgrade");
                    
                    if (EconomyManager.Instance != null && EconomyManager.Instance.Money < cost)
                        upgradeBtn.SetEnabled(false);
                    
                    card.Add(upgradeBtn);
                }
                else
                {
                    var maxLabel = new Label("👑 MAX");
                    maxLabel.AddToClassList("office-max");
                    card.Add(maxLabel);
                }
            }
            else
            {
                int cost = city.UnlockCost;
                var unlockBtn = new Button(() => OnUnlockClicked(city.Id, cost));
                unlockBtn.text = $"🔓 Desbloquear ${cost:N0}";
                unlockBtn.AddToClassList("office-btn");
                unlockBtn.AddToClassList("office-btn-unlock");
                
                if (EconomyManager.Instance != null && EconomyManager.Instance.Money < cost)
                    unlockBtn.SetEnabled(false);
                
                card.Add(unlockBtn);
            }
            
            return card;
        }
        
        private void OnUnlockClicked(string cityId, int cost)
        {
            if (EconomyManager.Instance == null) return;
            
            if (EconomyManager.Instance.TrySubtractMoney(cost, $"Desbloquear {cityId}"))
            {
                var city = CityDatabase.GetCity(cityId);
                if (city != null) city.IsUnlocked = true;
                Refresh();
                ShowNotification($"🏢 ¡{cityId} desbloqueada!", "success");
            }
            else
            {
                ShowNotification($"💰 Fondos insuficientes", "warning");
            }
        }
        
        private void OnUpgradeClicked(string cityId, int cost)
        {
            if (EconomyManager.Instance == null) return;
            
            if (EconomyManager.Instance.TrySubtractMoney(cost, $"Mejorar {cityId}"))
            {
                Refresh();
                ShowNotification($"⬆️ {cityId} mejorada!", "success");
            }
            else
            {
                ShowNotification($"💰 Fondos insuficientes", "warning");
            }
        }
        
        private void ShowNotification(string message, string type)
        {
            if (GameUI.Instance != null)
                GameUI.Instance.ShowNotification(message, type);
            else
                Debug.Log($"[OfficesPanel] {message}");
        }
    }
}