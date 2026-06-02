using UnityEngine;

public class AtmosphericHaloController : MonoBehaviour
{
    private Material _mat;
    private Camera   _cam;

// Inicializa el marcador: obtiene referencias, posiciona el objeto, crea el label y registra la ciudad.
    void Start()
    {
        _mat = GetComponent<MeshRenderer>()?.material;
        _cam = Camera.main;
    }

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
    void Update()
    {
        if (_mat == null || SunController.Instance == null) return;

        Vector3 sunDir = SunController.Instance.GetSunDirection();
        _mat.SetVector("_SunDir", sunDir);

        if (_cam != null)
        {
            Vector3 camToEarth = (transform.position - _cam.transform.position).normalized;
            float   backlit    = Mathf.Clamp01(Vector3.Dot(camToEarth, sunDir) * 2f);
            _mat.SetFloat("_BacklitFactor", backlit);
        }
    }
}