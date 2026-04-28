using UnityEngine;
using UnityEngine.UIElements;

namespace FreightForwarder.UI
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnEnable()
        {
            Debug.Log("===== GAMEUI INICIADO =====");
            
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("❌ No hay UIDocument en este GameObject");
                return;
            }
            
            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("❌ RootVisualElement es nulo");
                return;
            }
            
            var allButtons = root.Query<Button>().ToList();
            Debug.Log($"✅ Se encontraron {allButtons.Count} botones");
            
            foreach (var btn in allButtons)
            {
                string btnName = btn.name;
                btn.RegisterCallback<ClickEvent>(ev => {
                    Debug.Log($"🎯 Botón clickeado: {btnName}");
                    btn.style.backgroundColor = Color.red;
                });
            }
            
            var testLabel = new Label("✅ UI FUNCIONA - HAZ CLIC EN UN BOTÓN");
            testLabel.style.fontSize = 30;
            testLabel.style.color = Color.green;
            testLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            testLabel.style.marginTop = 100;
            root.Add(testLabel);
        }
        
        public void ShowNotification(string message, string type = "info")
        {
            Debug.Log($"[Notificación] {message} (Tipo: {type})");
        }
    }
}