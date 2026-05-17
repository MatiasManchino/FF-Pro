using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using static FreightForwarder.Models.Constants;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Map
{
    /// <summary>
    /// Escucha CargoManager y genera/destruye CargoRoute por cada carga en tránsito.
    /// Agregá este componente al GameObject [FF System].
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

            var go    = new GameObject($"Route_{cargo.Id.Substring(0, 8)}");
            var route = go.AddComponent<CargoRoute>();
            route.Initialize(cargo.Id, origin, dest, cargo.TransportMode);
            _routes[cargo.Id] = go;

            Debug.Log($"[RouteManager] Ruta creada: {cargo.OriginCityId} → {cargo.DestinationCityId} ({cargo.TransportMode})");
        }

        private void RemoveRoute(Cargo cargo)
        {
            if (_routes.TryGetValue(cargo.Id, out var go))
            {
                Destroy(go);
                _routes.Remove(cargo.Id);
            }
        }

        public int ActiveRouteCount => _routes.Count;
    }
}
