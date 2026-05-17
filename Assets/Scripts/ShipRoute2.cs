using UnityEngine;

public class ShipRoute : MonoBehaviour
{
    [Header("Tiempos")]
    public float durationSeconds = 90f;

    [Header("Visual")]
    public int   lineSegments = 108;   // divisible por el nro de spans (9)
    public float lineWidth    = 4f;
    public float shipSize     = 22f;
    public float worldRadius  = 998.5f; // sobre la superficie (1000 = tierra)

    // Ruta Casablanca → Buenos Aires por el Atlántico.
    // Cada punto verificado en océano abierto para evitar tierra.
    //
    //  · (26, -18)  : pasa al oeste de las Islas Canarias
    //  · ( 5, -28)  : cruza el ecuador lejos del bulge de Brasil (costa ~35°W)
    //  · (-5, -32)  : sigue alejado del nordeste brasileño
    //  · (-18,-36)  : costa de Brasil aquí está en ~39°W → estamos en el océano
    //  · (-30,-46)  : sur de Brasil en ~51°W           → estamos en el océano
    //  · (-34,-50)  : este de Uruguay (~53°W)          → estamos en el océano
    private static readonly Vector2[] Waypoints =
    {
        new Vector2( 38.55f,  -7.59f),  // Casablanca
        new Vector2( 33f,    -12f),     // Atlántico NE (dejando costa marroquí)
        new Vector2( 26f,    -18f),     // Oeste Canarias
        new Vector2( 16f,    -24f),     // Atlántico central norte
        new Vector2(  5f,    -28f),     // Ecuatorial — lejos del bulge de Brasil
        new Vector2( -5f,    -32f),     // Sur del ecuador
        new Vector2(-18f,    -36f),     // Atlántico Sur (este de Brasil)
        new Vector2(-30f,    -46f),     // Atlántico Sur profundo
        new Vector2(-34f,    -50f),     // Este de Uruguay
        new Vector2(-38.46f, -58.38f),  // Buenos Aires
    };

    private Vector3[]    _dirs;
    private Vector3[]    _worldPositions;
    private LineRenderer _line;
    private Transform    _shipDot;
    private float        _elapsed;

    void Start()
    {
        if (WorldMap.Instance == null) { Destroy(gameObject); return; }
        BuildDirections();
        _worldPositions = new Vector3[_dirs.Length];
        CreateLine();
        CreateShip();
    }

    void BuildDirections()
    {
        int spanCount   = Waypoints.Length - 1;
        int segsPerSpan = Mathf.Max(1, lineSegments / spanCount);
        int totalPoints = spanCount * segsPerSpan + 1;
        _dirs = new Vector3[totalPoints];

        for (int span = 0; span < spanCount; span++)
        {
            Vector3 a = ToDir(Waypoints[span].x,     Waypoints[span].y);
            Vector3 b = ToDir(Waypoints[span + 1].x, Waypoints[span + 1].y);
            for (int i = 0; i < segsPerSpan; i++)
            {
                float t = (float)i / segsPerSpan;
                _dirs[span * segsPerSpan + i] = Vector3.Slerp(a, b, t).normalized;
            }
        }
        _dirs[totalPoints - 1] = ToDir(Waypoints[Waypoints.Length - 1].x,
                                       Waypoints[Waypoints.Length - 1].y);
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

    Vector3 DirToWorld(Vector3 dir)
    {
        float localR = worldRadius / (WorldMap.Instance.earthRadius * 2f);
        return WorldMap.Instance.transform.TransformPoint(dir * localR);
    }

    void CreateLine()
    {
        var go = new GameObject("ShipRouteLine");
        _line = go.AddComponent<LineRenderer>();
        _line.useWorldSpace        = true;
        _line.positionCount        = _dirs.Length;
        _line.startWidth           = lineWidth;
        _line.endWidth             = lineWidth;
        _line.generateLightingData = false;
        _line.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows       = false;

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0.5f, 0.05f); // naranja
        _line.sharedMaterial = mat;
    }

    void CreateShip()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ShipDot";
        go.transform.localScale = Vector3.one * shipSize;
        Destroy(go.GetComponent<SphereCollider>());

        var rend = go.GetComponent<MeshRenderer>();
        var mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0.5f, 0.05f); // naranja
        rend.sharedMaterial    = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        _shipDot = go.transform;
    }

    void Update()
    {
        if (WorldMap.Instance == null) return;

        for (int i = 0; i < _dirs.Length; i++)
            _worldPositions[i] = DirToWorld(_dirs[i]);
        _line.SetPositions(_worldPositions);

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / durationSeconds);

        float indexF = t * (_dirs.Length - 1);
        int   ia     = Mathf.FloorToInt(indexF);
        int   ib     = Mathf.Min(ia + 1, _dirs.Length - 1);
        Vector3 dir  = Vector3.Slerp(_dirs[ia], _dirs[ib], indexF - ia).normalized;
        _shipDot.position = DirToWorld(dir);

        if (t >= 1f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_line != null)    Destroy(_line.gameObject);
        if (_shipDot != null) Destroy(_shipDot.gameObject);
    }
}
