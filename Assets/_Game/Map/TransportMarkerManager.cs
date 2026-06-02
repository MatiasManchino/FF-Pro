using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Map
{
    public class TransportMarkerManager : Singleton<TransportMarkerManager>
    {
        private readonly Dictionary<string, TransportMarker> _markers = new();

        // Devuelve el marcador (vehículo) de una carga, si existe en el mapa.
        public bool TryGetMarker(string cargoId, out TransportMarker marker)
            => _markers.TryGetValue(cargoId, out marker);

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
        private void Update()
        {
            if (CargoManager.Instance == null) return;

// Foreach
            foreach (var cargo in CargoManager.Instance.ActiveCargos)
            {
                if (cargo.TransportMode == Constants.TransportMode.Maritime) continue;
                if (_markers.ContainsKey(cargo.Id)) continue;

                var marker = TransportMarker.Create(cargo);
                if (marker != null)
                    _markers[cargo.Id] = marker;
            }

            // Elimina destroyed markers (Unity null check catches Destruye'd components)
            var toRemove = new List<string>();
// Foreach
            foreach (var kvp in _markers)
                if (kvp.Value == null) toRemove.Add(kvp.Key);
            foreach (var key in toRemove) _markers.Remove(key);
        }
    }
}