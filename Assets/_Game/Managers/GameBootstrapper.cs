using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Map;
using FreightForwarder.UI;

namespace FreightForwarder.Core
{
    /// <summary>
    /// GameBootstrapper - Orquestador principal del juego.
    /// Se encarga de inicializar todos los managers en el orden correcto.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private float _bootDelay = 0.5f;

        [Header("Referencias")]
        [SerializeField] private GameObject _timeManagerPrefab;
        [SerializeField] private GameObject _economyManagerPrefab;
        [SerializeField] private GameObject _cargoManagerPrefab;
        [SerializeField] private GameObject _clientManagerPrefab;
        [SerializeField] private GameObject _eventManagerPrefab;
        [SerializeField] private GameObject _agentManagerPrefab;
        [SerializeField] private GameObject _worldMapPrefab;
        [SerializeField] private GameObject _sunControllerPrefab;
        [SerializeField] private GameObject _gameUIPrefab;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            Debug.Log("[GameBootstrapper] Iniciando sistema...");
            yield return new WaitForSeconds(_bootDelay);

            // 1. Inicializar Managers (en orden de dependencia)
            InitializeManagers();

            // 2. Configurar datos iniciales
            SetupInitialData();

            // 3. Cargar escena del juego
            yield return StartCoroutine(LoadGameSceneAsync());
        }

        private void InitializeManagers()
        {
            // TimeManager (primero, otros dependen de él)
            if (TimeManager.Instance == null && _timeManagerPrefab != null)
                Instantiate(_timeManagerPrefab);

            // EconomyManager
            if (EconomyManager.Instance == null && _economyManagerPrefab != null)
                Instantiate(_economyManagerPrefab);

            // ClientManager (antes que CargoManager)
            if (ClientManager.Instance == null && _clientManagerPrefab != null)
                Instantiate(_clientManagerPrefab);

            // AgentManager
            if (AgentManager.Instance == null && _agentManagerPrefab != null)
                Instantiate(_agentManagerPrefab);

            // CargoManager
            if (CargoManager.Instance == null && _cargoManagerPrefab != null)
                Instantiate(_cargoManagerPrefab);

            // EventManager
            if (EventManager.Instance == null && _eventManagerPrefab != null)
                Instantiate(_eventManagerPrefab);

            Debug.Log("[GameBootstrapper] Managers inicializados");
        }

        private void SetupInitialData()
        {
            // Desbloquear ciudad inicial
            var buenosAires = CityDatabase.GetCity("buenos_aires");
            if (buenosAires != null)
                buenosAires.IsUnlocked = true;

            // Inicializar economía
            EconomyManager.Instance?.ResetGame();

            // Inicializar mercado de cargas
            var unlockedCities = CityDatabase.AllCities.Values
                .Where(c => c.IsUnlocked)
                .Select(c => c.Id)
                .ToList();

            CargoManager.Instance?.InitializeNewGame(unlockedCities);

            Debug.Log("[GameBootstrapper] Datos iniciales configurados");
        }

        private IEnumerator LoadGameSceneAsync()
        {
            var operation = SceneManager.LoadSceneAsync(_gameSceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                Debug.Log($"[GameBootstrapper] Cargando... {operation.progress * 100:F0}%");
                yield return null;
            }

            operation.allowSceneActivation = true;

            // Instanciar UI y Mapa después de cargar la escena
            yield return new WaitForSeconds(0.1f);

            if (WorldMap.Instance == null && _worldMapPrefab != null)
                Instantiate(_worldMapPrefab);

            if (SunController.Instance == null && _sunControllerPrefab != null)
                Instantiate(_sunControllerPrefab);

            if (GameUI.Instance == null && _gameUIPrefab != null)
                Instantiate(_gameUIPrefab);

            Debug.Log("[GameBootstrapper] Juego listo!");
        }
    }
}