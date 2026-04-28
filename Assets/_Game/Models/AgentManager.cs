using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FreightForwarder.Models;

namespace FreightForwarder.Managers
{
    /// <summary>
    /// AgentManager — Gestiona todos los agentes y sus decisiones activas.
    /// 
    /// Este Manager es el CEREBRO de los agentes. Decide cuándo:
    /// - Subir precios
    /// - Abandonar cargas
    /// - Desaparecer
    /// - Estafar
    /// - Mentir
    /// - Sabotear
    /// 
    /// DEPENDENCIAS:
    /// - TimeManager (para saber cuándo pasa un día)
    /// </summary>
    public class AgentManager : MonoBehaviour
    {
        public static AgentManager Instance { get; private set; }
        
        [Header("Configuración")]
        [SerializeField] private bool _enableAgentDecisions = true;
        
        private Dictionary<string, Agent> _agents;
        private Dictionary<string, List<string>> _agentActiveCargos; // AgentId -> Lista de CargoIds
        
        // Eventos que otros sistemas pueden escuchar
        public System.Action<Agent, string, float> OnPriceSurge;        // (agente, cargaId, nuevoMultiplicador)
        public System.Action<Agent, string> OnCargoAbandoned;           // (agente, cargaId)
        public System.Action<Agent, int> OnAgentDisappeared;            // (agente, dias)
        public System.Action<Agent, string, int> OnAgentScam;           // (agente, cargaId, extraCosto)
        public System.Action<Agent, string> OnAgentLied;                // (agente, cargaId)
        public System.Action<Agent, string> OnAgentSabotage;            // (agente, cargaId)
        public System.Action<Agent> OnAgentReturned;                    // (agente) cuando vuelve de desaparecer
        public System.Action<Agent> OnAgentBankrupt;                    // (agente) cuando quiebra
        
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
            }
        }
        
        private void Start()
        {
            InitializeAgents();
            _agentActiveCargos = new Dictionary<string, List<string>>();
            
            // Suscribirse al evento de cambio de día de TimeManager
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += OnDayPassed;
            }
            else
            {
                Debug.LogWarning("[AgentManager] TimeManager no encontrado. Las decisiones de agentes no se procesarán automáticamente.");
            }
        }
        
        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= OnDayPassed;
            }
        }
        
        /// <summary>
        /// Se llama cada vez que pasa un día en el juego.
        /// </summary>
        private void OnDayPassed()
        {
            if (!_enableAgentDecisions)
                return;
            
            ProcessAgentDecisions();
        }
        
        private void InitializeAgents()
        {
            _agents = new Dictionary<string, Agent>();
            
            // Crear agentes con personalidades variadas
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
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime, Constants.TransportMode.Air, Constants.TransportMode.Land }, 
                    1.25f, 1.20f, 0.88f, 7),
                    
                new Agent("agf", "AGF Logistics", "antwerp", Constants.AgentPersonality.Scammer,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime, Constants.TransportMode.Land }, 
                    0.85f, 0.95f, 0.70f, 4),
                    
                new Agent("blue_water", "Blue Water Shipping", "copenhagen", Constants.AgentPersonality.Envious,
                    new List<Constants.TransportMode> { Constants.TransportMode.Maritime }, 1.05f, 1.00f, 0.80f, 5),
                    
                new Agent("swift", "Swift Logistics", "los_angeles", Constants.AgentPersonality.Elusive,
                    new List<Constants.TransportMode> { Constants.TransportMode.Land, Constants.TransportMode.Air }, 
                    1.10f, 1.15f, 0.75f, 4),
            };
            
            foreach (var agent in agentsData)
            {
                _agents[agent.Id] = agent;
            }
            
            Debug.Log($"[AgentManager] Inicializados {_agents.Count} agentes");
        }
        
        /// <summary>
        /// Procesa decisiones activas de todos los agentes.
        /// </summary>
        private void ProcessAgentDecisions()
        {
            foreach (var agent in _agents.Values)
            {
                // Actualizar estado
                agent.UpdateState();
                agent.UpdatePriceSurge();
                
                // Incrementar días sin usar
                agent.DaysSinceLastUse++;
                
                // Verificar si el agente debería volver (si estaba desaparecido)
                if (agent.CurrentState == Constants.AgentState.Disappeared && agent.DaysUntilReturn <= 0)
                {
                    agent.CurrentState = Constants.AgentState.Idle;
                    OnAgentReturned?.Invoke(agent);
                    Debug.Log($"[AgentManager] {agent.Name} ha vuelto después de desaparecer.");
                }
                
                // Verificar quiebra
                if (agent.CurrentState == Constants.AgentState.Bankrupt)
                {
                    OnAgentBankrupt?.Invoke(agent);
                    continue;
                }
                
                // Decisiones según personalidad (solo si está idle)
                if (agent.CurrentState == Constants.AgentState.Idle)
                {
                    TryPriceSurge(agent);
                    TryDisappear(agent);
                }
            }
        }
        
        /// <summary>
        /// Intenta que el agente suba precios (personalidad Ambicioso)
        /// </summary>
        private void TryPriceSurge(Agent agent)
        {
            if (agent.Personality != Constants.AgentPersonality.Ambitious)
                return;
            
            if (agent.IsPriceSurgeActive)
                return;
            
            // 10% de chance por día
            if (Random.value < 0.10f)
            {
                agent.TriggerPriceSurge();
                OnPriceSurge?.Invoke(agent, null, agent.CurrentPriceMultiplier);
                Debug.Log($"[AgentManager] {agent.Name} subió sus precios! Nuevo multiplicador: {agent.CurrentPriceMultiplier}");
            }
        }
        
        /// <summary>
        /// Intenta que el agente desaparezca (personalidad Esquivo o Fugaz)
        /// </summary>
        private void TryDisappear(Agent agent)
        {
            var (willDisappear, days) = agent.DecideToDisappear();
            if (willDisappear)
            {
                agent.CurrentState = Constants.AgentState.Disappeared;
                agent.DaysUntilReturn = days;
                OnAgentDisappeared?.Invoke(agent, days);
                Debug.Log($"[AgentManager] {agent.Name} desapareció por {days} días!");
            }
        }
        
        // =========================================================================
        // GESTIÓN DE CARGAS ACTIVAS POR AGENTE
        // =========================================================================
        
        /// <summary>
        /// Asigna una carga a un agente.
        /// </summary>
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
                    Debug.Log($"[AgentManager] Carga {cargoId} asignada a {agent.Name}. Carga actual: {agent.CurrentLoad}/{agent.MaxCapacity}");
                }
            }
        }
        
        /// <summary>
        /// Remueve una carga de un agente (completada, fallida o abandonada).
        /// </summary>
        public void RemoveCargoFromAgent(string agentId, string cargoId)
        {
            if (_agentActiveCargos.TryGetValue(agentId, out var cargos))
            {
                if (cargos.Remove(cargoId))
                {
                    if (_agents.TryGetValue(agentId, out var agent))
                    {
                        agent.CurrentLoad = Mathf.Max(0, agent.CurrentLoad - 1);
                        Debug.Log($"[AgentManager] Carga {cargoId} removida de {agent.Name}. Carga actual: {agent.CurrentLoad}/{agent.MaxCapacity}");
                    }
                }
            }
        }
        
        // =========================================================================
        // VERIFICACIONES DE COMPORTAMIENTOS ACTIVOS
        // =========================================================================
        
        /// <summary>
        /// Verifica si un agente va a abandonar una carga específica.
        /// </summary>
        public bool CheckCargoAbandonment(Agent agent, Cargo cargo)
        {
            if (agent == null || cargo == null)
                return false;
            
            if (agent.DecideToAbandonCargo())
            {
                // Registrar el abandono
                cargo.RecordAgentIntervention("Abandoned");
                cargo.WasAbandonedByAgent = true;
                
                OnCargoAbandoned?.Invoke(agent, cargo.Id);
                Debug.Log($"[AgentManager] {agent.Name} abandonó la carga {cargo.Id}!");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Verifica si un agente va a estafar en una carga.
        /// </summary>
        public (bool willScam, int extraCost) CheckScam(Agent agent, Cargo cargo, int baseCost)
        {
            if (agent == null)
                return (false, 0);
            
            var (willScam, extraCost) = agent.DecideToScam(baseCost);
            if (willScam)
            {
                cargo.RecordAgentIntervention("Scam", extraCost);
                OnAgentScam?.Invoke(agent, cargo.Id, extraCost);
                Debug.Log($"[AgentManager] {agent.Name} intentó estafar con un extra de ${extraCost}!");
            }
            
            return (willScam, extraCost);
        }
        
        /// <summary>
        /// Verifica si un agente va a mentir sobre la entrega.
        /// </summary>
        public bool CheckLie(Agent agent, Cargo cargo)
        {
            if (agent == null)
                return false;
            
            if (agent.DecideToLie())
            {
                cargo.RecordAgentIntervention("Lie");
                OnAgentLied?.Invoke(agent, cargo.Id);
                Debug.Log($"[AgentManager] {agent.Name} mintió sobre la entrega de {cargo.Id}!");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Verifica si un agente va a sabotear al jugador.
        /// </summary>
        public bool CheckSabotage(Agent agent, Cargo cargo, int playerLevel)
        {
            if (agent == null)
                return false;
            
            if (agent.DecideToSabotage(playerLevel))
            {
                cargo.RecordAgentIntervention("Sabotage");
                OnAgentSabotage?.Invoke(agent, cargo.Id);
                Debug.Log($"[AgentManager] {agent.Name} saboteó la carga {cargo.Id} por envidia!");
                return true;
            }
            
            return false;
        }
        
        // =========================================================================
        // MÉTODOS DE RELACIÓN
        // =========================================================================
        
        /// <summary>
        /// Registra que el jugador cambió de agente.
        /// </summary>
        public void RecordAgentChange(string oldAgentId)
        {
            if (string.IsNullOrEmpty(oldAgentId))
                return;
            
            if (_agents.TryGetValue(oldAgentId, out var agent))
            {
                agent.OnPlayerChangedAgent();
                Debug.Log($"[AgentManager] {agent.Name} se sintió traicionado por cambiar de agente");
            }
        }
        
        /// <summary>
        /// Registra una entrega completada (éxito o fallo).
        /// </summary>
        public void RecordDelivery(string agentId, string cargoId, bool wasSuccessful, bool wasAbandoned = false)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                agent.RecordDelivery(wasSuccessful, wasAbandoned);
                RemoveCargoFromAgent(agentId, cargoId);
            }
        }
        
        // =========================================================================
        // MÉTODOS DE CONSULTA
        // =========================================================================
        
        /// <summary>
        /// Obtiene un agente por ID.
        /// </summary>
        public Agent GetAgent(string id)
        {
            _agents.TryGetValue(id, out var agent);
            return agent;
        }
        
        /// <summary>
        /// Obtiene todos los agentes.
        /// </summary>
        public List<Agent> GetAllAgents()
        {
            return _agents.Values.ToList();
        }
        
        /// <summary>
        /// Obtiene agentes disponibles (no desaparecidos, no quebrados, con capacidad).
        /// </summary>
        public List<Agent> GetAvailableAgents()
        {
            return _agents.Values
                .Where(a => a.IsAvailable)
                .ToList();
        }
        
        /// <summary>
        /// Obtiene agentes disponibles para un modo de transporte.
        /// </summary>
        public List<Agent> GetAvailableAgents(Constants.TransportMode mode)
        {
            return _agents.Values
                .Where(a => a.IsAvailable && a.OffersTransportMode(mode))
                .ToList();
        }
        
        /// <summary>
        /// Obtiene agentes disponibles para un modo de transporte y tipo de carga.
        /// </summary>
        public List<Agent> GetAvailableAgents(Constants.TransportMode mode, Constants.CargoType cargoType)
        {
            return _agents.Values
                .Where(a => a.IsAvailable && 
                           a.OffersTransportMode(mode) && 
                           a.CanHandleCargoType(cargoType))
                .ToList();
        }
        
        /// <summary>
        /// Obtiene el factor de riesgo de evento para un agente.
        /// </summary>
        public float GetEventRiskModifier(string agentId)
        {
            if (_agents.TryGetValue(agentId, out var agent))
                return agent.GetEventRiskModifier();
            return 1.0f;
        }
    }
}