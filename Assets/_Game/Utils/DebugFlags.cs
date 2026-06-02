namespace FreightForwarder.Utils
{
    // Interruptores ("flags") para los mensajes de depuración (logs) que aparecen en la consola.
    // Sirven para prender o apagar los avisos de cada sistema mientras se desarrolla el juego,
    // sin tener que borrar código. true = ese sistema imprime información; false = queda en silencio.
    public static class DebugFlags
    {
        // Interruptor general de depuración. Si está apagado, conviene no imprimir nada.
        public static bool DEBUG_MODE = true;

        // Interruptores por subsistema (se pueden cambiar desde el inspector con DebugFlagsController).
        public static bool LOG_ECONOMY  = true;   // economía: dinero, cobros, costos
        public static bool LOG_CARGO    = true;   // cargas: creación, aceptación, entrega
        public static bool LOG_AGENTS   = true;   // agentes (transportistas)
        public static bool LOG_EVENTS   = true;   // eventos: demoras, tormentas, problemas
        public static bool LOG_ROUTES   = true;   // rutas: cálculo de caminos
        public static bool LOG_WEATHER  = false;  // clima: apagado por defecto (genera demasiados mensajes)
    }
}
