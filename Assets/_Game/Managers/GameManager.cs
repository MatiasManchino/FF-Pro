using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager es el manager principal que coordina la lógica general del juego.
/// Gestiona el flujo del juego, estados, transiciones y lógica de alto nivel.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Configuración del Juego")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene = "Game";
    [SerializeField] private bool startWithMainMenu = true;

    [Header("Estado del Juego")]
    [SerializeField] private GameState currentState = GameState.Menu;
    [SerializeField] private bool isPaused = false;

    // Eventos
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnGamePaused;
    public System.Action OnGameResumed;
    public System.Action OnNewGameStarted;
    public System.Action OnGameOver;

    // Propiedades públicas
    public GameState CurrentState => currentState;
    public bool IsPaused => isPaused;
    public bool IsGameRunning => currentState == GameState.Playing && !isPaused;

    /// <summary>
    /// Inicializa el GameManager.
    /// </summary>
    public void Initialize()
    {
        currentState = GameState.Initializing;
        isPaused = false;

        Debug.Log("GameManager inicializado");

        // Cargar escena inicial
        if (startWithMainMenu)
        {
            LoadMainMenu();
        }
        else
        {
            StartNewGame();
        }
    }

    /// <summary>
    /// Inicia un nuevo juego.
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("Iniciando nuevo juego...");

        // Cambiar estado
        SetGameState(GameState.Playing);

        // Reiniciar managers
        ResetAllManagers();

        // Cargar escena del juego si no estamos en ella y la escena existe
        if (SceneManager.GetActiveScene().name != gameScene)
        {
            if (SceneExistsInBuildSettings(gameScene))
            {
                SceneManager.LoadScene(gameScene);
            }
            else
            {
                Debug.LogWarning($"Scene '{gameScene}' no está en Build Settings. Continuando en la escena actual.");
            }
        }

        OnNewGameStarted?.Invoke();
        Debug.Log("Nuevo juego iniciado");
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Carga el menú principal.
    /// </summary>
    public void LoadMainMenu()
    {
        SetGameState(GameState.Menu);

        if (SceneManager.GetActiveScene().name != mainMenuScene)
        {
            SceneManager.LoadScene(mainMenuScene);
        }

        Debug.Log("Menú principal cargado");
    }

    /// <summary>
    /// Carga un juego guardado.
    /// </summary>
    /// <param name="saveFileName">Nombre del archivo de guardado</param>
    public void LoadSavedGame(string saveFileName = null)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager no disponible para cargar juego");
            return;
        }

        Debug.Log("Cargando juego guardado...");

        if (SaveManager.Instance.LoadGame(saveFileName))
        {
            SetGameState(GameState.Playing);

            if (SceneManager.GetActiveScene().name != gameScene)
            {
                SceneManager.LoadScene(gameScene);
            }

            Debug.Log("Juego guardado cargado exitosamente");
        }
        else
        {
            Debug.LogError("Error al cargar juego guardado");
        }
    }

    /// <summary>
    /// Guarda el juego actual.
    /// </summary>
    public void SaveCurrentGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    /// <summary>
    /// Pausa el juego.
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        isPaused = true;
        Time.timeScale = 0f;

        // Pausar managers
        if (TimeManager.Instance != null) TimeManager.Instance.PauseGameTime();

        OnGamePaused?.Invoke();
        Debug.Log("Juego pausado");
    }

    /// <summary>
    /// Reanuda el juego.
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        // Reanudar managers
        if (TimeManager.Instance != null) TimeManager.Instance.ResumeGameTime();

        OnGameResumed?.Invoke();
        Debug.Log("Juego reanudado");
    }

    /// <summary>
    /// Termina el juego actual (game over).
    /// </summary>
    /// <param name="reason">Razón del game over</param>
    public void GameOver(string reason = "")
    {
        SetGameState(GameState.GameOver);

        // Pausar todo
        PauseGame();

        OnGameOver?.Invoke();
        Debug.Log($"Game Over: {reason}");

        // Mostrar pantalla de game over (en implementación real)
        // ShowGameOverScreen(reason);
    }

    /// <summary>
    /// Verifica condiciones de game over.
    /// </summary>
    private void CheckGameOverConditions()
    {
        if (EconomyManager.Instance == null) return;

        // Condición 1: Quiebra
        if (EconomyManager.Instance.IsBankrupt)
        {
            GameOver("Quiebra - Dinero insuficiente");
            return;
        }

        // Condición 2: Reputación baja
        if (EconomyManager.Instance.HasLowReputation)
        {
            GameOver("Reputación baja - Nadie quiere trabajar contigo");
            return;
        }
    }

    /// <summary>
    /// Sale del juego.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        // Guardar antes de salir
        SaveCurrentGame();

        // Salir de la aplicación
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Cambia el estado del juego.
    /// </summary>
    /// <param name="newState">Nuevo estado del juego</param>
    private void SetGameState(GameState newState)
    {
        GameState oldState = currentState;
        currentState = newState;

        OnGameStateChanged?.Invoke(currentState);
        Debug.Log($"Estado del juego cambiado: {oldState} → {newState}");
    }

    /// <summary>
    /// Reinicia todos los managers para un nuevo juego.
    /// </summary>
    private void ResetAllManagers()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.Initialize();
        if (EconomyManager.Instance != null) EconomyManager.Instance.ResetEconomy();
        if (CargoManager.Instance != null) CargoManager.Instance.ClearAllCargos();
        if (ClientManager.Instance != null) ClientManager.Instance.ClearAllClients();
        if (AgentManager.Instance != null) AgentManager.Instance.ClearAllAgents();
        if (EventManager.Instance != null) EventManager.Instance.ClearActiveEvents();

        Debug.Log("Todos los managers reiniciados");
    }

    /// <summary>
    /// Actualización por frame del GameManager.
    /// </summary>
    private void Update()
    {
        // Verificar condiciones de game over solo si estamos jugando
        if (currentState == GameState.Playing && !isPaused)
        {
            CheckGameOverConditions();
        }

        // Manejar input de pausa
        if (Input.GetKeyDown(KeyCode.Escape) && currentState == GameState.Playing)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    /// <summary>
    /// Obtiene estadísticas generales del juego.
    /// </summary>
    public System.Collections.Generic.Dictionary<string, object> GetGameStats()
    {
        var stats = new System.Collections.Generic.Dictionary<string, object>();

        stats["CurrentState"] = currentState;
        stats["IsPaused"] = isPaused;
        stats["GameTime"] = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentDateString() : "N/A";

        if (EconomyManager.Instance != null)
        {
            stats["Money"] = EconomyManager.Instance.CurrentMoney;
            stats["Reputation"] = EconomyManager.Instance.CurrentReputation;
            stats["CompletedCargos"] = EconomyManager.Instance.CompletedCargos;
            stats["FailedCargos"] = EconomyManager.Instance.FailedCargos;
        }

        if (CargoManager.Instance != null)
        {
            var cargoStats = CargoManager.Instance.GetCargoStats();
            foreach (var kvp in cargoStats)
            {
                stats[$"Cargo_{kvp.Key}"] = kvp.Value;
            }
        }

        return stats;
    }

    /// <summary>
    /// Reinicia completamente el juego a su estado inicial.
    /// </summary>
    public void FullGameReset()
    {
        Debug.Log("Reinicio completo del juego...");

        // Reiniciar estado
        SetGameState(GameState.Menu);
        isPaused = false;
        Time.timeScale = 1f;

        // Reiniciar managers
        ResetAllManagers();

        // Cargar menú principal
        LoadMainMenu();

        Debug.Log("Juego reiniciado completamente");
    }
}

/// <summary>
/// Estados posibles del juego.
/// </summary>
public enum GameState
{
    Initializing,  // Inicializando
    Menu,          // Menú principal
    Playing,       // Jugando activamente
    Paused,        // Pausado
    GameOver       // Fin del juego
}