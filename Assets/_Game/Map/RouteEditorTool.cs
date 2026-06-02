#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FreightForwarder.Map
{

    // Runtime waypoint authoring tool — only compiled in the Unity Editor.
    // Se crea automáticamente al entrar en Play Mode. No hace falta agregar el componente.
    //
    // CONTROLES:
    // - Clic izquierdo en la Tierra → agrega punto (latitud, longitud)
    // - Clic derecho → elimina último punto
    // - Tecla C → borra toda la ruta
    // - Tecla V → copia al portapapeles formateado
    // - Tecla H → oculta/muestra la UI

    public class RouteEditorTool : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
// Gestiona auto genera.
        static void AutoSpawn()
        {
            var go = new GameObject("[RouteEditorTool]");
            go.AddComponent<RouteEditorTool>();
            DontDestroyOnLoad(go);
        }

        [Header("Tool")]
        [Tooltip("Toggle with the checkbox in the panel or disable this component.")]
        public bool toolActive = false;

        [Header("Visuals")]
        public float markerSize = 30f;
        public float lineWidth  =  5f;

        // ── State ──────────────────────────────────────────────────────────────────

        private readonly List<Vector2> _pts     = new List<Vector2>(); // (lat, lonGame)
        private string                 _output  = "";
        private string                 _status  = "LClick=add  RClick=undo  C=clear  V=copy  H=UI";
        private bool                   _uiVisible = true;

        // ── Visuals ────────────────────────────────────────────────────────────────

        private LineRenderer             _line;
        private readonly List<Transform> _markers   = new List<Transform>();
        private Material                 _lineMat;
        private Material                 _markerMat;

        // ── IMGUI ──────────────────────────────────────────────────────────────────

        private Rect      _win    = new Rect(20f, 20f, 420f, 560f);
        private Vector2   _scroll;
        private GUIStyle  _bold;
        private GUIStyle  _small;
        private bool      _stylesReady;

        // ══════════════════════════════════════════════════════════════════════════
        // Lifecycle
        // Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.

        void Start()
        {
            _lineMat   = new Material(Shader.Find("Unlit/Color")) { color = Color.yellow };
            _markerMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.8f, 0f) };

            var go = new GameObject("_RouteEditorLine");
            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace     = true;
            _line.startWidth        = lineWidth;
            _line.endWidth          = lineWidth;
            _line.sharedMaterial    = _lineMat;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows    = false;
            _line.positionCount     = 0;

            Debug.Log("[RouteEditorTool] Ready. Click on the globe to capture waypoints.");
        }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
        void Update()
        {
            if (!toolActive) return;
            UpdateMarkerPositions();

            if (Input.GetMouseButtonDown(0) && !IsOverPanel()) TryCapture();
            if (Input.GetMouseButtonDown(1) && !IsOverPanel()) UndoLast();

            if (Input.GetKeyDown(KeyCode.C)) ClearAll();
            if (Input.GetKeyDown(KeyCode.V)) CopyToClipboard();
            if (Input.GetKeyDown(KeyCode.H)) _uiVisible = !_uiVisible;
        }

// Se ejecuta al dibujar la interfaz.
        void OnGUI()
        {
            if (!toolActive || !_uiVisible) return;
            EnsureStyles();
            _win = GUI.Window(0xF00D, _win, DrawPanel,
                toolActive ? "◆ Route Editor  [ON]" : "◆ Route Editor  [OFF]");
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        void OnDestroy()
        {
            foreach (var m in _markers) if (m) Destroy(m.gameObject);
            if (_line)      Destroy(_line.gameObject);
            if (_lineMat)   Destroy(_lineMat);
            if (_markerMat) Destroy(_markerMat);
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Point capture
        // Intenta capture

        void TryCapture()
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // RaycastAll lets us skip small CityMarker colliders and find the earth.
            RaycastHit[] hits = Physics.RaycastAll(ray);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

// Foreach
            foreach (var h in hits)
            {
                if (h.collider.GetComponent<CityMarker>() != null)
                    continue; // skip city dot colliders

                AddPoint(WorldToLatLon(h.point));
                return;
            }

            // Geometric fallback: direct ray-sphere intersection (works even without collider).
            if (WorldMap.Instance != null
                && RaySphere(ray.origin, ray.direction,
                             WorldMap.Instance.transform.position,
// Earth mundo radius
                             EarthWorldRadius(), out Vector3 p))
            {
                AddPoint(WorldToLatLon(p));
                return;
            }

            _status = "No hit — make sure the Earth sphere has a SphereCollider.";
        }

// Añade point
        void AddPoint(Vector2 ll)
        {
            _pts.Add(ll);
            SpawnMarker();
            RebuildOutput();
            _status = $"Point {_pts.Count} — lat={ll.x:F2}  lon={ll.y:F2}";
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Edit operations
        // Realiza undo last

        void UndoLast()
        {
            if (_pts.Count == 0) return;
            _pts.RemoveAt(_pts.Count - 1);
            Destroy(_markers[_markers.Count - 1].gameObject);
            _markers.RemoveAt(_markers.Count - 1);
            RebuildOutput();
            _status = $"Undo — {_pts.Count} point(s) remaining.";
        }

// Realiza clear all
        void ClearAll()
        {
            _pts.Clear();
            foreach (var m in _markers) if (m) Destroy(m.gameObject);
            _markers.Clear();
            _output = "";
            if (_line) _line.positionCount = 0;
            _status = "Cleared. Click on the globe to start a new route.";
        }

// Realiza copy to clipboard
        void CopyToClipboard()
        {
            _output = BuildFormatted();
            GUIUtility.systemCopyBuffer = _output;
            _status = "✓ Copied to clipboard!";
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Coordinate helpers
        // ══════════════════════════════════════════════════════════════════════════

        // Mundo to latitud longitud.
        static Vector2 WorldToLatLon(Vector3 world)
        {
            if (WorldMap.Instance == null) return default;
            Vector3 local = WorldMap.Instance.transform.InverseTransformPoint(world).normalized;
            float lat     = Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg;
            float lonGame = Mathf.Atan2(local.z, local.x)              * Mathf.Rad2Deg;
            return new Vector2(lat, lonGame);
        }

        // Latitud longitud to mundo.
        static Vector3 LatLonToWorld(float lat, float lonGame, float surfaceOffset = 6f)
        {
            if (WorldMap.Instance == null) return default;
            float latR = lat     * Mathf.Deg2Rad;
            float lonR = lonGame * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(
                Mathf.Cos(latR) * Mathf.Cos(lonR),
                Mathf.Sin(latR),
                Mathf.Cos(latR) * Mathf.Sin(lonR));
            float er = WorldMap.Instance.earthRadius;
            return WorldMap.Instance.transform.TransformPoint(dir * ((er + surfaceOffset) / (er * 2f)));
        }

        // Earth radius in world-space units (accounts for transform scale).
        static float EarthWorldRadius()
        {
            if (WorldMap.Instance == null) return 500f;
            Vector3 edge = WorldMap.Instance.transform.TransformPoint(new Vector3(0.5f, 0f, 0f));
            return (edge - WorldMap.Instance.transform.position).magnitude;
        }

        // Ray-sphere intersection. Devuelve the nearer hit point.
        static bool RaySphere(Vector3 origin, Vector3 dir, Vector3 center, float radius,
                              out Vector3 point)
        {
            Vector3 oc   = origin - center;
            float   b    = Vector3.Dot(oc, dir);
            float   disc = b * b - (oc.sqrMagnitude - radius * radius);
            if (disc < 0f) { point = default; return false; }
            float t = -b - Mathf.Sqrt(disc);
            if (t < 0f) t = -b + Mathf.Sqrt(disc);
            if (t < 0f) { point = default; return false; }
            point = origin + dir * t;
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Visuals
        // Realiza spawn marcador

        void SpawnMarker()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"WP_{_pts.Count}";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * markerSize;

            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial    = _markerMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            _markers.Add(go.transform);
        }

// Actualiza marcador positions
        void UpdateMarkerPositions()
        {
            if (_pts.Count == 0) return;
            for (int i = 0; i < _markers.Count && i < _pts.Count; i++)
                if (_markers[i])
                    _markers[i].position = LatLonToWorld(_pts[i].x, _pts[i].y);
            RedrawLine();
        }

// Realiza redraw line
        void RedrawLine()
        {
            if (_line == null || _pts.Count < 2) { if (_line) _line.positionCount = 0; return; }

            const int SUBDIV = 20; // subdivisions per segment for smooth globe-hugging
            int total = (_pts.Count - 1) * SUBDIV + 1;
            _line.positionCount = total;
            int idx = 0;

            for (int i = 0; i < _pts.Count - 1; i++)
            {
                for (int s = 0; s < SUBDIV; s++, idx++)
                {
                    float t   = s / (float)SUBDIV;
                    float lat = Mathf.Lerp(_pts[i].x, _pts[i + 1].x, t);
                    // Shortest-path longitude interpolation (avoids wrapping artefacts)
                    float dLon = _pts[i + 1].y - _pts[i].y;
                    if (dLon >  180f) dLon -= 360f;
                    if (dLon < -180f) dLon += 360f;
                    float lon = _pts[i].y + dLon * t;
                    _line.SetPosition(idx, LatLonToWorld(lat, lon));
                }
            }
            _line.SetPosition(total - 1,
                LatLonToWorld(_pts[_pts.Count - 1].x, _pts[_pts.Count - 1].y));
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Output
        // Realiza rebuild salida

        void RebuildOutput()
        {
            RedrawLine();
            _output = BuildFormatted();
        }

// Realiza build formatted
        string BuildFormatted()
        {
            if (_pts.Count == 0) return "(no points captured)";

            var sb = new StringBuilder();
            sb.AppendLine("new Vector2[]");
            sb.AppendLine("{");
            for (int i = 0; i < _pts.Count; i++)
            {
                string comma = i < _pts.Count - 1 ? "," : "";
                sb.AppendLine(
                    $"    new Vector2({_pts[i].x:F2}f, {_pts[i].y:F2}f){comma}");
            }
            sb.Append("}");
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════════════════════
        // IMGUI panel
        // Dibuja panel

        void DrawPanel(int _)
        {
            // ── Header ────────────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Points: {_pts.Count}", _bold);
            GUILayout.FlexibleSpace();
            toolActive = GUILayout.Toggle(toolActive, "  Active", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            GUILayout.Label(_status, _small);
            GUILayout.Space(5f);

            // ── Action botones ────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            GUI.enabled = _pts.Count > 0;
            if (GUILayout.Button("Undo",   GUILayout.Height(28))) UndoLast();
            if (GUILayout.Button("Clear",  GUILayout.Height(28))) ClearAll();
            GUI.enabled = true;
            if (GUILayout.Button("Copy ✓", GUILayout.Height(28))) CopyToClipboard();
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            // ── Point list (small, compact) ───────────────────────────────────────
            if (_pts.Count > 0)
            {
                GUILayout.Label("Captured points:", _small);
                var listRect = GUILayoutUtility.GetRect(0, Mathf.Min(_pts.Count * 16f, 96f),
                                                        GUILayout.ExpandWidth(true));
                GUI.Box(listRect, GUIContent.none);
                for (int i = 0; i < _pts.Count; i++)
                {
                    var r = new Rect(listRect.x + 4, listRect.y + i * 16f, listRect.width - 8, 16f);
                    if (r.yMax > listRect.yMax) break;
                    GUI.Label(r,
                        $"  [{i + 1}]  lat={_pts[i].x:F2}  lon={_pts[i].y:F2}", _small);
                }
            }

            GUILayout.Space(4f);

            // ── Output text area ──────────────────────────────────────────────────
            GUILayout.Label("Output — paste into RouteWaypointDB.cs:", _small);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(230f));
            _output = GUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.Space(3f);

            // ── Footer ────────────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild Format", GUILayout.Height(22)))
                _output = BuildFormatted();
            if (GUILayout.Button("Copy ✓", GUILayout.Height(22)))
                CopyToClipboard();
            GUILayout.EndHorizontal();

            // Make panel draggable
            GUI.DragWindow(new Rect(0, 0, _win.width, 22f));
        }

        // Indica si over panel

        bool IsOverPanel()
        {
            Vector2 mp = Input.mousePosition;
            // IMGUI uses Y-down; Input.mousePosition uses Y-up
            return _win.Contains(new Vector2(mp.x, Screen.height - mp.y));
        }

// Realiza ensure estilos
        void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _bold = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 14,
                normal    = { textColor = Color.yellow }
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal   = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            // Dark semi-transparent window background
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.10f, 0.92f));
            bgTex.Apply();
            GUI.skin.window.normal.background    = bgTex;
            GUI.skin.window.onNormal.background  = bgTex;
            GUI.skin.window.normal.textColor     = Color.yellow;
            GUI.skin.window.fontSize             = 13;
            GUI.skin.window.fontStyle            = FontStyle.Bold;
        }
    }
}

#endif