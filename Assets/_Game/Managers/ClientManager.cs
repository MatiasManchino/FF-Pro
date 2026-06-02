using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class ClientManager : Singleton<ClientManager>
    {
        private List<Client>               _activeClients;
        private Dictionary<string, Client> _clientIndex = new Dictionary<string, Client>();
        private List<string>               _companyNamePool;

// Devuelve la active clients
        public IReadOnlyList<Client> ActiveClients => _activeClients;

        public event Action<Client> OnClientAdded;
        public event Action<Client> OnClientBlacklisted;
        public event Action<Client> OnClientBecameVip;
        public event Action<Client, ClientTier> OnClientTierUp;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            _activeClients = new List<Client>();
            _clientIndex   = new Dictionary<string, Client>();
            InitializeCompanyNames();
        }

    // Inicializa ialize company names.
        private void InitializeCompanyNames()
        {
            _companyNamePool = new List<string>
            {
                // Alimentos y Bebidas
                "Alimentos del Sur S.A.",
                "Frigorífico Pampeano S.A.",
                "Cereales del Litoral SRL",
                "Bodegas Andinas S.A.",
                "Lácteos La Serranía S.A.",
                "Aceitera Río Grande SRL",
                "Molinos del Plata S.A.",
                "Yerba Mate Misionera S.A.",
                "Conservas Puerto Nuevo SRL",
                "Dulces Regionales S.A.S.",
                "Azucarera Tucumán S.A.",
                "Cervecería Austral SRL",

                // Industria y Metalurgia
                "Metalúrgica Rioplatense SRL",
                "Aceros Patagónicos S.A.",
                "Fundición del Norte S.A.",
                "Siderúrgica Campana S.A.",
                "Herrajes Industriales SRL",
                "Tornería Belgrano e Hijos",
                "Laminados del Centro S.A.",
                "Talleres Ferreyra e Hijos",

                // Tecnología
                "Sistemas Digitales Córdoba S.A.S.",
                "Red Global Informática SRL",
                "Soluciones TechnoSur S.A.S.",
                "Electrónica Federal S.A.",
                "DataLink Comunicaciones SRL",
                "SoftWare del Plata S.A.S.",
                "CompuRedes Argentina S.A.",

                // Minería y Energía
                "Minera Cordillerana S.A.",
                "Petrolera Austral S.A.",
                "Canteras del Oeste SRL",
                "Energía Renovable Cuyana S.A.",
                "Gas del Sur S.A.",
                "Litio Andino S.A.",

                // Agro y Ganadería
                "Estancia La Primavera S.A.",
                "Agroexport del Litoral SRL",
                "Semillas Pampeanas S.A.",
                "Ganadera Los Teros S.A.",
                "Forestal Mesopotámica SRL",
                "Vivero del Valle S.A.S.",
                "Oleaginosas Santa Fe S.A.",

                // Textil y Calzado
                "Textil Patagonia S.A.",
                "Hilandería del Norte SRL",
                "Confecciones Porteñas S.A.S.",
                "Calzados Libertador SRL",
                "Tejidos Artesanales Salta SRL",
                "Algodones del Chaco S.A.",

                // Química y Farmacéutica
                "Laboratorios Bioquím S.A.",
                "Química Industrial Rosario SRL",
                "Farmacéutica del Centro S.A.",
                "Agroquímicos Federales S.A.",
                "Pinturas Continental SRL",
                "Plásticos Modernos S.A.S.",

                // Construcción
                "Constructora del Plata S.A.",
                "Hormigones Argentinos SRL",
                "Cementos del Valle S.A.",
                "Mármoles y Granitos del Sur SRL",
                "Vialidad Nacional Construcciones S.A.",
                "Ingeniería Urbana S.A.S.",

                // Automotriz y Autopartes
                "Autopartes Rioplatenses SRL",
                "Frenos del Sur S.A.",
                "Caucho Industrial Córdoba S.A.",
                "Motores Nacionales S.A.",
                "Repuestos Federales SRL",

                // Transporte y Logística
                "Transportes Andinos S.A.",
                "Grupo Naviero del Plata S.A.",
                "Logística Intermodal S.A.",
                "Fletes del Mercosur SRL",
                "Aerocargas Nacionales S.A.",
                "Distribuidora del Centro SRL",

                // Importación y Exportación
                "Importadora Oriental S.A.S.",
                "Comercial Ultramar S.A.",
                "Trading Sur Américas SRL",
                "Aduanera del Puerto S.A.",
                "Exportadora Pampeana S.A.",

                // Varios
                "Papelera del Paraná S.A.",
                "Muebles Coloniales SRL",
                "Editorial Hemisferio Sur S.A.",
                "Vidriería Artística SRL",
                "Telecomunicaciones Federal S.A.",
                "Pesquera Mar Argentino S.A.",
                "Curtiembre del Litoral SRL",
                "Cooperativa Agraria Unión S.A.",
                "Envases del Plata S.A.S.",
                "Astillero Río Santiago SRL"
            };
        }
// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += ProcessDailyUpdates;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= ProcessDailyUpdates;
        }

// Gestiona process diario actualizaciones.
        private void ProcessDailyUpdates()
        {
            int day = FFTimeManager.Instance?.CurrentDay ?? 0;
// Foreach
            foreach (var client in _activeClients)
            {
                client.DecayAnger();
                // La relación baja cada día que no cerrás carga (ni lo llamás, a futuro).
                client.DecayRelationshipDaily(day, Constants.CLIENT_RELATIONSHIP_DECAY_PER_2WEEKS);
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
        // Obtiene or create cliente

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

// Crea new cliente
        public Client CreateNewClient(Constants.ClientType type, string name = "")
        {
            if (string.IsNullOrEmpty(name))
                name = GetRandomCompanyName();
            var client = new Client(name, type);
            client.LastInteractionDay = FFTimeManager.Instance?.CurrentDay ?? 0; // no decaer apenas aparece
            _activeClients.Add(client);
            _clientIndex[client.Id] = client;
            OnClientAdded?.Invoke(client);
            return client;
        }

// Obtiene random company nombre
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
            // Bonus por lealtad: los clientes fieles cierran trato más fácil.
            baseAcceptance += client.GetTierAcceptanceBonus();
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

// Obtiene rejection message
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
                                   string destId, int currentDay, bool wasDelayed = false, bool wasDamaged = false,
                                   int profit = 0)
        {
            var client = GetClientById(clientId);
            if (client == null) return;

            ClientTier prevTier = client.Tier;
            client.RecordDelivery(wasSuccessful, originId, destId, currentDay, wasDelayed, wasDamaged);
            if (profit > 0) client.RecordProfit(profit);
            client.PendingOffers = Math.Max(0, client.PendingOffers - 1);

            if (client.IsBlacklisted) OnClientBlacklisted?.Invoke(client);

            // Ascenso de nivel de lealtad (Frecuente → VIP → Diamante)
            if (client.Tier > prevTier)
            {
                if (client.Tier >= ClientTier.VIP && !client.IsVip)
                {
                    client.IsVip = true;
                    OnClientBecameVip?.Invoke(client);
                }
                OnClientTierUp?.Invoke(client, client.Tier);
            }
        }

        // ═══════════════════════════════════
        // CONSULTAS
        // Obtiene cliente by id

        public Client GetClientById(string id)
            => _clientIndex.TryGetValue(id, out var c) ? c : null;

        // Registra interacción con el cliente (cierre de carga / llamada): frena el decaimiento.
        public void NotifyInteraction(string clientId, int day)
        {
            var client = GetClientById(clientId);
            if (client != null) client.LastInteractionDay = day;
        }

// Obtiene random client type
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