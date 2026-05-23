using System;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Weather
{
    /// <summary>
    /// Public API for querying weather state. Other systems use this, not WeatherSystem directly.
    /// </summary>
    public class WeatherManager : Singleton<WeatherManager>
    {

        // Returns the cell at a geographic coordinate (safe: returns empty cell if system not ready)
        public WeatherCell GetCellAt(float lat, float lon)
        {
            var grid = WeatherSystem.Instance?.Grid;
            if (grid == null) return new WeatherCell();
            return grid.GetCellAtLatLon(lat, lon);
        }

        // Sample multiple points along a great-circle route and return total weather delay in days
        public float GetRouteWeatherDelay(float originLat, float originLon,
                                          float destLat,   float destLon,
                                          Constants.TransportMode mode,
                                          WeatherConfig config)
        {
            var grid = WeatherSystem.Instance?.Grid;
            if (grid == null || config == null) return 0f;

            const int SAMPLES = 10;
            float totalDelay = 0f;

            Vector3 a = LatLonToDir(originLat, originLon);
            Vector3 b = LatLonToDir(destLat,   destLon);

            for (int i = 0; i <= SAMPLES; i++)
            {
                float t   = (float)i / SAMPLES;
                Vector3 d = Vector3.Slerp(a, b, t).normalized;
                DirToLatLon(d, out float lat, out float lon);

                var cell = grid.GetCellAtLatLon(lat, lon);
                float delay = (cell.cloud   * config.cloudDelayPerCell)
                            + (cell.storm   * config.stormDelayPerCell)
                            + (cell.cyclone * config.cycloneDelayPerCell);
                totalDelay += delay;
            }

            float modeMultiplier = mode switch
            {
                Constants.TransportMode.Air        => config.airWeatherMultiplier,
                Constants.TransportMode.Maritime   => config.maritimeWeatherMultiplier,
                Constants.TransportMode.Multimodal => config.maritimeWeatherMultiplier,
                _                                  => config.landWeatherMultiplier,
            };

            return totalDelay * modeMultiplier;
        }

        private static Vector3 LatLonToDir(float lat, float lon)
        {
            float latR = lat * Mathf.Deg2Rad;
            float lonR = lon * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(latR) * Mathf.Cos(lonR),
                               Mathf.Sin(latR),
                               Mathf.Cos(latR) * Mathf.Sin(lonR));
        }

        private static void DirToLatLon(Vector3 dir, out float lat, out float lon)
        {
            lat = Mathf.Asin(dir.y)                  * Mathf.Rad2Deg;
            lon = Mathf.Atan2(dir.z, dir.x)          * Mathf.Rad2Deg;
        }
    }
}
