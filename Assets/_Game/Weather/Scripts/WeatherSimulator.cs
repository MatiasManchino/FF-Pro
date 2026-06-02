using FreightForwarder.Models;
using UnityEngine;

namespace FreightForwarder.Weather
{
    // Stateless helpers: update cloud, storm, and cyclone values on each tick.
    public static class WeatherSimulator
    {
// Actualiza clouds
        public static void UpdateClouds(WeatherCell cell, int x, int y, float timeOffset, WeatherConfig cfg)
        {
            float nx = (x + cell.noiseSeed + timeOffset) * cfg.noiseScale;
            float ny = (y + cell.noiseSeed + timeOffset) * cfg.noiseScale;
            float target = Mathf.PerlinNoise(nx, ny);
            cell.cloud = Mathf.Lerp(cell.cloud, target, cfg.cloudGrowthSpeed);
        }

// Actualiza storms
        public static void UpdateStorms(WeatherCell cell, WeatherConfig cfg)
        {
            if (!cell.isStorming && cell.cloud > cfg.cloudThresholdForStorm)
            {
                if (Random.value < cfg.stormChancePerUpdate)
                {
                    cell.isStorming = true;
                    cell.storm = 1f;
                }
            }

            if (cell.isStorming)
            {
                cell.storm = Mathf.MoveTowards(cell.storm, 0f, cfg.stormDecaySpeed);
                if (cell.storm <= 0.01f)
                {
                    cell.storm     = 0f;
                    cell.isStorming = false;
                    cell.isCyclone  = false;
                    cell.cyclone    = 0f;
                }
            }
        }

// Actualiza cyclones
        public static void UpdateCyclones(WeatherCell cell, WeatherConfig cfg)
        {
            if (cell.isStorming && !cell.isCyclone && cell.storm > cfg.stormThresholdForCyclone)
            {
                if (Random.value < cfg.cycloneChancePerStorm)
                {
                    cell.isCyclone = true;
                    cell.cyclone   = 1f;
                }
            }

            if (cell.isCyclone)
            {
                cell.cyclone = Mathf.MoveTowards(cell.cyclone, 0f, cfg.cycloneDecaySpeed);
                if (cell.cyclone <= 0.01f)
                {
                    cell.cyclone   = 0f;
                    cell.isCyclone = false;
                }
            }
        }
    }
}