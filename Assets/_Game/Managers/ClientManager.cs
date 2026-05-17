using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class ClientManager : Singleton<ClientManager>
    {
        private List<Client> _activeClients;
        private List<string> _companyNamePool;

        public IReadOnlyList<Client> ActiveClients => _activeClients;

        public event Action<Client> OnClientAdded;
        public event Action<Client> OnClientBlacklisted;
        public event Action<Client> OnClientBecameVip;
        public event Action<Client, Quote> OnNegotiationResult;

        protected override void OnAwake()
        {
            _activeClients = new List<Client>();
            InitializeCompanyNames();
        }

        private void InitializeCompanyNames()
        {
            _companyNamePool = new List<string>
            {
                "Aceros del Cono Sur", "Global Parts SA", "Farmacéutica Riviera",
                "TechShip International", "Alimentos Patagonia", "Química del Sur",
                "Electrónica Meridional", "Construcciones Andes", "Textil Pacífico",
                "Distribuidora Norte", "Importaciones Rápidas", "Logística Premium",
                "Comercio Exterior Plus", "Envíos Express SA", "Cargas Seguras Ltda",
                "Marítima del Plata", "Aérea Cargo SRL", "Transporte Unido",
                "Importex Corp", "ExportPro International", "Grupo Logístico Sur",
                "Comercial Atlántico", "Pacific Trade Co", "Euro Cargo GmbH",
                "Asia Connect Ltd", "Río de la Plata Shipping", "Meridan Logistics"
            };
        }

        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += ProcessDailyUpdates;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= ProcessDailyUpdates;
        }

        private void ProcessDailyUpdates()
        {
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 1;
            foreach (var client in _activeClients)
            {
                client.DecayAnger();
                if (client.HasActiveContract)
                {
                    client.ContractDaysRemaining--;
                    if (client.ContractDaysRemaining <= 0)
                        client.DecideToRenewContract();
                }
            }
        }

        // ═══════════════════════════════════
        // GENERACIÓN DE CLIENTES
        // ═══════════════════════════════════

        public Client GetOrCreateClient(Constants.ClientType type, string preferredName = "")
        {
            // Reusar cliente activo del mismo tipo si hay
            foreach (var c in _activeClients)
            {
                if (c.ClientType == type && c.IsActive && !c.IsBlacklisted && c.PendingOffers < 2)
                {
                    c.PendingOffers++;
                    return c;
                }
            }
            return CreateNewClient(type, preferredName);
        }

        public Client CreateNewClient(Constants.ClientType type, string name = "")
        {
            if (string.IsNullOrEmpty(name))
                name = GetRandomCompanyName();
            var client = new Client(name, type);
            _activeClients.Add(client);
            OnClientAdded?.Invoke(client);
            return client;
        }

        private string GetRandomCompanyName()
        {
            if (_companyNamePool.Count == 0) return "Empresa Anónima SA";
            int idx = UnityEngine.Random.Range(0, _companyNamePool.Count);
            string name = _companyNamePool[idx];
            // No remover para poder reutilizar con distinción por ID
            return name;
        }

        // ═══════════════════════════════════
        // NEGOCIACIÓN
        // ═══════════════════════════════════

        public Quote.NegotiationResult EvaluateQuote(Quote quote, Client client, Cargo cargo)
        {
            float baseAcceptance = Constants.NEGOTIATION_BASE_ACCEPTANCE;

            // Modificadores por tipo de cliente
            switch (client.ClientType)
            {
                case Constants.ClientType.UrgentClient:   baseAcceptance += 0.20f; break;
                case Constants.ClientType.GoodPayer:      baseAcceptance += 0.10f; break;
                case Constants.ClientType.ContractClient: baseAcceptance += 0.05f; break;
                case Constants.ClientType.CreditClient:   baseAcceptance -= 0.05f; break;
                case Constants.ClientType.BadPayer:       baseAcceptance -= 0.15f; break;
                case Constants.ClientType.VeryBadClient:  baseAcceptance -= 0.25f; break;
            }

            // Modificador por margen
            baseAcceptance += client.GetNegotiationBonus();
            var (rejChance, _) = client.ReactToHighPrice(quote.Margin);
            baseAcceptance -= rejChance * 0.5f;

            // Modo de transporte correcto para tipo de carga
            if (cargo.PreferredTransport == quote.TransportMode) baseAcceptance += 0.10f;

            baseAcceptance = Mathf.Clamp01(baseAcceptance);

            if (UnityEngine.Random.value < baseAcceptance)
            {
                return Quote.NegotiationResult.Acceptance("¡Trato cerrado! Sus condiciones son aceptables.", baseAcceptance);
            }

            // Contraoferta si acepta negociar
            if (client.AcceptsNegotiation && quote.NegotiationRound <= 2)
            {
                float desiredMult = client.GetDesiredPriceMultiplier();
                int counterPrice = (int)(quote.AgentCost * desiredMult * 1.1f);
                counterPrice = Math.Max(counterPrice, quote.AgentCost + 100);
                string msg = $"Le ofrezco ${counterPrice:N0}. Es el máximo que puedo pagar.";
                return Quote.NegotiationResult.CounterOffer(counterPrice, msg, baseAcceptance, quote.NegotiationRound);
            }

            string rejectMsg = GetRejectionMessage(client, quote);
            return Quote.NegotiationResult.Rejection(rejectMsg, baseAcceptance);
        }

        private string GetRejectionMessage(Client client, Quote quote)
        {
            if (quote.HasExcessiveMargin()) return "El precio es demasiado alto. No podemos aceptar este margen.";
            switch (client.ClientType)
            {
                case Constants.ClientType.VeryBadClient: return "No nos convence su propuesta.";
                case Constants.ClientType.BadPayer:      return "En este momento no podemos comprometernos.";
                default:                                 return "Sus condiciones no se ajustan a nuestras necesidades actuales.";
            }
        }

        // ═══════════════════════════════════
        // REGISTRO DE ENTREGAS
        // ═══════════════════════════════════

        public void NotifyDelivery(string clientId, bool wasSuccessful, string originId,
                                   string destId, int currentDay, bool wasDelayed = false, bool wasDamaged = false)
        {
            var client = GetClientById(clientId);
            if (client == null) return;

            client.RecordDelivery(wasSuccessful, originId, destId, currentDay, wasDelayed, wasDamaged);
            client.PendingOffers = Math.Max(0, client.PendingOffers - 1);

            if (client.IsBlacklisted) OnClientBlacklisted?.Invoke(client);
            if (!client.IsVip && client.DecideToBecomeVip())
            {
                client.IsVip = true;
                OnClientBecameVip?.Invoke(client);
            }
        }

        // ═══════════════════════════════════
        // CONSULTAS
        // ═══════════════════════════════════

        public Client GetClientById(string id)
        {
            foreach (var c in _activeClients)
                if (c.Id == id) return c;
            return null;
        }

        public Constants.ClientType GetRandomClientType()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.15f) return Constants.ClientType.ContractClient;
            if (roll < 0.30f) return Constants.ClientType.GoodPayer;
            if (roll < 0.45f) return Constants.ClientType.UrgentClient;
            if (roll < 0.60f) return Constants.ClientType.CreditClient;
            if (roll < 0.80f) return Constants.ClientType.BadPayer;
            return Constants.ClientType.VeryBadClient;
        }
    }
}
