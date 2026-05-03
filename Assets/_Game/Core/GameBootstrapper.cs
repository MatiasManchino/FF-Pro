using UnityEngine;

/// <summary>
/// GameBootstrapper es el punto de entrada principal del juego.
/// Se encarga de inicializar todos los managers del sistema en el orden correcto.
/// Debe ser el primer script que se ejecute en la escena principal.
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private ClientManager clientManager;
    [SerializeField] private AgentManager agentManager;
    [SerializeField] private CargoManager cargoManager;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SunController sunController;

    private void Awake()
    {
        // Inicializar managers en orden de dependencia
        InitializeManagers();
    }

    private void Start()
    {
        // Iniciar el juego después de que todos los managers estén listos
        StartGame();
    }

    /// <summary>
    /// Inicializa todos los managers en el orden correcto según las dependencias.
    /// </summary>
    private void InitializeManagers()
    {
        Debug.Log("Inicializando managers del sistema...");

        // 1. TimeManager - Base temporal del juego
        if (timeManager != null)
        {
            timeManager.Initialize();
            Debug.Log("TimeManager inicializado.");
        }

        // 2. EconomyManager - Sistema económico
        if (economyManager != null)
        {
            economyManager.Initialize();
            Debug.Log("EconomyManager inicializado.");
        }

        // 3. ClientManager - Gestión de clientes
        if (clientManager != null)
        {
            clientManager.Initialize();
            Debug.Log("ClientManager inicializado.");
        }

        // 4. AgentManager - Gestión de agentes de transporte
        if (agentManager != null)
        {
            agentManager.Initialize();
            Debug.Log("AgentManager inicializado.");
        }

        // 5. CargoManager - Gestión de cargas
        if (cargoManager != null)
        {
            cargoManager.Initialize();
            Debug.Log("CargoManager inicializado.");
        }

        // 6. EventManager - Sistema de eventos aleatorios
        if (eventManager != null)
        {
            eventManager.Initialize();
            Debug.Log("EventManager inicializado.");
        }

        // 7. SaveManager - Sistema de guardado/carga
        if (saveManager != null)
        {
            saveManager.Initialize();
            Debug.Log("SaveManager inicializado.");
        }

        // 8. GameManager - Lógica principal del juego
        if (gameManager != null)
        {
            gameManager.Initialize();
            Debug.Log("GameManager inicializado.");
        }

        // 9. SunController - Control visual del sol/mapa
        if (sunController != null)
        {
            sunController.Initialize();
            Debug.Log("SunController inicializado.");
        }

        Debug.Log("Todos los managers han sido inicializados correctamente.");
    }

    /// <summary>
    /// Inicia el flujo principal del juego después de la inicialización.
    /// </summary>
    private void StartGame()
    {
        Debug.Log("Iniciando juego...");

        // Cargar datos guardados si existen
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }

        // Iniciar el tiempo del juego
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartGameTime();
        }

        // Mostrar pantalla inicial o menú principal
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }

        Debug.Log("Juego iniciado correctamente.");
    }

    private void OnApplicationQuit()
    {
        // Guardar el estado del juego al salir
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        Debug.Log("Juego guardado y cerrado.");
    }
}