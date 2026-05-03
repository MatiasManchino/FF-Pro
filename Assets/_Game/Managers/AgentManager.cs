using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FreightForwarder.Models;
using FreightForwarder.Utils;

namespace FreightForwarder.Managers
{
    public class AgentManager : Singleton<AgentManager>
    {
        [Header("Configuración")]
        [SerializeField] private bool _enableAgentDecisions = true;

        private Dictionary<string, Agent> _agents;
        private Dictionary<string, List<string>> _agentActiveCargos;

        // Eventos
        public event System.Action<Agent, Cargo, float> OnPriceSurge;
        public event System.Action<Agent, string> OnCargoAbandoned;
        public event System.Action<Agent, int> OnAgentDisappeared;
        public event System.Action<Agent, string, int> OnAgentScam;
        public event System.Action<Agent, string> OnAgentLied;
        public event System.Action<Agent, string> OnAgentSabotage;
        public event System.Action<Agent> OnAgentReturned;
        public event System.Action<Agent> OnAgentBankrupt;

        // =========================================================================
        // MÉTODOS PÚBLICOS PARA SAVE/LOAD
        // =========================================================================
        
        public Dictionary<string, List<string>> GetAgentActiveCargos() => new Dictionary<string, List<string>>(_agentActiveCargos);
        
        public void RestoreState(List<Agent> agents, Dictionary<string, List<string>> agentActiveCargos)
        {
            _agents.Clear();
            _agentActiveCargos.Clear();
            
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    _agents[agent.Id] = agent;
                }
            }
            
            if (agentActiveCargos != null)
            {
                foreach (var kvp in agentActiveCargos)
                {
                    _agentActiveCargos[kvp.Key] = new List<string>(kvp.Value);
                }
            }
            
            Debug.Log($"[AgentManager] Estado restaurado. Agentes: {_agents.Count}");
        }

        protected override void OnAwake()
        {
            InitializeAgents();
            _agentActiveCargos = new Dictionary<string, List<string>>();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayPassed += OnDayPassed;
        }

        protected override void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayPassed -= OnDayPassed;
            base.OnDestroy();
        }

        private void InitializeAgents()
        {
            _agents = new Dictionary<string, Agent>();

            var agentsData = new List<Agent>
            {
                new Agent("maersk", "Maersk Logistics", "rotterdam", Constants.AgentPersonality.Reliable,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime }, 1.20f, 0.95f, 0.95f, 5),
                new Agent("cosco", "COSCO Shipping", "shanghai", Constants.AgentPersonality.Ambitious,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime }, 0.90f, 1.10f, 0.70f, 8),
                new Agent("fedex", "FedEx Express", "miami", Constants.AgentPersonality.Efficient,
                    new List<Constants.TransportMode> { Constants.TransportMode.Air }, 1.35f, 1.40f, 0.92f, 6),
                new Agent("emirates", "Emirates SkyCargo", "dubai", Constants.AgentPersonality.Cheap,
                    new List<Constants.TransportMode> { Constants.TransportMode.Air }, 0.80f, 1.00f, 0.65f, 4),
                new Agent("dhl", "DHL Ground", "buenos_aires", Constants.AgentPersonality.Friendly,
                    new List<Constants.TransportMode> { Constants.TransportMode.Land }, 0.95f, 0.90f, 0.85f, 5),
                new Agent("transporte_sur", "Transporte Sur SA", "sao_paulo", Constants.AgentPersonality.Lazy,
                    new List<Constants.TransportMode> { Constants.TransportMode.Land }, 0.70f, 0.70f, 0.60f, 3),
                new Agent("kuehne", "Kuehne+Nagel", "hamburg", Constants.AgentPersonality.Loyal,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime, Constants.TransportMode.Air, Constants.TransportMode.Land }, 1.25f, 1.20f, 0.88f, 7),
                new Agent("agf", "AGF Logistics", "antwerp", Constants.AgentPersonality.Scammer,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime, Constants.TransportMode.Land }, 0.85f, 0.95f, 0.70f, 4),
                new Agent("blue_water", "Blue Water Shipping", "copenhagen", Constants.AgentPersonality.Envious,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime }, 1.05f, 1.00f, 0.80f, 5),
                new Agent("swift", "Swift Logistics", "los_angeles", Constants.AgentPersonality.Elusive,
                    new List<Constants.TransportMode> { Constants.TransportMode.Land, Constants.TransportMode.Air }, 1.10f, 1.15f, 0.75f, 4),
            };

            foreach (var agent in agentsData)
                _agents[agent.Id] = agent;

            Debug.Log($"[AgentManager] Inicializados {_agents.Count} agentes");
        }

        private void OnDayPassed()
        {
            if (!_enableAgentDecisions) return;
            ProcessAgentDecisions();
        }

        private void ProcessAgentDecisions()
        {
            foreach (var agent in _agents.Values)
            {
                agent.UpdateState();
                agent.UpdatePriceSurge();
                agent.DaysSinceLastUse++;

                if (agent.CurrentState == Constants.AgentState.Disappeared && agent.DaysUntilReturn <= 0)
                {
                    agent.CurrentState = Constants.AgentState.Idle;
                    OnAgentReturned?.Invoke(agent);
                }

                if (agent.CurrentState == Constants.AgentState.Bankrupt)
                {
                    OnAgentBankrupt?.Invoke(agent);
                    continue;
                }

                if (agent.CurrentState == Constants.AgentState.Idle)
                {
                    TryPriceSurge(agent);
                    TryDisappear(agent);
                }
            }
        }

        private void TryPriceSurge(Agent agent)
        {
            if (agent.Personality != Constants.AgentPersonality.Ambitious) return;
            if (agent.IsPriceSurgeActive) return;
            if (Random.value < 0.10f)
            {
                agent.TriggerPriceSurge();
                OnPriceSurge?.Invoke(agent, null, agent.CurrentPriceMultiplier);
            }
        }

        private void TryDisappear(Agent agent)
        {
            var (willDisappear, days) = agent.DecideToDisappear();
            if (willDisappear)
            {
                agent.CurrentState = Constants.AgentState.Disappeared;
                agent.DaysUntilReturn = days;
                OnAgentDisappeared?.Invoke(agent, days);
            }
        }

        // Gestión de cargas
        public void AssignCargoToAgent(string agentId, string cargoId)
        {
            if (!_agentActiveCargos.ContainsKey(agentId))
                _agentActiveCargos[agentId] = new List<string>();

            if (!_agentActiveCargos[agentId].Contains(cargoId))
            {
                _agentActiveCargos[agentId].Add(cargoId);
                if (_agents.TryGetValue(agentId, out var agent))
                {
                    agent.CurrentLoad++;
                    agent.DaysSinceLastUse = 0;
                }
            }
        }

        public void RemoveCargoFromAgent(string agentId, string cargoId)
        {
            if (_agentActiveCargos.TryGetValue(agentId, out var cargos))
            {
                if (cargos.Remove(cargoId) && _agents.TryGetValue(agentId, out var agent))
                {
                    agent.CurrentLoad = Mathf.Max(0, agent.CurrentLoad - 1);
                }
            }
        }

        // Verificaciones de comportamiento
        public bool CheckCargoAbandonment(Agent agent, Cargo cargo)
        {
            if (agent?.DecideToAbandonCargo() != true) return false;

            cargo.RecordAgentIntervention("Abandoned");
            cargo.WasAbandonedByAgent = true;
            OnCargoAbandoned?.Invoke(agent, cargo.Id);
            return true;
        }

        public (bool willScam, int extraCost) CheckScam(Agent agent, Cargo cargo, int baseCost)
        {
            var result = agent?.DecideToScam(baseCost) ?? (false, 0);
            if (result.willScam)
            {
                cargo.RecordAgentIntervention("Scam", result.extraCost);
                OnAgentScam?.Invoke(agent, cargo.Id, result.extraCost);
            }
            return result;
        }

        public bool CheckLie(Agent agent, Cargo cargo)
        {
            if (agent?.DecideToLie() != true) return false;

            cargo.RecordAgentIntervention("Lie");
            OnAgentLied?.Invoke(agent, cargo.Id);
            return true;
        }

        public bool CheckSabotage(Agent agent, Cargo cargo, int playerLevel)
        {
            if (agent?.DecideToSabotage(playerLevel) != true) return false;

            cargo.RecordAgentIntervention("Sabotage");
            OnAgentSabotage?.Invoke(agent, cargo.Id);
            return true;
        }

        // Relaciones
        public void RecordAgentChange(string oldAgentId)
        {
            if (!string.IsNullOrEmpty(oldAgentId) && _agents.TryGetValue(oldAgentId, out var agent))
                agent.OnPlayerChangedAgent();
        }

        public void RecordDelivery(string agentId, string cargoId, bool wasSuccessful, bool wasAbandoned = false)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                agent.RecordDelivery(wasSuccessful, wasAbandoned);
                RemoveCargoFromAgent(agentId, cargoId);
            }
        }

        // Consultas
        public Agent GetAgent(string id) => _agents.TryGetValue(id, out var a) ? a : null;
        public List<Agent> GetAllAgents() => _agents.Values.ToList();
        public List<Agent> GetAvailableAgents() => _agents.Values.Where(a => a.IsAvailable).ToList();
        public List<Agent> GetAvailableAgents(Constants.TransportMode mode) =>
            _agents.Values.Where(a => a.IsAvailable && a.OffersTransportMode(mode)).ToList();
        public List<Agent> GetAvailableAgents(Constants.TransportMode mode, Constants.CargoType cargoType) =>
            _agents.Values.Where(a => a.IsAvailable && a.OffersTransportMode(mode) && a.CanHandleCargoType(cargoType)).ToList();
        public float GetEventRiskModifier(string agentId) =>
            _agents.TryGetValue(agentId, out var a) ? a.GetEventRiskModifier() : 1.0f;
    }
}