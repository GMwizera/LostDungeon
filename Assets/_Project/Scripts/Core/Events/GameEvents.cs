using System;

namespace LostDungeon.Core.Events
{
    /// <summary>
    /// All game events in one place (Observer pattern).
    /// Gameplay sends events and UI, sound and saving listen. They never know about each other.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>(current, max)</summary>
        public static event Action<int, int> PlayerHealthChanged;
        public static event Action PlayerDied;

        /// <summary>Enemy id, e.g. "Warrior".</summary>
        public static event Action<string> EnemyDefeated;

        /// <summary>Id of the enemy that just attacked, for sounds.</summary>
        public static event Action<string> EnemyAttacked;

        /// <summary>(item id, amount)</summary>
        public static event Action<string, int> ItemCollected;

        /// <summary>(ability name, cooldown seconds)</summary>
        public static event Action<string, float> AbilityActivated;

        /// <summary>Level number, from 1.</summary>
        public static event Action<int> LevelCompleted;

        public static event Action<GameState> GameStateChanged;

        /// <summary>(item id, new count) when the inventory changes.</summary>
        public static event Action<string, int> InventoryChanged;

        /// <summary>The current goal text.</summary>
        public static event Action<string> ObjectiveChanged;

        /// <summary>(defeated, total enemies, key needed, key held, exit open) for the HUD.</summary>
        public static event Action<int, int, bool, bool, bool> ObjectiveStatusChanged;

        /// <summary>Hint for the closest thing you can use, or null.</summary>
        public static event Action<string> InteractionPromptChanged;

        /// <summary>(level number, level name) when a level is loaded.</summary>
        public static event Action<int, string> LevelStarted;

        /// <summary>Loading progress from 0 to 1.</summary>
        public static event Action<float> LevelLoadProgress;

        /// <summary>Error text when a level fails to load.</summary>
        public static event Action<string> LevelLoadFailed;

        /// <summary>(title, description) of today's weather effects.</summary>
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
        public static void RaiseObjectiveStatusChanged(int defeated, int total, bool requiresKey, bool hasKey, bool exitOpen) =>
            ObjectiveStatusChanged?.Invoke(defeated, total, requiresKey, hasKey, exitOpen);
        public static void RaiseInteractionPromptChanged(string prompt) => InteractionPromptChanged?.Invoke(prompt);
        public static void RaiseLevelStarted(int level, string name) => LevelStarted?.Invoke(level, name);
        public static void RaiseLevelLoadProgress(float progress) => LevelLoadProgress?.Invoke(progress);
        public static void RaiseLevelLoadFailed(string message) => LevelLoadFailed?.Invoke(message);
        public static void RaiseConditionsChanged(string title, string description) => ConditionsChanged?.Invoke(title, description);

        /// <summary>
        /// Removes all listeners. Runs when Play starts and in tests.
        /// Domain reload is off, so old listeners would stay around otherwise.
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
            ObjectiveStatusChanged = null;
            LevelStarted = null;
            LevelLoadProgress = null;
            LevelLoadFailed = null;
            ConditionsChanged = null;
        }
    }
}
