using LostDungeon.Core.Events;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Inventory;
using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>
    /// The level goal: beat all enemies, find the key if needed, then leave.
    /// It sends its text out with an event and opens the door when done.
    /// </summary>
    public class LevelObjective : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private bool requiresKey;

        private int defeated;
        private bool hasKey;

        public bool RequiresKey => requiresKey;
        public bool EnemiesCleared => spawner == null || defeated >= spawner.TotalCount;
        public bool ExitUnlocked => EnemiesCleared && (!requiresKey || hasKey);

        private void OnEnable()
        {
            GameEvents.EnemyDefeated += OnEnemyDefeated;
            GameEvents.InventoryChanged += OnInventoryChanged;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDefeated -= OnEnemyDefeated;
            GameEvents.InventoryChanged -= OnInventoryChanged;
        }

        private void Start() => Publish();

        public void Publish()
        {
            string text;
            if (!EnemiesCleared)
                text = $"Defeat the enemies ({defeated}/{spawner.TotalCount})";
            else if (requiresKey && !hasKey)
                text = "Find the key in the chest";
            else
                text = "Reach the exit door";
            GameEvents.RaiseObjectiveChanged(text);
            GameEvents.RaiseObjectiveStatusChanged(defeated, spawner != null ? spawner.TotalCount : 0, requiresKey, hasKey, ExitUnlocked);
        }

        private void OnEnemyDefeated(string enemyId)
        {
            defeated++;
            Publish();
        }

        private void OnInventoryChanged(string itemId, int count)
        {
            if (itemId != ItemIds.Key)
                return;
            hasKey = count > 0;
            Publish();
        }
    }
}
