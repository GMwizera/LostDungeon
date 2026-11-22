using LostDungeon.Core.AI;
using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    // Each state is a small class with one job (State pattern). Every enemy gets its own instances,
    // so a state can keep its own timers.

    public class IdleState : IState<EnemyController>
    {
        public void Enter(EnemyController enemy) => enemy.Stop();
        public void Tick(EnemyController enemy, float deltaTime) => enemy.Stop();
        public void Exit(EnemyController enemy) { }
    }

    public class ChaseState : IState<EnemyController>
    {
        public void Enter(EnemyController enemy) => enemy.ClearPath();
        public void Tick(EnemyController enemy, float deltaTime) => enemy.FollowPathTo(enemy.TargetPosition, deltaTime);
        public void Exit(EnemyController enemy) => enemy.Stop();
    }

    public class AttackState : IState<EnemyController>
    {
        public void Enter(EnemyController enemy) => enemy.Stop();

        public void Tick(EnemyController enemy, float deltaTime)
        {
            enemy.Stop();
            enemy.FaceTarget();
            if (enemy.AttackReady)
                enemy.PerformAttack();
        }

        public void Exit(EnemyController enemy) { }
    }

    public class RetreatState : IState<EnemyController>
    {
        public void Enter(EnemyController enemy) { }

        public void Tick(EnemyController enemy, float deltaTime)
        {
            Vector2 away = enemy.Position - enemy.TargetPosition;
            enemy.MoveTowards(enemy.Position + away.normalized, 0.8f);
        }

        public void Exit(EnemyController enemy) => enemy.Stop();
    }

    public class ReturnHomeState : IState<EnemyController>
    {
        public void Enter(EnemyController enemy) => enemy.ClearPath();
        public void Tick(EnemyController enemy, float deltaTime) => enemy.FollowPathTo(enemy.HomePosition, deltaTime);
        public void Exit(EnemyController enemy) => enemy.Stop();
    }

    public class DeadState : IState<EnemyController>
    {
        private const float FadeSeconds = 0.6f;
        private float elapsed;

        public void Enter(EnemyController enemy)
        {
            elapsed = 0f;
            enemy.Stop();
        }

        public void Tick(EnemyController enemy, float deltaTime)
        {
            elapsed += deltaTime;
            enemy.SetFade(1f - elapsed / FadeSeconds);
            if (elapsed >= FadeSeconds)
                enemy.Remove();
        }

        public void Exit(EnemyController enemy) { }
    }
}
