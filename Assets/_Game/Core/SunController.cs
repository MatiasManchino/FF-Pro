using UnityEngine;

/// <summary>
/// SunController gestiona la iluminación ambiental y la rotación del sol en el juego.
/// </summary>
public class SunController : MonoBehaviour
{
    [Header("Configuración del sol")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float rotationSpeed = 1f;

    private void Reset()
    {
        if (directionalLight == null)
        {
            directionalLight = GetComponent<Light>();
        }
    }

    public void Initialize()
    {
        if (directionalLight == null)
        {
            Debug.LogWarning("SunController no tiene luz direccional asignada.");
            return;
        }

        Debug.Log("SunController inicializado.");
    }

    private void Update()
    {
        if (directionalLight == null) return;

        directionalLight.transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
    }
}