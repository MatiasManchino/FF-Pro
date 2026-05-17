using UnityEngine;

public class FlightRoute : MonoBehaviour
{
    [Header("Tiempos")]
    public float durationSeconds = 60f;

    [Header("Visual")]
    public int   lineSegments = 100;
    public float lineWidth    = 5f;
    public float planeSize    = 25f;

    [Header("Altitud")]
    public float groundRadius = 1000f;  // radio al despegar/aterrizar
    public float cruiseRadius = 1050f;  // radio en el punto medio (crucero)

    // Shanghai → Pacific mid → Buenos Aires (fuerza ruta por el Pacífico)
    private static readonly Vector2 Shanghai    = new Vector2( 35.88f,  121.47f);
    private static readonly Vector2 PacificMid  = new Vector2(  5f,    -150f);
    private static readonly Vector2 BuenosAires = new Vector2(-38.46f,  -58.38f);

    private Vector3[]    _dirs;
    private float[]      _radii;          // radio mundial por punto — curva seno
    private Vector3[]    _worldPositions;
    private LineRenderer _line;
    private Transform    _planeDot;
    private float        _elapsed;

    void Start()
    {
        if (WorldMap.Instance == null) { Destroy(gameObject); return; }

        BuildDirections();
        _worldPositions = new Vector3[_dirs.Length];
        CreateLine();
        CreatePlane();
    }

    void BuildDirections()
    {
        _dirs  = new Vector3[lineSegments + 1];
        _radii = new float  [lineSegments + 1];

        Vector3 a   = ToDir(Shanghai.x,    Shanghai.y);
        Vector3 mid = ToDir(PacificMid.x,  PacificMid.y);
        Vector3 b   = ToDir(BuenosAires.x, BuenosAires.y);

        int half = lineSegments / 2;

        for (int i = 0; i <= half; i++)
            _dirs[i] = Vector3.Slerp(a, mid, (float)i / half).normalized;

        for (int i = 1; i <= lineSegments - half; i++)
            _dirs[half + i] = Vector3.Slerp(mid, b, (float)i / (lineSegments - half)).normalized;

        // Altitud: seno de 0 a π → despegue suave, crucero, aterrizaje suave
        for (int i = 0; i <= lineSegments; i++)
        {
            float t = (float)i / lineSegments;
            _radii[i] = groundRadius + (cruiseRadius - groundRadius) * Mathf.Sin(t * Mathf.PI);
        }
    }

    static Vector3 ToDir(float lat, float lon)
    {
        float latR = lat * Mathf.Deg2Rad;
        float lonR = lon * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(latR) * Mathf.Cos(lonR),
            Mathf.Sin(latR),
            Mathf.Cos(latR) * Mathf.Sin(lonR));
    }

    Vector3 DirToWorld(Vector3 dir, float worldRadius)
    {
        float localR = worldRadius / (WorldMap.Instance.earthRadius * 2f);
        return WorldMap.Instance.transform.TransformPoint(dir * localR);
    }

    void CreateLine()
    {
        var go = new GameObject("RouteLine");
        _line = go.AddComponent<LineRenderer>();
        _line.useWorldSpace        = true;
        _line.positionCount        = _dirs.Length;
        _line.startWidth           = lineWidth;
        _line.endWidth             = lineWidth;
        _line.generateLightingData = false;
        _line.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows       = false;

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.2f, 0.5f, 1f);
        _line.sharedMaterial = mat;
    }

    void CreatePlane()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PlaneDot";
        go.transform.localScale = Vector3.one * planeSize;
        Destroy(go.GetComponent<SphereCollider>());

        var rend = go.GetComponent<MeshRenderer>();
        var mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.2f, 0.5f, 1f);
        rend.sharedMaterial    = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        _planeDot = go.transform;
    }

    void Update()
    {
        if (WorldMap.Instance == null) return;

        for (int i = 0; i < _dirs.Length; i++)
            _worldPositions[i] = DirToWorld(_dirs[i], _radii[i]);
        _line.SetPositions(_worldPositions);

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / durationSeconds);

        float indexF = t * (_dirs.Length - 1);
        int   ia     = Mathf.FloorToInt(indexF);
        int   ib     = Mathf.Min(ia + 1, _dirs.Length - 1);
        float frac   = indexF - ia;

        Vector3 dir    = Vector3.Slerp(_dirs[ia], _dirs[ib], frac).normalized;
        float   radius = Mathf.Lerp(_radii[ia], _radii[ib], frac);
        _planeDot.position = DirToWorld(dir, radius);

        if (t >= 1f)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_line != null)     Destroy(_line.gameObject);
        if (_planeDot != null) Destroy(_planeDot.gameObject);
    }
}
