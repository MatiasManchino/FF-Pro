using UnityEngine;

/// <summary>
/// CityMarker representa un punto de ciudad en el mapa.
/// Se puede usar para mostrar la ubicación y el nombre de una ciudad.
/// </summary>
public class CityMarker : MonoBehaviour
{
    [Header("Configuración de marcador")]
    [SerializeField] private Color markerColor = Color.red;
    [SerializeField] private float labelOffset = 0.25f;
    [SerializeField] private TextMesh label;

    public WorldCity City { get; private set; }

    /// <summary>
    /// Inicializa el marcador con una ciudad y una posición.
    /// </summary>
    public void Initialize(WorldCity city, Vector3 position, Color color)
    {
        City = city;
        transform.position = position;
        markerColor = color;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = markerColor;
        }

        if (label != null)
        {
            label.text = city != null ? city.Name : "Ciudad";
            label.transform.position = position + Vector3.up * labelOffset;
            label.color = markerColor;
        }

        name = city != null ? $"CityMarker_{city.Name}" : "CityMarker";
    }

    private void OnDrawGizmosSelected()
    {
        if (City == null) return;
        Gizmos.color = markerColor;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}
