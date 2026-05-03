using UnityEngine;

/// <summary>
/// Mueve el sol (luz direccional) alrededor de la Tierra según la hora del juego.
/// La Tierra no rota — el sol orbita.
/// </summary>
public class SunController : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private float elevation = 23.5f; // inclinación axial

    public void Initialize()
    {
        if (sunLight == null)
            sunLight = GetComponent<Light>();
        UpdateSunAngle();
        Debug.Log("SunController inicializado.");
    }

    public void SetLight(Light light)
    {
        sunLight = light;
    }

    private void Update()
    {
        if (sunLight == null || TimeManager.Instance == null) return;
        UpdateSunAngle();
    }

    private void UpdateSunAngle()
    {
        // El sol recorre 360° en 24 h de juego.
        // Hora 6 → amanecer (90°), hora 12 → mediodía (180°), hora 0/24 → medianoche (0°/360°)
        float azimuth = (TimeManager.Instance.CurrentHour / 24f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(elevation, azimuth, 0f);
    }
}
