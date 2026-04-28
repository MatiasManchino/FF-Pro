using UnityEngine;
using FreightForwarder.Models;

namespace FreightForwarder.Map
{
    public class CityMarker : MonoBehaviour
    {
        private WorldCity _city;
        private WorldMap _worldMap;
        private Renderer _renderer;
        private Vector3 _originalScale;
        
        public WorldCity City => _city;
        
        public void Initialize(WorldCity city, WorldMap worldMap)
        {
            _city = city;
            _worldMap = worldMap;
            _renderer = GetComponent<Renderer>();
            _originalScale = transform.localScale;
            
            // Asegurar collider para clicks
            if (GetComponent<Collider>() == null)
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.5f;
            }
        }
        
        public void SetColor(Color color)
        {
            if (_renderer != null)
                _renderer.material.color = color;
        }
        
        private void OnMouseEnter()
        {
            transform.localScale = _originalScale * 1.3f;
            _worldMap?.OnCityHoveredInternal(_city);
        }
        
        private void OnMouseExit()
        {
            transform.localScale = _originalScale;
        }
        
        private void OnMouseDown()
        {
            _worldMap?.OnCityClickedInternal(_city);
        }
    }
}