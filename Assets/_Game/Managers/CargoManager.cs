using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class CargoManager : Singleton<CargoManager>
    {
        public List<Cargo> MarketCargos { get; private set; }
        public List<Cargo> ActiveCargos { get; private set; }
        public List<Cargo> CompletedCargos { get; private set; }
        public List<Cargo> FailedCargos { get; private set; }

        private int _maxMarketCargos = Constants.MAX_MARKET_CARGOS;
        private float _newCargoChancePerDay = 0.3f;
        private List<string> _unlockedCityIds;

        public event Action<Cargo> OnCargoAddedToMarket;
        public event Action<Cargo> OnCargoAccepted;
        public event Action<Cargo> OnCargoCompleted;
        public event Action<Cargo> OnCargoFailed;
        public event Action<Cargo> OnCargoExpired;

        protected override void OnAwake()
        {
            MarketCargos = new List<Cargo>();
            ActiveCargos = new List<Cargo>();
            CompletedCargos = new List<Cargo>();
            FailedCargos = new List<Cargo>();
            _unlockedCityIds = new List<string> { "buenos_aires" };
        }

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

        private void RefreshUnlockedCities()
        {
            _unlockedCityIds.Clear();
            foreach (var city in CityDatabase.AllCities.Values)
                if (city.IsUnlocked) _unlockedCityIds.Add(city.Id);
        }

        // ═══════════════════════════════════
        // CICLO DIARIO
        // ═══════════════════════════════════

        private void OnDayPassed()
        {
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 1;
            UpdateActiveCargos(currentDay);
            CheckExpiredCargos(currentDay);

            if (MarketCargos.Count < _maxMarketCargos && UnityEngine.Random.value < _newCargoChancePerDay)
                GenerateCargo();
        }

        private void UpdateActiveCargos(int currentDay)
        {
            var toComplete = new List<Cargo>();
            foreach (var cargo in ActiveCargos)
            {
                cargo.DaysRemaining--;
                if (cargo.DaysRemaining <= 0)
                    toComplete.Add(cargo);
            }
            foreach (var cargo in toComplete)
                CompleteCargo(cargo, currentDay);
        }

        private void CheckExpiredCargos(int currentDay)
        {
            var toExpire = new List<Cargo>();
            foreach (var cargo in MarketCargos)
                if (cargo.IsExpired(currentDay)) toExpire.Add(cargo);

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

        private void GenerateCargo()
        {
            if (MarketCargos.Count >= _maxMarketCargos) return;

            var allIds = new List<string>(CityDatabase.AllCities.Keys);
            if (allIds.Count < 2) return;

            string originId = null, destinationId = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                string a = allIds[UnityEngine.Random.Range(0, allIds.Count)];
                string b = allIds[UnityEngine.Random.Range(0, allIds.Count)];
                if (a != b) { originId = a; destinationId = b; break; }
            }
            if (originId == null) return;

            Constants.CargoType cargoType = RollCargoType();
            Constants.ClientType clientType = ClientManager.Instance?.GetRandomClientType()
                                              ?? Constants.ClientType.GoodPayer;

            Client client = ClientManager.Instance?.GetOrCreateClient(clientType);
            string clientName = client?.CompanyName ?? "Empresa Anónima";

            float weight = UnityEngine.Random.Range(1f, 500f);
            float volume = UnityEngine.Random.Range(1f, 200f);
            float distance = CityDatabase.GetDistance(originId, destinationId);
            float baseValue = 1000f + (distance / 20000f) * 500000f;
            float typeMultiplier = Constants.CargoValueMultipliers[cargoType];
            int declaredValue = (int)(baseValue * typeMultiplier * UnityEngine.Random.Range(0.8f, 1.2f));
            declaredValue = Mathf.Clamp(declaredValue, 1000, 500000);

            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 1;
            int expirationDay = currentDay + Constants.CARGO_EXPIRATION_DAYS;

            var cargo = new Cargo(originId, destinationId, cargoType, clientType,
                                  clientName, weight, volume, declaredValue,
                                  expirationDay, currentDay);
            if (client != null) cargo.ClientId = client.Id;

            cargo.PreferredTransport = DeterminePreferredTransport(cargoType, distance, originId, destinationId);
            cargo.TransportReason = GetTransportReason(cargo.PreferredTransport, cargoType, distance);

            MarketCargos.Add(cargo);
            OnCargoAddedToMarket?.Invoke(cargo);
        }

        private Constants.CargoType RollCargoType()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.40f) return Constants.CargoType.General;
            if (roll < 0.60f) return Constants.CargoType.Refrigerated;
            if (roll < 0.75f) return Constants.CargoType.Urgent;
            if (roll < 0.90f) return Constants.CargoType.Valuable;
            return Constants.CargoType.Dangerous;
        }

        private Constants.TransportMode DeterminePreferredTransport(Constants.CargoType type, float distance,
                                                                      string originId, string destId)
        {
            WorldCity origin = CityDatabase.GetCity(originId);
            WorldCity dest   = CityDatabase.GetCity(destId);

            if (type == Constants.CargoType.Urgent || type == Constants.CargoType.Valuable)
            {
                if (origin != null && dest != null && origin.HasAirport && dest.HasAirport)
                    return Constants.TransportMode.Air;
            }
            if (type == Constants.CargoType.Dangerous)
            {
                if (origin != null && dest != null && origin.HasPort && dest.HasPort)
                    return Constants.TransportMode.Maritime;
            }
            if (distance < 3000f && origin != null && dest != null && origin.CanLandTransportTo(dest))
                return Constants.TransportMode.Land;
            if (origin != null && dest != null && origin.HasPort && dest.HasPort)
                return Constants.TransportMode.Maritime;
            return Constants.TransportMode.Maritime;
        }

        private string GetTransportReason(Constants.TransportMode mode, Constants.CargoType type, float distance)
        {
            switch (mode)
            {
                case Constants.TransportMode.Air:
                    return type == Constants.CargoType.Urgent ? "Urgente: requiere entrega rápida." : "Carga valiosa: mayor seguridad en vuelo.";
                case Constants.TransportMode.Maritime:
                    return type == Constants.CargoType.Dangerous ? "Peligrosa: el marítimo tiene menos restricciones." :
                           distance > 5000f ? "Larga distancia: marítimo más económico." : "Puerto disponible en ambas ciudades.";
                case Constants.TransportMode.Land:
                    return "Distancia corta: transporte terrestre más eficiente.";
                default:
                    return "Modo óptimo según las condiciones de la ruta.";
            }
        }

        // ═══════════════════════════════════
        // ACEPTAR COTIZACIÓN
        // ═══════════════════════════════════

        public bool AcceptQuote(Cargo cargo, Quote quote, int currentDay)
        {
            if (!MarketCargos.Contains(cargo)) return false;

            MarketCargos.Remove(cargo);

            cargo.Status = Constants.CargoStatus.Active;
            cargo.QuotedPrice = quote.OfferedPrice;
            cargo.FinalPrice = quote.IsAgreementReached ? quote.FinalPrice : quote.OfferedPrice;
            cargo.AgentCost = quote.AgentCost;
            cargo.Margin = quote.Margin;
            cargo.TransportMode = quote.TransportMode;
            cargo.AgentId = quote.AgentId;
            cargo.HasInsurance = quote.HasInsurance;
            cargo.StartDay = currentDay;

            float distance = CityDatabase.GetDistance(cargo.OriginCityId, cargo.DestinationCityId);
            Agent agent = AgentManager.Instance?.GetAgent(quote.AgentId);
            float speed = agent?.GetCurrentSpeedMultiplier() ?? 1f;

            int baseDays = CalculateTransitDays(quote.TransportMode, distance, speed);
            cargo.TotalTransitDays = baseDays;
            cargo.DaysRemaining = baseDays;
            cargo.EstimatedArrivalDay = currentDay + baseDays;

            AgentManager.Instance?.AssignCargoToAgent(quote.AgentId, cargo.Id);
            ActiveCargos.Add(cargo);
            OnCargoAccepted?.Invoke(cargo);
            return true;
        }

        private int CalculateTransitDays(Constants.TransportMode mode, float distanceKm, float speedMult)
        {
            float kmPerDay;
            switch (mode)
            {
                case Constants.TransportMode.Air:        kmPerDay = 15000f; break;
                case Constants.TransportMode.Land:       kmPerDay = 600f;   break;
                case Constants.TransportMode.Rail:       kmPerDay = 800f;   break;
                case Constants.TransportMode.Multimodal: kmPerDay = 3000f;  break;
                default:                                 kmPerDay = 2000f;  break; // Maritime
            }
            int days = Mathf.CeilToInt(distanceKm / (kmPerDay * speedMult));
            return Mathf.Max(1, days);
        }

        // ═══════════════════════════════════
        // COMPLETAR / FALLAR
        // ═══════════════════════════════════

        private void CompleteCargo(Cargo cargo, int currentDay)
        {
            ActiveCargos.Remove(cargo);
            cargo.Status = Constants.CargoStatus.Completed;
            cargo.ActualArrivalDay = currentDay;

            if (CompletedCargos.Count >= 100) CompletedCargos.RemoveAt(0);
            CompletedCargos.Add(cargo);

            EconomyManager.Instance?.RecordCargoCompleted(cargo.FinalPrice, cargo.AgentCost);
            AgentManager.Instance?.RecordDelivery(cargo.AgentId, cargo.Id, true, false);
            ClientManager.Instance?.NotifyDelivery(cargo.ClientId, true,
                cargo.OriginCityId, cargo.DestinationCityId, currentDay);

            OnCargoCompleted?.Invoke(cargo);
        }

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
        // ═══════════════════════════════════

        public Cargo GetCargoById(string id)
        {
            foreach (var c in MarketCargos)   if (c.Id == id) return c;
            foreach (var c in ActiveCargos)   if (c.Id == id) return c;
            foreach (var c in CompletedCargos) if (c.Id == id) return c;
            foreach (var c in FailedCargos)   if (c.Id == id) return c;
            return null;
        }

        public List<Cargo> GetAvailableCargos()
        {
            var result = new List<Cargo>();
            foreach (var c in MarketCargos)
                if (c.Status == Constants.CargoStatus.Available) result.Add(c);
            return result;
        }

        public int GetTotalCargos() => MarketCargos.Count + ActiveCargos.Count + CompletedCargos.Count + FailedCargos.Count;

        public float GetSuccessRate()
        {
            int done = CompletedCargos.Count + FailedCargos.Count;
            return done == 0 ? 0f : (float)CompletedCargos.Count / done;
        }

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
