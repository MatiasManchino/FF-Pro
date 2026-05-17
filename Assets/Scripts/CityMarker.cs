using System;
using System.Collections;
using UnityEngine;

public class CityMarker : MonoBehaviour
{
    [Header("Coordenadas geograficas")]
    public float latitude;
    public float longitude;
    public float surfaceOffset = 3f;

    [Header("Colores")]
    public Color normalColor   = new Color(1f, 0.9f, 0f);
    public Color selectedColor = new Color(1f, 0.45f, 0f);

    [Header("Daylight Tracking")]
    public string cityName;
    public float  expectedDaylightHours;
    public float  actualDaylightHours;
    public bool   isInDaylight;

    [Header("Label")]
    public float labelOffsetY = -40f;
    public float labelScale = 0.0035f;

    private Renderer          rend;
    private DaylightVerifier  verifier;
    private GameObject        _labelContainer;
    private TextMesh          _labelText;
    private MeshRenderer      _bgRenderer;
    private Material          _bgMaterial;
    private Transform         _cameraTransform;
    private Coroutine         _fadeRoutine;

    private DateTime lastSampledUtc;
    private double   accumulatedDaylightSeconds;
    private int      lastSampledDayOfYear = -1;
    private bool     trackingInitialized;
    private bool     firstDayCompleted;

    private const float HORIZON_THRESHOLD = -0.01454f;

    void Start()
    {
        if (WorldMap.Instance == null) { Debug.LogError("[CityMarker] WorldMap.Instance no disponible."); enabled = false; return; }
        rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = normalColor;
        PlaceOnSurface();
        CreateLabel();
        transform.SetParent(WorldMap.Instance.transform, worldPositionStays: true);
        _cameraTransform = Camera.main?.transform;
        verifier = FindFirstObjectByType<DaylightVerifier>();
        if (verifier != null) verifier.RegisterCity(this);
    }

    void CreateLabel()
    {
        _labelContainer = new GameObject("Label_" + cityName);
        _labelContainer.transform.SetParent(transform, false);
        _labelContainer.transform.localPosition = new Vector3(0f, labelOffsetY, 0f);

        // ── Fondo ──────────────────────────────────────────────────────────
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BG";
        bg.transform.SetParent(_labelContainer.transform, false);
        // Z = 0.1 para que esté físicamente detrás del texto (Z = 0)
        bg.transform.localPosition = new Vector3(0f, 0f, 0.1f); 
        bg.transform.localRotation = Quaternion.identity;
        Destroy(bg.GetComponent<Collider>());

        _bgRenderer = bg.GetComponent<MeshRenderer>();

        Texture2D whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();

        // Sprites/Default soporta _Color y transparencia en Built-in pipeline
        _bgMaterial = new Material(Shader.Find("Sprites/Default"));
        _bgMaterial.mainTexture = whiteTex;
        _bgMaterial.color = new Color(0f, 0.15f, 0.05f, 0.9f);
        // Forzar que el fondo se dibuje antes que el texto en el motor de renderizado
        _bgMaterial.renderQueue = 2999; 
        _bgRenderer.sharedMaterial = _bgMaterial;

        // ── Texto ─────────────────────────────────────────────────────────
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(_labelContainer.transform, false);
        textGO.transform.localPosition = Vector3.zero; // Texto en el frente

        _labelText = textGO.AddComponent<TextMesh>();
        _labelText.text = cityName;
        _labelText.fontSize = 10;
        _labelText.anchor = TextAnchor.MiddleCenter;
        _labelText.alignment = TextAlignment.Center;
        _labelText.color = new Color(0.8f, 1f, 0.8f);
        _labelText.fontStyle = FontStyle.Bold;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) _labelText.font = f;

        float bgWidth = (float)cityName.Length * 0.55f + 1.5f;
        float bgHeight = 2.2f;
        bg.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);

        _labelContainer.transform.localScale = Vector3.zero;
        _labelContainer.SetActive(false);
    }


    void LateUpdate()
    {
        if (_labelContainer == null || _cameraTransform == null || !_labelContainer.activeSelf) return;
        _labelContainer.transform.LookAt(_cameraTransform);
        _labelContainer.transform.Rotate(0f, 180f, 0f);
        float d = Vector3.Distance(_labelContainer.transform.position, _cameraTransform.position);
        _labelContainer.transform.localScale = Vector3.one * (d * labelScale);
    }

    public void PlaceOnSurface()
    {
        if (WorldMap.Instance == null) return;
        Vector3 pos = WorldMap.Instance.LatLonToPosition(latitude, longitude, WorldMap.Instance.earthRadius);
        Vector3 n = pos.normalized;
        transform.position = pos + n * surfaceOffset;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
    }

    void Update() { CheckDaylight(); }

    void CheckDaylight()
    {
        if (SunController.Instance == null || WorldMap.Instance == null || TimeManager.Instance == null) return;
        Vector3 sd = SunController.Instance.GetSunDirection();
        Vector3 cn = (transform.position - WorldMap.Instance.transform.position).normalized;
        isInDaylight = Vector3.Dot(cn, sd) > HORIZON_THRESHOLD;
        DateTime utcNow = TimeManager.Instance.CurrentUtcTime;
        int doy = utcNow.DayOfYear;
        if (!trackingInitialized) { lastSampledUtc = utcNow; lastSampledDayOfYear = doy; trackingInitialized = true; UpdateExpectedDaylight(); UpdateDaylightColor(); return; }
        if (doy != lastSampledDayOfYear) { if (firstDayCompleted) actualDaylightHours = (float)(accumulatedDaylightSeconds / 3600.0); accumulatedDaylightSeconds = 0.0; lastSampledDayOfYear = doy; firstDayCompleted = true; UpdateExpectedDaylight(); }
        double delta = (utcNow - lastSampledUtc).TotalSeconds;
        if (delta > 0.0 && isInDaylight) accumulatedDaylightSeconds += delta;
        lastSampledUtc = utcNow;
        UpdateDaylightColor();
    }

    void UpdateExpectedDaylight()
    {
        if (verifier != null && !string.IsNullOrEmpty(cityName))
        { int m = TimeManager.Instance.CurrentUtcTime.Month; float e = verifier.GetExpectedDaylight(cityName, m); if (e >= 0f) expectedDaylightHours = e; }
    }

    void UpdateDaylightColor()
    {
        if (rend != null) rend.material.color = isInDaylight ? Color.Lerp(normalColor, Color.white, 0.3f) : Color.Lerp(normalColor, new Color(0.3f, 0.3f, 0.4f), 0.5f);
    }

    public bool HasCompletedFirstDay() => firstDayCompleted;

    void OnMouseEnter()
    {
        SetColor(selectedColor);
        if (_labelContainer != null) { if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); _fadeRoutine = StartCoroutine(FadeIn()); }
    }

    void OnMouseExit()
    {
        SetColor(normalColor);
        if (_labelContainer != null) { if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); _fadeRoutine = StartCoroutine(FadeOut()); }
    }

    IEnumerator FadeIn()
    {
        _labelContainer.SetActive(true);
        float duration = 0.3f;
        float el = 0f;
        while (el < duration)
        {
            el += Time.deltaTime;
            float t = el / duration;
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
            
            _labelContainer.transform.localScale = Vector3.one * easedT * (Vector3.Distance(_labelContainer.transform.position, _cameraTransform.position) * labelScale);
            SetLabelAlpha(easedT);
            yield return null;
        }
    }

    IEnumerator FadeOut()
    {
        float duration = 0.2f;
        float el = 0f;
        Vector3 startScale = _labelContainer.transform.localScale;
        while (el < duration)
        {
            el += Time.deltaTime;
            float t = el / duration;
            float easedT = 1f - (t * t); // Ease in
            
            _labelContainer.transform.localScale = startScale * easedT;
            SetLabelAlpha(easedT);
            yield return null;
        }
        _labelContainer.SetActive(false);
    }


    void SetLabelAlpha(float a)
    {
        if (_bgMaterial != null) { Color c = _bgMaterial.color; c.a = 0.9f * a; _bgMaterial.color = c; }
        if (_labelText != null) { Color t = _labelText.color; t.a = a; _labelText.color = t; }
    }

    void SetColor(Color c) { if (rend != null) rend.material.color = c; }

    void OnDestroy()
    {
        if (verifier != null) verifier.UnregisterCity(this);
        if (_labelContainer != null) Destroy(_labelContainer);
    }

#if UNITY_EDITOR
    [ContextMenu("Actualizar posicion (editor)")]
    private void EditorPlace() => PlaceOnSurface();
#endif
}