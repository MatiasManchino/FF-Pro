using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class AgentManager : Singleton<AgentManager>
    {
        private Dictionary<string, Agent> _agents;

        public event Action<Agent, string, float> OnPriceSurge;
        public event Action<Agent, string> OnCargoAbandoned;
        public event Action<Agent, int> OnAgentDisappeared;
        public event Action<Agent, string, int> OnAgentScam;
        public event Action<Agent, string> OnAgentLied;
        public event Action<Agent, string> OnAgentSabotage;
        public event Action<Agent> OnAgentReturned;
        public event Action<Agent> OnAgentBankrupt;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            _agents = new Dictionary<string, Agent>();
            InitializeAgents();
        }

// Inicializa ialize agentes.
        private void InitializeAgents()
        {
            CreateAgent("maersk", "Maersk Logistics", "rotterdam",
                Constants.AgentPersonality.Reliable, 1.20f, 0.95f, 0.95f, 5,
                new[] { Constants.TransportMode.Maritime });

            CreateAgent("cosco", "COSCO Shipping", "shanghai",
                Constants.AgentPersonality.Ambitious, 0.90f, 1.10f, 0.70f, 8,
                new[] { Constants.TransportMode.Maritime });

            CreateAgent("fedex", "FedEx Express", "miami",
                Constants.AgentPersonality.Efficient, 1.35f, 1.40f, 0.92f, 6,
                new[] { Constants.TransportMode.Air });

            CreateAgent("emirates", "Emirates SkyCargo", "dubai",
                Constants.AgentPersonality.Cheap, 0.80f, 1.00f, 0.65f, 4,
                new[] { Constants.TransportMode.Air });

            CreateAgent("dhl", "DHL Ground", "buenos_aires",
                Constants.AgentPersonality.Friendly, 0.95f, 0.90f, 0.85f, 5,
                new[] { Constants.TransportMode.Land });

            CreateAgent("transporte_sur", "Transporte Sur SA", "sao_paulo",
                Constants.AgentPersonality.Lazy, 0.70f, 0.70f, 0.60f, 3,
                new[] { Constants.TransportMode.Land });

            CreateAgent("kuehne", "Kuehne+Nagel", "hamburg",
                Constants.AgentPersonality.Loyal, 1.25f, 1.20f, 0.88f, 7,
                new[] { Constants.TransportMode.Maritime, Constants.TransportMode.Air, Constants.TransportMode.Land });

            CreateAgent("agf", "AGF Logistics", "antwerp",
                Constants.AgentPersonality.Scammer, 0.85f, 0.95f, 0.70f, 4,
                new[] { Constants.TransportMode.Maritime, Constants.TransportMode.Land });

            CreateAgent("blue_water", "Blue Water Shipping", "copenhagen",
                Constants.AgentPersonality.Envious, 1.05f, 1.00f, 0.80f, 5,
                new[] { Constants.TransportMode.Maritime });

            CreateAgent("swift", "Swift Logistics", "los_angeles",
                Constants.AgentPersonality.Elusive, 1.10f, 1.15f, 0.75f, 4,
                new[] { Constants.TransportMode.Land, Constants.TransportMode.Air });
        }

        private void CreateAgent(string id, string name, string homeCity,
                                  Constants.AgentPersonality personality,
                                  float price, float speed, float reliability, int capacity,
                                  Constants.TransportMode[] modes)
        {
            var agent = new Agent(id, name, homeCity, personality, price, speed, reliability, capacity);
// Foreach
            foreach (var mode in modes)
                agent.SupportedModes.Add(mode);
            _agents[id] = agent;
        }

        // ═══════════════════════════════════
        // PROCESAMIENTO DIARIO
        // Se ejecuta al iniciar el componente.

        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += ProcessAgentDecisions;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= ProcessAgentDecisions;
        }

// Gestiona process agente decisions.
        private void ProcessAgentDecisions()
        {
// Foreach
            foreach (var agent in _agents.Values)
            {
                if (agent.CurrentState == Constants.AgentState.Bankrupt) continue;

                bool wasDisappeared = agent.CurrentState == Constants.AgentState.Disappeared;
                agent.UpdateState();
                agent.UpdatePriceSurge();
                agent.DaysSinceLastUse++;

                // Volvió de desaparición
                if (wasDisappeared && agent.CurrentState != Constants.AgentState.Disappeared)
                    OnAgentReturned?.Invoke(agent);

                // Nueva quiebra
                if (agent.CurrentState == Constants.AgentState.Bankrupt)
                {
                    OnAgentBankrupt?.Invoke(agent);
                    continue;
                }

                // Notificar price surge
                if (agent.IsInPriceSurge)
                    OnPriceSurge?.Invoke(agent, $"{agent.Name} subió sus precios un 25%.", Constants.AGENT_PRICE_SURGE_MULTIPLIER);

                // Elusive: desaparición aleatoria
                if (agent.Personality == Constants.AgentPersonality.Elusive &&
                    agent.CurrentState == Constants.AgentState.Idle &&
                    UnityEngine.Random.value < 0.05f)
                {
                    agent.CurrentState = Constants.AgentState.Disappeared;
                    agent.DaysUntilReturn = UnityEngine.Random.Range(Constants.AGENT_DISAPPEAR_DAYS_MIN, Constants.AGENT_DISAPPEAR_DAYS_MAX + 1);
                    OnAgentDisappeared?.Invoke(agent, agent.DaysUntilReturn);
                }
            }
        }

        // ═══════════════════════════════════
        // GESTIÓN DE CARGAS
        // Gestiona assign cargamento to agente.

        public void AssignCargoToAgent(string agentId, string cargoId)
        {
            if (string.IsNullOrEmpty(agentId)) return;
            if (!_agents.TryGetValue(agentId, out Agent agent)) return;
            if (!agent.CurrentCargoIds.Contains(cargoId))
            {
                agent.CurrentCargoIds.Add(cargoId);
                agent.CurrentLoad++;
            }
        }

// Elimina cargamento from agent
        public void RemoveCargoFromAgent(string agentId, string cargoId)
        {
            if (string.IsNullOrEmpty(agentId)) return;
            if (!_agents.TryGetValue(agentId, out Agent agent)) return;
            agent.CurrentCargoIds.Remove(cargoId);
            agent.CurrentLoad = Math.Max(0, agent.CurrentLoad - 1);
        }

// Registra entrega.
        public void RecordDelivery(string agentId, string cargoId, bool wasSuccessful, bool wasAbandoned)
        {
            if (string.IsNullOrEmpty(agentId)) return;
            if (!_agents.TryGetValue(agentId, out Agent agent)) return;
            agent.RecordDelivery(wasSuccessful, wasAbandoned);
            RemoveCargoFromAgent(agentId, cargoId);
        }

// Registra agente change.
        public void RecordAgentChange(string oldAgentId)
        {
            if (string.IsNullOrEmpty(oldAgentId)) return;
            if (!_agents.TryGetValue(oldAgentId, out Agent agent)) return;
            agent.AgentTrust = Math.Max(0, agent.AgentTrust - Constants.AGENT_TRUST_LOSS_PER_ABANDON);
            agent.UpdateState();
        }

        // ═══════════════════════════════════
        // VERIFICACIONES DE COMPORTAMIENTO
        // Verifica cargamento abandonment.

        public bool CheckCargoAbandonment(Agent agent, Cargo cargo)
        {
            if (agent.Personality == Constants.AgentPersonality.Lazy && agent.CurrentLoad > 2)
            {
                if (UnityEngine.Random.value < 0.15f)
                {
                    OnCargoAbandoned?.Invoke(agent, cargo.Id);
                    return true;
                }
            }
            return false;
        }

// Public
        public (bool willScam, int extraCost) CheckScam(Agent agent, Cargo cargo, int baseCost)
        {
            if (agent.Personality != Constants.AgentPersonality.Scammer) return (false, 0);
            float chance = 0.25f + (agent.PlayerTrust > 70 ? 0.10f : 0f);
            if (UnityEngine.Random.value < chance)
            {
                int extra = UnityEngine.Random.Range(100, 501);
                OnAgentScam?.Invoke(agent, cargo.Id, extra);
                return (true, extra);
            }
            return (false, 0);
        }

// Verifica lie.
        public bool CheckLie(Agent agent, Cargo cargo)
        {
            if (agent.Personality != Constants.AgentPersonality.Liar) return false;
            float chance = 0.15f + (agent.PlayerTrust > 70 ? 0.10f : 0f);
            if (UnityEngine.Random.value < chance)
            {
                OnAgentLied?.Invoke(agent, cargo.Id);
                return true;
            }
            return false;
        }

// Verifica sabotage.
        public bool CheckSabotage(Agent agent, string targetAgentId, int playerLevel)
        {
            if (agent.Personality != Constants.AgentPersonality.Envious) return false;
            if (playerLevel < 3) return false;

            float chance = 0f;
            if (playerLevel >= 5 && agent.Relationship <= Constants.AgentRelationship.Neutral)
                chance = 0.20f;
            // Realiza if
            else if (playerLevel >= 3 && agent.TotalDeliveries == 0)
                chance = 0.15f;

            if (chance > 0 && UnityEngine.Random.value < chance)
            {
                OnAgentSabotage?.Invoke(agent, targetAgentId);
                return true;
            }
            return false;
        }

        // ═══════════════════════════════════
        // CONSULTAS
        // Obtiene agent

        public Agent GetAgent(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _agents.TryGetValue(id, out Agent agent);
            return agent;
        }

// Obtiene all agents
        public Dictionary<string, Agent> GetAllAgents() => _agents;

// Obtiene available agents
        public List<Agent> GetAvailableAgents(Constants.TransportMode mode)
        {
            var result = new List<Agent>();
// Foreach
            foreach (var agent in _agents.Values)
            {
                if (agent.IsAvailable() && agent.OffersTransportMode(mode))
                    result.Add(agent);
            }
            return result;
        }

        // ═══════════════════════════════════
        // SAVE / RESTORE
        // Obtiene save datos

        public List<AgentSaveData> GetSaveData()
        {
            var list = new List<AgentSaveData>();
// Foreach
            foreach (var agent in _agents.Values)
                list.Add(new AgentSaveData(agent));
            return list;
        }

// Gestiona restore from guarda.
        public void RestoreFromSave(List<AgentSaveData> saveData)
        {
// Foreach
            foreach (var data in saveData)
            {
                if (_agents.TryGetValue(data.AgentId, out Agent agent))
                    data.ApplyTo(agent);
            }
        }
    }
}