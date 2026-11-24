using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// Game flow (Singleton): which level is being played, the game state (menu, loading, playing,
    /// paused, complete, game over) and scene changes. Menus and the HUD call these methods;
    /// everything else learns about state changes through GameEvents.GameStateChanged.
    /// </summary>
    [DefaultExecutionOrder(-100)] // set up before menus and gameplay scripts ask for it
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
                return; // locked levels can't be started, even from code
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
