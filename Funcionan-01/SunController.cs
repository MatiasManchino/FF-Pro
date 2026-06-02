using System;
using UnityEngine;

public class SunController : MonoBehaviour
{
    [Header("Referencias")]
    public Light     sunLight;
    public Transform earthTransform;

    [Header("Inclinación del eje terrestre")]
    [Range(0f, 45f)]
    public float axialTilt = 23.44f;

    [Header("Sol visual (opcional)")]
    public Transform sunVisual;
    public float sunDistance = 150f;

    [Header("Iluminación CONSTANTE")]
    public float sunIntensity = 1.2f;
    public Color ambientColor = new Color(0.25f, 0.25f, 0.35f);

// Configura referencias tempranas antes de Start.
    void Awake()
    {
        ApplyConstantLighting();
        if (earthTransform != null)
            earthTransform.rotation = Quaternion.identity;
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (TimeManager.Instance == null || sunLight == null || earthTransform == null)
            return;

        DateTime utc = TimeManager.Instance.CurrentUtcTime;
        RotateEarth(utc);
        AimSun(utc);
    }

// Aplica constant lighting
    void ApplyConstantLighting()
    {
        if (sunLight != null)
        {
            sunLight.intensity = sunIntensity;
            sunLight.color = Color.white;
        }
        RenderSettings.ambientLight = ambientColor;
    }

    // ── Rotación simple y estable: Greenwich mira al sol a las 12:00 UTC ──
    void RotateEarth(DateTime utc)
    {
        double hoursSinceNoon = utc.TimeOfDay.TotalHours - 12.0;
        float angleY = (float)(hoursSinceNoon * 15.0);
        earthTransform.rotation = Quaternion.Euler(axialTilt, -angleY, 0f);
    }

    // ── Dirección de la luz solar con declinación estacional ─────────────
    void AimSun(DateTime utc)
    {
        float declination = SolarDeclination(utc);
        float declinationRad = declination * Mathf.Deg2Rad;

        Vector3 sunDir = new Vector3(
            Mathf.Cos(declinationRad),
            Mathf.Sin(declinationRad),
            0f
        );

        sunLight.transform.rotation = Quaternion.LookRotation(-sunDir, Vector3.up);

        if (sunVisual != null)
            sunVisual.position = sunDir * sunDistance;
    }

// Realiza solar declination
    float SolarDeclination(DateTime utc)
    {
        int dayOfYear = utc.DayOfYear;
        return -23.44f * Mathf.Cos(2.0f * Mathf.PI * (dayOfYear + 10) / 365.0f);
    }
}