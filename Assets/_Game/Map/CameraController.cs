using UnityEngine;

namespace FreightForwarder.Map
{
    public class CameraController : MonoBehaviour
    {
        private Camera _camera;
        private Transform _target;
        private Vector3 _currentRotation;
        private Vector3 _targetRotation;
        private float _currentDistance = 15f;
        private float _targetDistance = 15f;
        
        [SerializeField] private float _rotationSpeed = 2f;
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minZoomDistance = 5f;
        [SerializeField] private float _maxZoomDistance = 25f;
        
        private bool _isDragging;
        private Vector2 _lastMousePosition;
        
        public void Initialize(Camera camera, Transform target)
        {
            _camera = camera;
            _target = target;
            _currentRotation = new Vector3(20f, 0f, 0f);
            _targetRotation = _currentRotation;
            UpdateCameraPosition();
        }
        
        private void Update()
        {
            if (_target == null) return;
            
            HandleMouseInput();
            
            _currentRotation = Vector3.Lerp(_currentRotation, _targetRotation, Time.deltaTime * _smoothSpeed);
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * _smoothSpeed);
            
            UpdateCameraPosition();
        }
        
        private void HandleMouseInput()
        {
            // Detectar si estamos en el editor o build
            // Usar Input.GetMouseButton (funciona con "Both" en settings)
            
            // Rotación con click derecho
            if (Input.GetMouseButtonDown(1))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                _isDragging = false;
            }
            
            if (_isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastMousePosition;
                _targetRotation.y += delta.x * _rotationSpeed * 0.1f;
                _targetRotation.x += -delta.y * _rotationSpeed * 0.1f;
                _targetRotation.x = Mathf.Clamp(_targetRotation.x, 5f, 85f);
                _lastMousePosition = Input.mousePosition;
            }
            
            // Zoom con scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                _targetDistance -= scroll * _zoomSpeed;
                _targetDistance = Mathf.Clamp(_targetDistance, _minZoomDistance, _maxZoomDistance);
            }
        }
        
        private void UpdateCameraPosition()
        {
            if (_target == null) return;
            
            Quaternion rotation = Quaternion.Euler(_currentRotation);
            Vector3 offset = rotation * new Vector3(0, 0, -_currentDistance);
            Vector3 targetPosition = _target.position + offset;
            
            _camera.transform.position = targetPosition;
            _camera.transform.LookAt(_target);
        }
        
        public void FocusOnPoint(Vector3 worldPoint)
        {
            if (_target == null) return;
            
            Vector3 directionToPoint = (worldPoint - _target.position).normalized;
            float yaw = Mathf.Atan2(directionToPoint.x, directionToPoint.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(directionToPoint.y) * Mathf.Rad2Deg;
            
            _targetRotation = new Vector3(pitch, yaw, 0f);
            _targetDistance = 8f;
        }
        
        public void ResetView()
        {
            _targetRotation = new Vector3(20f, 0f, 0f);
            _targetDistance = 15f;
        }
    }
}