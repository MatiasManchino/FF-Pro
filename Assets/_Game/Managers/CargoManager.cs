using System;
using System.Collections.Generic;
using FreightForwarder.Map;
using FreightForwarder.Models;
using FreightForwarder.Systems.Maritime;
using FreightForwarder.Systems.Progression;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class CargoManager : Singleton<CargoManager>
    {
// Mercado cargos.
        public List<Cargo> MarketCargos { get; private set; }
// Gestiona active cargos.
        public List<Cargo> ActiveCargos { get; private set; }
// Completado cargos.
        public List<Cargo> CompletedCargos { get; private set; }
// Fallado cargos.
        public List<Cargo> FailedCargos { get; private set; }

        private int _maxMarketCargos = Constants.MAX_MARKET_CARGOS;
        private float _newCargoChancePerDay = 0.3f;
        private List<string> _unlockedCityIds;

        public event Action<Cargo> OnCargoAddedToMarket;
        public event Action<Cargo> OnCargoAccepted;
        public event Action<Cargo> OnCargoCompleted;
        public event Action<Cargo> OnCargoFailed;
        public event Action<Cargo> OnCargoExpired;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            MarketCargos = new List<Cargo>();
            ActiveCargos = new List<Cargo>();
            CompletedCargos = new List<Cargo>();
            FailedCargos = new List<Cargo>();
            _unlockedCityIds = new List<string> { "buenos_aires" };
        }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            CityDatabase.Initialize();
            RefreshUnlockedCities();

            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;

            // Mercado inicial con 3-4 cargas
            int initial = UnityEngine.Random.Range(3, 5);
            for (int i = 0; i < initial; i++) GenerateCargo();
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
        }

// Refresca unlocked ciudades
        private void RefreshUnlockedCities()
        {
            _unlockedCityIds.Clear();
// Foreach
            foreach (var city in CityDatabase.AllCities.Values)
                if (city.IsUnlocked) _unlockedCityIds.Add(city.Id);
        }

        // ═══════════════════════════════════
        // CICLO DIARIO
        // Se invoca al terminar un día de juego.

        private void OnDayPassed()
        {
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 1;
            PaymentManager.Instance?.ProcessDuePayments(currentDay);
            UpdateActiveCargos(currentDay);
            CheckExpiredCargos(currentDay);

            if (MarketCargos.Count < _maxMarketCargos && UnityEngine.Random.value < _newCargoChancePerDay)
                GenerateCargo();
        }

// Actualiza active cargos
        private void UpdateActiveCargos(int currentDay)
        {
            var toComplete = new List<Cargo>();
// Foreach
            foreach (var cargo in ActiveCargos)
            {
                cargo.DaysRemaining--;
                if (cargo.DaysRemaining <= 0)
                    toComplete.Add(cargo);
            }
// Foreach
            foreach (var cargo in toComplete)
                CompleteCargo(cargo, currentDay);
        }

// Verifica expirado cargos.
        private void CheckExpiredCargos(int currentDay)
        {
            var toExpire = new List<Cargo>();
// Foreach
            foreach (var cargo in MarketCargos)
                if (cargo.IsExpired(currentDay)) toExpire.Add(cargo);

// Foreach
            foreach (var cargo in toExpire)
            {
                MarketCargos.Remove(cargo);
                cargo.Status = Constants.CargoStatus.Expired;
                OnCargoExpired?.Invoke(cargo);
            }
        }

        // ═══════════════════════════════════
        // GENERACIÓN DE CARGAS
        // ═══════════════════════════════════

        // ── Port → cityId mapping ──────────────────────────────────────────
        private static readonly Dictionary<string, string> _portToCityId = new Dictionary<string, string>
        {
            { "São Paulo",      "sao_paulo"    }, { "Buenos Aires", "buenos_aires" },
            { "Valparaíso",     "valparaiso"   }, { "Santiago",     "santiago"     },
            { "Lima",           "lima"         }, { "Panamá",       "panama"       },
            { "Cartagena",      "cartagena"    }, { "Miami",        "miami"        },
            { "New York",       "new_york"     }, { "Los Ángeles",  "los_angeles"  },
            { "Houston",        "houston"      }, { "Vancouver",    "vancouver"    },
            { "Tokio",          "tokio"        }, { "Busan",        "busan"        },
            { "Vladivostok",    "vladivostok"  }, { "Shanghái",     "shanghai"     },
            { "Taipéi",         "taipei"       }, { "Hong Kong",    "hong_kong"    },
            { "Singapur",       "singapur"     }, { "Bangkok",      "bangkok"      },
            { "Ho Chi Minh",    "ho_chi_minh"  }, { "Manila",       "manila"       },
            { "Dubái",          "dubai"        }, { "Jeddah",       "jeddah"       },
            { "Mumbai",         "mumbai"       }, { "Karachi",      "karachi"      },
            { "Colombo",        "colombo"      }, { "Mombasa",      "mombasa"      },
            { "Port Said",      "port_said"    }, { "Cape Town",    "cape_town"    },
            { "Johannesburgo",  "johannesburg" }, { "Rotterdam",    "rotterdam"    },
            { "London",         "london"       }, { "Amberes",      "amberes"      },
            { "Barcelona",      "barcelona"    }, { "Marsella",     "marsella"     },
            { "Hamburgo",       "hamburgo"     }, { "Casablanca",   "casablanca"   },
            { "Atenas",         "atenas"       }, { "Estambul",     "estambul"     },
            { "Sídney",         "sidney"       }, { "Auckland",     "auckland"     },
        };

// Puerto to ciudad id.
        private static string PortToCityId(string port) =>
            _portToCityId.TryGetValue(port, out var id) ? id : port.ToLowerInvariant().Replace(' ', '_');

// Genera cargamento.
        private void GenerateCargo()
        {
            if (MarketCargos.Count >= _maxMarketCargos) return;

            // Pick a random ruta from the maritime database
            var routes = MaritimeRouteDatabase.Routes;
            if (routes == null || routes.Length == 0) return;

            var entry = routes[UnityEngine.Random.Range(0, routes.Length)];
            string[] parts = SplitPortNames(entry.Item1);
            if (parts == null) return;

            string originPort = MaritimeSimulationManager.Canonical(parts[0]);
            string destPort   = MaritimeSimulationManager.Canonical(parts[1]);
            string originId   = PortToCityId(originPort);
            string destId     = PortToCityId(destPort);

            Constants.CargoType  cargoType  = RollCargoType();
            Constants.ClientType clientType = ClientManager.Instance?.GetRandomClientType()
                                              ?? Constants.ClientType.GoodPayer;
            Client client    = ClientManager.Instance?.GetOrCreateClient(clientType);
            string clientName = client?.CompanyName ?? "Empresa Anónima";

            float weight       = UnityEngine.Random.Range(5f, 500f);
            float volume       = UnityEngine.Random.Range(5f, 200f);
            float ttBaseDays   = entry.Item2;
            float baseValue    = 2000f + ttBaseDays * 800f;
            int declaredValue  = Mathf.Clamp(
                (int)(baseValue * Constants.CargoValueMultipliers[cargoType] * UnityEngine.Random.Range(0.8f, 1.2f)),
                1000, 500000);

            int currentDay    = FFTimeManager.Instance?.CurrentDay ?? 1;
            int expirationDay = currentDay + Constants.CARGO_EXPIRATION_DAYS;

            var cargo = new Cargo(originId, destId, cargoType, clientType,
                                  clientName, weight, volume, declaredValue,
                                  expirationDay, currentDay)
            {
                PreferredTransport = Constants.TransportMode.Maritime,
                TransportReason    = "Ruta marítima disponible.",
            };
            if (client != null) cargo.ClientId = client.Id;

            // Store puerto names para que the mercado puede look up ruta options
            cargo.RouteWaypoints.Add(originPort);
            cargo.RouteWaypoints.Add(destPort);

            MarketCargos.Add(cargo);
            OnCargoAddedToMarket?.Invoke(cargo);
        }

// Gestiona split puerto names.
        private static string[] SplitPortNames(string routeName)
        {
            int idx = routeName.IndexOf(" – ");
            if (idx < 0) idx = routeName.IndexOf(" - ");
            if (idx < 0) return null;
            string sep = routeName.IndexOf(" – ") >= 0 ? " – " : " - ";
            return new[] { routeName.Substring(0, idx).Trim(), routeName.Substring(idx + sep.Length).Trim() };
        }

// Ejecuta roll cargo type
        private Constants.CargoType RollCargoType()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.40f) return Constants.CargoType.General;
            if (roll < 0.60f) return Constants.CargoType.Refrigerated;
            if (roll < 0.75f) return Constants.CargoType.Urgent;
            if (roll < 0.90f) return Constants.CargoType.Valuable;
            return Constants.CargoType.Dangerous;
        }


        // ═══════════════════════════════════
        // ACEPTAR COTIZACIÓN
        // Aceptado cotización.

        public bool AcceptQuote(Cargo cargo, Quote quote, int currentDay)
        {
            if (!MarketCargos.Contains(cargo)) return false;
            MarketCargos.Remove(cargo);

            cargo.Status          = Constants.CargoStatus.Active;
            cargo.QuotedPrice     = quote.OfferedPrice;
            cargo.FinalPrice      = quote.IsAgreementReached ? quote.FinalPrice : quote.OfferedPrice;
            cargo.AgentCost       = quote.AgentCost;
            cargo.Margin          = quote.Margin;
            cargo.TransportMode   = quote.TransportMode;
            cargo.AgentId         = quote.AgentId;
            cargo.HasInsurance    = quote.HasInsurance;
            cargo.StartDay        = currentDay;

            float distance = CityDatabase.GetDistance(cargo.OriginCityId, cargo.DestinationCityId);
            Agent agent    = AgentManager.Instance?.GetAgent(quote.AgentId);
            float speed    = agent?.GetCurrentSpeedMultiplier() ?? 1f;
            int baseDays   = CalculateTransitDaysV2(cargo.OriginCityId, cargo.DestinationCityId,
                                                     quote.TransportMode, speed);
            // + días de operación detenido en la terminal de origen y destino (carga/descarga).
            int totalDays = baseDays + 2 * Constants.TERMINAL_OPERATION_DAYS;
            cargo.TotalTransitDays   = totalDays;
            cargo.DaysRemaining      = totalDays;
            cargo.EstimatedArrivalDay = currentDay + totalDays;

            AgentManager.Instance?.AssignCargoToAgent(quote.AgentId, cargo.Id);
            ClientManager.Instance?.NotifyInteraction(cargo.ClientId, currentDay);
            ActiveCargos.Add(cargo);
            OnCargoAccepted?.Invoke(cargo);
            return true;
        }

        // Maritime ruta acceptance — called by MarketPanel when jugador picks a ShipmentOption
        public bool AcceptMaritimeOption(Cargo cargo, ShipmentOption option, int finalPrice, int currentDay)
        {
            if (!MarketCargos.Contains(cargo)) return false;
            MarketCargos.Remove(cargo);

            cargo.Status             = Constants.CargoStatus.Active;
            cargo.FinalPrice         = finalPrice;
            cargo.QuotedPrice        = finalPrice;
            cargo.AgentCost          = option.EstimatedCostUSD;
            cargo.Margin             = finalPrice > 0 ? (float)(finalPrice - option.EstimatedCostUSD) / finalPrice : 0f;
            cargo.TransportMode      = Constants.TransportMode.Maritime;
            cargo.StartDay           = currentDay;
            cargo.TotalTransitDays   = option.TotalTTDays;
            cargo.DaysRemaining      = option.TotalTTDays;
            cargo.EstimatedArrivalDay = currentDay + option.TotalTTDays;

            // Start visual simulation
            MaritimeSimulationManager.Instance?.StartShipment(cargo.Id, option, currentDay);
            ShipMarker.Create(MaritimeSimulationManager.Instance?.GetShipment(cargo.Id));

            ClientManager.Instance?.NotifyInteraction(cargo.ClientId, currentDay);
            ActiveCargos.Add(cargo);
            OnCargoAccepted?.Invoke(cargo);
            return true;
        }

// Completo maritime shipment.
        public void CompleteMaritimeShipment(string cargoId, int currentDay)
        {
            var cargo = GetCargoById(cargoId);
            if (cargo != null && cargo.IsActive())
                CompleteCargo(cargo, currentDay);
        }

        private int CalculateTransitDaysV2(string originId, string destId,
                                            Constants.TransportMode mode, float speedMult)
        {
            float kmPerDay;
            switch (mode)
            {
                case Constants.TransportMode.Air:        kmPerDay = 15000f; break;
                case Constants.TransportMode.Land:       kmPerDay = 600f;   break;
                case Constants.TransportMode.Rail:       kmPerDay = 800f;   break;
                default:                                 kmPerDay = 2000f;  break;
            }
            float dist = CityDatabase.GetDistance(originId, destId);
            return Mathf.Max(1, Mathf.CeilToInt(dist / (kmPerDay * speedMult)));
        }

        // ═══════════════════════════════════
        // COMPLETAR / FALLAR
        // Completo cargamento.

        private void CompleteCargo(Cargo cargo, int currentDay)
        {
            ActiveCargos.Remove(cargo);
            cargo.Status = Constants.CargoStatus.Completed;
            cargo.ActualArrivalDay = currentDay;

            if (CompletedCargos.Count >= 100) CompletedCargos.RemoveAt(0);
            CompletedCargos.Add(cargo);

            // Período de gracia: las primeras operaciones NO descuentan el costo del transportista.
            // Luego se paga al contado al entregar (riesgo de caja / bancarrota) y se difiere el cobro bruto.
            var eco = EconomyManager.Instance;
            int completedSoFar = eco?.TotalCargosCompleted ?? 0;
            bool grace = completedSoFar < Constants.PAYMENT_GRACE_OPERATIONS;

            eco?.RecordCargoCompletedDeferred();   // estadística + XP (incrementa el contador)

            int deferredAmount;
            if (grace)
            {
                deferredAmount = Math.Max(0, cargo.FinalPrice - cargo.AgentCost); // neto diferido, sin gasto
            }
            else
            {
                eco?.PayCarrierCost(cargo.AgentCost);  // costo del transportista al contado (puede fundir)
                deferredAmount = cargo.FinalPrice;     // cobro bruto del cliente, diferido
            }
            PaymentManager.Instance?.SchedulePayment(cargo, currentDay, deferredAmount);

            AgentManager.Instance?.RecordDelivery(cargo.AgentId, cargo.Id, true, false);
            int clientProfit = Math.Max(0, cargo.FinalPrice - cargo.AgentCost);
            ClientManager.Instance?.NotifyDelivery(cargo.ClientId, true,
                cargo.OriginCityId, cargo.DestinationCityId, currentDay, false, false, clientProfit);

            if (FeatureFlags.USE_AGENT_BONUS)
                AgentBonusSystem.RecordRoute(cargo.AgentId, cargo.OriginCityId, cargo.DestinationCityId);

            OnCargoCompleted?.Invoke(cargo);
        }

// Gestiona fail cargamento.
        private void FailCargo(Cargo cargo, int currentDay, string reason)
        {
            ActiveCargos.Remove(cargo);
            cargo.Status = Constants.CargoStatus.Failed;
            cargo.ActualArrivalDay = currentDay;
            FailedCargos.Add(cargo);

            int penalty = cargo.FinalPrice / 2;
            EconomyManager.Instance?.RecordCargoFailed(penalty);
            AgentManager.Instance?.RecordDelivery(cargo.AgentId, cargo.Id, false, false);
            ClientManager.Instance?.NotifyDelivery(cargo.ClientId, false,
                cargo.OriginCityId, cargo.DestinationCityId, currentDay);

            OnCargoFailed?.Invoke(cargo);
            Debug.LogWarning($"[CargoManager] Carga fallida: {cargo.Id} — {reason}");
        }

// Gestiona abandon cargamento.
        public void AbandonCargo(Cargo cargo, int currentDay)
        {
            ActiveCargos.Remove(cargo);
            cargo.Status = Constants.CargoStatus.Failed;
            cargo.WasAbandonedByAgent = true;
            FailedCargos.Add(cargo);

            int penalty = cargo.FinalPrice / 2;
            EconomyManager.Instance?.RecordCargoAbandoned(penalty);
            AgentManager.Instance?.RecordDelivery(cargo.AgentId, cargo.Id, false, true);
            ClientManager.Instance?.NotifyDelivery(cargo.ClientId, false,
                cargo.OriginCityId, cargo.DestinationCityId, currentDay);

            OnCargoFailed?.Invoke(cargo);
        }

        // ═══════════════════════════════════
        // CONSULTAS
        // Obtiene cargamento by id

        public Cargo GetCargoById(string id)
        {
            foreach (var c in MarketCargos)   if (c.Id == id) return c;
            foreach (var c in ActiveCargos)   if (c.Id == id) return c;
            foreach (var c in CompletedCargos) if (c.Id == id) return c;
            foreach (var c in FailedCargos)   if (c.Id == id) return c;
            return null;
        }

// Obtiene available cargos
        public List<Cargo> GetAvailableCargos()
        {
            var result = new List<Cargo>();
// Foreach
            foreach (var c in MarketCargos)
                if (c.Status == Constants.CargoStatus.Available) result.Add(c);
            return result;
        }

// Obtiene total cargos
        public int GetTotalCargos() => MarketCargos.Count + ActiveCargos.Count + CompletedCargos.Count + FailedCargos.Count;

// Obtiene success rate
        public float GetSuccessRate()
        {
            int done = CompletedCargos.Count + FailedCargos.Count;
            return done == 0 ? 0f : (float)CompletedCargos.Count / done;
        }

// Gestiona unlock ciudad.
        public void UnlockCity(string cityId)
        {
            WorldCity city = CityDatabase.GetCity(cityId);
            if (city != null)
            {
                city.IsUnlocked = true;
                RefreshUnlockedCities();
            }
        }
    }
}