using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MainMenuUI : MonoBehaviour
{
    private enum MenuState { Main, Settings }

    private MenuState currentState = MenuState.Main;
    private float audioVolume = 1f;
    private bool showLoadError = false;
    private string loadErrorMessage = string.Empty;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle errorStyle;
    private GUIStyle panelStyle;
    private bool stylesInitialized = false;

    private const float PanelW = 380f;
    private const float PanelH = 440f;
    private const float BtnH = 48f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateMenuObject()
    {
        if (FindAnyObjectByType<MainMenuUI>() == null)
        {
            GameObject go = new GameObject("MainMenuUI");
            DontDestroyOnLoad(go);
            go.AddComponent<MainMenuUI>();
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        errorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(1f, 0.35f, 0.35f) }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.grayTexture }
        };
    }

    private bool IsMenuVisible()
    {
        if (GameManager.Instance == null) return true;
        return !GameManager.Instance.IsGameRunning;
    }

    private void OnGUI()
    {
        if (!IsMenuVisible()) return;
        InitStyles();

        // Fondo oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float px = (Screen.width - PanelW) / 2f;
        float py = (Screen.height - PanelH) / 2f;

        GUI.Box(new Rect(px - 8, py - 8, PanelW + 16, PanelH + 16), GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(px, py, PanelW, PanelH));

        GUILayout.Space(18f);
        GUILayout.Label("Freight Forwarder", titleStyle, GUILayout.Height(40f));
        GUILayout.Space(6f);

        if (currentState == MenuState.Main)
            DrawMainMenu();
        else
            DrawSettings();

        GUILayout.EndArea();
    }

    private void DrawMainMenu()
    {
        GUILayout.Label("Menú Principal", labelStyle, GUILayout.Height(24f));
        GUILayout.Space(16f);

        if (GUILayout.Button("Nueva Partida", buttonStyle, GUILayout.Height(BtnH)))
            StartNewGame();

        GUILayout.Space(6f);

        if (GUILayout.Button("Cargar Partida", buttonStyle, GUILayout.Height(BtnH)))
            LoadGame();

        GUILayout.Space(6f);

        if (GUILayout.Button("Configuración", buttonStyle, GUILayout.Height(BtnH)))
        {
            currentState = MenuState.Settings;
            showLoadError = false;
        }

        GUILayout.Space(6f);

        if (GUILayout.Button("Salir", buttonStyle, GUILayout.Height(BtnH)))
            QuitGame();

        GUILayout.FlexibleSpace();

        if (showLoadError)
            GUILayout.Label(loadErrorMessage, errorStyle, GUILayout.Height(36f));

        GUILayout.Space(10f);
    }

    private void DrawSettings()
    {
        GUILayout.Label("Configuración", labelStyle, GUILayout.Height(24f));
        GUILayout.Space(16f);

        GUILayout.Label($"Volumen de Audio: {Mathf.RoundToInt(audioVolume * 100f)}%", labelStyle);
        audioVolume = GUILayout.HorizontalSlider(audioVolume, 0f, 1f);
        AudioListener.volume = audioVolume;

        GUILayout.Space(20f);
        GUILayout.Label("Más opciones próximamente.", labelStyle, GUILayout.Height(30f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Volver", buttonStyle, GUILayout.Height(BtnH)))
            currentState = MenuState.Main;

        GUILayout.Space(10f);
    }

    private void StartNewGame()
    {
        if (GameManager.Instance == null) { SetError("GameManager no disponible."); return; }
        GameManager.Instance.StartNewGame();
        showLoadError = false;
    }

    private void LoadGame()
    {
        if (GameManager.Instance == null) { SetError("GameManager no disponible."); return; }
        if (SaveManager.Instance == null) { SetError("SaveManager no disponible."); return; }
        if (!SaveManager.Instance.HasSaveFile) { SetError("No se encontró partida guardada."); return; }
        GameManager.Instance.LoadSavedGame();
        showLoadError = false;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetError(string msg)
    {
        showLoadError = true;
        loadErrorMessage = msg;
    }
}
