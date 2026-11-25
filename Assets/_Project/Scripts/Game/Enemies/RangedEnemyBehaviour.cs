using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// Archer: keeps its distance, backs off if the player gets close, shoots from range.
    /// If the player is right on top of it, or it is backed into a wall and cannot retreat,
    /// it stands and fires point-blank instead of running in place.
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
