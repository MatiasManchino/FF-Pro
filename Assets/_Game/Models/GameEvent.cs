using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    [Serializable]
    public class GameEvent
    {
        // Identificación
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Tipo y severidad
        public Constants.EventType Type { get; set; }
        public int Severity { get; set; }   // 1 (leve) a 5 (catastrófico)

        // Condiciones de aplicación
        public List<Constants.TransportMode> AffectedTransportModes { get; set; }
        public List<string> AffectedStages { get; set; }       // "origin", "transit", "destination"
        public List<string> AffectedCountries { get; set; }
        public List<string> AffectedCities { get; set; }
        public List<Constants.CargoType> AffectedCargoTypes { get; set; }
        public List<int> AffectedMonths { get; set; }           // 1-12
        public List<int> AffectedDays { get; set; }             // día del mes
        public int? AgentTrustThreshold { get; set; }

        // Probabilidad
        public float BaseProbability { get; set; }

        // Efectos
        public int DaysExtra { get; set; }
        public int MoneyCost { get; set; }
        public int ReputationLoss { get; set; }

        // Opciones
        public bool RequiresChoice { get; set; }
        public List<EventOption> Options { get; set; }

        public GameEvent()
        {
            Options = new List<EventOption>();
        }

        public bool AppliesToCargo(Cargo cargo, string currentStage, int currentMonth,
                                   int currentDay, float agentTrust)
        {
            if (AffectedTransportModes != null && AffectedTransportModes.Count > 0)
                if (!AffectedTransportModes.Contains(cargo.TransportMode)) return false;

            if (AffectedStages != null && AffectedStages.Count > 0)
                if (!AffectedStages.Contains(currentStage)) return false;

            if (AffectedCargoTypes != null && AffectedCargoTypes.Count > 0)
                if (!AffectedCargoTypes.Contains(cargo.CargoType)) return false;

            if (AffectedMonths != null && AffectedMonths.Count > 0)
                if (!AffectedMonths.Contains(currentMonth)) return false;

            if (AffectedDays != null && AffectedDays.Count > 0)
                if (!AffectedDays.Contains(currentDay)) return false;

            if (AgentTrustThreshold.HasValue)
                if (agentTrust >= AgentTrustThreshold.Value) return false;

            return true;
        }

        public float GetAdjustedProbability(float agentTrust, Constants.AgentState agentState)
        {
            float prob = BaseProbability;
            if (AgentTrustThreshold.HasValue && agentTrust < AgentTrustThreshold.Value)
                prob *= 1.5f;
            if (agentState == Constants.AgentState.Overworked) prob *= 1.3f;
            if (agentState == Constants.AgentState.Angry)      prob *= 1.4f;
            return Mathf.Clamp01(prob);
        }
    }

    [Serializable]
    public class EventOption
    {
        public string Label { get; set; }
        public int MoneyCost { get; set; }
        public int DaysExtra { get; set; }
        public int ReputationLoss { get; set; }
        public string ResultDescription { get; set; }

        public EventOption(string label, int moneyCost, int daysExtra, int reputationLoss, string resultDesc)
        {
            Label = label;
            MoneyCost = moneyCost;
            DaysExtra = daysExtra;
            ReputationLoss = reputationLoss;
            ResultDescription = resultDesc;
        }
    }
}
