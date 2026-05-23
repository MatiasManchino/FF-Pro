using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panel (opcional)")]
    public RectTransform uiHubPanel;

    // ── EVENTS (desacoplado) ────────────────────────────────────────────────
    public Action<int>    OnSpeedChanged;
    public Action         OnLockCamera;
    public Action<string> OnSearchSubmitted;

    // ── Refs ────────────────────────────────────────────────────────────────
    private Camera               _cam;
    private MapCameraController  _camController;

    // ── Hover coords ─────────────────────────────────────────────────────────
    private string  _coordText = "";
    private bool    _hovering;
    private Vector3 _lastMousePos = Vector3.negativeInfinity;

    // ── RefreshDisplay dirty-check cache ──────────────────────────────────────
    private string _lastCityName  = null;
    private int    _lastZoomInt   = -1;
    private bool   _lastHovering  = false;
    private string _lastCoordText = "";
    private bool   _lastTyping    = false;
    private string _lastSearch    = "";

    private static Font _fontCache;
    private static Font _font => _fontCache != null
        ? _fontCache : (_fontCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    // ── UGUI refs ─────────────────────────────────────────────────────────────
    private Text          _cityNavText;
    private Text          _cityNavHint;
    private Text          _coordsText;
    private RectTransform _coordsPanel;
    private GameObject    _searchOverlay;
    private Text          _searchText;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        _cam           = Camera.main;
        _camController = FindAnyObjectByType<MapCameraController>();

        BuildUI();
        CenterUIHubPanel();
    }

    private void Update()
    {
        UpdateHoverCoords();
        RefreshDisplay();
    }

    // ── Hover coords (unchanged logic) ───────────────────────────────────────

    private void UpdateHoverCoords()
    {
        if (_cam == null || WorldMap.Instance == null) { _hovering = false; return; }

        Vector3 mousePos = Input.mousePosition;
        if ((mousePos - _lastMousePos).sqrMagnitude < 4f) return;
        _lastMousePos = mousePos;

        Ray ray = _cam.ScreenPointToRay(mousePos);
        Vector3 center = WorldMap.Instance.transform.position;
        float   radius = WorldMap.Instance.earthRadius;

        if (RaySphere(ray, center, radius, out Vector3 hit))
        {
            Vector3 local = WorldMap.Instance.transform.InverseTransformPoint(hit);
            Vector3 dir   = local.normalized;
            float lat = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            float lon = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            _coordText = $"Lat: {lat:F2}°   Lon: {lon:F2}°";
            _hovering  = true;
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

    // ── Display refresh ───────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (_camController == null) return;

        string cityName = _camController.CurrentCityName;
        if (_cityNavText != null && cityName != _lastCityName)
        {
            _cityNavText.text = cityName;
            _lastCityName     = cityName;
        }

        if (_coordsText != null)
        {
            int  zoomInt = Mathf.RoundToInt(_camController.ZoomPercent);
            bool hov     = _hovering;
            if (zoomInt != _lastZoomInt || hov != _lastHovering || _coordText != _lastCoordText)
            {
                _coordsText.text = hov
                    ? $"Zoom: {zoomInt}%\n{_coordText}"
                    : $"Zoom: {zoomInt}%";
                _lastZoomInt   = zoomInt;
                _lastHovering  = hov;
                _lastCoordText = _coordText;
            }
        }

        if (_searchOverlay != null)
        {
            bool typing = _camController.IsTypingCityName;
            if (typing != _lastTyping)
            {
                _searchOverlay.SetActive(typing);
                _lastTyping = typing;
            }
            if (typing && _searchText != null)
            {
                string s = _camController.CitySearchString;
                if (s != _lastSearch)
                {
                    _searchText.text = s;
                    _lastSearch      = s;
                }
            }
        }
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasRT = GetOrCreateCanvas();

        // ── City navigation info — bottom center ──────────────────────────────
        var navPanel = MakeRect("CityNavPanel", canvasRT,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 28f + 6f + 44f), new Vector2(350, 44f));

        _cityNavText = MakeText("CityName", navPanel,
            Vector2.zero, Vector2.zero, "",
            14, FontStyle.Bold, Color.white, TextAnchor.UpperCenter, stretch: true);

        _cityNavHint = MakeText("CityHint", navPanel,
            new Vector2(0, 22), Vector2.zero, "↑↓: ciudades  |  F: buscar  |  R: reset",
            11, FontStyle.Normal, Color.gray, TextAnchor.UpperCenter, stretch: true);

        // ── Coordinates — bottom right ────────────────────────────────────────
        _coordsPanel = MakeRect("CoordsPanel", canvasRT,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-10f, 28f + 6f), new Vector2(280, 48f));
        MakeImage(_coordsPanel, new Color(0, 0, 0, 0.55f));

        _coordsText = MakeText("CoordsLabel", _coordsPanel,
            Vector2.zero, Vector2.zero, "Zoom: 0%",
            13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight, stretch: true);

        // ── Search overlay — centered modal ───────────────────────────────────
        _searchOverlay = new GameObject("SearchOverlay");
        var overlayRT  = _searchOverlay.AddComponent<RectTransform>();
        overlayRT.SetParent(canvasRT, false);
        overlayRT.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRT.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRT.pivot     = new Vector2(0.5f, 0.5f);
        overlayRT.anchoredPosition = Vector2.zero;
        overlayRT.sizeDelta = new Vector2(320, 90f);
        MakeImage(overlayRT, new Color(0f, 0f, 0f, 0.85f));

        MakeText("SearchTitle", overlayRT,
            new Vector2(0, -6), Vector2.zero, "Buscar Ciudad",
            14, FontStyle.Bold, Color.white, TextAnchor.UpperCenter, stretch: true);

        _searchText = MakeText("SearchInput", overlayRT,
            new Vector2(0, -32), Vector2.zero, "",
            16, FontStyle.Normal, Color.yellow, TextAnchor.MiddleCenter, stretch: true);

        MakeText("SearchHint", overlayRT,
            new Vector2(0, -58), Vector2.zero, "Escribiendo...  Enter / Esc para confirmar",
            10, FontStyle.Normal, Color.gray, TextAnchor.LowerCenter, stretch: true);

        _searchOverlay.SetActive(false);
    }

    // ── Legacy helpers ────────────────────────────────────────────────────────

    public void CenterUIHubPanel()
    {
        if (uiHubPanel == null) return;
        uiHubPanel.anchorMin = new Vector2(0.5f, 1f);
        uiHubPanel.anchorMax = new Vector2(0.5f, 1f);
        uiHubPanel.pivot     = new Vector2(0.5f, 1f);
        uiHubPanel.anchoredPosition = Vector2.zero;
    }

    // ── UGUI factory helpers ──────────────────────────────────────────────────

    private static RectTransform GetOrCreateCanvas()
    {
        var existing = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (existing != null) return existing.GetComponent<RectTransform>();

        var cgo = new GameObject("UICanvas");
        var c   = cgo.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();
        return cgo.GetComponent<RectTransform>();
    }

    private static RectTransform MakeRect(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return rt;
    }

    private static Image MakeImage(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text MakeText(string name, RectTransform parent,
        Vector2 offset, Vector2 size, string text,
        int fontSize, FontStyle style, Color color, TextAnchor anchor,
        bool stretch = false)
    {
        RectTransform rt;
        if (stretch)
        {
            var go = new GameObject(name);
            rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offset;
            rt.offsetMax = size;
        }
        else
        {
            rt = MakeRect(name, parent,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                offset, size);
        }
        var t = rt.gameObject.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = anchor;
        t.font      = _font;
        return t;
    }
}
