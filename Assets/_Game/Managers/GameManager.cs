using UnityEngine;
using System;
using System.Linq;
using FreightForwarder.Utils;
using FreightForwarder.Managers;
using FreightForwarder.Models;

namespace FreightForwarder.Core
{
    public enum GameState { MainMenu, Playing, Paused, GameOver }

    public class GameManager : Singleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        // Eventos globales
        public event Action<GameState> OnGameStateChanged;
        public event Action OnNewGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameOver;

        [Header("Configuración")]
        [SerializeField] private int _startingMoney = 5000;
        [SerializeField] private int _startingReputation = 50;

        protected override void OnAwake()
        {
            base.OnAwake();
            Debug.Log("[GameManager] Inicializado");
        }

        public void StartNewGame()
        {
            CurrentState = GameState.Playing;
            OnGameStateChanged?.Invoke(CurrentState);
            OnNewGameStarted?.Invoke();

            // Resetear todos los managers
            EconomyManager.Instance?.ResetGame();

            var unlockedCities = CityDatabase.AllCities.Values
                .Where(c => c.IsUnlocked)
                .Select(c => c.Id)
                .ToList();

            CargoManager.Instance?.InitializeNewGame(unlockedCities);

            Debug.Log("[GameManager] Nueva partida iniciada");
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            OnGameStateChanged?.Invoke(CurrentState);
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            OnGameStateChanged?.Invoke(CurrentState);
            OnGameResumed?.Invoke();
        }

        public void TriggerGameOver()
        {
            CurrentState = GameState.GameOver;
            Time.timeScale = 0f;
            OnGameStateChanged?.Invoke(CurrentState);
            OnGameOver?.Invoke();
            Debug.Log("[GameManager] GAME OVER");
        }

        public void SetTimeScale(float scale)
        {
            if (CurrentState == GameState.Paused) return;
            Time.timeScale = scale;
        }
    }
}