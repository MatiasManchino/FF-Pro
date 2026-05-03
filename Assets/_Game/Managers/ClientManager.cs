using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using FreightForwarder.Managers;
using Constants = FreightForwarder.Models.Constants;

namespace FreightForwarder.Managers
{
    /// <summary>
    /// ClientManager — Gestiona clientes, relaciones y negociación.
    /// 
    /// RESPONSABILIDADES:
    /// - Mantener pool de clientes
    /// - Gestionar relación con cada cliente
    /// - Calcular probabilidad de aceptación de cotizaciones
    /// - Procesar negociación (contraofertas)
    /// - Actualizar relación según resultados
    /// </summary>
    public class ClientManager : Singleton<ClientManager>
    {
        // =========================================================================
        // DATOS DE CLIENTES
        // =========================================================================
        
        /// <summary>
        /// Diccionario de clientes por ID
        /// </summary>
        public Dictionary<string, Client> Clients { get; private set; }
        
        /// <summary>
        /// Diccionario de relación con clientes (valor -100 a 100)
        /// </summary>
        public Dictionary<string, float> RelationshipWithClients { get; private set; }
        
        /// <summary>
        /// Diccionario de ofertas pendientes por cliente
        /// </summary>
        public Dictionary<string, List<Quote>> PendingQuotes { get; private set; }
        
        // =========================================================================
        // NOMBRES DE CLIENTES PREDEFINIDOS
        // =========================================================================
        
        private readonly string[] _goodPayerNames = { "Aceros del Cono Sur", "Farmacéutica Rioplatense", "Agro Export SA" };
        private readonly string[] _badPayerNames = { "Importadora del Pacífico", "Textiles Unidos" };
        private readonly string[] _urgentClientNames = { "Tech Components Inc", "Auto Parts Global" };
        private readonly string[] _creditClientNames = { "Megastore Retail", "Consumer Goods Co" };
        private readonly string[] _veryBadClientNames = { "FastDeal LLC", "QuickShip Ltd" };
        private readonly string[] _contractClientNames = { "Minera Andina", "Petroquímica del Sur" };
        
        // =========================================================================
        // EVENTOS
        // =========================================================================
        
        public event Action<Client, float> OnRelationshipChanged;
        //public event Action<Client, Quote> OnQuoteAccepted;
        //public event Action<Client, Quote> OnQuoteRejected;
        //public event Action<Client, int> OnCounterOfferReceived;
        public event Action<Client> OnClientBlacklisted;

        // =========================================================================
        // MÉTODOS PÚBLICOS PARA SAVE/LOAD
        // =========================================================================
        
        public List<Client> GetAllClients() => new List<Client>(Clients.Values);
        
        public void RestoreState(List<Client> clients, Dictionary<string, float> relationships)
        {
            Clients.Clear();
            RelationshipWithClients.Clear();
            PendingQuotes.Clear();
            
            if (clients != null)
            {
                foreach (var client in clients)
                {
                    Clients[client.Id] = client;
                    RelationshipWithClients[client.Id] = client.RelationshipLevel;
                    PendingQuotes[client.Id] = new List<Quote>();
                }
            }
            
            if (relationships != null)
            {
                foreach (var kvp in relationships)
                {
                    RelationshipWithClients[kvp.Key] = kvp.Value;
                }
            }
            
            Debug.Log($"[ClientManager] Estado restaurado. Clientes: {Clients.Count}");
        }
        
        public void RestorePendingQuotes(List<Quote> pendingQuotes)
        {
            if (pendingQuotes == null) return;
            
            foreach (var quote in pendingQuotes)
            {
                if (!string.IsNullOrEmpty(quote.ClientId) && PendingQuotes.ContainsKey(quote.ClientId))
                {
                    PendingQuotes[quote.ClientId].Add(quote);
                }
            }
        }
        
        public List<Agent> GetAllAgents()
        {
            if (AgentManager.Instance != null)
                return AgentManager.Instance.GetAllAgents();
            return new List<Agent>();
        }
        
        // =========================================================================
        // INICIALIZACIÓN
        // =========================================================================
        
        protected override void OnAwake()
        {
            Clients = new Dictionary<string, Client>();
            RelationshipWithClients = new Dictionary<string, float>();
            PendingQuotes = new Dictionary<string, List<Quote>>();
            
            InitializeClientPool();
            
            Debug.Log($"[ClientManager] Inicializado con {Clients.Count} clientes");
        }
        
        /// <summary>
        /// Inicializa el pool de clientes predefinidos.
        /// </summary>
        private void InitializeClientPool()
        {
            // Good Payers
            foreach (string name in _goodPayerNames)
            {
                CreateClient(name, Constants.ClientType.GoodPayer);
            }
            
            // Bad Payers
            foreach (string name in _badPayerNames)
            {
                CreateClient(name, Constants.ClientType.BadPayer);
            }
            
            // Urgent Clients
            foreach (string name in _urgentClientNames)
            {
                CreateClient(name, Constants.ClientType.UrgentClient);
            }
            
            // Credit Clients
            foreach (string name in _creditClientNames)
            {
                CreateClient(name, Constants.ClientType.CreditClient);
            }
            
            // Very Bad Clients
            foreach (string name in _veryBadClientNames)
            {
                CreateClient(name, Constants.ClientType.VeryBadClient);
            }
            
            // Contract Clients
            foreach (string name in _contractClientNames)
            {
                CreateClient(name, Constants.ClientType.ContractClient);
            }
        }
        
        /// <summary>
        /// Crea un nuevo cliente.
        /// </summary>
        public Client CreateClient(string companyName, Constants.ClientType clientType)
        {
            // Verificar si ya existe
            var existing = Clients.Values.FirstOrDefault(c => c.CompanyName == companyName);
            if (existing != null)
                return existing;
            
            Client client = new Client(companyName, clientType);
            Clients[client.Id] = client;
            RelationshipWithClients[client.Id] = client.RelationshipLevel;
            PendingQuotes[client.Id] = new List<Quote>();
            
            Debug.Log($"[ClientManager] Nuevo cliente: {companyName} ({Constants.GetClientTypeName(clientType)})");
            
            return client;
        }
        
        /// <summary>
        /// Obtiene un cliente por nombre.
        /// </summary>
        public Client GetClientByName(string name)
        {
            return Clients.Values.FirstOrDefault(c => c.CompanyName == name);
        }
        
        /// <summary>
        /// Obtiene un cliente por ID.
        /// </summary>
        public Client GetClientById(string id)
        {
            Clients.TryGetValue(id, out Client client);
            return client;
        }
        
        // =========================================================================
        // CÁLCULO DE PROBABILIDAD DE ACEPTACIÓN
        // =========================================================================
        
        /// <summary>
        /// Calcula la probabilidad de que un cliente acepte una cotización.
        /// </summary>
        public float GetQuoteAcceptanceProbability(Client client, Quote quote, int currentDay)
        {
            if (client == null || quote == null)
                return 0f;
            
            if (client.IsBlacklisted)
                return 0f;
            
            float baseChance = Constants.NEGOTIATION_BASE_ACCEPTANCE; // 0.15
            
            // 1. Ajuste por precio vs valor de referencia
            float referenceValue = quote.AgentCost * 1.5f; // Referencia aproximada
            float priceRatio = (float)quote.OfferedPrice / referenceValue;
            
            if (priceRatio <= 0.7f)
                baseChance += 0.35f;  // Precio muy competitivo
            else if (priceRatio <= 0.9f)
                baseChance += 0.20f;  // Precio competitivo
            else if (priceRatio <= 1.0f)
                baseChance += 0.10f;  // Precio justo
            else if (priceRatio <= 1.2f)
                baseChance -= 0.10f;  // Precio caro
            else
                baseChance -= 0.30f;  // Precio muy caro
            
            // 2. Ajuste por relación con el cliente
            float relationshipBonus = (client.RelationshipLevel - 50f) / 100f;
            baseChance += relationshipBonus * 0.3f;
            
            // 3. Ajuste por reputación del jugador
            if (EconomyManager.Instance != null)
            {
                float reputationBonus = (EconomyManager.Instance.Reputation - 50f) / 100f;
                baseChance += reputationBonus * 0.2f;
            }
            
            // 4. Ajuste por tipo de cliente
            switch (client.ClientType)
            {
                case Constants.ClientType.UrgentClient:
                    baseChance += 0.20f;  // Pagan lo que sea por urgencia
                    break;
                case Constants.ClientType.GoodPayer:
                    baseChance += 0.10f;
                    break;
                case Constants.ClientType.CreditClient:
                    baseChance -= 0.05f;  // Más exigentes
                    break;
                case Constants.ClientType.BadPayer:
                    baseChance -= 0.15f;
                    break;
                case Constants.ClientType.VeryBadClient:
                    baseChance -= 0.25f;
                    break;
                case Constants.ClientType.ContractClient:
                    baseChance += 0.05f;  // Algo de confianza por contrato
                    break;
            }
            
            // 5. Ajuste por margen excesivo
            if (quote.HasExcessiveMargin())
                baseChance -= 0.15f;
            
            // 6. Ajuste por nivel de enojo del cliente
            baseChance -= client.AngerLevel * 0.05f;
            
            // 7. Cliente VIP tiene más tolerancia
            if (client.IsVip)
                baseChance += 0.10f;
            
            return Mathf.Clamp(baseChance, 0f, 0.95f);
        }
        
        // =========================================================================
        // PROCESAMIENTO DE NEGOCIACIÓN
        // =========================================================================
        
        /// <summary>
        /// Procesa la negociación con el cliente.
        /// </summary>
        public NegotiationResult ProcessNegotiation(Client client, Cargo cargo, Quote quote, int currentDay, int attemptNumber)
        {
            if (client == null)
                return NegotiationResult.Rejection("Cliente no encontrado", 0f);
            
            if (client.IsBlacklisted)
                return NegotiationResult.Rejection("Este cliente te ha bloqueado. No acepta más cotizaciones.", 0f);
            
            float acceptanceChance = GetQuoteAcceptanceProbability(client, quote, currentDay);
            
            // Verificar si es la última oportunidad (intento 3)
            bool isLastAttempt = attemptNumber >= Constants.MAX_QUOTES_PER_CARGO;
            
            // El cliente puede hacer contraoferta (70% de las veces si no acepta)
            float roll = UnityEngine.Random.value;
            
            if (roll < acceptanceChance)
            {
                // Acepta la cotización
                string message = GetAcceptanceMessage(client, quote);
                UpdateRelationshipAfterAcceptance(client, quote);
                return NegotiationResult.Acceptance(message, acceptanceChance);
            }
            else if (!isLastAttempt && UnityEngine.Random.value < 0.7f)
            {
                // Hace contraoferta
                int counterOffer = CalculateCounterOffer(client, cargo, quote);
                string message = GetCounterOfferMessage(client, quote, counterOffer);
                return NegotiationResult.CounterOffer(counterOffer, message, acceptanceChance, attemptNumber + 1);
            }
            else
            {
                // Rechaza definitivamente
                string message = GetRejectionMessage(client, quote);
                UpdateRelationshipAfterRejection(client, quote);
                return NegotiationResult.Rejection(message, acceptanceChance);
            }
        }
        
        /// <summary>
        /// Procesa la respuesta del jugador a una contraoferta.
        /// </summary>
        public NegotiationResult ProcessCounterOfferResponse(Client client, Cargo cargo, Quote quote, bool accepted, int currentDay)
        {
            if (accepted)
            {
                string message = $"✅ ¡Aceptaste mi contraoferta de ${quote.CounterOfferPrice}! Trato cerrado.";
                UpdateRelationshipAfterAcceptance(client, quote);
                return NegotiationResult.Acceptance(message, 1f);
            }
            else
            {
                string message = GetRejectionAfterCounterMessage(client);
                UpdateRelationshipAfterRejection(client, quote);
                return NegotiationResult.Rejection(message, 0f);
            }
        }
        
        /// <summary>
        /// Calcula una contraoferta razonable del cliente.
        /// </summary>
        private int CalculateCounterOffer(Client client, Cargo cargo, Quote quote)
        {
            float referenceValue = quote.AgentCost * 1.5f;
            
            // El cliente pide un precio entre 5% menos y 15% más que la referencia
            float multiplier;
            
            switch (client.ClientType)
            {
                case Constants.ClientType.UrgentClient:
                    multiplier = UnityEngine.Random.Range(1.0f, 1.25f);  // Acepta precios más altos
                    break;
                case Constants.ClientType.VeryBadClient:
                    multiplier = UnityEngine.Random.Range(0.7f, 0.9f);   // Quiere precios bajos
                    break;
                case Constants.ClientType.ContractClient:
                    multiplier = UnityEngine.Random.Range(0.85f, 1.05f); // Precios justos
                    break;
                default:
                    multiplier = UnityEngine.Random.Range(0.9f, 1.1f);
                    break;
            }
            
            // Ajuste por relación
            multiplier += (client.RelationshipLevel - 50f) / 200f;
            
            int counterOffer = Mathf.RoundToInt(referenceValue * multiplier);
            counterOffer = Mathf.Max(counterOffer, quote.AgentCost + 50); // Mínimo viable
            
            return counterOffer;
        }
        
        // =========================================================================
        // MENSAJES DE CLIENTE
        // =========================================================================
        
        private string GetAcceptanceMessage(Client client, Quote quote)
        {
            float margin = quote.Margin;
            
            if (margin > 0.3f)
                return $"💰 Acepto tu cotización de ${quote.OfferedPrice}, pero no abuses con los márgenes.";
            
            if (margin > 0.2f)
                return $"🤝 Trato hecho. ${quote.OfferedPrice} me parece justo.";
            
            if (margin > 0.1f)
                return $"✅ Acepto. Buen precio para ambas partes.";
            
            return $"👍 Acepto. Gracias por la buena oferta.";
        }
        
        private string GetCounterOfferMessage(Client client, Quote quote, int counterOffer)
        {
            float margin = (float)(counterOffer - quote.AgentCost) / counterOffer;
            
            if (margin < 0.05f)
                return $"❌ Tu precio es demasiado alto. Te ofrezco ${counterOffer}, es lo máximo que puedo pagar.";
            
            if (margin < 0.1f)
                return $"🔄 Tu cotización es alta. Podemos cerrar en ${counterOffer}?";
            
            return $"📝 Bajemos a ${counterOffer} y tenemos trato.";
        }
        
        private string GetRejectionMessage(Client client, Quote quote)
        {
            float margin = quote.Margin;
            
            if (margin > 0.4f)
                return $"😤 ¡${quote.OfferedPrice} es un robo! Buscaré otro freight forwarder.";
            
            if (margin > 0.3f)
                return $"😐 Lo siento, tu precio es demasiado alto. No podemos trabajar así.";
            
            return $"❌ No me convence tu oferta. Rechazada.";
        }
        
        private string GetRejectionAfterCounterMessage(Client client)
        {
            int anger = client.AngerLevel;
            
            if (anger >= 4)
                return $"💢 ¡Basta! No quiero seguir negociando. Me voy a otro lado.";
            
            if (anger >= 2)
                return $"😤 No hay acuerdo. Buscaré otras opciones.";
            
            return $"❌ Lástima. No pudimos llegar a un acuerdo.";
        }
        
        // =========================================================================
        // ACTUALIZACIÓN DE RELACIONES
        // =========================================================================
        
        private void UpdateRelationshipAfterAcceptance(Client client, Quote quote)
        {
            float gain = 5f; // Base
            
            // Si el precio fue bueno para el cliente, gana más relación
            if (quote.Margin < 0.15f)
                gain += 5f;
            
            // Si es cliente VIP, más sensible a buenos precios
            if (client.IsVip)
                gain *= 1.5f;
            
            client.RelationshipLevel = Mathf.Min(100, client.RelationshipLevel + gain);
            RelationshipWithClients[client.Id] = client.RelationshipLevel;
            
            // Calmar enojo
            if (client.AngerLevel > 0)
                client.AngerLevel--;
            
            OnRelationshipChanged?.Invoke(client, gain);
            
            Debug.Log($"[ClientManager] Relación con {client.CompanyName} +{gain} → {client.RelationshipLevel}");
        }
        
        private void UpdateRelationshipAfterRejection(Client client, Quote quote)
        {
            float loss = 3f; // Base
            
            // Si el precio fue abusivo, pierde más relación
            if (quote.Margin > 0.35f)
                loss += 5f;
            
            // Si es cliente VIP, más sensible a malos precios
            if (client.IsVip)
                loss *= 1.5f;
            
            client.RelationshipLevel = Mathf.Max(0, client.RelationshipLevel - loss);
            RelationshipWithClients[client.Id] = client.RelationshipLevel;
            
            // Aumentar enojo
            if (quote.Margin > 0.4f)
                client.AngerLevel = Mathf.Min(5, client.AngerLevel + 2);
            else
                client.AngerLevel = Mathf.Min(5, client.AngerLevel + 1);
            
            OnRelationshipChanged?.Invoke(client, -loss);
            
            // Verificar bloqueo
            if (client.AngerLevel >= 5 && !client.IsBlacklisted)
            {
                client.IsBlacklisted = true;
                client.IsActive = false;
                OnClientBlacklisted?.Invoke(client);
                Debug.LogWarning($"[ClientManager] {client.CompanyName} te ha BLOQUEADO por malas prácticas.");
            }
            
            Debug.Log($"[ClientManager] Relación con {client.CompanyName} -{loss} → {client.RelationshipLevel}");
        }
        
        // =========================================================================
        // ACTUALIZACIÓN DIARIA
        // =========================================================================
        
        /// <summary>
        /// Se llama cada día para actualizar estados de clientes.
        /// </summary>
        public void OnDailyUpdate()
        {
            foreach (var client in Clients.Values)
            {
                // Disminuir enojo gradualmente
                client.DecayAnger();
                
                // Actualizar contrato
                client.UpdateContract(TimeManager.Instance.CurrentDay);
                
                // Verificar si cliente quiere ser VIP
                if (client.DecideToBecomeVip() && !client.IsVip)
                {
                    client.IsVip = true;
                    Debug.Log($"🎉 {client.CompanyName} se ha convertido en CLIENTE VIP! Mejores condiciones.");
                }
                
                // Verificar si cliente quiere recomendar
                if (client.DecideToRecommend())
                {
                    client.RecordRecommendation();
                    Debug.Log($"⭐ {client.CompanyName} te ha recomendado a otros clientes! +5 reputación.");
                    
                    if (EconomyManager.Instance != null)
                    {
                        EconomyManager.Instance.AddReputation(5);
                    }
                }
            }
        }
        
        // =========================================================================
        // MÉTODOS DE CONSULTA
        // =========================================================================
        
        public float GetRelationshipWithClient(string clientId)
        {
            return RelationshipWithClients.GetValueOrDefault(clientId, 50f);
        }
        
        public bool IsClientBlacklisted(string clientId)
        {
            if (Clients.TryGetValue(clientId, out Client client))
                return client.IsBlacklisted;
            return false;
        }
        
        public List<Client> GetActiveClients()
        {
            return Clients.Values.Where(c => c.IsActive && !c.IsBlacklisted).ToList();
        }
        
        public List<Client> GetBlacklistedClients()
        {
            return Clients.Values.Where(c => c.IsBlacklisted).ToList();
        }
    }
}