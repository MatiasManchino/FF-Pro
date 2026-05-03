using UnityEngine;

/// <summary>
/// Controlador de cámara orbital para el globo.
/// - Botón izquierdo del mouse: orbitar alrededor del globo.
/// - Rueda del mouse: acercar / alejar.
/// </summary>
public class MapCameraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform focusPoint;

    [Header("Órbita")]
    [SerializeField] private float orbitSpeed = 120f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 60f;

    private Vector3 lastMousePos;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void SetFocusPoint(Transform t) => focusPoint = t;
    public void SetCamera(Camera cam)      => targetCamera = cam;

    private void Update()
    {
        if (targetCamera == null) return;
        HandleOrbit();
        HandleZoom();
    }

    private void HandleOrbit()
    {
        if (Input.GetMouseButtonDown(0)) lastMousePos = Input.mousePosition;

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos  = Input.mousePosition;

            Vector3 pivot = focusPoint != null ? focusPoint.position : Vector3.zero;
            float dt      = Time.unscaledDeltaTime; // usar unscaled para que funcione pausado
            float hDeg    = delta.x * orbitSpeed * dt;
            float vDeg    = -delta.y * orbitSpeed * dt;

            targetCamera.transform.RotateAround(pivot, Vector3.up, hDeg);
            targetCamera.transform.RotateAround(pivot, targetCamera.transform.right, vDeg);

            // Mantener cámara mirando al pivote
            targetCamera.transform.LookAt(pivot);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        Vector3 pivot = focusPoint != null ? focusPoint.position : Vector3.zero;
        float dist    = Vector3.Distance(targetCamera.transform.position, pivot);
        float newDist = Mathf.Clamp(dist - scroll * zoomSpeed, minDistance, maxDistance);

        Vector3 dir = (targetCamera.transform.position - pivot).normalized;
        targetCamera.transform.position = pivot + dir * newDist;
    }
}
