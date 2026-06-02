using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Systems.Maritime
{
    public enum ShipStatus
    {
        OperatingOrigin,    // Loading at origin port (2 days)
        AtSea,              // Navigating open water
        Storm,              // Fighting storm at sea
        OperatingWayport,   // Loading/unloading at intermediate port (2 days)
        OperatingDest,      // Unloading at destination port (2 days)
        Delivered
    }

    public class MaritimeShipment
    {
        public string CargoId;
        public string OriginPort;
        public string DestinationPort;
        public string DisplayName;

        // Combined waypoints from all ruta segments
        public Vector2[] Waypoints;

        // Timing — all in game days
        public int TotalTTDays;     // Total including all port days
        public int StartDay;
        public int DaysElapsed;

        // Per-segment data for multi-leg routes
        public List<string> Legs;          // route names
        public float TotalBaseTTDays;       // sum of route base TT

        // Port stop markers: list of (waypointFraction, portName) for intermediate stops
        // waypointFraction is 0..1 along the full waypoint array
        public List<(float fraction, string portName, int entryDay)> IntermediateStops;

        // Actual status
        public ShipStatus Status;
// Ejecuta at
        public int CurrentStopIndex; // which intermediate stop we're at (-1 = none)

        // Storm event
        public bool HasActiveStorm;
        public int StormEndDay;       // absolute game day when storm clears
        public bool StormRolled;      // has the storm check been done for this voyage

        // Log
        public List<string> Log;

// Devuelve el progress porcentaje
        public float ProgressPercent =>
            TotalTTDays > 0 ? Mathf.Clamp01((float)DaysElapsed / TotalTTDays) * 100f : 0f;

// Days remaining
        public int DaysRemaining => Mathf.Max(0, TotalTTDays - DaysElapsed);

// Indica si completado
        public bool IsCompleted => Status == ShipStatus.Delivered;

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case ShipStatus.OperatingOrigin:   return "Operando en puerto de origen";
                    case ShipStatus.AtSea:             return "En agua";
                    case ShipStatus.Storm:             return "Luchando la tormenta";
                    case ShipStatus.OperatingWayport:  return "Operando en puerto de escala";
                    case ShipStatus.OperatingDest:     return "Operando en puerto de arribo";
                    case ShipStatus.Delivered:         return "Entregado";
                    default:                           return "En tránsito";
                }
            }
        }

        // Gestiona get actual posición.
        public Vector2 GetCurrentPosition(float progressFraction)
        {
            if (Waypoints == null || Waypoints.Length == 0) return Vector2.zero;
            if (Waypoints.Length == 1) return Waypoints[0];

            float t = Mathf.Clamp01(progressFraction) * (Waypoints.Length - 1);
            int idx = Mathf.Min((int)t, Waypoints.Length - 2);
            float frac = t - idx;
            return Vector2.Lerp(Waypoints[idx], Waypoints[idx + 1], frac);
        }

// Realiza maritime shipment
        public MaritimeShipment()
        {
            Legs = new List<string>();
            IntermediateStops = new List<(float, string, int)>();
            Log = new List<string>();
            CurrentStopIndex = -1;
        }
    }

    // Market option shown to the player for a given origin→destination pair
    public class ShipmentOption
    {
// Gestiona option type.
        public enum OptionType { Direct, MixedA, MixedB }

        public OptionType Type;
        public string DisplayLabel;         // "Directo", "1 Escala", "2 Escalas"
        public List<string> RouteNames;     // names of the component routes
        public List<string> PortSequence;   // origin, [stops...], destination
        public Vector2[] CombinedWaypoints;
        public float BaseTTDays;            // sum of route TT values
// Ejecuta days
        public int TotalTTDays;             // base + port days (2 per port)
        public int EstimatedCostUSD;

// Realiza shipment option
        public ShipmentOption()
        {
            RouteNames = new List<string>();
            PortSequence = new List<string>();
        }
    }
}