namespace FreightForwarder.Utils
{
    /// <summary>
    /// Flags para activar sistemas V2 sin romper los sistemas existentes.
    /// Cambiar a true uno por uno para migración incremental.
    /// </summary>
    public static class FeatureFlags
    {
        // FASE 5: RouteGraph con Dijkstra en lugar de Haversine directo
        public static bool USE_ROUTE_GRAPH = true;

        // FASE 6: NegotiationEngine V2 en lugar de ClientManager.EvaluateQuote
        public static bool USE_NEGOTIATION_V2 = true;

        // FASE 7: WorldStateManager activo (fuel/demand/risk multipliers)
        public static bool USE_WORLD_STATE = true;

        // FASE 8: ProgressionManager activo (tier system + city unlocks)
        public static bool USE_PROGRESSION = true;

        // FASE 9: AgentBonusSystem activo (route history + specialization)
        public static bool USE_AGENT_BONUS = true;
    }
}
