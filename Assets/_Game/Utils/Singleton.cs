using UnityEngine;

namespace FreightForwarder.Utils
{
    // ─────────────────────────────────────────────────────────────────────────
    // Singleton (patrón "instancia única")
    //
    // Sirve para que un sistema (por ejemplo el manejador del tiempo, la economía,
    // los clientes, etc.) tenga UNA sola copia viva en todo el juego, y que se pueda
    // usar desde cualquier parte escribiendo "MiClase.Instance".
    //
    // Cómo funciona, en simple:
    //  • La primera vez que alguien pide ".Instance": si todavía no existe, se busca
    //    en la escena; si tampoco está ahí, se crea sola en un objeto nuevo.
    //  • Si por error llegaran a existir dos copias, la copia de más se autodestruye,
    //    así siempre queda una y solo una.
    //  • "T" es el tipo concreto que hereda de esta clase (cada manager pone el suyo).
    // ─────────────────────────────────────────────────────────────────────────
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // La única instancia viva. Es "static" (compartida) para que sea la misma en todo el juego.
        private static T    _instance;
        // Se pone en true cuando el juego se está cerrando, para no recrear nada al salir.
        private static bool _applicationIsQuitting = false;

        // Punto de acceso global: escribir "MiClase.Instance" devuelve la instancia única.
        public static T Instance
        {
            get
            {
                // Si el juego se está cerrando, no devolvemos nada (evita errores al apagar).
                if (_applicationIsQuitting) return null;
                if (_instance == null)
                {
                    // Buscar una instancia que ya exista en la escena.
                    _instance = FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        // No había ninguna: creamos un objeto nuevo y le agregamos el componente.
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                        // Que sobreviva aunque se cambie de escena.
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Unity llama a Awake automáticamente al "despertar" el objeto (antes de empezar a jugar).
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                // Somos la primera instancia: nos registramos como LA instancia oficial.
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnAwake();   // gancho para que las clases hijas hagan su inicialización
            }
            else if (_instance != this)
            {
                // Ya existía otra instancia: ésta es una copia duplicada, así que la eliminamos.
                Destroy(gameObject);
            }
        }

        // Gancho opcional de inicialización para las clases hijas. Se ejecuta una sola vez,
        // cuando se crea la instancia válida. Por defecto no hace nada.
        protected virtual void OnAwake() { }

        // Unity lo llama cuando la aplicación se cierra: marcamos que estamos saliendo.
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        // Unity lo llama al destruir el objeto: si éramos la instancia activa, la liberamos
        // (dejamos el hueco libre para que se pueda crear otra si hiciera falta).
        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
