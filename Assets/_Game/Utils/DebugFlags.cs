namespace FreightForwarder.Utils
{
    public static class DebugFlags
    {
        public static bool DEBUG_MODE = true;

        // Subsystem flags — toggle en inspector via DebugFlagsController
        public static bool LOG_ECONOMY  = true;
        public static bool LOG_CARGO    = true;
        public static bool LOG_AGENTS   = true;
        public static bool LOG_EVENTS   = true;
        public static bool LOG_ROUTES   = true;
        public static bool LOG_WEATHER  = false;
    }
}
