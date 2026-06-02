using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    // "Foto" de toda la partida para guardarla en disco y poder retomarla después.
    // Junta el dinero, la reputación, el progreso, las cargas y los agentes en un solo objeto
    // que se convierte a texto (JSON) al guardar. [Serializable] habilita esa conversión.
    [Serializable]
    public class SaveData
    {
        public int SaveVersion { get; set; } = 1;   // versión del formato (para futuras compatibilidades)
        public string SaveDate { get; set; }        // fecha real en que se guardó

        // ── Estado del jugador ──
        public int Money { get; set; }                 // dinero actual
        public int Reputation { get; set; }            // reputación actual
        public int Level { get; set; }                 // nivel del jugador
        public int CurrentXP { get; set; }             // experiencia acumulada
        public int TotalCargosCompleted { get; set; }  // total de cargas entregadas
        public int TotalCargosFailed { get; set; }     // total de cargas fallidas
        public int TotalRevenue { get; set; }          // ingresos totales
        public int TotalCosts { get; set; }            // costos totales

        // ── Estado del tiempo ──
        public int CurrentDay { get; set; }            // día de juego actual
        public string CurrentDateString { get; set; }  // fecha de juego en texto
        public float ContinuousDays { get; set; }      // días transcurridos con decimales (tiempo continuo)

        // ── Cargas, separadas por su situación ──
        public List<Cargo> MarketCargos { get; set; }     // disponibles en el mercado
        public List<Cargo> ActiveCargos { get; set; }     // aceptadas y en tránsito
        public List<Cargo> CompletedCargos { get; set; }  // entregadas
        public List<Cargo> FailedCargos { get; set; }     // fallidas

        // ── Agentes (transportistas) ──
        public List<AgentSaveData> Agents { get; set; }

        // Ciudades que el jugador ya desbloqueó.
        public List<string> UnlockedCityIds { get; set; }

        // Constructor: pone la fecha de guardado y crea las listas vacías para que no sean nulas.
        public SaveData()
        {
            SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            MarketCargos = new List<Cargo>();
            ActiveCargos = new List<Cargo>();
            CompletedCargos = new List<Cargo>();
            FailedCargos = new List<Cargo>();
            Agents = new List<AgentSaveData>();
            UnlockedCityIds = new List<string>();
        }
    }

    // Versión "guardable" de un Agente: copia sólo los datos que cambian durante la partida
    // (confianza, relación, estadísticas de entregas, etc.), no su definición fija.
    [Serializable]
    public class AgentSaveData
    {
        public string AgentId { get; set; }                            // identificador del agente
        public float PlayerTrust { get; set; }                         // cuánto confía el jugador en él
        public float AgentTrust { get; set; }                          // cuánto confía el agente en el jugador
        public Constants.AgentState CurrentState { get; set; }         // estado (disponible, estresado, enojado…)
        public Constants.AgentRelationship Relationship { get; set; }  // nivel de relación (enemigo…socio)
        public int TotalDeliveries { get; set; }                       // entregas totales encargadas
        public int SuccessfulDeliveries { get; set; }                  // entregas exitosas
        public int FailedDeliveries { get; set; }                      // entregas fallidas
        public int AbandonedDeliveries { get; set; }                   // entregas abandonadas
        public int ConsecutiveDeliveries { get; set; }                 // entregas exitosas seguidas (racha)
        public int DaysUntilReturn { get; set; }                       // días hasta que vuelve (si desapareció)
        public float CurrentPriceMultiplier { get; set; }              // multiplicador de precio actual
        public bool IsInPriceSurge { get; set; }                       // está en un "pico" de precios caros
        public int PriceSurgeDaysRemaining { get; set; }               // días que le quedan a ese pico

        // Constructor vacío: necesario para cargar desde disco.
        public AgentSaveData() { }

        // Constructor que copia el estado actual de un agente vivo hacia este objeto guardable.
        public AgentSaveData(Agent agent)
        {
            AgentId = agent.Id;
            PlayerTrust = agent.PlayerTrust;
            AgentTrust = agent.AgentTrust;
            CurrentState = agent.CurrentState;
            Relationship = agent.Relationship;
            TotalDeliveries = agent.TotalDeliveries;
            SuccessfulDeliveries = agent.SuccessfulDeliveries;
            FailedDeliveries = agent.FailedDeliveries;
            AbandonedDeliveries = agent.AbandonedDeliveries;
            ConsecutiveDeliveries = agent.ConsecutiveDeliveries;
            DaysUntilReturn = agent.DaysUntilReturn;
            CurrentPriceMultiplier = agent.CurrentPriceMultiplier;
            IsInPriceSurge = agent.IsInPriceSurge;
            PriceSurgeDaysRemaining = agent.PriceSurgeDaysRemaining;
        }

        // Vuelca estos datos guardados de nuevo sobre un agente vivo (al cargar la partida).
        public void ApplyTo(Agent agent)
        {
            agent.PlayerTrust = PlayerTrust;
            agent.AgentTrust = AgentTrust;
            agent.CurrentState = CurrentState;
            agent.Relationship = Relationship;
            agent.TotalDeliveries = TotalDeliveries;
            agent.SuccessfulDeliveries = SuccessfulDeliveries;
            agent.FailedDeliveries = FailedDeliveries;
            agent.AbandonedDeliveries = AbandonedDeliveries;
            agent.ConsecutiveDeliveries = ConsecutiveDeliveries;
            agent.DaysUntilReturn = DaysUntilReturn;
            agent.CurrentPriceMultiplier = CurrentPriceMultiplier;
            agent.IsInPriceSurge = IsInPriceSurge;
            agent.PriceSurgeDaysRemaining = PriceSurgeDaysRemaining;
        }
    }
}
