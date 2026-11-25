using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>Warrior: runs at the player and hits close up.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Enemies/Melee Behaviour")]
    public class MeleeEnemyBehaviour : EnemyBehaviour
    {
        public override EnemyStateId Decide(EnemyController enemy)
        {
            if (!IsEngaged(enemy))
                return EnemyStateId.Idle;
            return enemy.DistanceToTarget <= enemy.Data.AttackRange ? EnemyStateId.Attack : EnemyStateId.Chase;
        }
    }
}
