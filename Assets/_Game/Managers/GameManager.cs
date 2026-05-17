using System;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class GameManager : Singleton<GameManager>
    {
        public enum GameState { MainMenu, Playing, Paused, GameOver }

        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action OnNewGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameOver;

        protected override void OnAwake()
        {
            CurrentState = GameState.MainMenu;
        }

        public void StartNewGame()
        {
            CurrentState = GameState.Playing;
            OnNewGameStarted?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Paused;
            TimeManager.Instance?.SetSpeedIndex(0); // Pausa el tiempo del mapa
            OnGamePaused?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            CurrentState = GameState.Playing;
            TimeManager.Instance?.SetSpeedIndex(1); // Reanuda en 1x
            OnGameResumed?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void TriggerGameOver()
        {
            CurrentState = GameState.GameOver;
            TimeManager.Instance?.SetSpeedIndex(0); // Detiene el tiempo del mapa
            OnGameOver?.Invoke();
            OnGameStateChanged?.Invoke(CurrentState);
            Debug.Log("[GameManager] GAME OVER");
        }

        public void SetTimeScale(int speedIndex)
        {
            TimeManager.Instance?.SetSpeedIndex(speedIndex);
        }

        public bool IsPlaying => CurrentState == GameState.Playing;
    }
}
