using UnityEngine;

/// <summary>
/// Dibuja una ruta simple sobre el mapa usando un LineRenderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MapRouteRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ConfigureRenderer();
    }

    private void ConfigureRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 0.1f);
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
    }

    /// <summary>
    /// Inicializa la ruta con dos puntos y un color.
    /// </summary>
    public void Initialize(Vector3 start, Vector3 end, Color color, float width = 0.1f)
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureRenderer();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = Mathf.Max(0.01f, width);
        lineRenderer.endWidth = Mathf.Max(0.01f, width);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    /// <summary>
    /// Establece una ruta a través de varios puntos.
    /// </summary>
    public void SetPoints(Vector3[] points, Color color, float width = 0.1f)
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureRenderer();
        }

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startWidth = Mathf.Max(0.01f, width);
        lineRenderer.endWidth = Mathf.Max(0.01f, width);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
