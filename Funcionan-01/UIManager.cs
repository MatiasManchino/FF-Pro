using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panel (opcional)")]
    public RectTransform uiHubPanel;

    // ── Estilos ────────────────────────────────────────────────────────────────
    private GUIStyle _btnStyle;
    private GUIStyle _btnActiveStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _coordStyle;
    private bool     _stylesReady;

    // ── Hover coords ──────────────────────────────────────────────────────────
    private Camera  _cam;
    private string  _coordText = "";
    private bool    _hovering;

    // ── Botón diseño ─────────────────────────────────────────────────────────
    private static readonly string[] BTN_LABELS  = { "PAUSA", "x1", "x10", "x100", "x1000" };
    private const float BTN_H  = 32f;
    private const float BTN_W0 = 80f;   // PAUSA
    private const float BTN_W1 = 58f;   // speeds
    private const float GAP    = 4f;
    private const float TOP    = 8f;

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        _cam = Camera.main;
        CenterUIHubPanel();
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        UpdateHoverCoords();
    }

    // ── Ratón → latitud/longitud ───────────────────────────────────────────────────────
    private void UpdateHoverCoords()
    {
        if (_cam == null || WorldMap.Instance == null) { _hovering = false; return; }

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        Vector3 earthCenter = WorldMap.Instance.transform.position;
        float   earthRadius = WorldMap.Instance.earthRadius;

        if (RaySphere(ray, earthCenter, earthRadius, out Vector3 hit))
        {
            // Transform hit to Earth's local space (accounts for Earth rotation)
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

    // Analytical ray-sphere intersection — does not depend on colliders
    private static bool RaySphere(Ray ray, Vector3 center, float radius, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        Vector3 oc = ray.origin - center;
        float   b  = Vector3.Dot(oc, ray.direction);
        float   c  = Vector3.Dot(oc, oc) - radius * radius;
        float   d  = b * b - c;
        if (d < 0f) return false;
        float t = -b - Mathf.Sqrt(d);
        if (t < 0f) t = -b + Mathf.Sqrt(d);
        if (t < 0f) return false;
        hitPoint = ray.origin + t * ray.direction;
        return true;
    }

    // Se ejecuta al dibujar la interfaz.
    void OnGUI()
    {
        EnsureStyles();

        if (TimeManager.Instance == null) return;

        DrawSpeedButtons();
        DrawDateTime();
        if (_hovering) DrawCoords();
    }

// Dibuja velocidad botones
    private void DrawSpeedButtons()
    {
        // Total ancho of all botones
        float totalW = BTN_W0 + 4f * BTN_W1 + 4f * GAP;
        float startX = (Screen.width - totalW) * 0.5f;
        float x      = startX;

        for (int i = 0; i < BTN_LABELS.Length; i++)
        {
            float w    = i == 0 ? BTN_W0 : BTN_W1;
            var   rect = new Rect(x, TOP, w, BTN_H);
            bool  active = TimeManager.Instance.CurrentSpeedIndex == i;

            if (GUI.Button(rect, BTN_LABELS[i], active ? _btnActiveStyle : _btnStyle))
                TimeManager.Instance.SetSpeedIndex(i);

            x += w + GAP;
        }
    }

// Dibuja date tiempo
    private void DrawDateTime()
    {
        string text = TimeManager.Instance.CurrentLocalTime.ToString("dd/MM/yyyy   HH:mm:ss") + "  BUE";
        float  w    = 320f;
        float  y    = TOP + BTN_H + 4f;
        GUI.Label(new Rect((Screen.width - w) * 0.5f, y, w, 24f), text, _labelStyle);
    }

// Dibuja coords
    private void DrawCoords()
    {
        // Dark semi-transparent background + white text — bottom left
        float w = 240f, h = 26f;
        float x = 10f, y = Screen.height - h - 10f;
        GUI.Box(new Rect(x - 4, y - 2, w + 8, h + 4), GUIContent.none, _coordBoxStyle);
        GUI.Label(new Rect(x, y, w, h), _coordText, _coordStyle);
    }

    // ── Estilos (debe be creado inside OnGUI) ─────────────────────────────────
    private GUIStyle _coordBoxStyle;

// Gestiona ensure styles.
    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
        };
        _btnStyle.normal.textColor  = Color.white;
        _btnStyle.hover.textColor   = Color.yellow;

        _btnActiveStyle = new GUIStyle(_btnStyle);
        _btnActiveStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.5f, 0.1f, 0.95f));
        _btnActiveStyle.normal.textColor  = Color.yellow;
        _btnActiveStyle.hover.textColor   = Color.yellow;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _labelStyle.normal.textColor = Color.white;

        _coordStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
        };
        _coordStyle.normal.textColor = Color.white;

        _coordBoxStyle = new GUIStyle(GUI.skin.box);
        _coordBoxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.55f));
    }

// Gestiona make tex.
    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h);
        t.SetPixels(pix); t.Apply();
        return t;
    }

// Gestiona center UI hub panel.
    public void CenterUIHubPanel()
    {
        if (uiHubPanel == null) return;
        uiHubPanel.anchorMin        = new Vector2(0.5f, 1f);
        uiHubPanel.anchorMax        = new Vector2(0.5f, 1f);
        uiHubPanel.pivot            = new Vector2(0.5f, 1f);
        uiHubPanel.anchoredPosition = Vector2.zero;
    }
}