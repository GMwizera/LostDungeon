using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>Archer: keeps its distance, backs off if the player gets close, shoots from range.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Enemies/Ranged Behaviour")]
    public class RangedEnemyBehaviour : EnemyBehaviour
    {
        public override EnemyStateId Decide(EnemyController enemy)
        {
            if (!IsEngaged(enemy))
                return EnemyStateId.Idle;
            if (enemy.DistanceToTarget < enemy.Data.PreferredDistance * 0.6f)
                return EnemyStateId.Retreat;
            return enemy.DistanceToTarget <= enemy.Data.AttackRange ? EnemyStateId.Attack : EnemyStateId.Chase;
        }
    }
}
