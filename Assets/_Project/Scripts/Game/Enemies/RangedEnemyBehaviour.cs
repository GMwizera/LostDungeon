using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// Archer: stays back and shoots from far away.
    /// If it is stuck or the player is too close, it just shoots.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Enemies/Ranged Behaviour")]
    public class RangedEnemyBehaviour : EnemyBehaviour
    {
        private const float PointBlank = 1.1f;

        public override EnemyStateId Decide(EnemyController enemy)
        {
            if (!IsEngaged(enemy))
                return EnemyStateId.Idle;

            float distance = enemy.DistanceToTarget;
            bool tooClose = distance < enemy.Data.PreferredDistance * 0.6f;
            if (tooClose && distance > PointBlank && !enemy.IsStuck)
                return EnemyStateId.Retreat;
            return distance <= enemy.Data.AttackRange ? EnemyStateId.Attack : EnemyStateId.Chase;
        }
    }
}
