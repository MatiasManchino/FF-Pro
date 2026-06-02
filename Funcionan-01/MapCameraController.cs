using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform earthTransform;

    [Header("Zoom")]
    public float initialDistance = 20f;
    public float minDistance     = 12f;
    public float maxDistance     = 50f;
    [Tooltip("Fracción de la distancia actual por unidad de scroll.")]
    public float zoomSpeed       = 0.12f;
    public float zoomSmooth      = 0.12f;

    [Header("Inercia al soltar")]
    [Range(0f, 15f)]
    public float inertiaDamping  = 7f;

    [Header("Suavizado de órbita (solo al soltar)")]
    [Range(0.01f, 0.5f)]
    public float orbitSmooth     = 0.08f;

    // ── Objetivo state ─────────────────────────────────────────────────────────
    private float   _tgtRotX;
    private float   _tgtRotY;
    private float   _tgtDist;
    private Vector3 _tgtLookAt;

    // ── Smoothed state ────────────────────────────────────────────────────────
    private float   _sRotX;
    private float   _sRotY;
    private float   _sDist;
    private Vector3 _sLookAt;

    // ── SmoothDamp velocities ─────────────────────────────────────────────────
    private float   _velRotX, _velRotY, _velDist;
    private Vector3 _velLookAt;

    // ── Drag ─────────────────────────────────────────────────────────────────
    private bool    _dragging;
    private bool    _didDrag;
    private Vector2 _prevMousePx;
    private float   _inertiaX;
    private float   _inertiaY;

    private const float DRAG_THRESHOLD_PX = 6f;

    // ── Focus ─────────────────────────────────────────────────────────────────
    private bool      _isFollowing;
    private Coroutine _followRoutine;

    // ── Follow permanente (R + inicio) ────────────────────────────────────────
    private bool      _followPermanent;
    private Transform _followTarget;

    private Camera _cam;

    // Coordenadas de Buenos Aires para el respaldo
    private const float BA_LAT = -38.45f;
    private const float BA_LON = -58.38f;

// Configura referencias tempranas antes de Start.
    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (earthTransform == null && WorldMap.Instance != null)
            earthTransform = WorldMap.Instance.transform;
    }

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        if (earthTransform == null)
        {
            Debug.LogError("[Camera] earthTransform no asignado.");
            enabled = false;
            return;
        }

        _tgtDist   = initialDistance;
        _sDist     = initialDistance;
        _tgtLookAt = _sLookAt = earthTransform.position;

        // Esperar un fotograma para que los CityMarker se hayan posicionado
        StartCoroutine(InitFollow());
    }

// Inicializa seguimiento.
    private IEnumerator InitFollow()
    {
        yield return null; // espera a que todos los Start() terminen
        StartPermanentFollow("Buenos Aires", BA_LAT, BA_LON);
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (_followPermanent && _followTarget != null)
            UpdateFollowTargetFromTransform();

        HandleDrag();
        HandleZoom();
        if (Input.GetKeyDown(KeyCode.R)) ResetCameraPosition();
        ApplyInertia();
        SmoothAndApply();
    }

    // ── Seguimiento permanente (usado al inicio y con R) ──────────────────────
    private void StartPermanentFollow(string cityName, float lat, float lon)
    {
        if (_followRoutine != null)
        {
            StopCoroutine(_followRoutine);
            _followRoutine = null;
        }

        GameObject cityGo = GameObject.Find(cityName);
        if (cityGo != null)
        {
            _followTarget = cityGo.transform;
        }
        else
        {
            Debug.LogWarning($"[Camera] Marcador '{cityName}' no encontrado, usando coordenadas.");
            _followTarget = null;
        }

        _followPermanent = true;
        _isFollowing     = true;
        _inertiaX = _inertiaY = 0f;

        if (_followTarget != null)
        {
            SetCameraToTarget(_followTarget);
        }
        else
        {
            SetCameraToLatLon(lat, lon);
        }
    }

// Establece cámara to objetivo.
    private void SetCameraToTarget(Transform target)
    {
        if (earthTransform == null) return;
        Vector3 earthCenter = earthTransform.position;
        Vector3 targetWorldPos = target.position;
        Vector3 dirFromCenter = (targetWorldPos - earthCenter).normalized;

        _tgtRotX = Mathf.Asin(Mathf.Clamp(dirFromCenter.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = Mathf.Atan2(dirFromCenter.x, dirFromCenter.z) * Mathf.Rad2Deg;
        _tgtDist = initialDistance;
        _tgtLookAt = earthCenter;
        SnapSmoothedToTarget();
    }

// Establece cámara to latitud longitud.
    private void SetCameraToLatLon(float lat, float lon)
    {
        if (WorldMap.Instance == null || earthTransform == null) return;

        // Obtener dirección local pura (radio = 1)
        Vector3 localDir = WorldMap.Instance.LatLonToPosition(lat, lon, 1.0f);
        // Aplicar solo la rotación de la Tierra (sin escala)
        Vector3 worldDir = earthTransform.TransformDirection(localDir).normalized;

        Vector3 earthCenter = earthTransform.position;

        _tgtRotX = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
        _tgtDist = initialDistance;
        _tgtLookAt = earthCenter;
        SnapSmoothedToTarget();
    }

// Acomoda smoothed to objetivo
    private void SnapSmoothedToTarget()
    {
        _sRotX = _tgtRotX; _velRotX = 0f;
        _sRotY = _tgtRotY; _velRotY = 0f;
        _sDist = _tgtDist; _velDist = 0f;
        _sLookAt = _tgtLookAt; _velLookAt = Vector3.zero;
        _inertiaX = _inertiaY = 0f;
    }

    // ── Arrastre con clic izquierdo ─────────────────────────────────────────────
    private void HandleDrag()
    {
        if (_isFollowing && !_followPermanent) return;

        if (Input.GetMouseButtonDown(0))
        {
            _dragging     = true;
            _didDrag      = false;
            _prevMousePx  = Input.mousePosition;
            _inertiaX     = _inertiaY = 0f;

            if (_followPermanent)
            {
                _followPermanent = false;
                _isFollowing     = false;
                _followTarget    = null;
            }
        }

        if (Input.GetMouseButton(0) && _dragging)
        {
            Vector2 cur   = Input.mousePosition;
            Vector2 delta = cur - _prevMousePx;
            _prevMousePx  = cur;

            if (delta.sqrMagnitude > DRAG_THRESHOLD_PX * DRAG_THRESHOLD_PX)
                _didDrag = true;

            if (_didDrag && delta.sqrMagnitude > 0f)
            {
                float earthR = WorldMap.Instance ? WorldMap.Instance.earthRadius : 10f;
                float dpp = _cam.fieldOfView / Screen.height * (_sDist / Mathf.Max(earthR, 0.01f));

                float dY = delta.x * dpp;
                float dX = delta.y * dpp;

                _tgtRotY += dY;
                _tgtRotX -= dX;
                _tgtRotX  = Mathf.Clamp(_tgtRotX, -89f, 89f);
                _tgtRotY  = NormalizeAngle(_tgtRotY);

                if (Time.deltaTime > 0f)
                {
                    float alpha = 1f - Mathf.Exp(-15f * Time.deltaTime);
                    _inertiaX = Mathf.Lerp(_inertiaX, dY / Time.deltaTime, alpha);
                    _inertiaY = Mathf.Lerp(_inertiaY, dX / Time.deltaTime, alpha);
                }

                _sRotX = _tgtRotX; _velRotX = 0f;
                _sRotY = _tgtRotY; _velRotY = 0f;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;

            if (!_didDrag)
            {
                _inertiaX = _inertiaY = 0f;
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var city = hit.collider.GetComponent<CityMarker>();
                    if (city != null) FocusOnCity(city.latitude, city.longitude);
                }
            }
        }
    }

    // Aplica inercia
    private void ApplyInertia()
    {
        if (_dragging || _isFollowing) return;
        if (Mathf.Abs(_inertiaX) < 0.05f && Mathf.Abs(_inertiaY) < 0.05f)
        {
            _inertiaX = _inertiaY = 0f;
            return;
        }

        _tgtRotY += _inertiaX * Time.deltaTime;
        _tgtRotX -= _inertiaY * Time.deltaTime;
        _tgtRotX  = Mathf.Clamp(_tgtRotX, -89f, 89f);
        _tgtRotY  = NormalizeAngle(_tgtRotY);

        float k = Mathf.Exp(-inertiaDamping * Time.deltaTime);
        _inertiaX *= k;
        _inertiaY *= k;
    }

    // Gestiona zoom.
    private void HandleZoom()
    {
        if (_isFollowing && !_followPermanent) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _tgtDist *= 1f - scroll * zoomSpeed * 10f;
        _tgtDist  = Mathf.Clamp(_tgtDist, minDistance, maxDistance);
        _inertiaX = _inertiaY = 0f;
    }

    // ── Suavizado y aplicación ──────────────────────────────────────────────
    private void SmoothAndApply()
    {
        if (_followPermanent && _followTarget != null)
        {
            // En follow permanente: sin suavizado, seguir instantáneamente
            _sRotX = _tgtRotX;
            _sRotY = _tgtRotY;
            _sDist = _tgtDist;
            _sLookAt = _tgtLookAt;
            _velRotX = _velRotY = _velDist = 0f;
            _velLookAt = Vector3.zero;
        }
        // Realiza if
        else if (!_dragging)
        {
            _sRotX = Mathf.SmoothDampAngle(_sRotX, _tgtRotX, ref _velRotX, orbitSmooth);
            _sRotY = Mathf.SmoothDampAngle(_sRotY, _tgtRotY, ref _velRotY, orbitSmooth);
        }

        _sDist   = Mathf.SmoothDamp(_sDist,   _tgtDist,   ref _velDist,   zoomSmooth);
        _sLookAt = Vector3.SmoothDamp(_sLookAt, _tgtLookAt, ref _velLookAt, orbitSmooth);

        Quaternion rot = Quaternion.Euler(_sRotX, _sRotY, 0f);
        transform.rotation = rot;
        transform.position  = rot * new Vector3(0f, 0f, -_sDist) + _sLookAt;
    }

    // ── Foco animado (clic en ciudad) ──────────────────────────────────────
    public void FocusOnCity(float lat, float lon)
    {
        if (_followRoutine != null) StopCoroutine(_followRoutine);
        _followRoutine = StartCoroutine(FocusRoutine(lat, lon));
    }

// Enfoque routine.
    private IEnumerator FocusRoutine(float lat, float lon)
    {
        _isFollowing = true;
        _followPermanent = false;
        _inertiaX = _inertiaY = 0f;

        if (WorldMap.Instance == null) { _isFollowing = false; yield break; }

        Vector3 localDir = WorldMap.Instance.LatLonToPosition(lat, lon, 1.0f);
        Vector3 worldDir = earthTransform.TransformDirection(localDir).normalized;

        _tgtRotX = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = NormalizeAngle(Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg);
        _tgtDist = minDistance + (maxDistance - minDistance) * 0.15f;
        _tgtLookAt = earthTransform.position;

        while (Mathf.Abs(_sRotX - _tgtRotX) > 0.3f ||
               Mathf.Abs(Mathf.DeltaAngle(_sRotY, _tgtRotY)) > 0.3f ||
               Mathf.Abs(_sDist - _tgtDist) > 0.05f)
        {
            yield return null;
        }

        _isFollowing = false;
    }

    // ── Reset (tecla R) ────────────────────────────────────────────────────
    public void ResetCameraPosition()
    {
        if (_followRoutine != null) { StopCoroutine(_followRoutine); _followRoutine = null; }

        GameObject baCity = GameObject.Find("Buenos Aires");
        
        if (baCity != null && earthTransform != null)
        {
            Vector3 toMarker = baCity.transform.position - earthTransform.position;
            float dist = toMarker.magnitude;
            float expectedRadius = WorldMap.Instance != null ? WorldMap.Instance.earthRadius : 10f;
            
            if (Mathf.Abs(dist - expectedRadius) < 2f)
            {
                _followTarget = baCity.transform;
                _followPermanent = true;
                _isFollowing = true;
                _inertiaX = _inertiaY = 0f;
                SetCameraToTarget(_followTarget);
                return;
            }
            else
            {
                Debug.LogWarning($"[Camera] Marcador 'Buenos Aires' está a distancia {dist}, expected {expectedRadius}. Usando coordenadas directas.");
            }
        }

        if (earthTransform != null && WorldMap.Instance != null)
        {
            SetCameraToLatLon(BA_LAT, BA_LON);
            _followTarget = null;
            _followPermanent = true;
            _isFollowing = true;
            _inertiaX = _inertiaY = 0f;
        }
    }
// Actualiza seguimiento objetivo from transform
    private void UpdateFollowTargetFromTransform()
    {
        if (_followTarget == null || earthTransform == null) return;

        Vector3 earthCenter = earthTransform.position;
        Vector3 targetWorldPos = _followTarget.position;
        Vector3 dirFromCenter = (targetWorldPos - earthCenter).normalized;

        _tgtRotX = Mathf.Asin(Mathf.Clamp(dirFromCenter.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = Mathf.Atan2(dirFromCenter.x, dirFromCenter.z) * Mathf.Rad2Deg;
        _tgtLookAt = earthCenter;
    }

// Normaliza ángulo.
    private static float NormalizeAngle(float a) => ((a % 360f) + 360f) % 360f;

// Inicializa ialize cámara posición.
    public void InitializeCameraPosition()
    {
        // Compatibilidad con GameBootstrapper
    }
}