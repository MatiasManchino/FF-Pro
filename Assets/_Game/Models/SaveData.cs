using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    [Serializable]
    public class SaveData
    {
        // Versión para compatibilidad futura
        public int SaveVersion { get; set; } = 1;
        public string SaveDate { get; set; }

        // Estado del jugador
        public int Money { get; set; }
        public int Reputation { get; set; }
        public int Level { get; set; }
        public int CurrentXP { get; set; }
        public int TotalCargosCompleted { get; set; }
        public int TotalCargosFailed { get; set; }
        public int TotalRevenue { get; set; }
        public int TotalCosts { get; set; }

        // Tiempo de juego
        public int CurrentDay { get; set; }
        public string CurrentDateString { get; set; }
        public float ContinuousDays { get; set; }

        // Cargas activas y mercado
        public List<Cargo> MarketCargos { get; set; }
        public List<Cargo> ActiveCargos { get; set; }
        public List<Cargo> CompletedCargos { get; set; }
        public List<Cargo> FailedCargos { get; set; }

        // Agentes
        public List<AgentSaveData> Agents { get; set; }

        // Ciudades desbloqueadas
        public List<string> UnlockedCityIds { get; set; }

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

    [Serializable]
    public class AgentSaveData
    {
        public string AgentId { get; set; }
        public float PlayerTrust { get; set; }
        public float AgentTrust { get; set; }
        public Constants.AgentState CurrentState { get; set; }
        public Constants.AgentRelationship Relationship { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public int AbandonedDeliveries { get; set; }
        public int ConsecutiveDeliveries { get; set; }
        public int DaysUntilReturn { get; set; }
        public float CurrentPriceMultiplier { get; set; }
        public bool IsInPriceSurge { get; set; }
        public int PriceSurgeDaysRemaining { get; set; }

        public AgentSaveData() { }

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
