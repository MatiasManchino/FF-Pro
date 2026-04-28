using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using FreightForwarder.Managers;

namespace FreightForwarder.Managers
{
    /// <summary>
    /// CargoManager — Gestiona el mercado de cargas, cargas activas e historial.
    /// 
    /// RESPONSABILIDADES:
    /// - Generar nuevas cargas en el mercado
    /// - Gestionar el mercado (máximo 7 cargas)
    /// - Actualizar cargas activas (avanzar días, completar, fallar)
    /// - Integrar con TimeManager (días)
    /// - Integrar con EconomyManager (dinero, reputación, XP)
    /// </summary>
    public class CargoManager : Singleton<CargoManager>
    {
        [Header("Configuración")]
        [SerializeField] private int _maxMarketCargos = Constants.MAX_MARKET_CARGOS;
        [SerializeField] private float _newCargoChancePerDay = 0.3f; // 30% chance por día
        
        // =========================================================================
        // LISTAS DE CARGAS
        // =========================================================================
        
        /// <summary>
        /// Cargas disponibles en el mercado (para cotizar)
        /// </summary>
        public List<Cargo> MarketCargos { get; private set; }
        
        /// <summary>
        /// Cargas activas (en tránsito)
        /// </summary>
        public List<Cargo> ActiveCargos { get; private set; }
        
        /// <summary>
        /// Historial de cargas completadas (últimas 100)
        /// </summary>
        public List<Cargo> CompletedCargos { get; private set; }
        
        /// <summary>
        /// Historial de cargas fallidas
        /// </summary>
        public List<Cargo> FailedCargos { get; private set; }
        
        // =========================================================================
        // CIUDADES DESBLOQUEADAS (para generar cargas)
        // =========================================================================
        
        private List<string> _unlockedCityIds;
        
        // =========================================================================
        // EVENTOS
        // =========================================================================
        
        public event Action<Cargo> OnCargoAddedToMarket;
        public event Action<Cargo> OnCargoAccepted;
        public event Action<Cargo> OnCargoCompleted;
        public event Action<Cargo> OnCargoFailed;
        public event Action<Cargo> OnCargoExpired;
        
        // =========================================================================
        // INICIALIZACIÓN
        // =========================================================================
        
        protected override void OnAwake()
        {
            MarketCargos = new List<Cargo>();
            ActiveCargos = new List<Cargo>();
            CompletedCargos = new List<Cargo>();
            FailedCargos = new List<Cargo>();
            _unlockedCityIds = new List<string>();
            
            // Suscribirse al evento de cambio de día
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += OnDayPassed;
            }
            
            Debug.Log("[CargoManager] Inicializado");
        }
        
        protected override void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= OnDayPassed;
            }
        }
        
        // =========================================================================
        // INICIALIZACIÓN DE PARTIDA NUEVA
        // =========================================================================
        
        /// <summary>
        /// Inicializa el mercado al comenzar una nueva partida.
        /// </summary>
        public void InitializeNewGame(List<string> unlockedCities)
        {
            _unlockedCityIds = new List<string>(unlockedCities);
            
            // Limpiar listas
            MarketCargos.Clear();
            ActiveCargos.Clear();
            CompletedCargos.Clear();
            FailedCargos.Clear();
            
            // Generar cargas iniciales (2-3 cargas)
            int initialCargos = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < initialCargos; i++)
            {
                GenerateCargo();
            }
            
            Debug.Log($"[CargoManager] Nueva partida iniciada. {MarketCargos.Count} cargas en mercado.");
        }
        
        /// <summary>
        /// Actualiza las ciudades desbloqueadas (cuando se abren nuevas oficinas).
        /// </summary>
        public void UpdateUnlockedCities(List<string> unlockedCities)
        {
            _unlockedCityIds = new List<string>(unlockedCities);
        }
        
        // =========================================================================
        // GENERACIÓN DE CARGAS
        // =========================================================================
        
        /// <summary>
        /// Genera una nueva carga en el mercado.
        /// </summary>
        public void GenerateCargo()
        {
            if (_unlockedCityIds.Count < 2)
            {
                Debug.LogWarning("[CargoManager] No hay suficientes ciudades desbloqueadas para generar carga.");
                return;
            }
            
            // Evitar generar más del máximo
            if (MarketCargos.Count >= _maxMarketCargos)
                return;
            
            // Seleccionar origen y destino (diferentes)
            string originId = GetRandomCity();
            string destinationId = GetRandomCity();
            
            int attempts = 0;
            while (originId == destinationId && attempts < 10)
            {
                destinationId = GetRandomCity();
                attempts++;
            }
            
            if (originId == destinationId)
                return;
            
            // Obtener datos de las ciudades
            WorldCity origin = CityDatabase.GetCity(originId);
            WorldCity destination = CityDatabase.GetCity(destinationId);
            
            if (origin == null || destination == null)
                return;
            
            // Calcular distancia aproximada
            float distanceKm = origin.DistanceTo(destination);
            
            // Determinar tipo de carga (con probabilidades)
            Constants.CargoType cargoType = GetRandomCargoType();
            
            // Determinar tipo de cliente
            Constants.ClientType clientType = GetRandomClientType();
            
            // Nombre de cliente aleatorio del pool
            string clientName = GetRandomClientName();
            
            // Calcular peso (1-500 toneladas)
            float weight = UnityEngine.Random.Range(1f, 500f);
            
            // Calcular volumen (1-200 m3)
            float volume = UnityEngine.Random.Range(1f, 200f);
            
            // Calcular valor base (1000 - 500000) según distancia y tipo
            float baseValue = 1000f + (distanceKm / 20000f) * 500000f;
            float multiplier = Constants.CargoValueMultipliers.GetValueOrDefault(cargoType, 1.0f);
            int declaredValue = Mathf.RoundToInt(baseValue * multiplier * UnityEngine.Random.Range(0.8f, 1.2f));
            declaredValue = Mathf.Clamp(declaredValue, 1000, 500000);
            
            // Determinar transporte preferido
            Constants.TransportMode preferredTransport = DeterminePreferredTransport(origin, destination, cargoType, clientType);
            string transportReason = GetTransportReason(preferredTransport, cargoType, clientType, distanceKm);
            
            // Día de expiración (7 días)
            int expirationDay = TimeManager.Instance.CurrentDay + Constants.CARGO_EXPIRATION_DAYS;
            int dayCreated = TimeManager.Instance.CurrentDay;
            
            // Crear la carga
            Cargo cargo = new Cargo(originId, destinationId, cargoType, clientType, clientName, 
                                     weight, volume, declaredValue, expirationDay, dayCreated);
            cargo.PreferredTransport = preferredTransport;
            cargo.TransportReason = transportReason;
            
            MarketCargos.Add(cargo);
            OnCargoAddedToMarket?.Invoke(cargo);
            
            Debug.Log($"[CargoManager] Nueva carga generada: {origin.DisplayName} → {destination.DisplayName} | {Constants.GetCargoTypeName(cargoType)} | ${declaredValue}");
        }
        
        /// <summary>
        /// Obtiene una ciudad aleatoria de las desbloqueadas.
        /// </summary>
        private string GetRandomCity()
        {
            if (_unlockedCityIds.Count == 0)
                return null;
            
            int index = UnityEngine.Random.Range(0, _unlockedCityIds.Count);
            return _unlockedCityIds[index];
        }
        
        /// <summary>
        /// Obtiene tipo de carga aleatorio con probabilidades.
        /// </summary>
        private Constants.CargoType GetRandomCargoType()
        {
            float roll = UnityEngine.Random.value;
            
            if (roll < 0.40f) return Constants.CargoType.General;      // 40%
            if (roll < 0.60f) return Constants.CargoType.Refrigerated; // 20%
            if (roll < 0.75f) return Constants.CargoType.Urgent;       // 15%
            if (roll < 0.90f) return Constants.CargoType.Valuable;     // 15%
            return Constants.CargoType.Dangerous;                      // 10%
        }
        
        /// <summary>
        /// Obtiene tipo de cliente aleatorio.
        /// </summary>
        private Constants.ClientType GetRandomClientType()
        {
            float roll = UnityEngine.Random.value;
            
            if (roll < 0.15f) return Constants.ClientType.ContractClient;
            if (roll < 0.30f) return Constants.ClientType.GoodPayer;
            if (roll < 0.45f) return Constants.ClientType.UrgentClient;
            if (roll < 0.60f) return Constants.ClientType.CreditClient;
            if (roll < 0.80f) return Constants.ClientType.BadPayer;
            return Constants.ClientType.VeryBadClient;
        }
        
        /// <summary>
        /// Obtiene nombre de cliente aleatorio.
        /// </summary>
        private string GetRandomClientName()
        {
            string[] clientNames = {
                "Aceros del Cono Sur", "Farmacéutica Rioplatense", "Agro Export SA",
                "Importadora del Pacífico", "Textiles Unidos", "Tech Components Inc",
                "Auto Parts Global", "Megastore Retail", "Consumer Goods Co",
                "FastDeal LLC", "QuickShip Ltd", "Minera Andina", "Petroquímica del Sur"
            };
            
            int index = UnityEngine.Random.Range(0, clientNames.Length);
            return clientNames[index];
        }
        
        /// <summary>
        /// Determina el modo de transporte preferido según origen, destino, tipo de carga y cliente.
        /// </summary>
        private Constants.TransportMode DeterminePreferredTransport(WorldCity origin, WorldCity destination, 
                                                                     Constants.CargoType cargoType, 
                                                                     Constants.ClientType clientType)
        {
            bool hasMaritime = origin.HasPort && destination.HasPort;
            bool hasAir = origin.HasAirport && destination.HasAirport;
            bool hasLand = origin.IsLandHub && destination.IsLandHub && origin.CanLandTransportTo(destination);
            
            // Carga urgente o cliente urgente → priorizar aire
            if (cargoType == Constants.CargoType.Urgent || clientType == Constants.ClientType.UrgentClient)
            {
                if (hasAir) return Constants.TransportMode.Air;
                if (hasLand) return Constants.TransportMode.Land;
                if (hasMaritime) return Constants.TransportMode.Maritime;
            }
            
            // Carga valiosa → priorizar aire por seguridad
            if (cargoType == Constants.CargoType.Valuable)
            {
                if (hasAir) return Constants.TransportMode.Air;
                if (hasLand) return Constants.TransportMode.Land;
                if (hasMaritime) return Constants.TransportMode.Maritime;
            }
            
            // Carga peligrosa → priorizar marítimo (menos restricciones)
            if (cargoType == Constants.CargoType.Dangerous)
            {
                if (hasMaritime) return Constants.TransportMode.Maritime;
                if (hasLand) return Constants.TransportMode.Land;
                if (hasAir) return Constants.TransportMode.Air;
            }
            
            // Distancia corta (< 3000 km) → terrestre
            float distance = origin.DistanceTo(destination);
            if (distance < 3000f && hasLand)
                return Constants.TransportMode.Land;
            
            // Distancia media (3000-10000 km) → marítimo
            if (distance >= 3000f && distance < 10000f && hasMaritime)
                return Constants.TransportMode.Maritime;
            
            // Larga distancia → marítimo
            if (hasMaritime) return Constants.TransportMode.Maritime;
            if (hasAir) return Constants.TransportMode.Air;
            if (hasLand) return Constants.TransportMode.Land;
            
            return Constants.TransportMode.Maritime;
        }
        
        /// <summary>
        /// Obtiene la razón del transporte preferido (para UI).
        /// </summary>
        private string GetTransportReason(Constants.TransportMode mode, Constants.CargoType cargoType, 
                                          Constants.ClientType clientType, float distanceKm)
        {
            if (cargoType == Constants.CargoType.Urgent || clientType == Constants.ClientType.UrgentClient)
            {
                if (mode == Constants.TransportMode.Air)
                    return "✈️ Cliente requiere urgencia → Aéreo es la opción más rápida";
                if (mode == Constants.TransportMode.Land)
                    return "🚛 Sin aeropuerto disponible → Terrestre es la alternativa más rápida";
                return "🚢 Única opción disponible para entrega urgente";
            }
            
            if (cargoType == Constants.CargoType.Valuable)
            {
                if (mode == Constants.TransportMode.Air)
                    return "✈️ Carga de alto valor → Aéreo reduce riesgo y tiempo";
                return "🚛 Transporte terrestre para mercancía valiosa";
            }
            
            if (cargoType == Constants.CargoType.Refrigerated)
            {
                if (distanceKm > 8000f && mode == Constants.TransportMode.Air)
                    return "✈️ Larga distancia + refrigerada → Aéreo preserva la cadena de frío";
                if (mode == Constants.TransportMode.Land)
                    return "🚛 Distancia corta → Terrestre mantiene cadena de frío a menor costo";
                return "Mejor opción disponible para carga refrigerada";
            }
            
            if (cargoType == Constants.CargoType.Dangerous)
            {
                if (mode == Constants.TransportMode.Maritime)
                    return "🚢 Carga peligrosa tiene menos restricciones por vía marítima";
                return "Opción disponible para carga con materiales peligrosos";
            }
            
            if (distanceKm < 3000f && mode == Constants.TransportMode.Land)
                return $"🚛 Distancia corta ({distanceKm:F0} km) → Terrestre es más económico";
            
            if (distanceKm < 10000f && mode == Constants.TransportMode.Maritime)
                return $"🚢 Distancia media ({distanceKm:F0} km) → Marítimo equilibra costo y tiempo";
            
            if (mode == Constants.TransportMode.Maritime)
                return $"🚢 Larga distancia ({distanceKm:F0} km) → Marítimo es el más económico";
            
            return "Mejor opción disponible para esta ruta";
        }
        
        // =========================================================================
        // PROCESAMIENTO DE DÍAS (desde TimeManager)
        // =========================================================================
        
        /// <summary>
        /// Se llama cada día. Actualiza cargas activas, verifica expiración y genera nuevas.
        /// </summary>
        private void OnDayPassed()
        {
            int currentDay = TimeManager.Instance.CurrentDay;
            
            // 1. Actualizar cargas activas
            UpdateActiveCargos(currentDay);
            
            // 2. Verificar expiración de cargas en mercado
            CheckExpiredCargos(currentDay);
            
            // 3. Generar nuevas cargas (30% chance)
            if (MarketCargos.Count < _maxMarketCargos && UnityEngine.Random.value < _newCargoChancePerDay)
            {
                GenerateCargo();
            }
        }
        
        /// <summary>
        /// Actualiza el progreso de las cargas activas.
        /// </summary>
        private void UpdateActiveCargos(int currentDay)
        {
            List<Cargo> completed = new List<Cargo>();
            List<Cargo> failed = new List<Cargo>();
            
            foreach (Cargo cargo in ActiveCargos)
            {
                cargo.DaysRemaining--;
                
                // Verificar si llegó a destino
                if (cargo.DaysRemaining <= 0)
                {
                    completed.Add(cargo);
                }
            }
            
            // Procesar cargas completadas
            foreach (Cargo cargo in completed)
            {
                CompleteCargo(cargo, currentDay);
            }
            
            // Procesar cargas fallidas (se puede expandir con sistema de eventos)
            foreach (Cargo cargo in failed)
            {
                FailCargo(cargo, currentDay, "desconocida");
            }
        }
        
        /// <summary>
        /// Verifica y elimina cargas expiradas del mercado.
        /// </summary>
        private void CheckExpiredCargos(int currentDay)
        {
            List<Cargo> expired = new List<Cargo>();
            
            foreach (Cargo cargo in MarketCargos)
            {
                if (cargo.IsExpired(currentDay))
                {
                    expired.Add(cargo);
                }
            }
            
            foreach (Cargo cargo in expired)
            {
                MarketCargos.Remove(cargo);
                cargo.Status = Constants.CargoStatus.Expired;
                FailedCargos.Add(cargo);
                OnCargoExpired?.Invoke(cargo);
                Debug.Log($"[CargoManager] Carga expirada: {cargo.Id}");
            }
        }
        
        // =========================================================================
        // ACEPTAR COTIZACIÓN
        // =========================================================================
        
        /// <summary>
        /// Acepta una cotización y mueve la carga a activas.
        /// </summary>
        public bool AcceptQuote(Cargo cargo, Quote quote, int currentDay)
        {
            if (cargo == null || quote == null)
                return false;
            
            if (!MarketCargos.Contains(cargo))
                return false;
            
            // Mover de mercado a activas
            MarketCargos.Remove(cargo);
            
            // Actualizar datos de la carga
            cargo.Status = Constants.CargoStatus.Active;
            cargo.QuotedPrice = quote.OfferedPrice;
            cargo.FinalPrice = quote.OfferedPrice;
            cargo.AgentCost = quote.AgentCost;
            cargo.TransportMode = quote.TransportMode;
            cargo.AgentId = quote.AgentId;
            cargo.HasInsurance = quote.HasInsurance;
            cargo.StartDay = currentDay;
            cargo.EstimatedArrivalDay = currentDay + quote.EstimatedDays;
            cargo.DaysRemaining = quote.EstimatedDays;
            cargo.TotalTransitDays = quote.EstimatedDays;
            cargo.CalculateMargin();
            
            ActiveCargos.Add(cargo);
            OnCargoAccepted?.Invoke(cargo);
            
            // Registrar en AgentManager
            if (AgentManager.Instance != null && !string.IsNullOrEmpty(quote.AgentId))
            {
                AgentManager.Instance.AssignCargoToAgent(quote.AgentId, cargo.Id);
            }
            
            Debug.Log($"[CargoManager] Carga aceptada: {cargo.Id} | Agente: {quote.AgentName} | Precio: ${quote.OfferedPrice}");
            
            return true;
        }
        
        // =========================================================================
        // COMPLETAR Y FALLAR CARGAS
        // =========================================================================
        
        /// <summary>
        /// Completa una carga exitosamente.
        /// </summary>
        private void CompleteCargo(Cargo cargo, int currentDay)
        {
            ActiveCargos.Remove(cargo);
            cargo.Status = Constants.CargoStatus.Completed;
            cargo.ActualArrivalDay = currentDay;
            
            CompletedCargos.Add(cargo);
            
            // Limitar historial a 100 cargas
            while (CompletedCargos.Count > 100)
            {
                CompletedCargos.RemoveAt(0);
            }
            
            // Registrar en EconomyManager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.RecordCargoCompleted(cargo.FinalPrice, cargo.AgentCost);
            }
            
            // Registrar en AgentManager
            if (AgentManager.Instance != null && !string.IsNullOrEmpty(cargo.AgentId))
            {
                AgentManager.Instance.RecordDelivery(cargo.AgentId, cargo.Id, true, false);
                AgentManager.Instance.RemoveCargoFromAgent(cargo.AgentId, cargo.Id);
            }
            
            OnCargoCompleted?.Invoke(cargo);
            Debug.Log($"[CargoManager] Carga completada: {cargo.Id} | Ganancia: ${cargo.FinalPrice - cargo.AgentCost}");
        }
        
        /// <summary>
        /// Marca una carga como fallida.
        /// </summary>
        private void FailCargo(Cargo cargo, int currentDay, string reason)
        {
            ActiveCargos.Remove(cargo);
            cargo.Status = Constants.CargoStatus.Failed;
            cargo.ActualArrivalDay = currentDay;
            
            FailedCargos.Add(cargo);
            
            // Registrar en EconomyManager (penalidad base)
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.RecordCargoFailed(cargo.FinalPrice / 2);
            }
            
            // Registrar en AgentManager
            if (AgentManager.Instance != null && !string.IsNullOrEmpty(cargo.AgentId))
            {
                AgentManager.Instance.RecordDelivery(cargo.AgentId, cargo.Id, false, false);
                AgentManager.Instance.RemoveCargoFromAgent(cargo.AgentId, cargo.Id);
            }
            
            OnCargoFailed?.Invoke(cargo);
            Debug.Log($"[CargoManager] Carga fallida: {cargo.Id} | Razón: {reason}");
        }
        
        // =========================================================================
        // MÉTODOS DE CONSULTA
        // =========================================================================
        
        public Cargo GetCargoById(string id)
        {
            // Buscar en mercado
            Cargo cargo = MarketCargos.FirstOrDefault(c => c.Id == id);
            if (cargo != null) return cargo;
            
            // Buscar en activas
            cargo = ActiveCargos.FirstOrDefault(c => c.Id == id);
            if (cargo != null) return cargo;
            
            // Buscar en completadas
            cargo = CompletedCargos.FirstOrDefault(c => c.Id == id);
            if (cargo != null) return cargo;
            
            // Buscar en fallidas
            cargo = FailedCargos.FirstOrDefault(c => c.Id == id);
            return cargo;
        }
        
        public List<Cargo> GetAvailableCargos()
        {
            return MarketCargos.Where(c => c.Status == Constants.CargoStatus.Available).ToList();
        }
        
        public int GetTotalCargos()
        {
            return CompletedCargos.Count + FailedCargos.Count + ActiveCargos.Count;
        }
        
        public float GetSuccessRate()
        {
            int total = CompletedCargos.Count + FailedCargos.Count;
            if (total == 0) return 0.5f;
            return (float)CompletedCargos.Count / total;
        }
        
        // =========================================================================
        // MÉTODOS DE DEBUG
        // =========================================================================
        
        public void DebugPrintStatus()
        {
            Debug.Log($"=== CARGO MANAGER STATUS ===");
            Debug.Log($"Mercado: {MarketCargos.Count}/{_maxMarketCargos} cargas");
            Debug.Log($"Activas: {ActiveCargos.Count} cargas");
            Debug.Log($"Completadas: {CompletedCargos.Count}");
            Debug.Log($"Fallidas: {FailedCargos.Count}");
            Debug.Log($"Tasa de éxito: {GetSuccessRate():P0}");
        }
    }
}