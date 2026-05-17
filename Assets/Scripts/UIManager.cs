using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    [Header("Panel (opcional)")]
    public RectTransform uiHubPanel;

    // ── EVENTS (desacoplado) ────────────────────────────────────────────────
    public Action<int> OnSpeedChanged;
    public Action OnLockCamera;
    public Action<string> OnSearchSubmitted;

    // ── Styles ───────────────────────────────────────────────────────────────
    private GUIStyle _btnStyle;
    private GUIStyle _btnActiveStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _coordStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _searchStyle;
    private GUIStyle _coordBoxStyle;
    private bool _stylesReady;

    // ── Hover coords ─────────────────────────────────────────────────────────
    private Camera _cam;
    private MapCameraController _camController;
    private DaylightVerifier _daylightVerifier;

    private string _coordText = "";
    private bool _hovering;
    private Vector3 _lastMousePos = Vector3.negativeInfinity;

    // ── Search ───────────────────────────────────────────────────────────────
    private string _searchBuffer = "";
    private bool _focusSearchNextFrame;

    // ── Button layout ────────────────────────────────────────────────────────
    private static readonly string[] BTN_LABELS = { "PAUSA", "x1", "x10", "x100", "x1000" };
    private const float BTN_H = 32f;
    private const float BTN_W0 = 80f;
    private const float BTN_W1 = 58f;
    private const float GAP = 4f;
    private const float TOP = 8f;

    void Start()
    {
        _cam = Camera.main;
        _camController = FindAnyObjectByType<MapCameraController>();
        _daylightVerifier = FindAnyObjectByType<DaylightVerifier>();

        CenterUIHubPanel();
    }

    void Update()
    {
        UpdateHoverCoords();
    }

    // ── Hover coords optimizado ──────────────────────────────────────────────
    private void UpdateHoverCoords()
    {
        if (_cam == null || WorldMap.Instance == null)
        {
            _hovering = false;
            return;
        }

        Vector3 mousePos = Input.mousePosition;

        // 🔥 OPTIMIZACIÓN REAL
        if ((mousePos - _lastMousePos).sqrMagnitude < 4f) return;
        _lastMousePos = mousePos;

        Ray ray = _cam.ScreenPointToRay(mousePos);

        Vector3 center = WorldMap.Instance.transform.position;
        float radius = WorldMap.Instance.earthRadius;

        if (RaySphere(ray, center, radius, out Vector3 hit))
        {
            Vector3 local = WorldMap.Instance.transform.InverseTransformPoint(hit);
            Vector3 dir = local.normalized;

            float lat = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            float lon = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

            _coordText = $"Lat: {lat:F2}°   Lon: {lon:F2}°";
            _hovering = true;
        }
        else
        {
            _hovering = false;
        }
    }

    private static bool RaySphere(Ray ray, Vector3 center, float radius, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        Vector3 oc = ray.origin - center;
        float b = Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float d = b * b - c;

        if (d < 0f) return false;

        float t = -b - Mathf.Sqrt(d);
        if (t < 0f) t = -b + Mathf.Sqrt(d);
        if (t < 0f) return false;

        hitPoint = ray.origin + t * ray.direction;
        return true;
    }

    // ── OnGUI ────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        EnsureStyles();

        if (TimeManager.Instance == null) return;

        // DrawSpeedButtons y DrawDateTime: movidos al top bar de FFUIManager
        DrawCityNavigationInfo();
        DrawSearchBar();
        DrawCoords();
    }

    // ── BOTONES VELOCIDAD ────────────────────────────────────────────────────
    private void DrawSpeedButtons()
    {
        float totalW = BTN_W0 + 4f * BTN_W1 + 4f * GAP;
        float startX = (Screen.width - totalW) * 0.5f;
        float x = startX;

        for (int i = 0; i < BTN_LABELS.Length; i++)
        {
            float w = i == 0 ? BTN_W0 : BTN_W1;
            var rect = new Rect(x, TOP, w, BTN_H);

            bool active = TimeManager.Instance.CurrentSpeedIndex == i;

            if (GUI.Button(rect, BTN_LABELS[i], active ? _btnActiveStyle : _btnStyle))
            {
                OnSpeedChanged?.Invoke(i);

                // fallback (por si no conectaste eventos todavía)
                TimeManager.Instance.SetSpeedIndex(i);
            }

            x += w + GAP;
        }
    }

    // ── FECHA ────────────────────────────────────────────────────────────────
    private void DrawDateTime()
    {
        string text = TimeManager.Instance.CurrentLocalTime.ToString("dd/MM/yyyy   HH:mm:ss") + "  BUE";

        float w = 320f;
        float y = TOP + BTN_H + 4f;

        GUI.Label(new Rect((Screen.width - w) * 0.5f, y, w, 24f), text, _labelStyle);
    }

    // ── NAVEGACIÓN ───────────────────────────────────────────────────────────
    private void DrawCityNavigationInfo()
    {
        if (_camController == null) return;

        float w = 350f;
        float y = Screen.height - 100f;
        float x = (Screen.width - w) * 0.5f;

        GUI.Label(new Rect(x, y, w, 22f),
            $"{_camController.CurrentCityName}",
            _labelStyle);

        y += 22f;

        GUI.Label(new Rect(x, y, w, 18f),
            "↑↓: ciudades | F: buscar | R: reset",
            _smallStyle);
    }

    // ── SEARCH BAR (ARREGLADO) ───────────────────────────────────────────────
    private void DrawSearchBar()
    {
        if (_camController == null || !_camController.IsTypingCityName)
            return;

        float w = 300f;
        float h = 30f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.5f - h * 0.5f;

        GUI.Box(new Rect(x - 10, y - 40, w + 20, h + 50),
            "Buscar Ciudad",
            _searchStyle);

        GUI.SetNextControlName("CitySearch");

        // 🔥 IMPORTANTE: SOLO MOSTRAR (no asignar)
        GUI.TextField(
            new Rect(x, y, w, h),
            _camController.CitySearchString
        );

        // 🔥 Forzar foco (para que el controller reciba teclado)
        GUI.FocusControl("CitySearch");

        GUI.Label(new Rect(x, y + h + 5, w, 20f),
            "Escribiendo... Enter / Esc manejado por sistema",
            _smallStyle);
    }

    // ── COORDS ───────────────────────────────────────────────────────────────
    private void DrawCoords()
    {
        float zoom = _camController != null ? _camController.ZoomPercent : 0f;

        float lineH = 22f;
        float rows = _hovering ? 2f : 1f;

        float w = 280f;
        float h = lineH * rows + 4f;

        float x = Screen.width - w - 10f;
        float y = Screen.height - h - 28f - 6f;  // 28f = altura del ticker

        GUI.Box(new Rect(x, y - 2, w, h + 4), GUIContent.none, _coordBoxStyle);

        GUI.Label(new Rect(x, y, w, lineH),
            $"Zoom: {zoom:F0}%",
            _coordStyle);

        if (_hovering)
        {
            GUI.Label(new Rect(x, y + lineH, w, lineH),
                _coordText,
                _coordStyle);
        }
    }

    // ── STYLES ───────────────────────────────────────────────────────────────
    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        _btnStyle.normal.textColor = Color.white;
        _btnStyle.hover.textColor = Color.yellow;

        _btnActiveStyle = new GUIStyle(_btnStyle);
        _btnActiveStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.5f, 0.1f, 0.95f));
        _btnActiveStyle.normal.textColor = Color.yellow;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _labelStyle.normal.textColor = Color.white;

        _coordStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        _coordStyle.normal.textColor = Color.white;

        _smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };
        _smallStyle.normal.textColor = Color.gray;

        _searchStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _searchStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.85f));
        _searchStyle.normal.textColor = Color.white;

        _coordBoxStyle = new GUIStyle(GUI.skin.box);
        _coordBoxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.55f));
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;

        Texture2D tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    public void CenterUIHubPanel()
    {
        if (uiHubPanel == null) return;

        uiHubPanel.anchorMin = new Vector2(0.5f, 1f);
        uiHubPanel.anchorMax = new Vector2(0.5f, 1f);
        uiHubPanel.pivot = new Vector2(0.5f, 1f);
        uiHubPanel.anchoredPosition = Vector2.zero;
    }
}