using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class EventManager : Singleton<EventManager>
    {
        private List<GameEvent> _eventPool;

        public event Action<GameEvent, Cargo> OnEventTriggered;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            _eventPool = new List<GameEvent>();
            InitializeEventPool();
        }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += ProcessDailyEvents;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= ProcessDailyEvents;
        }

        // ═══════════════════════════════════
        // POOL DE EVENTOS
        // Inicializa ialize evento pool.

        private void InitializeEventPool()
        {
            Add(Constants.EventType.CustomsDelay, "Demora Aduanera",
                "La aduana retrasa el despacho de la carga.", 2,
                daysExtra: 3, cost: 200, repLoss: 2, prob: 0.08f);

            Add(Constants.EventType.PortCongestion, "Congestión Portuaria",
                "El puerto está saturado y no hay espacio para atracar.", 2,
                daysExtra: 4, cost: 150, repLoss: 1, prob: 0.07f,
                modes: new[] { Constants.TransportMode.Maritime });

            Add(Constants.EventType.Weather, "Clima Adverso",
                "Condiciones climáticas severas interrumpen el tránsito.", 3,
                daysExtra: 5, cost: 0, repLoss: 2, prob: 0.06f);

            Add(Constants.EventType.Damage, "Daño a la Mercancía",
                "Parte de la carga sufrió daños durante el transporte.", 4,
                daysExtra: 0, cost: 500, repLoss: 10, prob: 0.04f);

            Add(Constants.EventType.Strike, "Huelga de Trabajadores",
                "Los trabajadores del sector están en huelga.", 3,
                daysExtra: 7, cost: 300, repLoss: 5, prob: 0.04f);

            Add(Constants.EventType.DocumentationError, "Error en Documentación",
                "Los papeles de la carga tienen errores que deben corregirse.", 2,
                daysExtra: 2, cost: 100, repLoss: 1, prob: 0.09f);

            Add(Constants.EventType.EquipmentShortage, "Falta de Contenedores",
                "No hay contenedores disponibles en este momento.", 2,
                daysExtra: 3, cost: 0, repLoss: 2, prob: 0.06f,
                modes: new[] { Constants.TransportMode.Maritime });

            Add(Constants.EventType.RoadClosure, "Ruta Cerrada",
                "La ruta principal está cortada por obras o accidente.", 2,
                daysExtra: 2, cost: 0, repLoss: 1, prob: 0.05f,
                modes: new[] { Constants.TransportMode.Land, Constants.TransportMode.Rail });

            Add(Constants.EventType.AirportClosure, "Aeropuerto Cerrado",
                "El aeropuerto está temporalmente cerrado.", 3,
                daysExtra: 2, cost: 400, repLoss: 5, prob: 0.03f,
                modes: new[] { Constants.TransportMode.Air });

            Add(Constants.EventType.CargoTheft, "Robo de Carga",
                "Parte de la carga fue robada durante el tránsito.", 5,
                daysExtra: 0, cost: 1000, repLoss: 15, prob: 0.02f,
                trustThreshold: 40);

            Add(Constants.EventType.FuelSurcharge, "Sobrecosto de Combustible",
                "Los precios del combustible aumentaron inesperadamente.", 2,
                daysExtra: 0, cost: 250, repLoss: 0, prob: 0.08f);

            Add(Constants.EventType.CarrierBankruptcy, "Quiebra del Transportista",
                "El transportista declaró quiebra y no puede completar la entrega.", 5,
                daysExtra: 10, cost: 800, repLoss: 20, prob: 0.01f,
                trustThreshold: 30);

            Add(Constants.EventType.WeightMisdeclaration, "Peso Mal Declarado",
                "El peso real de la carga no coincide con lo declarado.", 3,
                daysExtra: 1, cost: 300, repLoss: 5, prob: 0.05f);

            Add(Constants.EventType.WarehouseFire, "Incendio en Almacén",
                "Un incendio en el almacén dañó parte de la mercancía.", 5,
                daysExtra: 5, cost: 2000, repLoss: 25, prob: 0.01f);

            Add(Constants.EventType.QuarantineInspection, "Inspección Fitosanitaria",
                "La carga está retenida para inspección sanitaria.", 3,
                daysExtra: 4, cost: 200, repLoss: 3, prob: 0.04f,
                types: new[] { Constants.CargoType.Refrigerated });

            Add(Constants.EventType.FestivityDelay, "Feriado No Laborable",
                "El destino celebra un feriado y las operaciones están suspendidas.", 1,
                daysExtra: 2, cost: 0, repLoss: 0, prob: 0.05f,
                months: new[] { 12, 1 });

            Add(Constants.EventType.BorderDelay, "Demora en Frontera",
                "El cruce de frontera presenta demoras por controles reforzados.", 2,
                daysExtra: 3, cost: 100, repLoss: 2, prob: 0.06f,
                modes: new[] { Constants.TransportMode.Land, Constants.TransportMode.Rail });

            Add(Constants.EventType.RejectedCargo, "Carga Rechazada",
                "El destino rechaza recibir la carga por incumplimiento.", 4,
                daysExtra: 0, cost: 500, repLoss: 15, prob: 0.02f);

            Add(Constants.EventType.InsuranceDispute, "Disputa con Seguro",
                "La aseguradora disputa la cobertura del siniestro.", 3,
                daysExtra: 0, cost: 300, repLoss: 5, prob: 0.03f);

            Add(Constants.EventType.LaborDay, "Día del Trabajador",
                "Paro general por el día del trabajador.", 1,
                daysExtra: 1, cost: 0, repLoss: 0, prob: 0.10f,
                months: new[] { 5 }, days: new[] { 1 });
        }

        private void Add(Constants.EventType type, string name, string description, int severity,
                         int daysExtra, int cost, int repLoss, float prob,
                         Constants.TransportMode[] modes = null,
                         Constants.CargoType[] types = null,
                         int[] months = null, int[] days = null,
                         int? trustThreshold = null)
        {
            var evt = new GameEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Name = name,
                Description = description,
                Severity = severity,
                DaysExtra = daysExtra,
                MoneyCost = cost,
                ReputationLoss = repLoss,
                BaseProbability = prob
            };

            if (modes  != null) evt.AffectedTransportModes = new List<Constants.TransportMode>(modes);
            if (types  != null) evt.AffectedCargoTypes     = new List<Constants.CargoType>(types);
            if (months != null) evt.AffectedMonths          = new List<int>(months);
            if (days   != null) evt.AffectedDays            = new List<int>(days);
            if (trustThreshold.HasValue) evt.AgentTrustThreshold = trustThreshold;

            _eventPool.Add(evt);
        }

        // ═══════════════════════════════════
        // PROCESAMIENTO DIARIO
        // ═══════════════════════════════════

        // Probabilidad base de que ocurra ALGÚN evento en un día dado
        private const float DAILY_EVENT_CHANCE = 0.12f;
        private readonly List<GameEvent> _applicable = new List<GameEvent>();

// Gestiona process diario eventos.
        private void ProcessDailyEvents()
        {
            if (CargoManager.Instance == null) return;
            int currentDay        = FFTimeManager.Instance?.CurrentDay ?? 1;
            int currentMonth      = FFTimeManager.Instance?.CurrentDate.Month ?? 1;
            int currentDayOfMonth = FFTimeManager.Instance?.CurrentDate.Day ?? 1;

// Foreach
            foreach (var cargo in CargoManager.Instance.ActiveCargos)
            {
                // Límite: 1 evento por cada 4 días de tránsito original
                int maxEvents = Mathf.Max(1, cargo.TotalTransitDays / 4);
                if (cargo.EventsEncountered.Count >= maxEvents) continue;

                // Roll único: 12% de chance diaria de que algo ocurra
                if (UnityEngine.Random.value > DAILY_EVENT_CHANCE) continue;

                Agent agent = AgentManager.Instance?.GetAgent(cargo.AgentId);
                float agentTrust = agent?.PlayerTrust ?? 50f;
                Constants.AgentState agentState = agent?.CurrentState ?? Constants.AgentState.Idle;

                string stage = GetCurrentStage(cargo);

                // Reunir eventos aplicables y elegir uno al azar
                _applicable.Clear();
// Foreach
                foreach (var evt in _eventPool)
                {
                    if (evt.AppliesToCargo(cargo, stage, currentMonth, currentDayOfMonth, agentTrust))
                        _applicable.Add(evt);
                }
                if (_applicable.Count == 0) continue;

                var chosen = _applicable[UnityEngine.Random.Range(0, _applicable.Count)];
                ApplyEvent(chosen, cargo, currentDay);
            }
        }

// Obtiene actual stage
        private string GetCurrentStage(Cargo cargo)
        {
            if (cargo.TotalTransitDays <= 0) return "transit";
            // Usa días transcurridos reales para que los eventos no distorsionen la etapa
            int elapsed = (FFTimeManager.Instance?.CurrentDay ?? cargo.StartDay) - cargo.StartDay;
            float progress = Mathf.Clamp01((float)elapsed / cargo.TotalTransitDays);
            if (progress < 0.15f) return "origin";
            if (progress > 0.85f) return "destination";
            return "transit";
        }

// Aplica evento
        private void ApplyEvent(GameEvent evt, Cargo cargo, int currentDay)
        {
            cargo.EventsEncountered.Add(evt.Id);
            cargo.DaysRemaining += evt.DaysExtra;

            if (evt.MoneyCost > 0)
                EconomyManager.Instance?.SubtractMoney(evt.MoneyCost, $"Evento: {evt.Name}");

            if (evt.ReputationLoss > 0)
                EconomyManager.Instance?.AddReputation(-evt.ReputationLoss);

            OnEventTriggered?.Invoke(evt, cargo);
            Debug.Log($"[EventManager] Evento '{evt.Name}' en carga {cargo.Id} — +{evt.DaysExtra}d, -${evt.MoneyCost}");
        }
    }
}