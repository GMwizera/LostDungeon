using LostDungeon.Game.Weather;
using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>Base for enemy brains, so I can pick one in the Inspector.</summary>
    public abstract class EnemyBehaviour : ScriptableObject, IEnemyBehaviour
    {
        public abstract EnemyStateId Decide(EnemyController enemy);

        /// <summary>
        /// Sees the player up close but only gives up further away.
        /// This stops enemies flicking between chase and idle.
        /// </summary>
        protected static bool IsEngaged(EnemyController enemy)
        {
            if (!enemy.HasTarget)
                return false;
            float range = enemy.CurrentState == EnemyStateId.Idle
                ? enemy.Data.DetectionRange
                : enemy.Data.DetectionRange * 1.5f;
            range *= WorldModifiers.EnemyDetection; // shorter at night, from the weather API
            return enemy.DistanceToTarget <= range;
        }
    }
}
