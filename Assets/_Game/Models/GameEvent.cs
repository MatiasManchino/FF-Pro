using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    // Un "evento" que le puede pasar a una carga durante el viaje: una demora en aduana, una
    // tormenta, un robo, una huelga, etc. Cada evento define A QUÉ cargas puede afectar
    // (condiciones), QUÉ probabilidad tiene y QUÉ consecuencias trae (días extra, costo, reputación).
    // [Serializable] permite guardarlo y verlo en el inspector de Unity.
    [Serializable]
    public class GameEvent
    {
        public string Id { get; set; }           // identificador único del evento
        public string Name { get; set; }         // nombre corto (para mostrar)
        public string Description { get; set; }  // descripción de lo que pasó

        // Tipo de evento y qué tan grave es.
        public Constants.EventType Type { get; set; }
        public int Severity { get; set; }   // de 1 (leve) a 5 (catastrófico)

        // ── Condiciones: el evento sólo se aplica si la carga cumple TODAS estas (las que estén definidas) ──
        public List<Constants.TransportMode> AffectedTransportModes { get; set; }  // modos de transporte afectados
        public List<string> AffectedStages { get; set; }       // etapas: "origin", "transit", "destination"
        public List<string> AffectedCountries { get; set; }    // países afectados
        public List<string> AffectedCities { get; set; }       // ciudades afectadas
        public List<Constants.CargoType> AffectedCargoTypes { get; set; }  // tipos de carga afectados
        public List<int> AffectedMonths { get; set; }          // meses afectados (1 a 12)
        public List<int> AffectedDays { get; set; }            // días del mes afectados
        public int? AgentTrustThreshold { get; set; }          // sólo aplica si la confianza del agente es MENOR a esto

        // Probabilidad base de que ocurra (0..1), antes de ajustes.
        public float BaseProbability { get; set; }

        // ── Consecuencias del evento ──
        public int DaysExtra { get; set; }        // días de demora que agrega
        public int MoneyCost { get; set; }        // costo en dinero
        public int ReputationLoss { get; set; }   // reputación que hace perder

        // Si el evento le da a elegir al jugador qué hacer (cada opción tiene su propio costo).
        public bool RequiresChoice { get; set; }
        public List<EventOption> Options { get; set; }

        // Constructor: arranca con la lista de opciones vacía.
        public GameEvent()
        {
            Options = new List<EventOption>();
        }

        // Decide si este evento PUEDE afectar a una carga dada, en el momento actual.
        // Va chequeando cada condición: si alguna no se cumple, devuelve false (no aplica).
        // Si todas las condiciones definidas se cumplen, devuelve true.
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

            // Si hay umbral de confianza, el evento sólo aplica cuando la confianza es BAJA
            // (un agente confiable evita el problema).
            if (AgentTrustThreshold.HasValue)
                if (agentTrust >= AgentTrustThreshold.Value) return false;

            return true;
        }

        // Calcula la probabilidad final del evento, ajustando la base según el contexto:
        // sube si la confianza del agente es baja, o si el agente está sobrecargado o enojado.
        // El resultado se limita al rango 0..1.
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

    // Una opción que el jugador puede elegir cuando un evento le pide decidir (RequiresChoice).
    // Cada opción tiene su propio costo en dinero, días y reputación, y un texto del resultado.
    [Serializable]
    public class EventOption
    {
        public string Label { get; set; }              // texto del botón (ej. "Pagar el soborno")
        public int MoneyCost { get; set; }             // cuánto dinero cuesta esta opción
        public int DaysExtra { get; set; }             // cuántos días de demora agrega
        public int ReputationLoss { get; set; }        // cuánta reputación hace perder
        public string ResultDescription { get; set; }  // qué pasó al elegir esta opción

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
