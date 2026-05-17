using UnityEngine;

namespace FreightForwarder.Weather
{
    [CreateAssetMenu(menuName = "Freight Forwarder/Weather Config", fileName = "WeatherConfig")]
    public class WeatherConfig : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth  = 64;
        public int gridHeight = 32;

        [Header("Cloud simulation")]
        public float cloudGrowthSpeed = 0.08f;
        public float noiseScale       = 0.18f;
        public float noiseTimeSpeed   = 0.004f;

        [Header("Storm thresholds")]
        [Range(0f, 1f)] public float cloudThresholdForStorm  = 0.62f;
        [Range(0f, 1f)] public float stormThresholdForCyclone = 0.70f;
        public float stormChancePerUpdate  = 0.012f;
        public float cycloneChancePerStorm = 0.12f;
        public float stormDecaySpeed       = 0.015f;
        public float cycloneDecaySpeed     = 0.007f;

        [Header("Route delay (days)")]
        public float cloudDelayPerCell   = 0.05f;
        public float stormDelayPerCell   = 0.40f;
        public float cycloneDelayPerCell = 0.90f;

        [Header("Timing")]
        public float updateIntervalSeconds = 4f;

        [Header("Transport mode modifiers")]
        [Tooltip("Air routes take full weather penalty")]
        public float airWeatherMultiplier      = 1.0f;
        [Tooltip("Maritime routes take moderate penalty")]
        public float maritimeWeatherMultiplier = 0.7f;
        [Tooltip("Land/rail routes take small penalty")]
        public float landWeatherMultiplier     = 0.3f;
    }
}
