using System;
using System.Collections.Generic;
using LostDungeon.Core.Abilities;
using LostDungeon.Core.AI;
using LostDungeon.Core.Combat;
using LostDungeon.Core.Events;
using LostDungeon.Core.Navigation;
using LostDungeon.Game.Abilities;
using LostDungeon.Game.Navigation;
using LostDungeon.Game.Player;
using LostDungeon.Game.Pooling;
using LostDungeon.Game.Visuals;
using LostDungeon.Game.Weather;
using LostDungeon.Game.World;
using LostDungeon.Game.Combat;
using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// Shared body for every enemy type. Each frame the EnemyData's behaviour picks a state,
    /// and the state machine runs it. The controller only offers basic actions (move, follow a path,
    /// attack, die) that states use, so it never needs to change when a new enemy type is added.
    /// Pooled: spawned and removed through PoolManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyController : MonoBehaviour, IDamageable, IPoolable
    {
        private const float RepathSeconds = 0.4f;
        private const float WaypointReachedDistance = 0.08f;

        [SerializeField] private EnemyData data;
        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private HitFlash hitFlash;

        private Rigidbody body;
        private Collider[] colliders;
        private Vector3 modelRestPosition;
        private Health health;
        private Cooldown attackCooldown;
        private StateMachine<EnemyController> stateMachine;
        private Dictionary<EnemyStateId, IState<EnemyController>> states;
        private Transform target;
        private IDamageable targetHealth;
        private List<GridPoint> path = new List<GridPoint>();
        private int pathIndex;
        private float repathTimer;
        private bool initialised;

        public event Action<EnemyController> Died;

        public EnemyData Data => data;
        public EnemyStateId CurrentState { get; private set; }
        public Vector2 Position => Ground.ToFloor(body.position);
        public Vector2 HomePosition { get; private set; }
        public bool HasTarget => target != null && targetHealth != null && !targetHealth.IsDead;
        public Vector2 TargetPosition => target != null ? Ground.ToFloor(target.position) : Position;
        public float DistanceToTarget => HasTarget ? Vector2.Distance(Position, TargetPosition) : float.PositiveInfinity;
        public float DistanceToHome => Vector2.Distance(Position, HomePosition);
        public bool AttackReady => attackCooldown != null && attackCooldown.IsReady;
        public bool IsDead => health == null || health.IsDead;

        /// <summary>For the HUD's name plate: 0 to 1.</summary>
        public float HealthFraction => health == null ? 0f : (float)health.Current / health.Max;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            colliders = GetComponents<Collider>();
            if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
            if (hitFlash == null) hitFlash = GetComponent<HitFlash>();
            if (characterAnimator != null && characterAnimator.Model != null)
                modelRestPosition = characterAnimator.Model.localPosition;

            stateMachine = new StateMachine<EnemyController>(this);
            states = new Dictionary<EnemyStateId, IState<EnemyController>>
            {
                [EnemyStateId.Idle] = new IdleState(),
                [EnemyStateId.Chase] = new ChaseState(),
                [EnemyStateId.Attack] = new AttackState(),
                [EnemyStateId.Retreat] = new RetreatState(),
                [EnemyStateId.ReturnHome] = new ReturnHomeState(),
                [EnemyStateId.Dead] = new DeadState(),
            };
        }

        // Placed straight into a scene (not spawned from a pool): set up on Start.
        private void Start()
        {
            if (!initialised)
                Initialise();
        }

        public void OnSpawned() => Initialise();

        public void OnDespawned()
        {
            initialised = false;
            stateMachine.ChangeState(null);
        }

        private void Initialise()
        {
            initialised = true;
            health = new Health(data.MaxHealth);
            attackCooldown = new Cooldown(data.Attack != null ? data.Attack.CooldownSeconds : 1f);
            HomePosition = Ground.ToFloor(transform.position);
            transform.localScale = Vector3.one * data.Scale;
            if (hitFlash != null)
                hitFlash.SetBaseTint(data.Tint);
            foreach (Collider c in colliders)
                c.enabled = true;
            body.isKinematic = false;
            if (characterAnimator != null)
            {
                characterAnimator.SetDead(false);
                characterAnimator.Model.localPosition = modelRestPosition;
                characterAnimator.FaceDirection(Vector2.down, instant: true);
            }

            PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
            target = player != null ? player.transform : null;
            targetHealth = player;

            ClearPath();
            CurrentState = EnemyStateId.Idle;
            stateMachine.ChangeState(states[EnemyStateId.Idle]);
        }

        private void Update()
        {
            if (!initialised)
                return;

            if (!IsDead)
            {
                attackCooldown.Tick(Time.deltaTime);
                SetState(data.Behaviour.Decide(this));
            }

            stateMachine.Tick(Time.deltaTime);
        }

        private void SetState(EnemyStateId next)
        {
            if (next == CurrentState)
                return;
            CurrentState = next;
            stateMachine.ChangeState(states[next]);
        }

        // ---------- Actions used by states ----------

        public void Stop()
        {
            if (!body.isKinematic)
                body.linearVelocity = Vector3.zero;
        }

        public void MoveTowards(Vector2 point, float speedMultiplier = 1f)
        {
            Vector2 offset = point - Position;
            Vector2 velocity = offset.sqrMagnitude < 0.0004f
                ? Vector2.zero
                : offset.normalized * (data.MoveSpeed * speedMultiplier * WorldModifiers.EnemySpeed);
            body.linearVelocity = Ground.ToWorld(velocity);
        }

        public void ClearPath()
        {
            path.Clear();
            pathIndex = 0;
            repathTimer = 0f;
        }

        /// <summary>Walks along an A* path to the destination, recalculating it a few times per second.</summary>
        public void FollowPathTo(Vector2 destination, float deltaTime)
        {
            LevelNavigationGrid grid = LevelNavigationGrid.Current;
            if (grid == null)
            {
                MoveTowards(destination); // no level grid: walk straight
                return;
            }

            repathTimer -= deltaTime;
            if (repathTimer <= 0f)
            {
                path = AStarPathfinder.FindPath(grid, grid.WorldToCell(Position), grid.WorldToCell(destination));
                pathIndex = 0;
                repathTimer = RepathSeconds;
            }

            while (pathIndex < path.Count &&
                   Vector2.Distance(Position, grid.CellToWorld(path[pathIndex])) < WaypointReachedDistance)
                pathIndex++;

            // Same cell as the destination (or no path): head straight for it.
            Vector2 next = pathIndex < path.Count ? grid.CellToWorld(path[pathIndex]) : destination;
            MoveTowards(next);
        }

        public void FaceTarget()
        {
            if (characterAnimator != null && HasTarget)
                characterAnimator.FaceDirection(TargetPosition - Position);
        }

        public void PerformAttack()
        {
            if (data.Attack == null || !HasTarget)
                return;

            data.Attack.Activate(new AbilityContext(gameObject, Position, TargetPosition - Position));
            attackCooldown.Start();
            if (characterAnimator != null)
                characterAnimator.PlayAction(data.Attack.AnimationTrigger);
            GameEvents.RaiseEnemyAttacked(data.Id);
        }

        /// <summary>After the death animation the body sinks into the floor (0 = standing, 1 = gone).</summary>
        public void SetSink(float amount)
        {
            if (characterAnimator != null && characterAnimator.Model != null)
                characterAnimator.Model.localPosition = modelRestPosition + Vector3.down * (Mathf.Clamp01(amount) * 1.2f);
        }

        /// <summary>Back to the pool (or destroyed if it was placed by hand).</summary>
        public void Remove() => PoolManager.Instance.Despawn(gameObject);

        // ---------- Damage ----------

        public void TakeDamage(int amount)
        {
            if (IsDead)
                return;

            health.TakeDamage(DamageCalculator.ApplyArmor(amount, data.Armor));
            if (hitFlash != null)
                hitFlash.Flash();

            if (health.IsDead)
                Die();
            else if (characterAnimator != null && CurrentState != EnemyStateId.Attack)
                characterAnimator.PlayHit();
        }

        private void Die()
        {
            foreach (Collider c in colliders)
                c.enabled = false;
            Stop();
            body.isKinematic = true; // the body stays where it fell
            if (characterAnimator != null)
                characterAnimator.SetDead(true);
            SetState(EnemyStateId.Dead);
            GameEvents.RaiseEnemyDefeated(data.Id);
            Died?.Invoke(this);
        }
    }
}
