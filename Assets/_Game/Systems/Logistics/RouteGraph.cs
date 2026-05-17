using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using UnityEngine;

namespace FreightForwarder.Systems.Logistics
{
    /// <summary>
    /// Grafo de rutas entre ciudades. Conecta nodos con aristas según capacidad
    /// (puerto, aeropuerto, hub terrestre). Pathfinding con Dijkstra.
    /// Uso: RouteGraph.Instance.FindRoute(originId, destId, mode, agentSpeed)
    /// </summary>
    public class RouteGraph
    {
        private static RouteGraph _instance;
        public  static RouteGraph Instance => _instance ??= new RouteGraph();

        private readonly Dictionary<string, RouteNode> _nodes = new Dictionary<string, RouteNode>();
        private bool _built;

        // ── Construcción ──────────────────────────────────────────────────────

        public void Build(Dictionary<string, WorldCity> cities)
        {
            _nodes.Clear();

            foreach (var city in cities.Values)
                _nodes[city.Id] = new RouteNode(city);

            var nodeList = new List<RouteNode>(_nodes.Values);

            for (int i = 0; i < nodeList.Count; i++)
            {
                for (int j = i + 1; j < nodeList.Count; j++)
                {
                    var a = nodeList[i];
                    var b = nodeList[j];
                    float dist = Haversine(a.Lat, a.Lon, b.Lat, b.Lon);

                    // Marítimo: entre puertos
                    if (a.HasPort && b.HasPort)
                    {
                        a.Edges.Add(new RouteEdge(a, b, Constants.TransportMode.Maritime, dist));
                        b.Edges.Add(new RouteEdge(b, a, Constants.TransportMode.Maritime, dist));
                    }

                    // Aéreo: entre aeropuertos
                    if (a.HasAirport && b.HasAirport)
                    {
                        a.Edges.Add(new RouteEdge(a, b, Constants.TransportMode.Air, dist));
                        b.Edges.Add(new RouteEdge(b, a, Constants.TransportMode.Air, dist));
                    }

                    // Terrestre: hubs en misma zona y dist < 3500 km
                    if (a.IsLandHub && b.IsLandHub && dist < 3500f
                        && a.LandZone == b.LandZone)
                    {
                        a.Edges.Add(new RouteEdge(a, b, Constants.TransportMode.Land, dist));
                        b.Edges.Add(new RouteEdge(b, a, Constants.TransportMode.Land, dist));
                    }
                }
            }

            _built = true;
            Debug.Log($"[RouteGraph] Construido: {_nodes.Count} nodos.");
        }

        // ── Dijkstra ──────────────────────────────────────────────────────────

        public RouteResult FindRoute(string originId, string destId,
                                     Constants.TransportMode preferredMode,
                                     float agentSpeedMult = 1f,
                                     float worldFuelMult  = 1f)
        {
            if (!_built)
            {
                Debug.LogWarning("[RouteGraph] Grafo no construido, usando Haversine directo.");
                return FallbackRoute(originId, destId, preferredMode, agentSpeedMult, worldFuelMult);
            }

            if (!_nodes.TryGetValue(originId, out var start) ||
                !_nodes.TryGetValue(destId,   out var end))
                return FallbackRoute(originId, destId, preferredMode, agentSpeedMult, worldFuelMult);

            // Dijkstra por costo total (distancia × costo/km × fuelMult)
            var dist  = new Dictionary<string, float>();
            var prev  = new Dictionary<string, (RouteNode node, RouteEdge edge)>();
            var queue = new SortedSet<(float cost, string id)>(
                Comparer<(float, string)>.Create((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : string.Compare(a.Item2, b.Item2, StringComparison.Ordinal)));

            foreach (var n in _nodes.Values) dist[n.CityId] = float.MaxValue;
            dist[originId] = 0f;
            queue.Add((0f, originId));

            while (queue.Count > 0)
            {
                var (curCost, curId) = queue.Min;
                queue.Remove(queue.Min);

                if (curId == destId) break;
                if (curCost > dist[curId]) continue;

                var curNode = _nodes[curId];
                foreach (var edge in curNode.Edges)
                {
                    // Penalizar aristas que no coinciden con el modo preferido
                    float modePenalty = edge.Mode == preferredMode ? 1f : 2.5f;
                    float newCost = dist[curId] + edge.GetCost(worldFuelMult) * modePenalty;

                    if (newCost < dist[edge.To.CityId])
                    {
                        dist[edge.To.CityId] = newCost;
                        prev[edge.To.CityId] = (curNode, edge);
                        queue.Add((newCost, edge.To.CityId));
                    }
                }
            }

            if (dist[destId] == float.MaxValue)
                return FallbackRoute(originId, destId, preferredMode, agentSpeedMult, worldFuelMult);

            // Reconstruir path
            var path = new List<RouteEdge>();
            string cur = destId;
            float totalDist = 0f, totalCost = 0f, totalDays = 0f;

            while (prev.ContainsKey(cur))
            {
                var (_, edge) = prev[cur];
                path.Insert(0, edge);
                totalDist += edge.DistanceKm;
                totalCost += edge.GetCost(worldFuelMult);
                totalDays += edge.GetDays(agentSpeedMult);
                cur = edge.From.CityId;
            }

            return new RouteResult(originId, destId, path, totalDist,
                                   Mathf.CeilToInt(totalDays), (int)totalCost, true);
        }

        // ── Fallback Haversine directo ─────────────────────────────────────────

        private RouteResult FallbackRoute(string originId, string destId,
                                          Constants.TransportMode mode,
                                          float speedMult, float fuelMult)
        {
            float dist  = CityDatabase.GetDistance(originId, destId);
            var   edge  = new RouteEdge(null, null, mode, dist);
            int   days  = Mathf.Max(1, Mathf.CeilToInt(edge.GetDays(speedMult)));
            int   cost  = (int)(edge.GetCost(fuelMult));
            return new RouteResult(originId, destId, new List<RouteEdge>(), dist, days, cost, false);
        }

        // ── Haversine ────────────────────────────────────────────────────────

        public static float Haversine(float lat1, float lon1, float lat2, float lon2)
        {
            const float R = 6371f;
            float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            float dLon = (lon2 - lon1) * Mathf.Deg2Rad;
            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2)
                    + Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad)
                    * Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
            return R * 2f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        }

        public RouteNode GetNode(string cityId)
        {
            _nodes.TryGetValue(cityId, out var n);
            return n;
        }

        public bool IsBuilt => _built;
    }

    public class RouteResult
    {
        public string           OriginId    { get; }
        public string           DestId      { get; }
        public List<RouteEdge>  Path        { get; }
        public float            TotalDistKm { get; }
        public int              TotalDays   { get; }
        public int              TotalCost   { get; }
        public bool             UsedGraph   { get; }

        public RouteResult(string originId, string destId, List<RouteEdge> path,
                           float dist, int days, int cost, bool usedGraph)
        {
            OriginId    = originId;
            DestId      = destId;
            Path        = path;
            TotalDistKm = dist;
            TotalDays   = days;
            TotalCost   = cost;
            UsedGraph   = usedGraph;
        }
    }
}
