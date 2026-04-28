using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Managers;

namespace FreightForwarder.UI.Panels
{
    public class FinancesPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _container;
        private Label _moneyLabel;
        private Label _incomeLabel;
        private Label _expensesLabel;
        private Label _profitLabel;
        private Label _monthlyCostsLabel;
        private Label _levelLabel;
        private Label _cargosCompletedLabel;
        private Label _successRateLabel;
        
        private void OnEnable()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            CreateUI();
            Refresh();
            
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += _ => Refresh();
                EconomyManager.Instance.OnReputationChanged += _ => Refresh();
                EconomyManager.Instance.OnLevelUp += _ => Refresh();
            }
            
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoCompleted += _ => Refresh();
                CargoManager.Instance.OnCargoFailed += _ => Refresh();
            }
        }
        
        private void CreateUI()
        {
            var root = _uiDocument.rootVisualElement;
            
            _container = new VisualElement();
            _container.AddToClassList("panel-container");
            
            var title = new Label("💰 FINANZAS");
            title.AddToClassList("panel-title");
            _container.Add(title);
            
            var grid = new VisualElement();
            grid.AddToClassList("finances-grid");
            
            // Columna izquierda
            var leftCol = new VisualElement();
            leftCol.AddToClassList("finances-column");
            
            leftCol.Add(CreateCard("💰 EFECTIVO", () => _moneyLabel = CreateValueLabel("$0")));
            leftCol.Add(CreateSeparator());
            leftCol.Add(CreateStatRow("📈 Ingresos Totales", () => _incomeLabel = CreateStatValueLabel("$0")));
            leftCol.Add(CreateStatRow("📉 Gastos Totales", () => _expensesLabel = CreateStatValueLabel("$0")));
            leftCol.Add(CreateStatRow("💵 Beneficio Neto", () => _profitLabel = CreateProfitLabel("$0")));
            leftCol.Add(CreateSeparator());
            leftCol.Add(CreateStatRow("🏢 Costos Mensuales", () => _monthlyCostsLabel = CreateStatValueLabel("$0")));
            
            grid.Add(leftCol);
            
            // Columna derecha
            var rightCol = new VisualElement();
            rightCol.AddToClassList("finances-column");
            
            rightCol.Add(CreateCard("📊 ESTADÍSTICAS"));
            rightCol.Add(CreateStatRow("⭐ Nivel", () => _levelLabel = CreateStatValueLabel("1")));
            rightCol.Add(CreateStatRow("📦 Cargas Completadas", () => _cargosCompletedLabel = CreateStatValueLabel("0")));
            rightCol.Add(CreateStatRow("📊 Tasa de Éxito", () => _successRateLabel = CreateStatValueLabel("0%")));
            
            grid.Add(rightCol);
            
            _container.Add(grid);
            root.Add(_container);
        }
        
        private VisualElement CreateCard(string title, System.Action setupContent = null)
        {
            var card = new VisualElement();
            card.AddToClassList("finance-card");
            
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("finance-card-title");
            card.Add(titleLabel);
            
            setupContent?.Invoke();
            
            return card;
        }
        
        private Label CreateValueLabel(string defaultValue)
        {
            var label = new Label(defaultValue);
            label.AddToClassList("finance-value-large");
            return label;
        }
        
        private Label CreateProfitLabel(string defaultValue)
        {
            var label = new Label(defaultValue);
            label.AddToClassList("finance-value-large");
            label.AddToClassList("finance-profit");
            return label;
        }
        
        private Label CreateStatValueLabel(string defaultValue)
        {
            var label = new Label(defaultValue);
            label.AddToClassList("finance-stat-value");
            return label;
        }
        
        private VisualElement CreateStatRow(string labelText, System.Action addValueLabel)
        {
            var row = new VisualElement();
            row.AddToClassList("finance-stat-row");
            
            var label = new Label(labelText);
            label.AddToClassList("finance-stat-label");
            row.Add(label);
            
            addValueLabel?.Invoke();
            
            return row;
        }
        
        private VisualElement CreateSeparator()
        {
            var sep = new VisualElement();
            sep.AddToClassList("finance-separator");
            return sep;
        }
        
        public void Refresh()
        {
            if (EconomyManager.Instance == null) return;
            
            if (_moneyLabel != null)
                _moneyLabel.text = $"${EconomyManager.Instance.Money:N0}";
            
            if (_incomeLabel != null)
                _incomeLabel.text = $"${EconomyManager.Instance.TotalRevenue:N0}";
            
            if (_expensesLabel != null)
                _expensesLabel.text = $"${EconomyManager.Instance.TotalCosts:N0}";
            
            int profit = EconomyManager.Instance.GetNetProfit();
            if (_profitLabel != null)
            {
                _profitLabel.text = profit >= 0 ? $"+${profit:N0}" : $"-${-profit:N0}";
                _profitLabel.RemoveFromClassList(profit >= 0 ? "finance-profit-negative" : "finance-profit-positive");
                _profitLabel.AddToClassList(profit >= 0 ? "finance-profit-positive" : "finance-profit-negative");
            }
            
            if (_monthlyCostsLabel != null)
                _monthlyCostsLabel.text = $"${EconomyManager.Instance.MonthlyOfficeCosts:N0}/mes";
            
            if (_levelLabel != null)
                _levelLabel.text = $"{EconomyManager.Instance.Level}";
            
            if (_cargosCompletedLabel != null)
                _cargosCompletedLabel.text = $"{EconomyManager.Instance.TotalCargosCompleted}";
            
            if (_successRateLabel != null)
                _successRateLabel.text = $"{(EconomyManager.Instance.GetSuccessRate() * 100):F0}%";
        }
    }
}