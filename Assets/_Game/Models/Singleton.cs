using UnityEngine;

/// <summary>
/// Clase base genérica para implementar el patrón Singleton en Unity.
/// Garantiza que solo exista una instancia de la clase derivada en la escena.
/// </summary>
/// <typeparam name="T">Tipo de la clase que hereda de Singleton</typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// Instancia única del Singleton. Se crea automáticamente si no existe.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Buscar instancia existente en la escena
                        _instance = FindAnyObjectByType<T>();

                        if (_instance == null)
                        {
                            // Crear un nuevo GameObject para el Singleton
                            GameObject singletonObject = new GameObject(typeof(T).Name);
                            _instance = singletonObject.AddComponent<T>();

                            // Marcar como DontDestroyOnLoad para persistir entre escenas
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// Método llamado cuando el script se instancia.
    /// Verifica si ya existe una instancia y destruye duplicados.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Método llamado cuando el objeto es destruido.
    /// Limpia la referencia a la instancia si es la instancia actual.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}