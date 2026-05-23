namespace FreightForwarder.Utils
{
    /// <summary>
    /// Flags para activar sistemas V2 sin romper los sistemas existentes.
    /// Cambiar a true uno por uno para migración incremental.
    /// </summary>
    public static class FeatureFlags
    {
        public const bool USE_ROUTE_GRAPH     = true;
        public const bool USE_NEGOTIATION_V2  = true;
        public const bool USE_WORLD_STATE     = true;
        public const bool USE_PROGRESSION     = true;
        public const bool USE_AGENT_BONUS     = true;
        public const bool USE_MASK_VALIDATION = true;
    }
}
