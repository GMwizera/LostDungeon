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

        /// <summary>(item id, amount)</summary>
        public static event Action<string, int> ItemCollected;

        /// <summary>(ability name, cooldown seconds)</summary>
        public static event Action<string, float> AbilityActivated;

        /// <summary>Level number, starting at 1.</summary>
        public static event Action<int> LevelCompleted;

        public static event Action<GameState> GameStateChanged;

        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseEnemyDefeated(string enemyId) => EnemyDefeated?.Invoke(enemyId);
        public static void RaiseItemCollected(string itemId, int amount) => ItemCollected?.Invoke(itemId, amount);
        public static void RaiseAbilityActivated(string abilityName, float cooldown) => AbilityActivated?.Invoke(abilityName, cooldown);
        public static void RaiseLevelCompleted(int level) => LevelCompleted?.Invoke(level);
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);

        /// <summary>
        /// Removes every listener. Called when Play mode starts (the project has domain reload off,
        /// so static events would otherwise keep listeners from the last session) and by tests.
        /// </summary>
        public static void ResetAll()
        {
            PlayerHealthChanged = null;
            PlayerDied = null;
            EnemyDefeated = null;
            ItemCollected = null;
            AbilityActivated = null;
            LevelCompleted = null;
            GameStateChanged = null;
        }
    }
}
