using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FreightForwarder.Managers;

namespace FreightForwarder.Managers
{
    /// <summary>
    /// BootLoader - Punto de entrada del juego.
    /// Se ejecuta en la escena _Boot y carga asíncronamente la escena MainMenu.
    /// 
    /// Explicación para principiantes en C#:
    /// - "using" = importa namespaces (como bibliotecas) para usar sus clases
    /// - "namespace" = organiza el código en contenedores lógicos
    /// - "public class" = define una clase accesible desde cualquier parte
    /// - ": MonoBehaviour" = hereda de MonoBehaviour (clase base de Unity)
    /// - "[SerializeField]" = expone una variable en el Inspector de Unity
    /// 
    /// Flujo del juego:
    /// _Boot (esta escena) → MainMenu → (Nueva Partida) → Game
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [Header("Configuración de Carga")]
        [Tooltip("Nombre de la escena del menú principal")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        
        [Tooltip("Tiempo mínimo (en segundos) que se muestra la pantalla de carga")]
        [SerializeField] private float minLoadTime = 1.5f;
        
        [Header("Referencias UI (opcional)")]
        [Tooltip("Slider de progreso (si se usa pantalla de carga visual)")]
        [SerializeField] private UnityEngine.UI.Slider progressBar;
        
        [Tooltip("Texto de estado (si se usa pantalla de carga visual)")]
        [SerializeField] private TMPro.TextMeshProUGUI statusText;
        
        // Almacena la operación de carga asíncrona
        private AsyncOperation sceneLoadOperation;
        
        /// <summary>
        /// Start() se llama UNA VEZ cuando el objeto se activa.
        /// Inicia la carga asíncrona del menú principal.
        /// </summary>
        private void Start()
        {
            // Desactivamos el cursor del sistema (lo manejamos nosotros)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Iniciamos la corrutina de carga
            StartCoroutine(LoadMainMenuAsync());
        }
        
        /// <summary>
        /// IEnumerator = una función que puede pausarse y reanudarse (corrutina).
        /// "yield" le dice a Unity: "pausa aquí y continúa en el próximo frame".
        /// 
        /// Esta corrutina:
        /// 1. Inicia la carga asíncrona de MainMenu
        /// 2. Espera hasta que la carga esté al 90%
        /// 3. Espera el tiempo mínimo configurado
        /// 4. Activa la escena
        /// </summary>
        private IEnumerator LoadMainMenuAsync()
        {
            // Actualizar UI de inicio
            UpdateStatus("Inicializando sistemas...", 0f);
            
            // Registrar tiempo de inicio
            float startTime = Time.time;
            
            // INICIAR CARGA ASÍNCRONA
            // LoadSceneAsync carga la escena en segundo plano sin congelar el juego
            // allowSceneActivation = false → carga pero NO muestra la escena aún
            sceneLoadOperation = SceneManager.LoadSceneAsync(mainMenuSceneName);
            sceneLoadOperation.allowSceneActivation = false;
            
            // Mientras la carga no alcance el 90% (0.9 = listo para activar)
            while (sceneLoadOperation.progress < 0.9f)
            {
                // Actualizar barra de progreso (el progreso va de 0 a 0.9)
                float progress = sceneLoadOperation.progress / 0.9f;
                UpdateStatus("Cargando recursos...", progress);
                
                // yield return null = esperar 1 frame
                yield return null;
            }
            
            // ¡Carga completada! (al 90%)
            UpdateStatus("¡Carga completada!", 1f);
            
            // Calcular tiempo transcurrido
            float elapsedTime = Time.time - startTime;
            
            // Si la carga fue muy rápida, esperar el tiempo mínimo
            // Esto evita un "parpadeo" de la pantalla de carga
            if (elapsedTime < minLoadTime)
            {
                float remainingTime = minLoadTime - elapsedTime;
                UpdateStatus($"Finalizando en {remainingTime:F1} segundos...", 0.95f);
                yield return new WaitForSeconds(remainingTime);
            }
            
            // Pequeña pausa para que el jugador vea el "100%"
            yield return new WaitForSeconds(0.2f);
            
            // ACTIVAR LA ESCENA
            // Esto cambia de _Boot a MainMenu
            sceneLoadOperation.allowSceneActivation = true;
        }
        
        /// <summary>
        /// Actualiza los elementos de UI durante la carga.
        /// </summary>
        /// <param name="message">Mensaje de estado</param>
        /// <param name="progress">Progreso (0-1)</param>
        private void UpdateStatus(string message, float progress)
        {
            // Actualizar texto de estado si existe
            if (statusText != null)
            {
                statusText.text = message;
            }
            
            // Actualizar barra de progreso si existe
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
            
            // Log para debugging (se ve en la consola de Unity)
            Debug.Log($"[BootLoader] {message} ({progress * 100:F0}%)");
        }
        
        /// <summary>
        /// Método público para forzar la carga (útil para debugging).
        /// Se puede llamar desde un botón en la UI si algo falla.
        /// </summary>
        public void ForceContinue()
        {
            if (sceneLoadOperation != null && !sceneLoadOperation.allowSceneActivation)
            {
                Debug.Log("[BootLoader] Forzando continuación...");
                sceneLoadOperation.allowSceneActivation = true;
            }
        }
        
        /// <summary>
        /// Método público para recargar el boot (útil si hay errores).
        /// </summary>
        public void ReloadBoot()
        {
            SceneManager.LoadScene("_Boot");
        }
    }
}