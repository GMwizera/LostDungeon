using System;

namespace LostDungeon.Core.Events
{
    /// <summary>
    /// Central event hub (Observer pattern).
    /// Gameplay systems raise events here; UI, audio, saving and objectives listen.
    /// Neither side holds a reference to the other, so either can be replaced or removed.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>(current, max)</summary>
        public static event Action<int, int> PlayerHealthChanged;
        public static event Action PlayerDied;

        /// <summary>Enemy id, e.g. "Warrior".</summary>
        public static event Action<string> EnemyDefeated;

        /// <summary>Enemy id of the enemy that just attacked (for sounds).</summary>
        public static event Action<string> EnemyAttacked;

        /// <summary>(item id, amount)</summary>
        public static event Action<string, int> ItemCollected;

        /// <summary>(ability name, cooldown seconds)</summary>
        public static event Action<string, float> AbilityActivated;

        /// <summary>Level number, starting at 1.</summary>
        public static event Action<int> LevelCompleted;

        public static event Action<GameState> GameStateChanged;

        /// <summary>(item id, new count) whenever the player's inventory changes.</summary>
        public static event Action<string, int> InventoryChanged;

        /// <summary>Text of the current level objective.</summary>
        public static event Action<string> ObjectiveChanged;

        /// <summary>Prompt for the nearest interactable, or null when there is none.</summary>
        public static event Action<string> InteractionPromptChanged;

        /// <summary>(level number, level name) once a level has finished loading.</summary>
        public static event Action<int, string> LevelStarted;

        /// <summary>Loading progress from 0 to 1.</summary>
        public static event Action<float> LevelLoadProgress;

        /// <summary>Message shown when a level could not be loaded.</summary>
        public static event Action<string> LevelLoadFailed;

        /// <summary>(title, description) of the dungeon conditions from the weather API.</summary>
        public static event Action<string, string> ConditionsChanged;

        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseEnemyDefeated(string enemyId) => EnemyDefeated?.Invoke(enemyId);
        public static void RaiseEnemyAttacked(string enemyId) => EnemyAttacked?.Invoke(enemyId);
        public static void RaiseItemCollected(string itemId, int amount) => ItemCollected?.Invoke(itemId, amount);
        public static void RaiseAbilityActivated(string abilityName, float cooldown) => AbilityActivated?.Invoke(abilityName, cooldown);
        public static void RaiseLevelCompleted(int level) => LevelCompleted?.Invoke(level);
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);
        public static void RaiseInventoryChanged(string itemId, int count) => InventoryChanged?.Invoke(itemId, count);
        public static void RaiseObjectiveChanged(string text) => ObjectiveChanged?.Invoke(text);
        public static void RaiseInteractionPromptChanged(string prompt) => InteractionPromptChanged?.Invoke(prompt);
        public static void RaiseLevelStarted(int level, string name) => LevelStarted?.Invoke(level, name);
        public static void RaiseLevelLoadProgress(float progress) => LevelLoadProgress?.Invoke(progress);
        public static void RaiseLevelLoadFailed(string message) => LevelLoadFailed?.Invoke(message);
        public static void RaiseConditionsChanged(string title, string description) => ConditionsChanged?.Invoke(title, description);

        /// <summary>
        /// Removes every listener. Called when Play mode starts (the project has domain reload off,
        /// so static events would otherwise keep listeners from the last session) and by tests.
        /// </summary>
        public static void ResetAll()
        {
            PlayerHealthChanged = null;
            PlayerDied = null;
            EnemyDefeated = null;
            EnemyAttacked = null;
            ItemCollected = null;
            AbilityActivated = null;
            LevelCompleted = null;
            GameStateChanged = null;
            InventoryChanged = null;
            ObjectiveChanged = null;
            InteractionPromptChanged = null;
            LevelStarted = null;
            LevelLoadProgress = null;
            LevelLoadFailed = null;
            ConditionsChanged = null;
        }
    }
}
