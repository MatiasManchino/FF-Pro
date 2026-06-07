using UnityEngine;

public class CloudLayerController : MonoBehaviour
{
    public float rotationSpeed      = 1.5f;
    public float opacity            = 0.15f;
    public bool  randomizeDirection = false;

    private const float ACCEL_RATE = 0.25f;
    private const float DECEL_RATE = 0.6f;

    private Material _mat;
    private Vector3  _axis;
    private float    _currentSpeed;
    private float    _targetSpeed;
    private bool     _decelerating;
    private float    _timer;
    private float    _nextChangeSec;

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        var rend = GetComponent<MeshRenderer>();
        if (rend != null)
        {
            _mat = rend.material;
            if (_mat.HasProperty("_Color"))
            {
                var c = _mat.color;
                c.a = opacity;
                _mat.color = c;
            }
            SetupMaterialForTransparency(_mat);
        }

        _axis         = Vector3.up;
        _currentSpeed = 0f;
        _targetSpeed  = rotationSpeed;
        ScheduleNextChange();
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        float speedMult = TimeManager.Instance != null
            ? TimeManager.Instance.CurrentSpeedMultiplier : 1f;

        float rate = Mathf.Abs(_currentSpeed) > Mathf.Abs(_targetSpeed) ? DECEL_RATE : ACCEL_RATE;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, rate * Time.deltaTime);

        transform.Rotate(_axis, _currentSpeed * speedMult * Time.deltaTime);

        if (!randomizeDirection) return;

        _timer += Time.deltaTime;

        if (!_decelerating && _timer >= _nextChangeSec)
        {
            _targetSpeed  = 0f;
            _decelerating = true;
        }

        if (_decelerating && Mathf.Abs(_currentSpeed) < 0.05f)
        {
            _currentSpeed = 0f;
            _decelerating = false;
            _timer        = 0f;
            PickNewDirection();
            ScheduleNextChange();
        }
    }

// Gestiona pick new dirección.
    private void PickNewDirection()
    {
        float   tilt    = Random.Range(5f, 35f);
        Vector3 tiltDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        _axis = Quaternion.AngleAxis(tilt, tiltDir) * Vector3.up;

        float sign = Random.value < 0.3f ? -1f : 1f;
        _targetSpeed = rotationSpeed * sign;
    }

// Programa next change.
    private void ScheduleNextChange()
    {
        _nextChangeSec = Random.Range(20f, 60f);
    }

// Establece up material for transparency.
    private void SetupMaterialForTransparency(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
    }
}