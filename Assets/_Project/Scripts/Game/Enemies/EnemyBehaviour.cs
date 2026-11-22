using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>Base for behaviour assets, so a behaviour can be picked in the Inspector.</summary>
    public abstract class EnemyBehaviour : ScriptableObject, IEnemyBehaviour
    {
        public abstract EnemyStateId Decide(EnemyController enemy);

        /// <summary>
        /// Notices the player within detection range, and only gives up at a longer range,
        /// so enemies don't flicker between chasing and idling at the edge.
        /// </summary>
        protected static bool IsEngaged(EnemyController enemy)
        {
            if (!enemy.HasTarget)
                return false;
            float range = enemy.CurrentState == EnemyStateId.Idle
                ? enemy.Data.DetectionRange
                : enemy.Data.DetectionRange * 1.5f;
            return enemy.DistanceToTarget <= range;
        }
    }
}
