using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

[RequireComponent(typeof(Camera))]

// Controla la cámara del mapa 3D: orbita, zoom, arrastre e interacción con marcadores de ciudad.
// Incluye funcionalidades para seguir ciudades, bloquear la cámara y navegación por teclado.

public class MapCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform earthTransform;

    [Header("Zoom")]
    public float initialDistance = 2500f;
    public float minDistance     = 1200f;
    public float maxDistance     = 7000f;
    [Tooltip("Fracción de la distancia actual por unidad de scroll.")]
    public float zoomSpeed       = 0.06f;
    public float zoomSmooth      = 0.4f;

    [Header("Inercia al soltar")]
    [Range(0f, 15f)]
    public float inertiaDamping  = 12f;

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
    private GameObject _lockAnchor;
    private bool       _manualLock;
    private bool       _justReleasedLock;
    private int        _savedSpeedIndex = -1;

    // ── Navegación por ciudades ──────────────────────────────────────────────
    private List<CityMarker> _allCities = new List<CityMarker>();
    private int _currentCityIndex = -1;
    private string _citySearchString = "";
    private bool _isTypingCityName = false;

    private Camera _cam;

// Devuelve el zoom porcentaje
    public float ZoomPercent =>
        (1f - Mathf.Clamp01((_sDist - minDistance) / Mathf.Max(maxDistance - minDistance, 1f))) * 100f;


    // Indica si la cámara está bloqueada manualmente por el usuario.

    public bool IsManuallyLocked => _manualLock;

    // Coordenadas de Buenos Aires para el respaldo
    private const float BA_LAT = -38.46f;
    private const float BA_LON = -58.38f;


    // Inicialización temprana: obtiene la referencia a la cámara y el transform del planeta.
    // Se ejecuta antes de Start().

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


    // Corutina que inicializa la lista de ciudades y pone la cámara enfocada en Buenos Aires por defecto.
    // Espera un par de frames para asegurar que los `CityMarker` estén posicionados.

    private IEnumerator InitFollow()
    {
        yield return null;
        
        _allCities.Clear();
        _allCities.AddRange(FindObjectsByType<CityMarker>(FindObjectsInactive.Exclude));
        
        _allCities.Sort((a, b) => a.cityName.CompareTo(b.cityName));
        
        int baIndex = _allCities.FindIndex(c => c.cityName == "Buenos Aires");
        if (baIndex >= 0)
        {
            _currentCityIndex = baIndex;
        }
        
        StartPermanentFollow("Buenos Aires", BA_LAT, BA_LON);

        // Arrancar con FIJAR activo: esperar a que la cámara se ubique sobre Buenos Aires y bloquear.
        yield return null;
        yield return null;
        if (!_manualLock) LockToCurrentPosition();
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (_followPermanent && _followTarget != null)
        {
            UpdateFollowTargetFromTransform();
            _earthYInit = false;          // al volver a modo libre, re-sincroniza sin salto
        }
        else
        {
            // Modo libre: la cámara acompaña la rotación de la Tierra para que el globo se vea estable
            // (el día/noche lo da el sol). Sin esto, al soltar el FIJAR el globo "gira descontroladamente".
            CompensateEarthSpin();
        }

        // HandleMousePause();  // desactivado: el juego ya no se pausa al hacer clic en el mapa
        HandleDrag();
        HandleZoom();
        HandleCityNavigation();
        if (Input.GetKeyDown(KeyCode.R)) ResetCameraPosition();
        ApplyInertia();
        SmoothAndApply();
    }

    // ── Compensación de la rotación de la Tierra (globo estable en modo libre) ──
    private bool  _earthYInit;
    private float _lastEarthY;

    private void CompensateEarthSpin()
    {
        if (earthTransform == null) return;
        float earthY = earthTransform.eulerAngles.y;
        if (!_earthYInit) { _lastEarthY = earthY; _earthYInit = true; return; }

        float d = Mathf.DeltaAngle(_lastEarthY, earthY);
        _lastEarthY = earthY;
        if (Mathf.Abs(d) < 0.0001f) return;

        // La cámara orbita lo mismo que rotó la Tierra → el globo se ve quieto; el arrastre se suma encima.
        _tgtRotY = NormalizeAngle(_tgtRotY + d);
        _sRotY   = NormalizeAngle(_sRotY + d);
    }

    // ── Navegación por ciudades con teclado ──────────────────────────────────

    // Maneja la navegación por ciudades usando teclado, búsqueda por nombre y selección rápida.
    // Permite escribir el nombre de la ciudad, navegar con flechas y enfocar con Home.

    private void HandleCityNavigation()
    {
        if (_allCities.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            _inertiaX = _inertiaY = 0f;
            NavigateToCity(1);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            _inertiaX = _inertiaY = 0f;
            NavigateToCity(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.F) && !_isTypingCityName)
        {
            _isTypingCityName = true;
            _citySearchString = "";
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _isTypingCityName)
        {
            _isTypingCityName = false;
            _citySearchString = "";
        }

        if (_isTypingCityName)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                FindAndFocusCity(_citySearchString);
                _isTypingCityName = false;
                _citySearchString = "";
            }
            else if (Input.GetKeyDown(KeyCode.Backspace) && _citySearchString.Length > 0)
            {
                _citySearchString = _citySearchString.Substring(0, _citySearchString.Length - 1);
            }
            else
            {
// Foreach
                foreach (char c in Input.inputString)
                {
                    if (c == '\b') continue;
                    if (c == '\n' || c == '\r') continue;
                    _citySearchString += c;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            FindAndFocusCity("Buenos Aires");
        }
    }


    // Cambia el índice de ciudad actual en la dirección indicada y centra la cámara
    // en la ciudad resultante. Si la cámara está bloqueada mantendrá el lock en la nueva ciudad.

    private void NavigateToCity(int direction)
    {
        if (_allCities.Count == 0) return;

        _currentCityIndex += direction;
        if (_currentCityIndex >= _allCities.Count)
            _currentCityIndex = 0;
        // Realiza if
        else if (_currentCityIndex < 0)
            _currentCityIndex = _allCities.Count - 1;

        CityMarker targetCity = _allCities[_currentCityIndex];
        
        // Si FIJAR está activo, mantener el lock y actualizar el follow target
        if (_manualLock && _lockAnchor != null)
        {
            // Destruir el ancla vieja y crear una nueva en la nueva ciudad
            Destroy(_lockAnchor);
            _lockAnchor = new GameObject("_CameraLockAnchor");
            
            Vector3 cityPos = targetCity.transform.position;
            _lockAnchor.transform.position = cityPos;
            _lockAnchor.transform.SetParent(earthTransform, worldPositionStays: true);
            _followTarget = _lockAnchor.transform;
            
            SetCameraToTarget(_followTarget);
        }
        else
        {
            FocusOnCity(targetCity.latitude, targetCity.longitude);
        }
    }


    // Busca una ciudad por nombre (con y sin coincidencia parcial) y la enfoca si se encuentra.

    private void FindAndFocusCity(string searchName)
    {
        if (string.IsNullOrEmpty(searchName)) return;

        string normalizedSearch = RemoveDiacritics(searchName).ToLower();
        
        CityMarker found = _allCities.Find(c => 
            RemoveDiacritics(c.cityName).Equals(normalizedSearch, System.StringComparison.OrdinalIgnoreCase));
        
        if (found == null)
        {
            found = _allCities.Find(c => 
                RemoveDiacritics(c.cityName).IndexOf(normalizedSearch, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (found != null)
        {
            _currentCityIndex = _allCities.IndexOf(found);
            Debug.Log($"[Camera] Encontrada: {found.cityName}");
            FocusOnCity(found.latitude, found.longitude);
        }
        else
        {
            Debug.LogWarning($"[Camera] Ciudad no encontrada: '{searchName}'");
        }
    }


    // Quita diacríticos (acentos) de una cadena para normalizar búsquedas comparativas.

    private string RemoveDiacritics(string text)
    {
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

// Foreach
        foreach (char c in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    // Inicio permanente seguimiento.
    private void StartPermanentFollow(string cityName, float lat, float lon)
    {
        if (_followRoutine != null)
        {
            StopCoroutine(_followRoutine);
            _followRoutine = null;
        }

        _manualLock = false;
        if (_lockAnchor != null) { Destroy(_lockAnchor); _lockAnchor = null; }

        _followTarget = CityMarker.TryGetMarker(cityName, out var marker)
            ? marker.transform
            : null;
        if (_followTarget == null)
            Debug.LogWarning($"[Camera] Marcador '{cityName}' no encontrado, usando coordenadas.");

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

    // Sigue permanentemente a un Transform en movimiento (vehículo en tránsito), acercando la cámara.
    public void FollowTransform(Transform target)
    {
        if (target == null) return;
        if (_followRoutine != null) { StopCoroutine(_followRoutine); _followRoutine = null; }
        _manualLock = false;
        if (_lockAnchor != null) { Destroy(_lockAnchor); _lockAnchor = null; }
        _followTarget    = target;
        _followPermanent = true;
        _isFollowing     = true;
        _inertiaX = _inertiaY = 0f;
        SetCameraToTarget(target);
        _tgtDist = minDistance + (maxDistance - minDistance) * 0.12f;   // acercar al vehículo
    }

    // Ajusta la cámara para apuntar a un `Transform` objetivo (por ejemplo, un marcador de ciudad).
    // Calcula las rotaciones y distancia objetivo a partir de la posición del objetivo.

    private void SetCameraToTarget(Transform target)
    {
    
        if (earthTransform == null) return;
        Vector3 earthCenter = earthTransform.position;
        Vector3 targetWorldPos = target.position;
        Vector3 dirFromCenter = (targetWorldPos - earthCenter).normalized;

        _tgtRotX = Mathf.Asin(Mathf.Clamp(dirFromCenter.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = Mathf.Atan2(-dirFromCenter.x, -dirFromCenter.z) * Mathf.Rad2Deg;
        _tgtDist = initialDistance;
        _tgtLookAt = earthCenter;
        SnapSmoothedToTarget();
    }


    // Ajusta la cámara para apuntar a una latitud/longitud concretas sobre el planeta.
    // Convierte lat/lon a dirección y calcula rotaciones objetivo.

    private void SetCameraToLatLon(float lat, float lon)
    {
        if (WorldMap.Instance == null || earthTransform == null) return;

        Vector3 localDir = WorldMap.Instance.LatLonToPosition(lat, lon, 1.0f);
        Vector3 worldDir = earthTransform.TransformDirection(localDir).normalized;

        Vector3 earthCenter = earthTransform.position;

        _tgtRotX = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = Mathf.Atan2(-worldDir.x, -worldDir.z) * Mathf.Rad2Deg;
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

// Gestiona ratón pausa.
    private void HandleMousePause()
    {
        if (TimeManager.Instance == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            // No pausar si el clic es sobre la UI (botones superiores)
            if (IsMouseOverUI()) return;
            
            _savedSpeedIndex = TimeManager.Instance.CurrentSpeedIndex;
            TimeManager.Instance.SetSpeedIndex(0);
        }

        if (Input.GetMouseButtonUp(0) && _savedSpeedIndex >= 0)
        {
            if (TimeManager.Instance.CurrentSpeedIndex == 0)
                TimeManager.Instance.SetSpeedIndex(_savedSpeedIndex);
            _savedSpeedIndex = -1;
        }
    }
// Gestiona arrastre.
    private void HandleDrag()
    {
        if (_isFollowing && !_followPermanent) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOverUI()) return;
            _dragging     = true;
            _didDrag      = false;
            _prevMousePx  = Input.mousePosition;
            _inertiaX     = _inertiaY = 0f;

            // Al clickear y arrastrar el mapa se SUELTA el FIJAR (y el seguimiento) para girar libre.
            if (_followPermanent || _manualLock)
            {
                _manualLock      = false;
                _followPermanent = false;
                _isFollowing     = false;
                _followTarget    = null;
                if (_lockAnchor != null) { Destroy(_lockAnchor); _lockAnchor = null; }
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

            if (_manualLock)
            {
                _inertiaX = _inertiaY = 0f;
                if (_didDrag) UpdateLockAnchor();
            }
            // Realiza if
            else if (!_didDrag)
            {
                _inertiaX = _inertiaY = 0f;
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var city = hit.collider.GetComponent<CityMarker>();
                    if (city != null)
                    {
                        int cityIndex = _allCities.IndexOf(city);
                        if (cityIndex >= 0) _currentCityIndex = cityIndex;
                        FocusOnCity(city.latitude, city.longitude);
                    }
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

        if (IsMouseOverUI()) return;   // el scroll sobre paneles UI no debe acercar/alejar el mapa
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _tgtDist *= 1f - scroll * zoomSpeed * 10f;
        _tgtDist  = Mathf.Clamp(_tgtDist, minDistance, maxDistance);
        _inertiaX = _inertiaY = 0f;
    }

// Suaviza and aplica.
    private void SmoothAndApply()
    {
        if (_followPermanent && _followTarget != null)
        {
            _sRotX = _tgtRotX;
            _sRotY = _tgtRotY;
            _sDist = _tgtDist;
            _sLookAt = _tgtLookAt;
            _velRotX = _velRotY = _velDist = 0f;
            _velLookAt = Vector3.zero;
        }
        else if (!_dragging && !_justReleasedLock)   // ← AGREGAR: && !_justReleasedLock
        {
            _sRotX = Mathf.SmoothDampAngle(_sRotX, _tgtRotX, ref _velRotX, orbitSmooth);
            _sRotY = Mathf.SmoothDampAngle(_sRotY, _tgtRotY, ref _velRotY, orbitSmooth);
        }
        else
        {
            // Snap directo cuando _justReleasedLock es true
            _sRotX = _tgtRotX;
            _sRotY = _tgtRotY;
        }

        _sDist   = Mathf.SmoothDamp(_sDist,   _tgtDist,   ref _velDist,   zoomSmooth);
        _sLookAt = Vector3.SmoothDamp(_sLookAt, _tgtLookAt, ref _velLookAt, orbitSmooth);

        Quaternion rot = Quaternion.Euler(_sRotX, _sRotY, 0f);
        transform.rotation = rot;
        transform.position  = rot * new Vector3(0f, 0f, -_sDist) + _sLookAt;
        _justReleasedLock = false;
    }

// Enfoque on ciudad.
    public void FocusOnCity(float lat, float lon)
    {
        if (_followRoutine != null) StopCoroutine(_followRoutine);
        
        if (_justReleasedLock)
        {
            SnapToLatLon(lat, lon);
            // _justReleasedLock se resetea en el próximo Actualiza después de SmoothAndApply
            return;
        }
        
        _followRoutine = StartCoroutine(FocusRoutine(lat, lon));
    }

    // Acomoda to lat lon
    private void SnapToLatLon(float lat, float lon)
    {
        if (WorldMap.Instance == null || earthTransform == null) return;
        
        Vector3 localDir = WorldMap.Instance.LatLonToPosition(lat, lon, 1.0f);
        Vector3 worldDir = earthTransform.TransformDirection(localDir).normalized;
        
        _tgtRotX = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        _tgtRotY = NormalizeAngle(Mathf.Atan2(-worldDir.x, -worldDir.z) * Mathf.Rad2Deg);
        _tgtDist = minDistance + (maxDistance - minDistance) * 0.15f;
        _tgtLookAt = earthTransform.position;
        
        SnapSmoothedToTarget();
        _isFollowing = false;
        _inertiaX = _inertiaY = 0f;
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
        _tgtRotY = NormalizeAngle(Mathf.Atan2(-worldDir.x, -worldDir.z) * Mathf.Rad2Deg);
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

// Restablece cámara posición
    public void ResetCameraPosition()
    {
        ReleaseLockIfNeeded();
        if (_followRoutine != null) { StopCoroutine(_followRoutine); _followRoutine = null; }

        CityMarker.TryGetMarker("Buenos Aires", out var baCityMarker);
        GameObject baCity = baCityMarker != null ? baCityMarker.gameObject : null;
        
        if (baCity != null && earthTransform != null)
        {
            Vector3 toMarker = baCity.transform.position - earthTransform.position;
            float dist = toMarker.magnitude;
            float expectedRadius = WorldMap.Instance != null ? WorldMap.Instance.earthRadius : 10f;
            
            if (Mathf.Abs(dist - expectedRadius) < expectedRadius * 0.1f)
            {
                _followTarget = baCity.transform;
                _followPermanent = true;
                _isFollowing = true;
                _inertiaX = _inertiaY = 0f;
                
                int baIndex = _allCities.FindIndex(c => c.cityName == "Buenos Aires");
                if (baIndex >= 0) _currentCityIndex = baIndex;
                
                SetCameraToTarget(_followTarget);
                return;
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
        _tgtRotY = Mathf.Atan2(-dirFromCenter.x, -dirFromCenter.z) * Mathf.Rad2Deg;
        _tgtLookAt = earthCenter;
    }

// Normaliza ángulo.
    private static float NormalizeAngle(float a) => ((a % 360f) + 360f) % 360f;

// Inicializa ialize cámara posición.
    public void InitializeCameraPosition() { }

// Actualiza bloqueo anchor
    private void UpdateLockAnchor()
    {
        if (WorldMap.Instance == null || earthTransform == null) return;
        if (_lockAnchor != null) Destroy(_lockAnchor);

        Vector3 worldDir   = (transform.position - earthTransform.position).normalized;
        Vector3 surfacePos = worldDir * WorldMap.Instance.earthRadius;

        _lockAnchor = new GameObject("_CameraLockAnchor");
        _lockAnchor.transform.position = surfacePos;
        _lockAnchor.transform.SetParent(earthTransform, worldPositionStays: true);
        _followTarget = _lockAnchor.transform;
    }

// Bloqueo to actual posición.
    public void LockToCurrentPosition()
    {
        if (_manualLock)
        {
            _manualLock      = false;
            _followPermanent = false;
            _followTarget    = null;
            if (_lockAnchor != null) { Destroy(_lockAnchor); _lockAnchor = null; }
            return;
        }

        if (_followRoutine != null) { StopCoroutine(_followRoutine); _followRoutine = null; }

        UpdateLockAnchor();
        if (_followTarget == null) return;
        _followPermanent = true;
        _isFollowing     = false;
        _manualLock      = true;
        _inertiaX = _inertiaY = 0f;
    }

// Libera bloqueo if needed.
    private void ReleaseLockIfNeeded()
    {
        if (_manualLock)
        {
            _manualLock = false;
            _followPermanent = false;
            _isFollowing = false;
            _followTarget = null;
            if (_lockAnchor != null) { Destroy(_lockAnchor); _lockAnchor = null; }
        }
    }

    // Indica si ratón terminado UI.
    private static bool IsMouseOverUI()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }
// Devuelve el ciudad nombre actual
    public string CurrentCityName => 
        (_currentCityIndex >= 0 && _currentCityIndex < _allCities.Count) 
        ? _allCities[_currentCityIndex].cityName 
        : "";

// Devuelve el total de ciudades
    public int TotalCities => _allCities.Count;

// Devuelve el ciudad number actual
    public int CurrentCityNumber => _currentCityIndex + 1;

// Indica si typing ciudad nombre
    public bool IsTypingCityName => _isTypingCityName;

// Devuelve el ciudad búsqueda string
    public string CitySearchString => _citySearchString;
}