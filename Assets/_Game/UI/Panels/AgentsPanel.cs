using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using FreightForwarder.Models;
using FreightForwarder.Managers;

namespace FreightForwarder.UI.Panels
{
    public class AgentsPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private VisualElement _container;
        private ScrollView _agentsScroll;
        
        private List<Agent> _agents;
        
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
            _container.AddToClassList("panel-container");
            
            var title = new Label("🤝 AGENTES DE TRANSPORTE");
            title.AddToClassList("panel-title");
            _container.Add(title);
            
            _agentsScroll = new ScrollView();
            _agentsScroll.AddToClassList("agents-scroll");
            _container.Add(_agentsScroll);
            
            root.Add(_container);
        }
        
        public void Refresh()
        {
            if (_agentsScroll == null) return;
            _agentsScroll.Clear();
            
            if (AgentManager.Instance == null)
            {
                var errorLabel = new Label("AgentManager no disponible");
                errorLabel.AddToClassList("error-label");
                _agentsScroll.Add(errorLabel);
                return;
            }
            
            _agents = AgentManager.Instance.GetAllAgents();
            
            if (_agents == null || _agents.Count == 0)
            {
                var emptyLabel = new Label("No hay agentes disponibles");
                emptyLabel.AddToClassList("empty-label");
                _agentsScroll.Add(emptyLabel);
                return;
            }
            
            foreach (var agent in _agents)
            {
                var card = CreateAgentCard(agent);
                _agentsScroll.Add(card);
            }
        }
        
        private VisualElement CreateAgentCard(Agent agent)
        {
            var card = new VisualElement();
            card.AddToClassList("agent-card");
            
            // Añadir clase según nivel de confianza
            if (agent.PlayerTrust >= 70)
                card.AddToClassList("agent-trust-high");
            else if (agent.PlayerTrust >= 40)
                card.AddToClassList("agent-trust-medium");
            else
                card.AddToClassList("agent-trust-low");
            
            // Header
            var header = new VisualElement();
            header.AddToClassList("agent-header");
            
            var nameLabel = new Label($"{GetPersonalityEmoji(agent.Personality)} {agent.Name}");
            nameLabel.AddToClassList("agent-name");
            header.Add(nameLabel);
            
            var trustLabel = new Label($"🤝 Confianza: {agent.PlayerTrust}%");
            trustLabel.AddToClassList("agent-trust");
            header.Add(trustLabel);
            
            card.Add(header);
            
            // Personalidad
            var personalityLabel = new Label($"🎭 {GetPersonalityName(agent.Personality)}");
            personalityLabel.AddToClassList("agent-personality");
            card.Add(personalityLabel);
            
            // Especialidades
            var specialties = string.Join(", ", agent.TransportModes.Select(m => Constants.GetTransportModeName(m)));
            var specsLabel = new Label($"🚛 {specialties}");
            specsLabel.AddToClassList("agent-specs");
            card.Add(specsLabel);
            
            // Tarifas
            var ratesLabel = new Label($"💰 Precio: x{agent.BasePriceMultiplier:F2} | ⚡ Velocidad: x{agent.BaseSpeedMultiplier:F2}");
            ratesLabel.AddToClassList("agent-rates");
            card.Add(ratesLabel);
            
            // Estado
            var statusRow = new VisualElement();
            statusRow.AddToClassList("agent-status-row");
            
            var relationshipLabel = new Label(GetRelationshipEmoji(agent.Relationship));
            relationshipLabel.AddToClassList("agent-relationship");
            statusRow.Add(relationshipLabel);
            
            var stateLabel = new Label($"{GetStateEmoji(agent.CurrentState)} {GetStateName(agent.CurrentState)}");
            stateLabel.AddToClassList("agent-state");
            statusRow.Add(stateLabel);
            
            var loadLabel = new Label($"📦 Carga: {agent.CurrentLoad}/{agent.MaxCapacity}");
            loadLabel.AddToClassList("agent-load");
            statusRow.Add(loadLabel);
            
            card.Add(statusRow);
            
            // Tooltip con descripción
            card.tooltip = GetPersonalityDescription(agent.Personality);
            
            return card;
        }
        
        private string GetPersonalityEmoji(Constants.AgentPersonality personality)
        {
            return personality switch
            {
                Constants.AgentPersonality.Reliable => "🛡️",
                Constants.AgentPersonality.Cheap => "💰",
                Constants.AgentPersonality.Ambitious => "📈",
                Constants.AgentPersonality.Lazy => "😴",
                Constants.AgentPersonality.Friendly => "🤗",
                Constants.AgentPersonality.Elusive => "👻",
                Constants.AgentPersonality.Efficient => "⚡",
                Constants.AgentPersonality.Scammer => "🎭",
                Constants.AgentPersonality.Liar => "🤥",
                Constants.AgentPersonality.Bipolar => "🎢",
                Constants.AgentPersonality.Envious => "😤",
                Constants.AgentPersonality.Disappearing => "💨",
                Constants.AgentPersonality.Loyal => "🤝",
                Constants.AgentPersonality.Rival => "⚔️",
                _ => "❓"
            };
        }
        
        private string GetPersonalityName(Constants.AgentPersonality personality)
        {
            return Constants.GetAgentPersonalityName(personality);
        }
        
        private string GetRelationshipEmoji(Constants.AgentRelationship relationship)
        {
            return relationship switch
            {
                Constants.AgentRelationship.Partner => "💍 Socio",
                Constants.AgentRelationship.Ally => "🤝 Aliado",
                Constants.AgentRelationship.Friend => "😊 Amigo",
                Constants.AgentRelationship.Good => "👍 Bueno",
                Constants.AgentRelationship.Neutral => "😐 Neutral",
                Constants.AgentRelationship.Bad => "😠 Malo",
                Constants.AgentRelationship.Enemy => "👎 Enemigo",
                _ => "😐 Neutral"
            };
        }
        
        private string GetStateEmoji(Constants.AgentState state)
        {
            return state switch
            {
                Constants.AgentState.Idle => "✅",
                Constants.AgentState.Overworked => "⚠️",
                Constants.AgentState.Stressed => "😰",
                Constants.AgentState.Angry => "😤",
                Constants.AgentState.Greedy => "💰",
                Constants.AgentState.Disappeared => "👻",
                Constants.AgentState.Bankrupt => "💀",
                _ => "❓"
            };
        }
        
        private string GetStateName(Constants.AgentState state)
        {
            return Constants.GetAgentStateName(state);
        }
        
        private string GetPersonalityDescription(Constants.AgentPersonality personality)
        {
            return personality switch
            {
                Constants.AgentPersonality.Reliable => "🛡️ Confiable. Nunca falla, pero es caro y no negocia.",
                Constants.AgentPersonality.Cheap => "💰 Económico. Barato, pero a veces 'pierde' cargas.",
                Constants.AgentPersonality.Ambitious => "📈 Ambicioso. Sube precios si detecta desesperación.",
                Constants.AgentPersonality.Lazy => "😴 Perezoso. Responde lento, deja cargas olvidadas.",
                Constants.AgentPersonality.Friendly => "🤗 Amigable. Avisa antes de subir precios.",
                Constants.AgentPersonality.Elusive => "👻 Esquivo. Desaparece por días sin avisar.",
                Constants.AgentPersonality.Efficient => "⚡ Eficiente. Siempre a tiempo, pero colapsa si lo sobrecargas.",
                Constants.AgentPersonality.Scammer => "🎭 Estafador. Cobra extras falsos. ¡Cuidado!",
                Constants.AgentPersonality.Liar => "🤥 Mentiroso. Dice que entregó pero no entregó.",
                Constants.AgentPersonality.Bipolar => "🎢 Bipolar. Impredecible, un día excelente, otro horrible.",
                Constants.AgentPersonality.Envious => "😤 Envidioso. Te sabotea si creces mucho.",
                Constants.AgentPersonality.Disappearing => "💨 Fugaz. Puede desaparecer con tu carga si quiebra.",
                Constants.AgentPersonality.Loyal => "🤝 Leal. Mejor precio por usar siempre el mismo.",
                Constants.AgentPersonality.Rival => "⚔️ Rival. Odia a otros agentes, te penaliza si cambias.",
                _ => "Estándar."
            };
        }
    }
}