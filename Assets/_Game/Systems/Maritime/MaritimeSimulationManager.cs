using System.Collections.Generic;
using System.Linq;
using FreightForwarder.Map;
using FreightForwarder.Managers;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Systems.Maritime
{
    public class MaritimeSimulationManager : Singleton<MaritimeSimulationManager>
    {
        // ── Active shipments ─────────────────────────────────────────────
        private readonly List<MaritimeShipment> _active = new List<MaritimeShipment>();
// Devuelve la active shipments
        public IReadOnlyList<MaritimeShipment> ActiveShipments => _active;

        // ── Ruta adjacency graph ────────────────────────────────────────
        // portName → list of (neighborPort, routeName, ttBaseDays, waypoints)
        private Dictionary<string, List<RouteLink>> _adj;
        // canonical puerto name → ruta entries
        private Dictionary<string, string> _nameNorm; // normalized → canonical

        private struct RouteLink
        {
            public string ToPort;
            public string RouteName;
            public float TTDays;
            public Vector2[] Waypoints;
        }

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            BuildGraph();
        }

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed += OnDayPassed;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnDayPassed -= OnDayPassed;
        }

        // Construye graph.

        private void BuildGraph()
        {
            _adj = new Dictionary<string, List<RouteLink>>();
            _nameNorm = new Dictionary<string, string>();

// Foreach
            foreach (var entry in MaritimeRouteDatabase.Routes)
            {
                string rawName = entry.Item1;
                float tt = entry.Item2;
                Vector2[] wps = entry.Item3;

                string[] parts = SplitRouteName(rawName);
                if (parts == null || parts.Length != 2) continue;

                string portA = Canonical(parts[0]);
                string portB = Canonical(parts[1]);

                AddLink(portA, portB, rawName, tt, wps);
                // Reverse: reverse waypoints for B→A
                Vector2[] rev = new Vector2[wps.Length];
                for (int i = 0; i < wps.Length; i++) rev[i] = wps[wps.Length - 1 - i];
                AddLink(portB, portA, rawName + " (rev)", tt, rev);
            }

            Debug.Log($"[MaritimeSim] Grafo construido: {_adj.Count} puertos, " +
                      $"{MaritimeRouteDatabase.Routes.Length} rutas.");
        }

// Añade link
        private void AddLink(string from, string to, string name, float tt, Vector2[] wps)
        {
            if (!_adj.ContainsKey(from)) _adj[from] = new List<RouteLink>();
            _adj[from].Add(new RouteLink { ToPort = to, RouteName = name, TTDays = tt, Waypoints = wps });
        }

        // ── Port name normalization ──────────────────────────────────────

        private static string[] SplitRouteName(string name)
        {
            // Try em-dash first, then regular dash
            int idx = name.IndexOf(" – ");
            if (idx < 0) idx = name.IndexOf(" - ");
            if (idx < 0) return null;
            string sep = name.IndexOf(" – ") >= 0 ? " – " : " - ";
            return new[] { name.Substring(0, idx).Trim(), name.Substring(idx + sep.Length).Trim() };
        }

// Normaliza.
        private static string Normalize(string s)
        {
            if (s == null) return "";
            // lowercase, remove accents and special chars, trim
            return System.Text.RegularExpressions.Regex.Replace(
                s.ToLowerInvariant()
                 .Replace("á","a").Replace("é","e").Replace("í","i")
                 .Replace("ó","o").Replace("ú","u").Replace("ü","u")
                 .Replace("ñ","n").Replace("ö","o").Replace("ä","a")
                 .Replace("à","a").Replace("è","e").Replace("ï","i")
                 .Replace("â","a").Replace("ê","e").Replace("î","i")
                 .Replace("ô","o").Replace("û","u"),
                @"[^a-z0-9\s]", "").Trim();
        }

// Comprueba si puede onical.
        public static string Canonical(string raw)
        {
            // Map known typos/variants to canonical names
            string n = Normalize(raw);
            switch (n)
            {
                case "sao paolo":   case "sao paulo": return "São Paulo";
                case "sindey":      case "sidey":     case "sydney": return "Sídney";
                case "shangahi":    case "shanghai":  return "Shanghái";
                case "buenos aires":                  return "Buenos Aires";
                case "valparaiso":                    return "Valparaíso";
                case "santiago":                      return "Santiago";
                case "lima":                          return "Lima";
                case "panama":                        return "Panamá";
                case "cartagena":                     return "Cartagena";
                case "miami":                         return "Miami";
                case "new york":    case "nueva york": return "New York";
                case "los angeles":                   return "Los Ángeles";
                case "houston":                       return "Houston";
                case "vancouver":                     return "Vancouver";
                case "tokio":       case "tokyo":     return "Tokio";
                case "busan":                         return "Busan";
                case "vladivostok":                   return "Vladivostok";
                case "taipei":      case "taipe":     return "Taipéi";
                case "hong kong":                     return "Hong Kong";
                case "singapur":    case "singapore": return "Singapur";
                case "bangkok":                       return "Bangkok";
                case "ho chi minh":                   return "Ho Chi Minh";
                case "manila":                        return "Manila";
                case "dubai":                         return "Dubái";
                case "jeddah":                        return "Jeddah";
                case "mumbai":                        return "Mumbai";
                case "karachi":                       return "Karachi";
                case "colombo":                       return "Colombo";
                case "mombasa":                       return "Mombasa";
                case "port said":                     return "Port Said";
                case "cape town":                     return "Cape Town";
                case "johannesburg": case "johannesburgo": return "Johannesburgo";
                case "rotterdam":   case "roterdam":  return "Rotterdam";
                case "london":      case "londres":   return "London";
                case "antwerp":     case "amberes":   return "Amberes";
                case "barcelona":                     return "Barcelona";
                case "marseille":   case "marsella":  return "Marsella";
                case "hamburg":     case "hamburgo":  return "Hamburgo";
                case "casablanca":                    return "Casablanca";
                case "athens":      case "atenas":    return "Atenas";
                case "istanbul":    case "estambul":  return "Estambul";
                case "auckland":                      return "Auckland";
                case "parama":                        return "Panamá"; // typo in data
                case "parama city":                   return "Panamá";
                default: return raw.Trim();
            }
        }

// Obtiene all ports
        public List<string> GetAllPorts()
        {
            return new List<string>(_adj.Keys);
        }

        // ── Ruta option generation ──────────────────────────────────────

        public List<ShipmentOption> GetRouteOptions(string originPort, string destPort)
        {
            string from = Canonical(originPort);
            string to   = Canonical(destPort);
            var options = new List<ShipmentOption>();

            // Direct
            var direct = FindDirectRoute(from, to);
            if (direct != null) options.Add(BuildOption(direct, ShipmentOption.OptionType.Direct));

            // Mixed A: 1 intermediate stop
            var mixedA = FindPathViaStops(from, to, 1);
            if (mixedA != null) options.Add(BuildOption(mixedA, ShipmentOption.OptionType.MixedA));

            // Mixed B: 2 intermediate stops
            var mixedB = FindPathViaStops(from, to, 2);
            if (mixedB != null && (mixedA == null || mixedB.Count > mixedA.Count))
                options.Add(BuildOption(mixedB, ShipmentOption.OptionType.MixedB));

            // Ensure we always have 3 options by building synthetic ones if needed
            while (options.Count < 3 && options.Count > 0)
            {
                var base_ = options[options.Count - 1];
                var synthetic = new ShipmentOption
                {
                    Type = options.Count == 1 ? ShipmentOption.OptionType.MixedA : ShipmentOption.OptionType.MixedB,
                    DisplayLabel = options.Count == 1 ? "1 Escala" : "2 Escalas",
                    RouteNames = new List<string>(base_.RouteNames),
                    PortSequence = new List<string>(base_.PortSequence),
                    CombinedWaypoints = base_.CombinedWaypoints,
                    BaseTTDays = base_.BaseTTDays,
                };
                int extraStops = 3 - options.Count;
                synthetic.TotalTTDays = base_.TotalTTDays + (extraStops * 2);
                synthetic.EstimatedCostUSD = Mathf.Clamp(base_.EstimatedCostUSD - extraStops * 1500, 400, 8000);
                options.Add(synthetic);
            }

            return options;
        }

// Busca direct ruta
        private List<RouteLink> FindDirectRoute(string from, string to)
        {
            if (!_adj.TryGetValue(from, out var links)) return null;
// Foreach
            foreach (var link in links)
                if (link.ToPort == to) return new List<RouteLink> { link };
            return null;
        }

        // BFS to find path with exactly `stops` intermediate ports
        private List<RouteLink> FindPathViaStops(string from, string to, int stops)
        {
            if (stops == 0) return FindDirectRoute(from, to);

            // BFS limited to depth stops+1
            var frontier = new Queue<(string port, List<RouteLink> path)>();
            frontier.Enqueue((from, new List<RouteLink>()));

            while (frontier.Count > 0)
            {
                var (cur, path) = frontier.Dequeue();

                if (path.Count > stops + 1) continue; // too deep

                if (!_adj.TryGetValue(cur, out var links)) continue;

                // Puertos ya visitados en este camino (incluye el origen): no se puede repetir ninguno.
                var visited = new HashSet<string> { from };
                foreach (var l in path) visited.Add(l.ToPort);

// Foreach
                foreach (var link in links)
                {
                    if (visited.Contains(link.ToPort)) continue; // no repetir puertos (ni volver al origen)
                    var newPath = new List<RouteLink>(path) { link };

                    if (link.ToPort == to && newPath.Count == stops + 1)
                        return newPath;

                    if (link.ToPort != to && newPath.Count <= stops)
                        frontier.Enqueue((link.ToPort, newPath));
                }
            }
            return null;
        }

// Construye option.
        private ShipmentOption BuildOption(List<RouteLink> links, ShipmentOption.OptionType type)
        {
            var opt = new ShipmentOption { Type = type };
            opt.DisplayLabel = type == ShipmentOption.OptionType.Direct ? "Directo" :
                               type == ShipmentOption.OptionType.MixedA ? "1 Escala" : "2 Escalas";

            // Port sequence
            if (links.Count > 0)
            {
                // Reconstruct from port
                string origin = links[0].RouteName.Contains("(rev)")
                    ? SplitRouteName(links[0].RouteName.Replace(" (rev)", ""))?[1]
                    : SplitRouteName(links[0].RouteName)?[0];
                opt.PortSequence.Add(Canonical(origin ?? links[0].ToPort));
            }
// Foreach
            foreach (var link in links)
                opt.PortSequence.Add(link.ToPort);

            // Combine waypoints
            var allWps = new List<Vector2>();
// Foreach
            foreach (var link in links)
            {
                if (allWps.Count > 0 && link.Waypoints.Length > 0)
                {
                    // Skip first waypoint to avoid duplicate at junction
                    for (int i = 1; i < link.Waypoints.Length; i++)
                        allWps.Add(link.Waypoints[i]);
                }
                else
                {
                    allWps.AddRange(link.Waypoints);
                }
            }
            opt.CombinedWaypoints = allWps.ToArray();

            // Ruta names
            foreach (var link in links) opt.RouteNames.Add(link.RouteName);

            // TT calculation
            opt.BaseTTDays = links.Sum(l => l.TTDays);
            int portCount = opt.PortSequence.Count; // includes origin + dest + intermediates
            opt.TotalTTDays = Mathf.CeilToInt(opt.BaseTTDays) + portCount * 2;

            // Cost: Direct is most expensive, Mixed B cheapest
            float distFactor = opt.BaseTTDays * 120f; // rough: ~120 USD/day
            switch (type)
            {
                case ShipmentOption.OptionType.Direct: opt.EstimatedCostUSD = Mathf.RoundToInt(800f + distFactor * 3.0f); break;
                case ShipmentOption.OptionType.MixedA: opt.EstimatedCostUSD = Mathf.RoundToInt(550f + distFactor * 2.2f); break;
                default:                               opt.EstimatedCostUSD = Mathf.RoundToInt(400f + distFactor * 1.6f); break;
            }
            // Marítimo: rango $400–$8.000 (tope duro $8.000).
            opt.EstimatedCostUSD = Mathf.Clamp(opt.EstimatedCostUSD, 400, 8000);

            return opt;
        }

        // ── Start / manage shipments ─────────────────────────────────────

        public MaritimeShipment StartShipment(string cargoId, ShipmentOption option, int currentDay)
        {
            var s = new MaritimeShipment
            {
                CargoId        = cargoId,
                OriginPort     = option.PortSequence.Count > 0 ? option.PortSequence[0] : "?",
                DestinationPort = option.PortSequence.Count > 0 ? option.PortSequence[option.PortSequence.Count - 1] : "?",
                DisplayName    = option.PortSequence.Count >= 2
                                     ? $"{option.PortSequence[0]} → {option.PortSequence[option.PortSequence.Count - 1]}"
                                     : cargoId,
                Waypoints      = option.CombinedWaypoints,
                TotalTTDays    = option.TotalTTDays,
                TotalBaseTTDays = option.BaseTTDays,
                StartDay       = currentDay,
                DaysElapsed    = 0,
                Status         = ShipStatus.OperatingOrigin,
            };

            s.Legs.AddRange(option.RouteNames);

            // Register intermediate stops (each non-endpoint port)
            int totalWps = s.Waypoints?.Length ?? 1;
            for (int i = 1; i < option.PortSequence.Count - 1; i++)
            {
                float fraction = (float)i / (option.PortSequence.Count - 1);
                int waypointDay = currentDay + Mathf.RoundToInt(fraction * s.TotalTTDays);
                s.IntermediateStops.Add((fraction, option.PortSequence[i], waypointDay));
            }

            s.Log.Add($"Día {currentDay}: Cargando en {s.OriginPort}");
            _active.Add(s);

            OnShipmentStarted?.Invoke(s);
            return s;
        }

// Obtiene shipment
        public MaritimeShipment GetShipment(string cargoId)
        {
// Foreach
            foreach (var s in _active)
                if (s.CargoId == cargoId) return s;
            return null;
        }

        public event System.Action<MaritimeShipment> OnShipmentStarted;
        public event System.Action<MaritimeShipment> OnShipmentCompleted;
        public event System.Action<MaritimeShipment> OnShipmentUpdated;

        // Se invoca al terminar un día de juego.

        private void OnDayPassed()
        {
            int currentDay = FFTimeManager.Instance?.CurrentDay ?? 1;
            var toComplete = new List<MaritimeShipment>();

// Foreach
            foreach (var s in _active)
            {
                if (s.Status == ShipStatus.Delivered) continue;
                s.DaysElapsed++;
                UpdateShipmentStatus(s, currentDay);
                if (s.Status == ShipStatus.Delivered) toComplete.Add(s);
                else OnShipmentUpdated?.Invoke(s);
            }

// Foreach
            foreach (var s in toComplete)
            {
                _active.Remove(s);
                OnShipmentCompleted?.Invoke(s);
                // Forward completion to CargoManager
                CargoManager.Instance?.CompleteMaritimeShipment(s.CargoId, currentDay);
            }
        }

// Actualiza shipment status
        private void UpdateShipmentStatus(MaritimeShipment s, int currentDay)
        {
            float progress = s.TotalTTDays > 0 ? (float)s.DaysElapsed / s.TotalTTDays : 1f;

            // Delivered
            if (s.DaysElapsed >= s.TotalTTDays)
            {
                s.Status = ShipStatus.Delivered;
                s.Log.Add($"Día {currentDay}: Entregado en {s.DestinationPort}");
                return;
            }

            // Last 2 days = operating at destination
            if (s.DaysElapsed >= s.TotalTTDays - 2)
            {
                if (s.Status != ShipStatus.OperatingDest)
                {
                    s.Status = ShipStatus.OperatingDest;
                    s.Log.Add($"Día {currentDay}: Llegando a {s.DestinationPort}, operando en puerto");
                }
                return;
            }

            // First 2 days = operating at origin
            if (s.DaysElapsed <= 2)
            {
                s.Status = ShipStatus.OperatingOrigin;
                return;
            }

            // Check intermediate stops (entryDay is already an absolute game day)
            for (int i = 0; i < s.IntermediateStops.Count; i++)
            {
                var stop = s.IntermediateStops[i];
                int stopDay = stop.entryDay;
                if (currentDay >= stopDay && currentDay < stopDay + 2)
                {
                    if (s.Status != ShipStatus.OperatingWayport || s.CurrentStopIndex != i)
                    {
                        s.Status = ShipStatus.OperatingWayport;
                        s.CurrentStopIndex = i;
                        s.Log.Add($"Día {currentDay}: Escala en {stop.portName}");
                    }
                    return;
                }
            }

            // Storm check — rolls once between 20%–80% of voyage
            if (!s.StormRolled && progress >= 0.2f && progress <= 0.8f)
            {
                s.StormRolled = true;
                if (Random.value < 0.15f) // 15% chance
                {
                    int duration = Random.Range(1, 4);
                    s.HasActiveStorm = true;
                    s.StormEndDay = currentDay + duration;
                    s.Log.Add($"Día {currentDay}: ¡Tormenta! Duración estimada {duration} día(s)");
                }
            }

            if (s.HasActiveStorm && currentDay <= s.StormEndDay)
            {
                s.Status = ShipStatus.Storm;
                return;
            }
            if (s.HasActiveStorm && currentDay > s.StormEndDay)
            {
                s.HasActiveStorm = false;
                s.Log.Add($"Día {currentDay}: Tormenta superada, continuando navegación");
            }

            s.Status = ShipStatus.AtSea;
        }
    }
}