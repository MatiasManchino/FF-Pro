using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Weather
{
    /// <summary>
    /// Applies weather delays to cargo.
    /// Initial delay: on cargo accepted.
    /// Ongoing delay: once per game day if a cyclone sits on the route.
    /// </summary>
    public class WeatherImpact : Singleton<WeatherImpact>
    {
        private WeatherGrid   _grid;
        private WeatherConfig _config;

        public void Initialize(WeatherGrid grid, WeatherConfig config)
        {
            _grid   = grid;
            _config = config;

            if (CargoManager.Instance != null)
                CargoManager.Instance.OnCargoAccepted += OnCargoAccepted;

            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (CargoManager.Instance != null)
                CargoManager.Instance.OnCargoAccepted -= OnCargoAccepted;
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
        }

        private void OnCargoAccepted(Cargo cargo)
        {
            int delay = CalculateDelayDays(cargo);
            if (delay <= 0) return;

            cargo.DaysRemaining    += delay;
            cargo.TotalTransitDays += delay;
            Debug.Log($"[WeatherImpact] Carga {cargo.Id}: +{delay}d por clima al inicio");
        }

        // Once per game day: add 1 extra day only if a cyclone directly affects the route
        private void OnDayPassed()
        {
            if (CargoManager.Instance == null || _grid == null) return;
            foreach (var cargo in CargoManager.Instance.ActiveCargos)
            {
                if (RouteHasCyclone(cargo))
                {
                    cargo.DaysRemaining    += 1;
                    cargo.TotalTransitDays += 1;
                    Debug.Log($"[WeatherImpact] Ciclón activo: carga {cargo.Id} +1d");
                }
            }
        }

        private bool RouteHasCyclone(Cargo cargo)
        {
            if (_grid == null) return false;
            var origin = CityDatabase.GetCity(cargo.OriginCityId);
            var dest   = CityDatabase.GetCity(cargo.DestinationCityId);
            if (origin == null || dest == null) return false;

            const int SAMPLES = 6;
            Vector3 a = LatLonToDir(origin.Latitude, origin.Longitude);
            Vector3 b = LatLonToDir(dest.Latitude,   dest.Longitude);

            for (int i = 0; i <= SAMPLES; i++)
            {
                Vector3 d = Vector3.Slerp(a, b, (float)i / SAMPLES).normalized;
                DirToLatLon(d, out float lat, out float lon);
                if (_grid.GetCellAtLatLon(lat, lon).isCyclone) return true;
            }
            return false;
        }

        private int CalculateDelayDays(Cargo cargo)
        {
            if (_grid == null || _config == null) return 0;

            var origin = CityDatabase.GetCity(cargo.OriginCityId);
            var dest   = CityDatabase.GetCity(cargo.DestinationCityId);
            if (origin == null || dest == null) return 0;

            float rawDelay = WeatherManager.Instance?.GetRouteWeatherDelay(
                origin.Latitude, origin.Longitude,
                dest.Latitude,   dest.Longitude,
                cargo.TransportMode, _config) ?? 0f;

            return Mathf.RoundToInt(rawDelay);
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
            lat = Mathf.Asin(dir.y)         * Mathf.Rad2Deg;
            lon = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        }
    }
}
