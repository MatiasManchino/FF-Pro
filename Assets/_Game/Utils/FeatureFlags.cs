namespace FreightForwarder.Utils
{
    // "Feature flags": interruptores para activar o desactivar sistemas nuevos (versión 2)
    // sin romper los que ya funcionan. La idea es migrar de a poco: se pone uno en "true",
    // se prueba que ande bien, y recién después se activa el siguiente. Así, si algo falla,
    // se sabe exactamente qué sistema lo causó.
    public static class FeatureFlags
    {
        public const bool USE_NEGOTIATION_V2   = true;  // motor de negociación nuevo (regateo con clientes)
        public const bool USE_WORLD_STATE      = true;  // estado global del mundo (noticias, contexto)
        public const bool USE_PROGRESSION      = true;  // progresión del jugador (nivel y experiencia)
        public const bool USE_AGENT_BONUS      = true;  // bonificaciones por buena relación con los agentes
        public static bool USE_MASK_VALIDATION = true;  // validar posiciones con la máscara de agua/tierra del mapa
    }
}
