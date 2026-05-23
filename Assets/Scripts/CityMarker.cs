using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityMarker : MonoBehaviour
{
    // Registro global para lookup O(1) por nombre — evita GameObject.Find()
    private static readonly Dictionary<string, CityMarker> _registry = new Dictionary<string, CityMarker>();

    public static bool TryGetMarker(string name, out CityMarker marker)
        => _registry.TryGetValue(name, out marker);

    [Header("Coordenadas geograficas")]
    public float latitude;
    public float longitude;
    public float surfaceOffset = 3f;

    [Header("Daylight Tracking")]
    public string cityName;
    public float  expectedDaylightHours;
    public float  actualDaylightHours;
    public bool   isInDaylight;

    [Header("Label")]
    public float labelOffsetZ = -15f;
   public float labelScale = 0.0003f;  // Reducido de 0.0008f
    public float labelFontSize = 12;     // Reducido de 24

    private Renderer          rend;
    private Material          _rendMaterial;
    private DaylightVerifier  verifier;
    private GameObject        _labelContainer;
    private TextMesh          _labelText;
    private MeshRenderer      _bgRenderer;
    private Material          _bgMaterial;
    private Transform         _cameraTransform;
    private Transform         _earthTransform;
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
        _rendMaterial    = rend.material;
        _earthTransform  = WorldMap.Instance.transform;
        PlaceOnSurface();
        CreateLabel();
        transform.SetParent(_earthTransform, worldPositionStays: true);
        _cameraTransform = Camera.main?.transform;
        verifier = FindAnyObjectByType<DaylightVerifier>();
        if (verifier != null) verifier.RegisterCity(this);
        if (!string.IsNullOrEmpty(cityName)) _registry[cityName] = this;
    }

    void CreateLabel()
    {
        _labelContainer = new GameObject("Label_" + cityName);
        _labelContainer.transform.SetParent(transform, false);
        // Cambia estos valores: X (derecha/izquierda), Y (arriba/abajo), Z (profundidad)
        _labelContainer.transform.localPosition = new Vector3(0.4f, -0.4f, labelOffsetZ);
        _labelContainer.transform.localRotation = Quaternion.identity;

        // ── Fondo con borde suave (gradiente circular) ────────────────────
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BG";
        bg.transform.SetParent(_labelContainer.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.05f);
        bg.transform.localRotation = Quaternion.identity;
        Destroy(bg.GetComponent<Collider>());

        _bgRenderer = bg.GetComponent<MeshRenderer>();

        int texSize = 128;
        Texture2D gradientTex = new Texture2D(texSize, texSize);
        Color centerColor = new Color(0f, 0.2f, 0.05f, 0.85f);
        Color edgeColor = new Color(0f, 0.1f, 0.02f, 0f);

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - texSize / 2f) / (texSize / 2f);
                float dy = (y - texSize / 2f) / (texSize / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                dist = Mathf.Clamp01(dist);

                float alpha = 1f - Mathf.Pow(dist, 3f);
                Color pixel = Color.Lerp(edgeColor, centerColor, alpha);
                pixel.a *= alpha;
                gradientTex.SetPixel(x, y, pixel);
            }
        }
        gradientTex.Apply();
        gradientTex.wrapMode = TextureWrapMode.Clamp;
        gradientTex.filterMode = FilterMode.Bilinear;

        _bgMaterial = new Material(Shader.Find("Unlit/Transparent"));
        _bgMaterial.mainTexture = gradientTex;
        _bgMaterial.color = Color.white;
        _bgMaterial.renderQueue = 2999;
        _bgRenderer.sharedMaterial = _bgMaterial;

        float bgWidth = cityName.Length * 0.3f + 1.5f;  // Reducido de 0.6f y 3f
        float bgHeight = 1.5f;                          // Reducido de 3f
        bg.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);

        // ── Texto ─────────────────────────────────────────────────────────
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(_labelContainer.transform, false);
        textGO.transform.localPosition = Vector3.zero;

        _labelText = textGO.AddComponent<TextMesh>();
        _labelText.text = cityName;
        _labelText.fontSize = (int)labelFontSize;
        _labelText.anchor = TextAnchor.MiddleCenter;
        _labelText.alignment = TextAlignment.Center;
        _labelText.color = new Color(0.7f, 1f, 0.7f);
        _labelText.fontStyle = FontStyle.Bold;
        _labelText.characterSize = 0.50f;
        _labelText.lineSpacing = 1f;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) _labelText.font = f;

        _labelContainer.SetActive(false);
    }

    void LateUpdate()
    {
        if (_labelContainer == null || _cameraTransform == null || !_labelContainer.activeSelf) return;

        _labelContainer.transform.LookAt(_cameraTransform);
        _labelContainer.transform.Rotate(0f, 180f, 0f);

        float d = Vector3.Distance(_labelContainer.transform.position, _cameraTransform.position);
        float scale = d * labelScale;
        _labelContainer.transform.localScale = new Vector3(scale, scale, 1f);

        if (_earthTransform != null)
        {
            Vector3 toLabel = (_labelContainer.transform.position - _earthTransform.position).normalized;
            Vector3 toCamera = (_cameraTransform.position - _earthTransform.position).normalized;
            float dot = Vector3.Dot(toLabel, toCamera);

            float alpha = dot > 0.05f ? 1f : 0f;
            if (_bgMaterial != null)
            {
                Color c = _bgMaterial.color;
                c.a = alpha;
                _bgMaterial.color = c;
            }
            if (_labelText != null)
            {
                Color t = _labelText.color;
                t.a = alpha;
                _labelText.color = t;
            }
        }
    }

    public void PlaceOnSurface()
    {
        if (WorldMap.Instance == null) return;
        Vector3 pos = WorldMap.Instance.LatLonToPosition(latitude, longitude, WorldMap.Instance.earthRadius);
        Vector3 n   = pos.normalized;
        transform.position = pos + n * surfaceOffset;

        Vector3 north = Vector3.ProjectOnPlane(Vector3.up, n).normalized;
        if (north.sqrMagnitude < 0.01f)
            north = Vector3.ProjectOnPlane(Vector3.forward, n).normalized;
        transform.rotation = Quaternion.LookRotation(n, north);
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
        { int m = TimeManager.Instance.CurrentUtcTime.Month; expectedDaylightHours = DaylightVerifier.ComputeAstronomicalDaylight(latitude, m, 15); }
    }

    void UpdateDaylightColor()
    {
        if (_rendMaterial != null) _rendMaterial.SetFloat("_Brightness", isInDaylight ? 1.2f : 0.5f);
    }

    public bool HasCompletedFirstDay() => firstDayCompleted;

    void OnMouseEnter()
    {
        if (_rendMaterial != null) _rendMaterial.SetFloat("_Selected", 1f);
        if (_labelContainer != null) { if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); _fadeRoutine = StartCoroutine(FadeIn()); }
    }

    void OnMouseExit()
    {
        if (_rendMaterial != null) _rendMaterial.SetFloat("_Selected", 0f);
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
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            float d = Vector3.Distance(_labelContainer.transform.position, _cameraTransform.position);
            _labelContainer.transform.localScale = new Vector3(d * labelScale * easedT, d * labelScale * easedT, 1f);
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
            float easedT = 1f - (t * t);

            _labelContainer.transform.localScale = new Vector3(startScale.x * easedT, startScale.y * easedT, 1f);
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

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(cityName)) _registry.Remove(cityName);
        if (verifier != null) verifier.UnregisterCity(this);
        if (_labelContainer != null) Destroy(_labelContainer);
    }

#if UNITY_EDITOR
    [ContextMenu("Actualizar posicion (editor)")]
    private void EditorPlace() => PlaceOnSurface();
#endif
}