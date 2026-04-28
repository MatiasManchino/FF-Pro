using UnityEngine;
using FreightForwarder.Models;

namespace FreightForwarder.Map
{
    public class RouteRenderer : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private Vector3 _origin;
        private Vector3 _destination;
        private float _radius;
        private Constants.TransportMode _mode;
        private Color _routeColor;
        
        public void Initialize(Vector3 origin, Vector3 destination, float radius, Constants.TransportMode mode, Color color)
        {
            _origin = origin;
            _destination = destination;
            _radius = radius;
            _mode = mode;
            _routeColor = color;
            
            CreateLineRenderer();
            DrawArc();
        }
        
        private void CreateLineRenderer()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            
            _lineRenderer.startWidth = 0.05f;
            _lineRenderer.endWidth = 0.05f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = _routeColor;
            _lineRenderer.endColor = _routeColor;
        }
        
        private void DrawArc()
        {
            int segments = 50;
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                points[i] = GetArcPoint(t);
            }
            
            _lineRenderer.positionCount = points.Length;
            _lineRenderer.SetPositions(points);
        }
        
        private Vector3 GetArcPoint(float t)
        {
            Vector3 linearPoint = Vector3.Lerp(_origin, _destination, t);
            float arcFactor = Mathf.Sin(t * Mathf.PI) * 1.2f;
            Vector3 outward = linearPoint.normalized;
            return linearPoint + outward * arcFactor;
        }
        
        public void SetRouteColor(Color color)
        {
            _routeColor = color;
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
        }
    }
}