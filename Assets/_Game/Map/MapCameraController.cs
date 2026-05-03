using UnityEngine;

/// <summary>
/// Controlador simple de cámara para navegar sobre el mapa.
/// Permite desplazarse y hacer zoom sobre la visualización del mapa.
/// </summary>
public class MapCameraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform focusPoint;

    [Header("Movimiento")]
    [SerializeField] private float panSpeed = 5f;
    [SerializeField] private float zoomSpeed = 200f;
    [SerializeField] private float minZoomDistance = 10f;
    [SerializeField] private float maxZoomDistance = 200f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 80f;

    private Vector3 lastMousePosition;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandlePan();
        HandleZoom();
        HandleRotation();
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * Time.deltaTime;

            if (targetCamera != null)
            {
                targetCamera.transform.Translate(move, Space.Self);
            }

            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleZoom()
    {
        if (targetCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float distance = Vector3.Distance(targetCamera.transform.position, focusPoint != null ? focusPoint.position : Vector3.zero);
            float zoomAmount = scroll * zoomSpeed * Time.deltaTime;
            float targetDistance = Mathf.Clamp(distance - zoomAmount, minZoomDistance, maxZoomDistance);
            float deltaDistance = targetDistance - distance;

            if (Mathf.Abs(deltaDistance) > 0.001f)
            {
                targetCamera.transform.Translate(Vector3.forward * deltaDistance, Space.Self);
            }
        }
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float horizontal = Input.GetAxis("Mouse X");
            float vertical = Input.GetAxis("Mouse Y");

            if (targetCamera != null)
            {
                targetCamera.transform.RotateAround(focusPoint != null ? focusPoint.position : Vector3.zero, Vector3.up, horizontal * rotationSpeed * Time.deltaTime);
                targetCamera.transform.RotateAround(focusPoint != null ? focusPoint.position : Vector3.zero, targetCamera.transform.right, -vertical * rotationSpeed * Time.deltaTime);
            }
        }
    }
}
