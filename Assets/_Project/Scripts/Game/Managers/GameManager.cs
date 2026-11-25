using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// Runs the game flow (Singleton): the current level, the game state and scene changes.
    /// Other systems hear about state changes through an event.
    /// </summary>
    [DefaultExecutionOrder(-100)] // runs before other scripts need it
    public class GameManager : Singleton<GameManager>
    {
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Game";

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private float gameOverDelay = 1.2f;

        public LevelCatalog Catalog => catalog;
        public int CurrentLevel { get; private set; } = 1;
        public GameState State { get; private set; } = GameState.MainMenu;

        private void OnEnable() => GameEvents.PlayerDied += OnPlayerDied;
        private void OnDisable() => GameEvents.PlayerDied -= OnPlayerDied;

        public void SetState(GameState state)
        {
            State = state;
            bool frozen = state == GameState.Paused || state == GameState.LevelComplete || state == GameState.GameOver;
            Time.timeScale = frozen ? 0f : 1f;
            GameEvents.RaiseGameStateChanged(state);
        }

        public void PlayLevel(int level)
        {
            if (!SaveManager.Instance.Progress.IsPlayable(level))
                return; // can't start a locked level
            CurrentLevel = level;
            SetState(GameState.Loading);
            SceneManager.LoadScene(GameScene);
        }

        public void ContinueGame() => PlayLevel(SaveManager.Instance.Progress.NextLevelToPlay());
        public void StartGame() => PlayLevel(1);
        public void RestartLevel() => PlayLevel(CurrentLevel);

        public bool HasNextLevel => catalog != null && CurrentLevel < catalog.Count;

        public void NextLevel()
        {
            if (HasNextLevel)
                PlayLevel(CurrentLevel + 1);
            else
                ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            SetState(GameState.MainMenu);
            SceneManager.LoadScene(MenuScene);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
                SetState(GameState.Paused);
            else if (State == GameState.Paused)
                SetState(GameState.Playing);
        }

        public void QuitGame()
        {
            SaveManager.Instance.Save();
            PlatformFeatures.Quit();
        }

        private void OnPlayerDied() => StartCoroutine(ShowGameOver());

        private IEnumerator ShowGameOver()
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);
            if (State == GameState.Playing)
                SetState(GameState.GameOver);
        }
    }
}
