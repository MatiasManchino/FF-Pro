using UnityEngine;

/// <summary>
/// Dibuja un menú principal simple en pantalla usando OnGUI.
/// Se crea automáticamente al cargar cualquier escena si no existe un MainMenuUI.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MainMenuUI : MonoBehaviour
{
    private enum MenuState
    {
        Main,
        Settings
    }

    private MenuState currentState = MenuState.Main;
    private Rect menuWindow;
    private bool showMenu = true;
    private float audioVolume = 1f;
    private bool showLoadError = false;
    private string loadErrorMessage = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateMenuObject()
    {
        if (FindAnyObjectByType<MainMenuUI>() == null)
        {
            GameObject menuObject = new GameObject("MainMenuUI");
            DontDestroyOnLoad(menuObject);
            menuObject.AddComponent<MainMenuUI>();
        }
    }

    private void Awake()
    {
        menuWindow = new Rect((Screen.width - 360f) / 2f, (Screen.height - 420f) / 2f, 360f, 420f);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
        {
            showMenu = false;
        }
        else
        {
            showMenu = true;
        }
    }

    private void OnGUI()
    {
        if (!showMenu) return;

        menuWindow = GUI.Window(987654, menuWindow, DrawMainMenuWindow, "Freight Forwarder");
    }

    private void DrawMainMenuWindow(int windowId)
    {
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 16;
        GUI.skin.textField.fontSize = 16;

        GUILayout.Space(10);
        GUILayout.Label("Menú Principal", GUILayout.Height(28));
        GUILayout.Space(10);

        if (currentState == MenuState.Main)
        {
            if (GUILayout.Button("Nueva Partida", GUILayout.Height(42)))
            {
                StartNewGame();
            }

            if (GUILayout.Button("Cargar Partida", GUILayout.Height(42)))
            {
                LoadGame();
            }

            if (GUILayout.Button("Configuración", GUILayout.Height(42)))
            {
                currentState = MenuState.Settings;
                showLoadError = false;
            }

            if (GUILayout.Button("Salir", GUILayout.Height(42)))
            {
                QuitGame();
            }

            GUILayout.FlexibleSpace();

            if (showLoadError)
            {
                GUI.contentColor = Color.red;
                GUILayout.Label(loadErrorMessage, GUILayout.Height(40));
                GUI.contentColor = Color.white;
            }
        }
        else if (currentState == MenuState.Settings)
        {
            GUILayout.Label("Configuración", GUILayout.Height(28));
            GUILayout.Space(8);

            GUILayout.Label($"Volumen de Audio: {Mathf.RoundToInt(audioVolume * 100f)}%");
            audioVolume = GUILayout.HorizontalSlider(audioVolume, 0f, 1f);
            GUILayout.Space(10);

            GUILayout.Label("Opciones de juego futuras se agregarán aquí.", GUILayout.Height(40));
            GUILayout.Space(10);

            if (GUILayout.Button("Guardar y volver", GUILayout.Height(38)))
            {
                currentState = MenuState.Main;
            }

            if (GUILayout.Button("Volver", GUILayout.Height(38)))
            {
                currentState = MenuState.Main;
            }
        }

        GUILayout.Space(6);
        GUILayout.Label("Usa este menú para iniciar o cargar partida.");
        GUI.DragWindow(new Rect(0, 0, menuWindow.width, 30));
    }

    private void StartNewGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
            showLoadError = false;
        }
        else
        {
            SetLoadError("GameManager no disponible.");
        }
    }

    private void LoadGame()
    {
        if (GameManager.Instance == null)
        {
            SetLoadError("GameManager no disponible.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            SetLoadError("SaveManager no disponible.");
            return;
        }

        if (SaveManager.Instance.HasSaveFile)
        {
            GameManager.Instance.LoadSavedGame();
            showLoadError = false;
        }
        else
        {
            SetLoadError("No se encontró partida guardada.");
        }
    }

    private void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void SetLoadError(string message)
    {
        showLoadError = true;
        loadErrorMessage = message;
    }
}
