using UnityEngine;

// Requiere a collider para que MapCameraController's raycast puede detect it.
[RequireComponent(typeof(SphereCollider))]
public class CityMarker : MonoBehaviour
{
    [Header("Coordenadas geográficas")]
    public float latitude;
    public float longitude;

    [Tooltip("Distancia sobre la superficie de la esfera (en unidades Unity).")]
    public float surfaceOffset = 0.15f;

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        if (WorldMap.Instance == null)
        {
            Debug.LogError("[CityMarker] WorldMap.Instance no disponible.");
            enabled = false;
            return;
        }

        PlaceOnSurface();

        // Padre DESPUÉS DE colocar in mundo espacio para que Unity converts correctamente,
        // accounting for Earth's current rotation and scale (×20).
        // From here the marker rotates with Earth every fotograma.
        transform.SetParent(WorldMap.Instance.transform, worldPositionStays: true);
    }

// Posiciona el marcador sobre la superficie esférica del planeta según latitud y longitud.
    public void PlaceOnSurface()
    {
        if (WorldMap.Instance == null) return;

        Vector3 pos    = WorldMap.Instance.LatLonToPosition(latitude, longitude, WorldMap.Instance.earthRadius);
        Vector3 normal = pos.normalized;

        // Posición slightly above the surface
        transform.position = pos + normal * surfaceOffset;

        // Orient so transform.up points radially outward
        transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
    }

#if UNITY_EDITOR
    [ContextMenu("Actualizar posición (editor)")]
// Editor ubicación.
    private void EditorPlace() => PlaceOnSurface();
#endif
}