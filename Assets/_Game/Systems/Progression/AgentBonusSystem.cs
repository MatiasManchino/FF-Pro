using System.Collections.Generic;
using FreightForwarder.Models;
using UnityEngine;

namespace FreightForwarder.Systems.Progression
{
    /// <summary>
    /// Calcula bonus/penalizaciones de agentes basados en historial, especialización
    /// y experiencia en rutas específicas. No modifica Agent.cs ni AgentManager.cs.
    /// Los managers existentes llaman a AgentBonusSystem.GetModifiers() si USE_AGENT_V2 activo.
    /// </summary>
    public static class AgentBonusSystem
    {
        // ── Registro de rutas por agente ─────────────────────────────────────
        // key: agentId → (routeKey → completionCount)
        private static readonly Dictionary<string, Dictionary<string, int>> _routeHistory
            = new Dictionary<string, Dictionary<string, int>>();

        public static void RecordRoute(string agentId, string originId, string destId)
        {
            if (!_routeHistory.TryGetValue(agentId, out var routes))
            {
                routes = new Dictionary<string, int>();
                _routeHistory[agentId] = routes;
            }
            string key = RouteKey(originId, destId);
            routes[key] = routes.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        public static int GetRouteCount(string agentId, string originId, string destId)
        {
            if (!_routeHistory.TryGetValue(agentId, out var routes)) return 0;
            routes.TryGetValue(RouteKey(originId, destId), out int count);
            return count;
        }

        private static string RouteKey(string o, string d) => $"{o}>{d}";

        // ── Cálculo de modificadores ──────────────────────────────────────────

        public class Modifiers
        {
            public float SpeedBonus      { get; set; } = 1f;   // multiplicador
            public float CostReduction   { get; set; } = 0f;   // 0–0.3 = 0–30% descuento
            public float ReliabilityBonus{ get; set; } = 0f;   // suma directa 0–0.2
            public string Description    { get; set; } = "";
        }

        public static Modifiers GetModifiers(Agent agent, string originId, string destId,
                                              Constants.TransportMode mode)
        {
            var m = new Modifiers();
            var reasons = new System.Text.StringBuilder();

            // Bonus por ruta repetida
            int routeCount = GetRouteCount(agent.Id, originId, destId);
            if (routeCount >= 5)
            {
                m.SpeedBonus      += 0.15f;
                m.CostReduction   += 0.10f;
                m.ReliabilityBonus += 0.08f;
                reasons.Append($"+ruta x{routeCount} ");
            }
            else if (routeCount >= 2)
            {
                m.SpeedBonus    += 0.08f;
                m.CostReduction += 0.05f;
                reasons.Append($"+ruta x{routeCount} ");
            }

            // Bonus por especialización de modo
            if (agent.SupportedModes.Contains(mode) && agent.SupportedModes.Count == 1)
            {
                m.SpeedBonus       += 0.10f;
                m.ReliabilityBonus += 0.05f;
                reasons.Append($"+esp.{mode} ");
            }

            // Bonus por total de entregas (experiencia)
            if (agent.TotalDeliveries >= 50)
            {
                m.SpeedBonus       += 0.05f;
                m.ReliabilityBonus += 0.05f;
                reasons.Append("+veterano ");
            }
            else if (agent.TotalDeliveries >= 20)
            {
                m.SpeedBonus += 0.03f;
                reasons.Append("+experto ");
            }

            // Penalización por personalidad negativa
            switch (agent.Personality)
            {
                case Constants.AgentPersonality.Lazy:
                    m.SpeedBonus      -= 0.15f;
                    m.ReliabilityBonus -= 0.10f;
                    reasons.Append("-lazy ");
                    break;
                case Constants.AgentPersonality.Elusive:
                    m.ReliabilityBonus -= 0.08f;
                    reasons.Append("-elusive ");
                    break;
                case Constants.AgentPersonality.Scammer:
                    m.CostReduction   -= 0.05f;
                    reasons.Append("-scammer ");
                    break;
            }

            // Penalización por carga elevada
            if (agent.CurrentLoad >= agent.MaxCapacity)
            {
                m.SpeedBonus      -= 0.10f;
                m.ReliabilityBonus -= 0.05f;
                reasons.Append("-sobrecarga ");
            }

            // Clampear valores
            m.SpeedBonus       = Mathf.Clamp(m.SpeedBonus, 0.5f, 2.0f);
            m.CostReduction    = Mathf.Clamp(m.CostReduction, -0.1f, 0.30f);
            m.ReliabilityBonus = Mathf.Clamp(m.ReliabilityBonus, -0.20f, 0.25f);
            m.Description      = reasons.ToString().Trim();

            return m;
        }

        // ── Especialización por regiones ──────────────────────────────────────

        public static string GetSpecialization(Agent agent)
        {
            if (agent.TotalDeliveries < 10) return "Novato";

            if (agent.SupportedModes.Count == 1)
            {
                switch (agent.SupportedModes[0])
                {
                    case Constants.TransportMode.Air:      return "Especialista Aéreo";
                    case Constants.TransportMode.Maritime: return "Especialista Marítimo";
                    case Constants.TransportMode.Land:     return "Especialista Terrestre";
                }
            }

            if (agent.TotalDeliveries >= 50) return "Logístico Global";
            if (agent.TotalDeliveries >= 20) return "Operador Experto";
            return "Operador General";
        }
    }
}
