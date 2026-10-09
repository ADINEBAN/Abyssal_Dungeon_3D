using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace AbyssalDungeon.Core
{
    public enum GameState
    {
        MainMenu,
        Lobby,
        ClassSelection,
        Exploring,
        Combat,
        BossFight,
        Victory,
        Defeat,
        EndGame,
        Paused
    }

    public class GameManager : Singleton<GameManager>
    {
        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnSceneLoaded;

        [SerializeField] private GameState _currentState;
        [SerializeField] private bool _isPaused = false;

        public GameState CurrentState => _currentState;
        public bool IsPaused => _isPaused;

        public SaveData CurrentGameData { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            OnSceneLoaded?.Invoke(scene.name);
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;
            
            _currentState = newState;
            OnGameStateChanged?.Invoke(_currentState);
            Debug.Log($"[GameManager] State changed to: {_currentState}");
        }

        public void LoadScene(string sceneName)
        {
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadSceneAsync(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void PauseGame()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Time.timeScale = 0f;
            ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (!_isPaused) return;

            _isPaused = false;
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Game Resumed.");
        }

        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game...");
            Application.Quit();
        }
    }
}
