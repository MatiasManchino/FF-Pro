using System.Collections.Generic;
using UnityEngine;
using FreightForwarder.Utils;

namespace FreightForwarder.Map
{

    // Builds ocean/land/ice boolean grids from máscara texturas and provides
    // A*-based pathfinding to keep maritime routes over water.
    // Coordinate convention: waypoints are Vector2(lat, lonGame) where
    // lonGame = lonReal + 180°, normalized to [-180, 180].

    public class WaterMaskSampler : Singleton<WaterMaskSampler>
    {
        private const int GW = 720;  // 0.5° per cell, longitude
        private const int GH = 360;  // 0.5° per cell, latitude

        private bool[,] _water;
        private bool[,] _ice;
// Indica si ready.
        public bool IsReady { get; private set; }

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake() => BuildGrid();

        // Construye rejilla.

        private void BuildGrid()
        {
            var tex = Resources.Load<Texture2D>("Map/Textures/mask-water-land");
            if (tex == null)
            {
                Debug.LogError("[WaterMaskSampler] mask-water-land not found at Resources/Map/Textures/");
                return;
            }

            int tw = tex.width, th = tex.height;
            Color32[] pixels = tex.GetPixels32();

            _water = new bool[GW, GH];
            for (int gy = 0; gy < GH; gy++)
            {
                for (int gx = 0; gx < GW; gx++)
                {
                    float u  = (gx + 0.5f) / GW;
                    float v  = (gy + 0.5f) / GH;
                    int   tx = Mathf.Clamp(Mathf.FloorToInt(u * tw), 0, tw - 1);
                    int   ty = Mathf.Clamp(Mathf.FloorToInt(v * th), 0, th - 1);
                    _water[gx, gy] = pixels[ty * tw + tx].r > 127;
                }
            }
            Resources.UnloadAsset(tex);

            var iceTex = Resources.Load<Texture2D>("Map/Textures/mask-ice");
            if (iceTex != null)
            {
                int iw = iceTex.width, ih = iceTex.height;
                Color32[] icePixels = iceTex.GetPixels32();
                _ice = new bool[GW, GH];
                for (int gy = 0; gy < GH; gy++)
                {
                    for (int gx = 0; gx < GW; gx++)
                    {
                        float u  = (gx + 0.5f) / GW;
                        float v  = (gy + 0.5f) / GH;
                        int   tx = Mathf.Clamp(Mathf.FloorToInt(u * iw), 0, iw - 1);
                        int   ty = Mathf.Clamp(Mathf.FloorToInt(v * ih), 0, ih - 1);
                        _ice[gx, gy] = icePixels[ty * iw + tx].r > 127;
                    }
                }
                Resources.UnloadAsset(iceTex);
                Debug.Log("[WaterMaskSampler] Ice mask loaded");
            }

            IsReady = true;
            Debug.Log($"[WaterMaskSampler] Ready — {GW}x{GH} grid from {tw}x{th} texture");
        }

        // Indica si water.

        public bool IsWater(float lat, float lonGame) =>
            IsReady && _water[LonToGX(lonGame), LatToGY(lat)];

// Indica si ice.
        public bool IsIce(float lat, float lonGame) =>
            IsReady && _ice != null && _ice[LonToGX(lonGame), LatToGY(lat)];

        // Gestiona get terreno etiqueta.
        public string GetTerrainLabel(float lat, float lonGame)
        {
            if (!IsReady) return "?";
            if (IsIce(lat, lonGame))   return "Hielo";
            if (IsWater(lat, lonGame)) return "Agua";
            return "Tierra";
        }


        // Fixes a maritime waypoint path so no segment crosses land.
        // Each waypoint is Vector2(lat, lonGame).

        public Vector2[] FixMaritimePath(Vector2[] waypoints)
        {
            if (!IsReady || waypoints == null || waypoints.Length < 2)
                return waypoints;

            var result = new List<Vector2> { waypoints[0] };
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                var seg = FixSegment(waypoints[i], waypoints[i + 1]);
                for (int j = 1; j < seg.Length; j++)
                    result.Add(seg[j]);
            }
            return result.ToArray();
        }

        // Gestiona fix segment.

        private Vector2[] FixSegment(Vector2 from, Vector2 to)
        {
            if (!SegmentCrossesLand(from, to, 40))
                return new[] { from, to };

            int sx = LonToGX(from.y), sy = LatToGY(from.x);
            int ex = LonToGX(to.y),   ey = LatToGY(to.x);

            if (!_water[sx, sy]) FindNearestWater(sx, sy, out sx, out sy);
            if (!_water[ex, ey]) FindNearestWater(ex, ey, out ex, out ey);

            // Narrow straits and canals (Suez, Bosphorus, Malacca) are far below the
            // 0.5°/cell grid resolution.  Si the endpoints are within 10 grid cells
            // (~5° ≈ 500 km), the "land" the sampler detected is an unresolvable
            // sub-pixel passage — keep the direct segment instead of running A*.
            int gdx = Mathf.Abs(sx - ex);
            if (gdx > GW / 2) gdx = GW - gdx;
            if (gdx * gdx + (sy - ey) * (sy - ey) <= 100)
                return new[] { from, to };

            var gridPath = AStar(sx, sy, ex, ey);
            if (gridPath == null || gridPath.Count == 0)
                return new[] { from, to };

            return BuildSmoothedPath(gridPath, from, to);
        }

// Gestiona segment crosses land.
        private bool SegmentCrossesLand(Vector2 a, Vector2 b, int samples)
        {
            float lonDelta = b.y - a.y;
            if (lonDelta >  180f) lonDelta -= 360f;
            if (lonDelta < -180f) lonDelta += 360f;

            for (int i = 1; i < samples; i++)
            {
                float t   = i / (float)samples;
                float lat = Mathf.Lerp(a.x, b.x, t);
                float lon = a.y + lonDelta * t;
                if (lon >  180f) lon -= 360f;
                if (lon < -180f) lon += 360f;
                if (!IsWater(lat, lon)) return true;
            }
            return false;
        }

// Construye smoothed ruta.
        private Vector2[] BuildSmoothedPath(List<(int x, int y)> grid, Vector2 from, Vector2 to)
        {
            const int MAX_WP = 20;
            var result = new List<Vector2> { from };

            int stride = Mathf.Max(1, grid.Count / MAX_WP);
            for (int i = stride; i < grid.Count - stride / 2; i += stride)
                result.Add(GridToLatLon(grid[i].x, grid[i].y));

            result.Add(to);
            return result.ToArray();
        }

        // ── A* pathfinding on water grid ───────────────────────────────────────────

        private List<(int x, int y)> AStar(int sx, int sy, int ex, int ey)
        {
            if (sx == ex && sy == ey)
                return new List<(int, int)> { (sx, sy) };

            var gCost  = new Dictionary<int, float>();
            var parent = new Dictionary<int, int>();
            var open   = new SortedList<float, Queue<int>>();

            int startKey = CellKey(sx, sy);
            int endKey   = CellKey(ex, ey);

            gCost[startKey] = 0f;
            Enqueue(open, Heuristic(sx, sy, ex, ey), startKey);

            int[] nx8 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] ny8 = { -1, -1, -1, 0, 0, 1, 1, 1 };
            float[] cost8 = { 1.414f, 1f, 1.414f, 1f, 1f, 1.414f, 1f, 1.414f };

            const int MAX_ITER = 400_000;
            for (int iter = 0; iter < MAX_ITER && open.Count > 0; iter++)
            {
                int curr = Dequeue(open);
                if (curr == endKey)
                    return ReconstructPath(parent, curr, startKey);

                float cg = gCost[curr];
                int   cx = curr % GW, cy = curr / GW;

                for (int d = 0; d < 8; d++)
                {
                    int nx = ((cx + nx8[d]) % GW + GW) % GW;
                    int ny = cy + ny8[d];
                    if (ny < 0 || ny >= GH) continue;
                    if (!_water[nx, ny]) continue;

                    int   nk = CellKey(nx, ny);
                    float ng = cg + cost8[d];
                    if (gCost.TryGetValue(nk, out float prev) && prev <= ng) continue;

                    gCost[nk]  = ng;
                    parent[nk] = curr;
                    Enqueue(open, ng + Heuristic(nx, ny, ex, ey), nk);
                }
            }

            Debug.LogWarning($"[WaterMaskSampler] A* limit reached ({sx},{sy})→({ex},{ey})");
            return null;
        }

// Heurística.
        private static float Heuristic(int ax, int ay, int bx, int by)
        {
            int dx = Mathf.Abs(ax - bx);
            if (dx > GW / 2) dx = GW - dx;
            int dy = Mathf.Abs(ay - by);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

// Ejecuta reconstruct path
        private static List<(int, int)> ReconstructPath(Dictionary<int, int> parent, int end, int start)
        {
            var path = new List<(int, int)>();
            for (int curr = end; curr != start; curr = parent[curr])
                path.Add((curr % GW, curr / GW));
            path.Add((start % GW, start / GW));
            path.Reverse();
            return path;
        }

// Busca nearest water
        private void FindNearestWater(int gx, int gy, out int wx, out int wy)
        {
            for (int r = 1; r < 30; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        int nx = ((gx + dx) % GW + GW) % GW;
                        int ny = gy + dy;
                        if (ny < 0 || ny >= GH) continue;
                        if (_water[nx, ny]) { wx = nx; wy = ny; return; }
                    }
                }
            }
            wx = gx; wy = gy;
        }

        // Encola.

        private static void Enqueue(SortedList<float, Queue<int>> open, float f, int key)
        {
            if (!open.TryGetValue(f, out var q))
                open[f] = q = new Queue<int>();
            q.Enqueue(key);
        }

// Desencola.
        private static int Dequeue(SortedList<float, Queue<int>> open)
        {
            float first = open.Keys[0];
            var   q     = open[first];
            int   val   = q.Dequeue();
            if (q.Count == 0) open.Remove(first);
            return val;
        }

        // Longitud to gx.

        private static int LonToGX(float lonGame)
        {
            float lonReal = lonGame - 180f;
            if (lonReal < -180f) lonReal += 360f;
            return Mathf.Clamp(Mathf.FloorToInt((lonReal + 180f) / 360f * GW), 0, GW - 1);
        }

// Latitud to gy.
        private static int LatToGY(float lat) =>
            Mathf.Clamp(Mathf.FloorToInt((lat + 90f) / 180f * GH), 0, GH - 1);

// Rejilla to latitud longitud.
        private static Vector2 GridToLatLon(int gx, int gy)
        {
            float lonReal = (gx + 0.5f) / GW * 360f - 180f;
            float lonGame = lonReal + 180f;
            if (lonGame > 180f) lonGame -= 360f;
            float lat = (gy + 0.5f) / GH * 180f - 90f;
            return new Vector2(lat, lonGame);
        }

// Celda clave.
        private static int CellKey(int gx, int gy) => gy * GW + gx;
    }
}