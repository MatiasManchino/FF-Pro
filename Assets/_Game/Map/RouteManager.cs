using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using static FreightForwarder.Models.Constants;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Map
{
    /// <summary>
    /// Listens to CargoManager and spawns/destroys CargoRoute visuals.
    /// Routes are built from geographic waypoints so maritime routes follow
    /// real shipping lanes, air routes go through hub airports, and land
    /// routes follow road corridors.
    /// </summary>
    public class RouteManager : Singleton<RouteManager>
    {
        private readonly Dictionary<string, GameObject> _routes = new Dictionary<string, GameObject>();

        private void Start()
        {
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoAccepted  += SpawnRoute;
                CargoManager.Instance.OnCargoCompleted += RemoveRoute;
                CargoManager.Instance.OnCargoFailed    += RemoveRoute;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (CargoManager.Instance != null)
            {
                CargoManager.Instance.OnCargoAccepted  -= SpawnRoute;
                CargoManager.Instance.OnCargoCompleted -= RemoveRoute;
                CargoManager.Instance.OnCargoFailed    -= RemoveRoute;
            }
        }

        private void SpawnRoute(Cargo cargo)
        {
            if (_routes.ContainsKey(cargo.Id)) return;

            var origin = CityDatabase.GetCity(cargo.OriginCityId);
            var dest   = CityDatabase.GetCity(cargo.DestinationCityId);
            if (origin == null || dest == null)
            {
                Debug.LogWarning($"[RouteManager] Ciudad no encontrada para carga {cargo.Id}: {cargo.OriginCityId} → {cargo.DestinationCityId}");
                return;
            }

            Vector2[] waypoints = BuildWaypoints(cargo.OriginCityId, cargo.DestinationCityId,
                                                  origin, dest, cargo.TransportMode);

            var go    = new GameObject($"Route_{cargo.Id.Substring(0, 8)}");
            var route = go.AddComponent<CargoRoute>();
            route.Initialize(cargo.Id, waypoints, cargo.TransportMode, cargo);
            _routes[cargo.Id] = go;

            Debug.Log($"[RouteManager] Ruta creada: {cargo.OriginCityId} → {cargo.DestinationCityId} ({cargo.TransportMode}, {waypoints.Length} waypoints)");
        }

        private void RemoveRoute(Cargo cargo)
        {
            if (_routes.TryGetValue(cargo.Id, out var go))
            {
                Destroy(go);
                _routes.Remove(cargo.Id);
            }
        }

        // ── Waypoint resolution ───────────────────────────────────────────────────

        private static Vector2[] BuildWaypoints(string originId, string destId,
                                                WorldCity origin, WorldCity dest,
                                                TransportMode mode)
        {
            Vector2 oPos = origin.ToVector2();
            Vector2 dPos = dest.ToVector2();

            switch (mode)
            {
                case TransportMode.Maritime:
                {
                    var mid = RouteWaypointDB.GetMaritimeWaypoints(originId, destId);
                    return Assemble(oPos, mid, dPos);
                }

                case TransportMode.Air:
                {
                    string[] cities = RouteWaypointDB.GetAirRoute(originId, destId);
                    var pts = new Vector2[cities.Length];
                    for (int i = 0; i < cities.Length; i++)
                    {
                        var c = CityDatabase.GetCity(cities[i]);
                        pts[i] = c != null ? c.ToVector2() : (i == 0 ? oPos : dPos);
                    }
                    return pts;
                }

                case TransportMode.Land:
                case TransportMode.Rail:
                {
                    var mid = RouteWaypointDB.GetLandWaypoints(originId, destId);
                    return Assemble(oPos, mid, dPos);
                }

                case TransportMode.Multimodal:
                default:
                    return new[] { oPos, dPos };
            }
        }

        private static Vector2[] Assemble(Vector2 origin, Vector2[] mid, Vector2 dest)
        {
            if (mid == null || mid.Length == 0)
                return new[] { origin, dest };

            var all = new Vector2[mid.Length + 2];
            all[0] = origin;
            for (int i = 0; i < mid.Length; i++) all[i + 1] = mid[i];
            all[all.Length - 1] = dest;
            return all;
        }

        public int ActiveRouteCount => _routes.Count;
    }
}
