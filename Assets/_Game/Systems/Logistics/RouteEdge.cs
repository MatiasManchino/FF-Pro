using FreightForwarder.Models;

namespace FreightForwarder.Systems.Logistics
{
    public class RouteEdge
    {
        public RouteNode             From          { get; }
        public RouteNode             To            { get; }
        public Constants.TransportMode Mode        { get; }
        public float                 DistanceKm    { get; }
        public float                 BaseCostPerKm { get; }
        public float                 BaseDaysPerKm { get; }

        public RouteEdge(RouteNode from, RouteNode to,
                         Constants.TransportMode mode,
                         float distanceKm)
        {
            From        = from;
            To          = to;
            Mode        = mode;
            DistanceKm  = distanceKm;

            switch (mode)
            {
                case Constants.TransportMode.Air:
                    BaseCostPerKm = 4.5f;
                    BaseDaysPerKm = 1f / 15000f;
                    break;
                case Constants.TransportMode.Land:
                    BaseCostPerKm = 1.2f;
                    BaseDaysPerKm = 1f / 600f;
                    break;
                case Constants.TransportMode.Rail:
                    BaseCostPerKm = 0.9f;
                    BaseDaysPerKm = 1f / 800f;
                    break;
                default: // Maritime
                    BaseCostPerKm = 0.6f;
                    BaseDaysPerKm = 1f / 2000f;
                    break;
            }
        }

        public float GetCost(float worldFuelMultiplier = 1f)
            => DistanceKm * BaseCostPerKm * worldFuelMultiplier;

        public float GetDays(float agentSpeedMult = 1f)
            => DistanceKm * BaseDaysPerKm / agentSpeedMult;
    }
}
