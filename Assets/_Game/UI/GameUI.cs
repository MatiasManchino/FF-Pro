using System;
using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Managers;
using FreightForwarder.Core;

namespace FreightForwarder.UI
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _root;

        // HUD
        private Label _companyLabel;
        private Label _dateLabel;
        private Label _moneyLabel;
        private Label _reputationLabel;
        private Label _levelLabel;
        private Label _newsLabel;

        // Panel contenedor
        private VisualElement _panelContainer;
        private Label _panelTitle;
        private ScrollView _panelContent;

        // Botones de tabs
        private Button _tabMarket;
        private Button _tabActive;
        private Button _tabFinances;
        private Button _tabOffices;
        private Button _tabAgents;
        private Button _tabClients;
        private Button _tabMap;

        // Botones de velocidad
        private Button _pauseBtn;
        private Button _speed1Btn;
        private Button _speed2Btn;
        private Button _speed3Btn;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("❌ No hay UIDocument en este GameObject");
                return;
            }

            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("❌ RootVisualElement es nulo");
                return;
            }

            BindElements();
            SetupEvents();
            SubscribeToManagers();
            ShowPanel("market");
        }

        private void OnDisable()
        {
            UnsubscribeFromManagers();
        }

        private void BindElements()
        {
            // HUD
            _companyLabel = _root.Q<Label>("company-label");
            _dateLabel = _root.Q<Label>("date-label");
            _moneyLabel = _root.Q<Label>("money-label");
            _reputationLabel = _root.Q<Label>("reputation-label");
            _levelLabel = _root.Q<Label>("level-label");
            _newsLabel = _root.Q<Label>("news-label");

            // Panel
            _panelContainer = _root.Q<VisualElement>("panel-container");
            _panelTitle = _root.Q<Label>("panel-title");
            _panelContent = _root.Q<ScrollView>("panel-content");

            // Tabs
            _tabMarket = _root.Q<Button>("tab-market");
            _tabActive = _root.Q<Button>("tab-active");
            _tabFinances = _root.Q<Button>("tab-finances");
            _tabOffices = _root.Q<Button>("tab-offices");
            _tabAgents = _root.Q<Button>("tab-agents");
            _tabClients = _root.Q<Button>("tab-clients");
            _tabMap = _root.Q<Button>("tab-map");

            // Velocidad
            _pauseBtn = _root.Q<Button>("pause-btn");
            _speed1Btn = _root.Q<Button>("speed1-btn");
            _speed2Btn = _root.Q<Button>("speed2-btn");
            _speed3Btn = _root.Q<Button>("speed3-btn");
        }

        private void SetupEvents()
        {
            _tabMarket?.RegisterCallback<ClickEvent>(_ => ShowPanel("market"));
            _tabActive?.RegisterCallback<ClickEvent>(_ => ShowPanel("active"));
            _tabFinances?.RegisterCallback<ClickEvent>(_ => ShowPanel("finances"));
            _tabOffices?.RegisterCallback<ClickEvent>(_ => ShowPanel("offices"));
            _tabAgents?.RegisterCallback<ClickEvent>(_ => ShowPanel("agents"));
            _tabClients?.RegisterCallback<ClickEvent>(_ => ShowPanel("clients"));
            _tabMap?.RegisterCallback<ClickEvent>(_ => ShowPanel("map"));

            _pauseBtn?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.PauseGame());
            _speed1Btn?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.SetTimeScale(1f));
            _speed2Btn?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.SetTimeScale(2f));
            _speed3Btn?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.SetTimeScale(3f));
        }

        private void SubscribeToManagers()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += UpdateMoney;
                EconomyManager.Instance.OnReputationChanged += UpdateReputation;
                EconomyManager.Instance.OnLevelUp += UpdateLevel;
            }
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateChanged += UpdateDate;
            }
        }

        private void UnsubscribeFromManagers()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= UpdateMoney;
                EconomyManager.Instance.OnReputationChanged -= UpdateReputation;
                EconomyManager.Instance.OnLevelUp -= UpdateLevel;
            }
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateChanged -= UpdateDate;
            }
        }

        private void UpdateMoney(int newMoney)
        {
            if (_moneyLabel != null) _moneyLabel.text = $"${newMoney:N0}";
        }

        private void UpdateReputation(int newRep)
        {
            if (_reputationLabel != null) _reputationLabel.text = $"Rep: {newRep}/100";
        }

        private void UpdateLevel(int newLevel)
        {
            if (_levelLabel != null) _levelLabel.text = $"Nv. {newLevel}";
        }

        private void UpdateDate(DateTime newDate)
        {
            if (_dateLabel != null) _dateLabel.text = newDate.ToString("dd/MM/yyyy");
        }

        private void ShowPanel(string panelId)
        {
            // Limpiar contenido actual
            if (_panelContent != null) _panelContent.Clear();

            // Instanciar el panel correspondiente (placeholder por ahora)
            // En una versión completa, aquí se instanciarían los prefabs de cada panel.
            switch (panelId)
            {
                case "market":
                    _panelTitle.text = "📦 Mercado de Cargas";
                    // _panelContent.Add(Instantiate(marketPanelPrefab));
                    break;
                case "active":
                    _panelTitle.text = "🚢 En Tránsito";
                    break;
                case "finances":
                    _panelTitle.text = "💰 Finanzas";
                    break;
                case "offices":
                    _panelTitle.text = "🏢 Oficinas";
                    break;
                case "agents":
                    _panelTitle.text = "🤝 Agentes";
                    break;
                case "clients":
                    _panelTitle.text = "👥 Clientes";
                    break;
                case "map":
                    _panelTitle.text = "🌍 Mapa";
                    break;
                default:
                    _panelTitle.text = "Panel";
                    break;
            }

            // Resaltar tab activo (opcional)
            ResetTabStyles();
            switch (panelId)
            {
                case "market": _tabMarket?.AddToClassList("tab-button-active"); break;
                case "active": _tabActive?.AddToClassList("tab-button-active"); break;
                case "finances": _tabFinances?.AddToClassList("tab-button-active"); break;
                case "offices": _tabOffices?.AddToClassList("tab-button-active"); break;
                case "agents": _tabAgents?.AddToClassList("tab-button-active"); break;
                case "clients": _tabClients?.AddToClassList("tab-button-active"); break;
                case "map": _tabMap?.AddToClassList("tab-button-active"); break;
            }
        }

        private void ResetTabStyles()
        {
            _tabMarket?.RemoveFromClassList("tab-button-active");
            _tabActive?.RemoveFromClassList("tab-button-active");
            _tabFinances?.RemoveFromClassList("tab-button-active");
            _tabOffices?.RemoveFromClassList("tab-button-active");
            _tabAgents?.RemoveFromClassList("tab-button-active");
            _tabClients?.RemoveFromClassList("tab-button-active");
            _tabMap?.RemoveFromClassList("tab-button-active");
        }

        public void ShowNotification(string message, string type = "info")
        {
            Debug.Log($"[Notificación] {message} (Tipo: {type})");
            // Acá después se puede implementar una UI de notificaciones
        }
    }
}