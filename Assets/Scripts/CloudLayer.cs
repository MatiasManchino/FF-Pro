using UnityEngine;

// Capa de nubes que deriva independientemente de la Tierra.
// La velocidad se escala con el índice de velocidad del TimeManager.
public class CloudLayer : MonoBehaviour
{
    // Grados que avanzan las nubes por cada día de juego (30 s reales a x1).
    // ~5° por día = deriva lenta y visible respecto a la superficie.
    public float degreesPerGameDay = 5f;

    private static readonly float[] SpeedMults = { 0f, 1f, 10f, 100f, 1000f };

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        float mult = 1f;
        if (TimeManager.Instance != null)
        {
            int idx = Mathf.Clamp(TimeManager.Instance.CurrentSpeedIndex, 0, SpeedMults.Length - 1);
            mult = SpeedMults[idx];
        }

        // 1 día de juego = 1200 s reales a velocidad x1 (20 min, según TimeManager)
        float degsPerSec = degreesPerGameDay / 1200f * mult;
        transform.Rotate(0f, degsPerSec * Time.deltaTime, 0f, Space.Self);
    }
}