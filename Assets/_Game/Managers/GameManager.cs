using System;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class GameManager : Singleton<GameManager>
    {
// Juego estado.
        public enum GameState { MainMenu, Playing, Paused, GameOver }

// Actual estado.
        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action OnNewGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameOver;

        private bool _gameStarted;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            CurrentState = GameState.MainMenu;
        }

// Inicio new juego.
        public void StartNewGame()
        {
            if (_gameStarted) return;
            _gameStarted = true;
            CurrentState = GameState.Playing;
            OnNewGameStarted?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
        }

// Pausa juego.
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Paused;
            TimeManager.Instance?.SetSpeedIndex(0); // Pausa el tiempo del mapa
            OnGamePaused?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
        }

// Reanuda juego.
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            CurrentState = GameState.Playing;
            TimeManager.Instance?.SetSpeedIndex(1); // Reanuda en 1x
            OnGameResumed?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
        }

// Dispara juego terminado.
        public void TriggerGameOver()
        {
            CurrentState = GameState.GameOver;
            TimeManager.Instance?.SetSpeedIndex(0); // Detiene el tiempo del mapa
            OnGameOver?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
            Debug.Log("[GameManager] GAME OVER");
        }

// Establece tiempo escala.
        public void SetTimeScale(int speedIndex)
        {
            TimeManager.Instance?.SetSpeedIndex(speedIndex);
        }

// Indica si playing
        public bool IsPlaying => CurrentState == GameState.Playing;
    }
}
