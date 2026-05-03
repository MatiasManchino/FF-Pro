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
    /// EventManager — Gestiona eventos aleatorios durante el tránsito de cargas.
    /// 
    /// RESPONSABILIDADES:
    /// - Generar eventos contextuales según ubicación, modo de transporte, etapa, fecha
    /// - Ofrecer opciones de respuesta al jugador
    /// - Aplicar consecuencias (retrasos, costos, reputación)
    /// - Mantener historial de eventos por carga
    /// 
    /// DIFERENCIA CLAVE CON DISEÑO SIMPLE:
    /// Los eventos NO son aleatorios puros. Dependen de:
    /// - Ubicación geográfica (país/ciudad)
    /// - Modo de transporte (marítimo/aéreo/terrestre)
    /// - Etapa del viaje (origen/tránsito/destino)
    /// - Fecha calendario (feriados, estaciones)
    /// - Tipo de carga (peligrosa requiere inspecciones)
    /// - Confianza del agente (baja confianza = más eventos)
    /// </summary>
    public class EventManager : Singleton<EventManager>
    {
        [Header("Configuración")]
        [SerializeField] private float _baseEventProbability = Constants.EVENT_BASE_PROBABILITY;
        [SerializeField] private float _eventCheckIntervalDays = 1f;
        
        // =========================================================================
        // ESTADO
        // =========================================================================
        
        /// <summary>
        /// Evento pendiente actual (esperando respuesta del jugador)
        /// </summary>
        public GameEvent PendingEvent { get; private set; }
        
        /// <summary>
        /// Historial de eventos por ID de carga
        /// </summary>
        public Dictionary<string, List<GameEvent>> EventHistory { get; private set; }
        
        /// <summary>
        /// Pool de eventos predefinidos
        /// </summary>
        private List<GameEvent> _eventPool;
        
        // =========================================================================
        // EVENTOS PÚBLICOS
        // =========================================================================
        
        public event Action<GameEvent, Cargo> OnEventTriggered;
        public event Action<GameEvent, Cargo, int> OnEventResolved;  // (evento, carga, opción elegida)
        
        // =========================================================================
        // INICIALIZACIÓN
        // =========================================================================
        
        protected override void OnAwake()
        {
            EventHistory = new Dictionary<string, List<GameEvent>>();
            InitializeEventPool();
            
            // Suscribirse al cambio de día
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += OnDayPassed;
            }
            
            Debug.Log($"[EventManager] Inicializado con {_eventPool?.Count ?? 0} eventos disponibles");
        }
        
        protected override void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= OnDayPassed;
            }
        }
        
        // =========================================================================
        // INICIALIZACIÓN DEL POOL DE EVENTOS
        // =========================================================================
        
        private void InitializeEventPool()
        {
            _eventPool = new List<GameEvent>();
            
            // ==========================================
            // 1. CUSTOMS DELAY — Demora en aduana
            // ==========================================
            var customsDelay = new GameEvent
            {
                Name = "Inspección Aduanera",
                Description = "La aduana seleccionó tu carga para una inspección física. Requieren abrir el contenedor y verificar la mercancía.",
                Type = Constants.EventType.CustomsDelay,
                Severity = 2,
                AffectedStages = new List<string> { "destination" },
                BaseProbability = 0.08f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar inspección", 0, 3, -2),
                    new EventOption("Contratar agente urgente", 600, 1, 0),
                    new EventOption("Gestionar documentación electrónica", 300, 0, 1)
                }
            };
            _eventPool.Add(customsDelay);
            
            // ==========================================
            // 2. PORT CONGESTION — Congestión portuaria
            // ==========================================
            var portCongestion = new GameEvent
            {
                Name = "Congestión Portuaria",
                Description = "El puerto de destino está colapsado. Hay demoras en la descarga.",
                Type = Constants.EventType.PortCongestion,
                Severity = 2,
                AffectedTransportModes = new List<Constants.TransportMode> { Constants.TransportMode.Maritime },
                AffectedStages = new List<string> { "destination" },
                BaseProbability = 0.06f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Pagar demurrage y esperar", 400, 2, -1),
                    new EventOption("Descarga en terminal privada", 1200, 1, 0),
                    new EventOption("Negociar prioridad con operador", 700, 0, 1)
                }
            };
            _eventPool.Add(portCongestion);
            
            // ==========================================
            // 3. WEATHER — Clima adverso
            // ==========================================
            var weather = new GameEvent
            {
                Name = "Tormenta Severa",
                Description = "Condiciones climáticas adversas están afectando las rutas de transporte.",
                Type = Constants.EventType.Weather,
                Severity = 3,
                AffectedStages = new List<string> { "transit" },
                BaseProbability = 0.05f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar mejora climática", 0, 3, -2),
                    new EventOption("Desviar por ruta alternativa", 500, 2, 0),
                    new EventOption("Cambiar a transporte terrestre parcial", 900, 1, 0)
                }
            };
            _eventPool.Add(weather);
            
            // ==========================================
            // 4. DAMAGE — Daño a la mercancía
            // ==========================================
            var damage = new GameEvent
            {
                Name = "Daño en Mercancía",
                Description = "Una inspección revela daños en parte de la carga durante el manejo.",
                Type = Constants.EventType.Damage,
                Severity = 4,
                AffectedStages = new List<string> { "transit", "destination" },
                BaseProbability = 0.04f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Activar seguro de carga", 300, 0, -1, 0.9f, "insurance"),
                    new EventOption("Reparar en taller local", 1000, 2, 0),
                    new EventOption("Entregar con descuento al cliente", 0, 0, -5)
                }
            };
            _eventPool.Add(damage);
            
            // ==========================================
            // 5. STRIKE — Huelga portuaria
            // ==========================================
            var strike = new GameEvent
            {
                Name = "Huelga Portuaria",
                Description = "Los trabajadores portuarios están en paro. Las operaciones están detenidas.",
                Type = Constants.EventType.Strike,
                Severity = 5,
                AffectedTransportModes = new List<Constants.TransportMode> { Constants.TransportMode.Maritime },
                AffectedStages = new List<string> { "origin", "destination" },
                BaseProbability = 0.02f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Desviar a puerto alternativo", 1200, 5, 0),
                    new EventOption("Usar terminal privada", 2000, 2, 0),
                    new EventOption("Esperar fin de huelga", 0, 6, -3)
                }
            };
            _eventPool.Add(strike);
            
            // ==========================================
            // 6. DOCUMENTATION ERROR — Error en documentación
            // ==========================================
            var docError = new GameEvent
            {
                Name = "Error en Documentación",
                Description = "La naviera detecta una discrepancia entre la factura comercial y el conocimiento de embarque.",
                Type = Constants.EventType.DocumentationError,
                Severity = 2,
                AffectedStages = new List<string> { "origin", "destination" },
                BaseProbability = 0.06f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Corregir y reemitir documentos", 150, 1, 0),
                    new EventOption("Pagar multa por corrección exprés", 500, 0, 0),
                    new EventOption("Negociar con naviera", 0, 2, -2)
                }
            };
            _eventPool.Add(docError);
            
            // ==========================================
            // 7. EQUIPMENT SHORTAGE — Escasez de contenedores
            // ==========================================
            var equipmentShortage = new GameEvent
            {
                Name = "Escasez de Contenedores",
                Description = "La naviera no tiene contenedores disponibles en el puerto de origen.",
                Type = Constants.EventType.EquipmentShortage,
                Severity = 3,
                AffectedTransportModes = new List<Constants.TransportMode> { Constants.TransportMode.Maritime },
                AffectedStages = new List<string> { "origin" },
                BaseProbability = 0.05f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar disponibilidad", 0, 3, -1),
                    new EventOption("Usar contenedor de otra naviera", 600, 0, 0),
                    new EventOption("Alquilar contenedor especial", 400, 1, 0)
                }
            };
            _eventPool.Add(equipmentShortage);
            
            // ==========================================
            // 8. ROAD CLOSURE — Corte de ruta
            // ==========================================
            var roadClosure = new GameEvent
            {
                Name = "Corte de Ruta",
                Description = "Manifestaciones bloquean la principal ruta terrestre.",
                Type = Constants.EventType.RoadClosure,
                Severity = 3,
                AffectedTransportModes = new List<Constants.TransportMode> { Constants.TransportMode.Land, Constants.TransportMode.Rail },
                AffectedStages = new List<string> { "transit" },
                BaseProbability = 0.05f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar desbloqueo", 0, 3, -2),
                    new EventOption("Tomar ruta alternativa", 300, 2, 0),
                    new EventOption("Transbordo a ferrocarril", 500, 1, 0)
                }
            };
            _eventPool.Add(roadClosure);
            
            // ==========================================
            // 9. AIRPORT CLOSURE — Cierre de aeropuerto
            // ==========================================
            var airportClosure = new GameEvent
            {
                Name = "Cierre de Aeropuerto",
                Description = "Una nube de ceniza volcánica obliga a cerrar el aeropuerto.",
                Type = Constants.EventType.AirportClosure,
                Severity = 4,
                AffectedTransportModes = new List<Constants.TransportMode> { Constants.TransportMode.Air },
                AffectedStages = new List<string> { "origin", "destination" },
                BaseProbability = 0.03f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar reapertura", 0, 4, -2),
                    new EventOption("Redirigir a aeropuerto alternativo", 800, 2, 0),
                    new EventOption("Cambiar a transporte marítimo", 400, 5, -1)
                }
            };
            _eventPool.Add(airportClosure);
            
            // ==========================================
            // 10. CARGO THEFT — Robo de carga
            // ==========================================
            var cargoTheft = new GameEvent
            {
                Name = "Intento de Robo",
                Description = "El camión fue interceptado, pero la policía recuperó la carga.",
                Type = Constants.EventType.CargoTheft,
                Severity = 5,
                AffectedStages = new List<string> { "transit" },
                BaseProbability = 0.02f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Aceptar retraso", 0, 2, -3),
                    new EventOption("Contratar custodia adicional", 400, 0, 0),
                    new EventOption("Activar seguro contra robo", 200, 0, -1, 0.9f, "insurance")
                }
            };
            _eventPool.Add(cargoTheft);
            
            // ==========================================
            // 11. FUEL SURCHARGE — Aumento de combustible
            // ==========================================
            var fuelSurcharge = new GameEvent
            {
                Name = "Aumento de Combustible",
                Description = "La naviera aplica un sobrecargo por el alza en el precio del combustible.",
                Type = Constants.EventType.FuelSurcharge,
                Severity = 1,
                AffectedStages = new List<string> { "origin", "transit" },
                BaseProbability = 0.08f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Pagar sobrecargo", 300, 0, 0),
                    new EventOption("Negociar reducción", 150, 0, -1),
                    new EventOption("Transferir costo al cliente", 0, 0, -4)
                }
            };
            _eventPool.Add(fuelSurcharge);
            
            // ==========================================
            // 12. CARRIER BANKRUPTCY — Quiebra de transportista
            // ==========================================
            var bankruptcy = new GameEvent
            {
                Name = "Quiebra de Transportista",
                Description = "El transportista asignado entró en quiebra. Tu carga está detenida.",
                Type = Constants.EventType.CarrierBankruptcy,
                Severity = 5,
                AffectedStages = new List<string> { "origin", "transit" },
                BaseProbability = 0.01f,
                AgentTrustThreshold = 30,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Buscar nuevo transportista", 800, 4, -2),
                    new EventOption("Contratar servicio premium", 1500, 2, 0),
                    new EventOption("Reclamar seguro de responsabilidad", 0, 3, -4, 0.7f, "insurance")
                }
            };
            _eventPool.Add(bankruptcy);
            
            // ==========================================
            // 13. WEIGHT MISDECLARATION — Peso mal declarado
            // ==========================================
            var weightMisdeclaration = new GameEvent
            {
                Name = "Discrepancia de Peso",
                Description = "El peso declarado difiere del real. La naviera exige un ajuste.",
                Type = Constants.EventType.WeightMisdeclaration,
                Severity = 2,
                AffectedStages = new List<string> { "origin" },
                BaseProbability = 0.04f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Pagar multa", 400, 0, 0),
                    new EventOption("Rectificar documentación", 200, 1, 0),
                    new EventOption("Argumentar error de báscula", 0, 2, -2)
                }
            };
            _eventPool.Add(weightMisdeclaration);
            
            // ==========================================
            // 14. WAREHOUSE FIRE — Incendio en almacén
            // ==========================================
            var warehouseFire = new GameEvent
            {
                Name = "Incendio en Almacén",
                Description = "Un incendio afecta el depósito donde está almacenada tu carga.",
                Type = Constants.EventType.WarehouseFire,
                Severity = 5,
                AffectedStages = new List<string> { "origin", "destination" },
                BaseProbability = 0.01f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Activar seguro total", 500, 2, -2, 0.95f, "insurance"),
                    new EventOption("Recuperar carga dañada", 0, 3, -6, 0.6f),
                    new EventOption("Negociar compensación con almacén", 300, 0, -3)
                }
            };
            _eventPool.Add(warehouseFire);
            
            // ==========================================
            // 15. QUARANTINE INSPECTION — Inspección fitosanitaria
            // ==========================================
            var quarantine = new GameEvent
            {
                Name = "Inspección Fitosanitaria",
                Description = "Las autoridades exigen una inspección por posible plaga en la carga.",
                Type = Constants.EventType.QuarantineInspection,
                Severity = 3,
                AffectedCargoTypes = new List<Constants.CargoType> { Constants.CargoType.Refrigerated },
                AffectedStages = new List<string> { "destination" },
                BaseProbability = 0.05f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Aceptar inspección", 0, 2, 0),
                    new EventOption("Pagar fumigación exprés", 500, 0, 1),
                    new EventOption("Solicitar análisis urgente", 300, 1, 0)
                }
            };
            _eventPool.Add(quarantine);
            
            // ==========================================
            // 16. FESTIVITY DELAY — Feriado nacional
            // ==========================================
            var festivity = new GameEvent
            {
                Name = "Feriado Nacional",
                Description = "Es feriado nacional no previsto. El puerto y la aduana están cerrados.",
                Type = Constants.EventType.FestivityDelay,
                Severity = 1,
                AffectedStages = new List<string> { "origin", "destination" },
                BaseProbability = 0.03f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar reapertura", 0, 1, 0),
                    new EventOption("Pagar operación especial", 200, 0, 0)
                }
            };
            _eventPool.Add(festivity);
            
            // ==========================================
            // 17. BORDER DELAY — Congestión fronteriza
            // ==========================================
            var borderDelay = new GameEvent
            {
                Name = "Congestión Fronteriza",
                Description = "Largas filas en el paso fronterizo por controles adicionales.",
                Type = Constants.EventType.BorderDelay,
                Severity = 2,
                AffectedTransportModes = new List<Constants.TransportMode> { Constants.TransportMode.Land },
                AffectedStages = new List<string> { "transit" },
                BaseProbability = 0.06f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar turno", 0, 2, -1),
                    new EventOption("Contratar gestor fronterizo", 300, 0, 0),
                    new EventOption("Usar paso alternativo", 500, 1, 0)
                }
            };
            _eventPool.Add(borderDelay);
            
            // ==========================================
            // 18. REJECTED CARGO — Rechazo de carga
            // ==========================================
            var rejectedCargo = new GameEvent
            {
                Name = "Rechazo de Carga",
                Description = "El cliente rechaza la carga por daños en el embalaje.",
                Type = Constants.EventType.RejectedCargo,
                Severity = 4,
                AffectedStages = new List<string> { "destination" },
                BaseProbability = 0.03f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Reembalar y renegociar", 600, 3, -2),
                    new EventOption("Ofrecer descuento", 0, 0, -4),
                    new EventOption("Activar seguro de responsabilidad", 0, 2, -3, 0.7f, "insurance")
                }
            };
            _eventPool.Add(rejectedCargo);
            
            // ==========================================
            // 19. INSURANCE DISPUTE — Disputa con aseguradora
            // ==========================================
            var insuranceDispute = new GameEvent
            {
                Name = "Disputa con Aseguradora",
                Description = "La aseguradora cuestiona la cobertura del siniestro.",
                Type = Constants.EventType.InsuranceDispute,
                Severity = 3,
                BaseProbability = 0.02f,
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Aceptar acuerdo parcial", 0, 0, -1, 0.5f),
                    new EventOption("Contratar perito independiente", 400, 2, 0, 0.8f),
                    new EventOption("Negociar directamente", 200, 0, 1, 0.6f)
                }
            };
            _eventPool.Add(insuranceDispute);
            
            // ==========================================
            // 20. LABOR DAY — Día del Trabajador
            // ==========================================
            var laborDay = new GameEvent
            {
                Name = "Día del Trabajador",
                Description = "Paro general por el Día del Trabajador. Puertos y aduanas cerrados.",
                Type = Constants.EventType.LaborDay,
                Severity = 2,
                AffectedMonths = new List<int> { 5 },
                AffectedDays = new List<int> { 1 },
                AffectedStages = new List<string> { "origin", "destination" },
                BaseProbability = 0.8f, // Muy alta probabilidad en esa fecha
                RequiresChoice = true,
                Options = new List<EventOption>
                {
                    new EventOption("Esperar al día siguiente", 0, 1, 0),
                    new EventOption("Pagar servicio de emergencia", 300, 0, 0)
                }
            };
            _eventPool.Add(laborDay);
        }
        
        // =========================================================================
        // CHECK DE EVENTOS (se llama cada día)
        // =========================================================================
        
        private void OnDayPassed()
        {
            // No procesar si hay un evento pendiente
            if (PendingEvent != null)
                return;
            
            // Obtener todas las cargas activas
            var activeCargos = CargoManager.Instance?.ActiveCargos;
            if (activeCargos == null || activeCargos.Count == 0)
                return;
            
            int currentDay = TimeManager.Instance.CurrentDay;
            int currentMonth = TimeManager.Instance.CurrentDate.Month;
            int currentDayOfMonth = TimeManager.Instance.CurrentDate.Day;
            
            // Verificar eventos para cada carga activa
            foreach (var cargo in activeCargos)
            {
                if (cargo.Status != Constants.CargoStatus.Active)
                    continue;
                
                // Verificar si ocurre un evento
                GameEvent triggeredEvent = CheckForEvent(cargo, currentDay, currentMonth, currentDayOfMonth);
                
                if (triggeredEvent != null)
                {
                    PendingEvent = triggeredEvent;
                    OnEventTriggered?.Invoke(triggeredEvent, cargo);
                    Debug.Log($"[EventManager] Evento '{triggeredEvent.Name}' activado para carga {cargo.Id}");
                    break; // Solo un evento a la vez
                }
            }
        }
        
        /// <summary>
        /// Verifica si ocurre un evento para una carga específica.
        /// </summary>
        private GameEvent CheckForEvent(Cargo cargo, int currentDay, int currentMonth, int currentDayOfMonth)
        {
            // Obtener confianza del agente
            float agentTrust = 50f;
            if (AgentManager.Instance != null && !string.IsNullOrEmpty(cargo.AgentId))
            {
                var agent = AgentManager.Instance.GetAgent(cargo.AgentId);
                if (agent != null)
                    agentTrust = agent.PlayerTrust;
            }
            
            // Obtener país de origen/destino (ciudades → países)
            var cityCountryMap = GetCityCountryMap();
            
            // Determinar etapa actual
            string currentStage = DetermineCurrentStage(cargo, currentDay);
            
            // Filtrar eventos que aplican a esta carga
            var applicableEvents = _eventPool.Where(e => e.AppliesToCargo(cargo, currentStage, currentMonth, currentDayOfMonth, agentTrust, cityCountryMap)).ToList();
            
            if (applicableEvents.Count == 0)
                return null;
            
            // Calcular probabilidad para cada evento aplicable
            foreach (var evt in applicableEvents)
            {
                float probability = evt.GetFinalProbability(cargo, agentTrust);
                
                // Ajustar por modificadores externos (noticias, etc.)
                probability *= GetExternalEventModifier(cargo);
                
                // Ajustar por reputación del agente (de AgentManager)
                if (AgentManager.Instance != null)
                {
                    probability *= AgentManager.Instance.GetEventRiskModifier(cargo.AgentId);
                }
                
                if (UnityEngine.Random.value < probability)
                {
                    return evt;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Determina la etapa actual de la carga.
        /// </summary>
        private string DetermineCurrentStage(Cargo cargo, int currentDay)
        {
            if (cargo.StartDay == 0)
                return "origin";
            
            int daysSinceStart = currentDay - cargo.StartDay;
            int totalDays = cargo.TotalTransitDays;
            
            if (daysSinceStart <= 1)
                return "origin";
            else if (daysSinceStart >= totalDays - 1)
                return "destination";
            else
                return "transit";
        }
        
        /// <summary>
        /// Obtiene modificadores externos (noticias, eventos mundiales)
        /// </summary>
        private float GetExternalEventModifier(Cargo cargo)
        {
            float modifier = 1.0f;
            
            // Aquí se pueden aplicar modificadores por noticias, etc.
            // Por ahora, retornamos 1.0
            
            return modifier;
        }
        
        /// <summary>
        /// Obtiene mapa de ciudad → país (desde WorldCity)
        /// </summary>
        private Dictionary<string, string> GetCityCountryMap()
        {
            var map = new Dictionary<string, string>();
            foreach (var city in CityDatabase.AllCities.Values)
            {
                map[city.Id] = city.Country;
            }
            return map;
        }
        
        // =========================================================================
        // RESOLUCIÓN DE EVENTOS
        // =========================================================================
        
        /// <summary>
        /// Resuelve el evento pendiente con la opción elegida por el jugador.
        /// </summary>
        /// <param name="optionIndex">Índice de la opción elegida</param>
        /// <param name="cargo">Carga afectada</param>
        public void ResolveEvent(int optionIndex, Cargo cargo)
        {
            if (PendingEvent == null || cargo == null)
                return;
            
            if (optionIndex < 0 || optionIndex >= PendingEvent.Options.Count)
                return;
            
            var selectedOption = PendingEvent.Options[optionIndex];
            
            // Verificar si el jugador tiene el feature requerido
            if (!string.IsNullOrEmpty(selectedOption.RequiredFeature))
            {
                bool hasFeature = CheckFeatureUnlocked(selectedOption.RequiredFeature);
                if (!hasFeature)
                {
                    Debug.Log($"[EventManager] Opción '{selectedOption.Text}' requiere {selectedOption.RequiredFeature}. No disponible.");
                    return;
                }
            }
            
            // Verificar éxito de la opción (si no es 100%)
            bool isSuccessful = UnityEngine.Random.value < selectedOption.SuccessChance;
            
            if (!isSuccessful)
            {
                Debug.Log($"[EventManager] La opción '{selectedOption.Text}' falló. Aplicando consecuencias alternativas.");
                // Aplicar consecuencias de fallo (pueden ser peores)
                selectedOption = GetFallbackOption(selectedOption);
            }
            
            // Aplicar consecuencias
            ApplyEventConsequences(selectedOption, cargo);
            
            // Registrar en historial
            if (!EventHistory.ContainsKey(cargo.Id))
                EventHistory[cargo.Id] = new List<GameEvent>();
            EventHistory[cargo.Id].Add(PendingEvent);
            
            // Notificar resolución
            OnEventResolved?.Invoke(PendingEvent, cargo, optionIndex);
            
            // Limpiar evento pendiente
            PendingEvent = null;
            
            Debug.Log($"[EventManager] Evento resuelto con opción {optionIndex}: {selectedOption.Text}");
        }
        
        /// <summary>
        /// Aplica las consecuencias de la opción elegida.
        /// </summary>
        private void ApplyEventConsequences(EventOption option, Cargo cargo)
        {
            // Aplicar costo económico
            if (option.Cost > 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.SubtractMoney(option.Cost, $"Evento: {PendingEvent?.Name}");
                }
            }
            
            // Aplicar retraso
            if (option.DaysExtra > 0 && cargo != null)
            {
                cargo.DaysRemaining += option.DaysExtra;
                cargo.EstimatedArrivalDay += option.DaysExtra;
                
                // Registrar evento en la carga
                cargo.EventsEncountered.Add($"{PendingEvent?.Name}: +{option.DaysExtra} días");
            }
            
            // Aplicar impacto en reputación
            if (option.ReputationImpact != 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddReputation(option.ReputationImpact);
            }
            
            // Aplicar impacto en relación con el cliente
            if (cargo != null && ClientManager.Instance != null)
            {
                var client = ClientManager.Instance.GetClientByName(cargo.ClientName);
                if (client != null)
                {
                    if (option.ReputationImpact < 0)
                        client.RecordComplaint();
                    else if (option.ReputationImpact > 5)
                        client.RelationshipLevel = Mathf.Min(100, client.RelationshipLevel + 5);
                }
            }
        }
        
        /// <summary>
        /// Obtiene una opción de respaldo si la elegida falla.
        /// </summary>
        private EventOption GetFallbackOption(EventOption original)
        {
            // Opción por defecto más conservadora
            return new EventOption("Aceptar consecuencias", original.Cost * 2, original.DaysExtra * 2, original.ReputationImpact * 2);
        }
        
        /// <summary>
        /// Verifica si el jugador tiene un feature desbloqueado.
        /// </summary>
        private bool CheckFeatureUnlocked(string feature)
        {
            // Por ahora, simular que el seguro está disponible
            // En el futuro, conectar con GameManager o EconomyManager
            return feature != "insurance" || true;
        }
        
        // =========================================================================
        // MÉTODOS DE CONSULTA
        // =========================================================================
        
        /// <summary>
        /// Verifica si hay un evento pendiente.
        /// </summary>
        public bool HasPendingEvent => PendingEvent != null;
        
        /// <summary>
        /// Obtiene el historial de eventos para una carga.
        /// </summary>
        public List<GameEvent> GetEventHistory(string cargoId)
        {
            return EventHistory.ContainsKey(cargoId) 
    ? EventHistory[cargoId] 
    : new List<GameEvent>();
        }
        
        /// <summary>
        /// Fuerza un evento para testing.
        /// </summary>
        public void ForceEvent(string eventName, Cargo cargo)
        {
            var evt = _eventPool.FirstOrDefault(e => e.Name == eventName);
            if (evt != null)
            {
                PendingEvent = evt;
                OnEventTriggered?.Invoke(evt, cargo);
                Debug.Log($"[EventManager] Evento forzado: {eventName}");
            }
        }
        
        /// <summary>
        /// Debug: muestra estadísticas de eventos.
        /// </summary>
        public void DebugPrintStats()
        {
            Debug.Log($"=== EVENT MANAGER STATS ===");
            Debug.Log($"Eventos disponibles: {_eventPool.Count}");
            Debug.Log($"Evento pendiente: {(PendingEvent != null ? PendingEvent.Name : "Ninguno")}");
            Debug.Log($"Historial de eventos: {EventHistory.Sum(h => h.Value.Count)} eventos registrados");
        }
    }
}