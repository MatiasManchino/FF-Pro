using System.Collections.Generic;
using FreightForwarder.Models;

namespace FreightForwarder.Systems.Logistics
{
    public class RouteNode
    {
        public string CityId   { get; }
        public float  Lat      { get; }
        public float  Lon      { get; }
        public bool   HasPort     { get; }
        public bool   HasAirport  { get; }
        public bool   IsLandHub   { get; }
        public string LandZone    { get; }

        public readonly List<RouteEdge> Edges = new List<RouteEdge>();

        public RouteNode(WorldCity city)
        {
            CityId     = city.Id;
            Lat        = city.Latitude;
            Lon        = city.Longitude;
            HasPort    = city.HasPort;
            HasAirport = city.HasAirport;
            IsLandHub  = city.IsLandHub;
            LandZone   = city.LandZone;
        }
    }
}
