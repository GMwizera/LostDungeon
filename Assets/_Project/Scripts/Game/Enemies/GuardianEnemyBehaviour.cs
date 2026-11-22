using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>Guardian: defends a spot. Only fights players inside its guard radius, then walks back.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Enemies/Guardian Behaviour")]
    public class GuardianEnemyBehaviour : EnemyBehaviour
    {
        public override EnemyStateId Decide(EnemyController enemy)
        {
            bool intruder = enemy.HasTarget &&
                Vector2.Distance(enemy.TargetPosition, enemy.HomePosition) <= enemy.Data.GuardRadius;

            if (intruder)
                return enemy.DistanceToTarget <= enemy.Data.AttackRange ? EnemyStateId.Attack : EnemyStateId.Chase;

            return enemy.DistanceToHome > 0.15f ? EnemyStateId.ReturnHome : EnemyStateId.Idle;
        }
    }
}
