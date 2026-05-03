using UnityEngine;

/// <summary>
/// GameUIPanel muestra información del juego en pantalla usando OnGUI.
/// Se crea automáticamente si no existe.
/// </summary>
public class GameUIPanel : MonoBehaviour
{
    private static GameUIPanel instance;
    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private Rect panelRect = new Rect(10, 10, 340, 120);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameUIPanel()
    {
        if (FindAnyObjectByType<GameUIPanel>() != null)
            return;

        GameObject panelObject = new GameObject("GameUIPanel");
        panelObject.hideFlags = HideFlags.DontSave;
        panelObject.AddComponent<GameUIPanel>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnGUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentGameState != GameState.Playing)
            return;

        if (panelStyle == null)
        {
            InitializeStyles();
        }

        GUI.Box(panelRect, string.Empty, panelStyle);

        float labelX = panelRect.x + 12;
        float labelY = panelRect.y + 10;
        float labelWidth = panelRect.width - 24;
        float labelHeight = 22;

        GUI.Label(new Rect(labelX, labelY, labelWidth, labelHeight), $"Tiempo: {GetTimeText()}", labelStyle);
        GUI.Label(new Rect(labelX, labelY + 24, labelWidth, labelHeight), $"Dinero: ${GetMoneyText()}", labelStyle);
        GUI.Label(new Rect(labelX, labelY + 48, labelWidth, labelHeight), $"Estado: {(GameManager.Instance.IsPaused ? "PAUSADO" : "Jugando")}", labelStyle);

        float buttonWidth = 96;
        float buttonHeight = 30;
        float buttonY = panelRect.y + panelRect.height - buttonHeight - 14;

        if (GUI.Button(new Rect(labelX, buttonY, buttonWidth, buttonHeight), GameManager.Instance.IsPaused ? "Reanudar" : "Pausar", buttonStyle))
        {
            if (GameManager.Instance.IsPaused)
                GameManager.Instance.ResumeGame();
            else
                GameManager.Instance.PauseGame();
        }

        if (GUI.Button(new Rect(labelX + buttonWidth + 12, buttonY, buttonWidth, buttonHeight), "Guardar", buttonStyle))
        {
            GameManager.Instance.SaveCurrentGame();
        }

        if (GUI.Button(new Rect(labelX + 2 * (buttonWidth + 12), buttonY, buttonWidth, buttonHeight), "Menú", buttonStyle))
        {
            GameManager.Instance.SetGameState(GameState.Menu);
            Time.timeScale = 1f;
        }
    }

    private void InitializeStyles()
    {
        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.whiteTexture },
            border = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(8, 8, 8, 8),
            alignment = TextAnchor.UpperLeft,
            normalTextColor = Color.white,
            fontSize = 14
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fixedHeight = 30
        };
    }

    private string GetTimeText()
    {
        return TimeManager.Instance != null ? TimeManager.Instance.GetCurrentDateString() : "--";
    }

    private string GetMoneyText()
    {
        return EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney.ToString("0") : "0";
    }
}
