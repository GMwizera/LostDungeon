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
using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// Shared body for every enemy type. Each frame the EnemyData's behaviour picks a state,
    /// and the state machine runs it. The controller only offers basic actions (move, follow a path,
    /// attack, die) that states use, so it never needs to change when a new enemy type is added.
    /// Pooled: spawned and removed through PoolManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour, IDamageable, IPoolable
    {
        private const float RepathSeconds = 0.4f;
        private const float WaypointReachedDistance = 0.08f;

        [SerializeField] private EnemyData data;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private DirectionalSpriteAnimator spriteAnimator;
        [SerializeField] private HitFlash hitFlash;

        private Rigidbody2D body;
        private Collider2D[] colliders;
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
        public Vector2 Position => body.position;
        public Vector2 HomePosition { get; private set; }
        public bool HasTarget => target != null && targetHealth != null && !targetHealth.IsDead;
        public Vector2 TargetPosition => target != null ? (Vector2)target.position : Position;
        public float DistanceToTarget => HasTarget ? Vector2.Distance(Position, TargetPosition) : float.PositiveInfinity;
        public float DistanceToHome => Vector2.Distance(Position, HomePosition);
        public bool AttackReady => attackCooldown != null && attackCooldown.IsReady;
        public bool IsDead => health == null || health.IsDead;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            colliders = GetComponents<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteAnimator == null) spriteAnimator = GetComponentInChildren<DirectionalSpriteAnimator>();
            if (hitFlash == null) hitFlash = GetComponent<HitFlash>();

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
            HomePosition = transform.position;
            transform.localScale = Vector3.one * data.Scale;
            spriteRenderer.color = data.Tint;
            foreach (Collider2D c in colliders)
                c.enabled = true;

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

        public void Stop() => body.linearVelocity = Vector2.zero;

        public void MoveTowards(Vector2 point, float speedMultiplier = 1f)
        {
            Vector2 offset = point - Position;
            body.linearVelocity = offset.sqrMagnitude < 0.0004f
                ? Vector2.zero
                : offset.normalized * (data.MoveSpeed * speedMultiplier);
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
            TilemapNavigationGrid grid = TilemapNavigationGrid.Current;
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
            if (spriteAnimator != null && HasTarget)
                spriteAnimator.FaceDirection(TargetPosition - Position);
        }

        public void PerformAttack()
        {
            if (data.Attack == null || !HasTarget)
                return;

            data.Attack.Activate(new AbilityContext(gameObject, Position, TargetPosition - Position));
            attackCooldown.Start();
            if (spriteAnimator != null)
                spriteAnimator.PlayAction();
            GameEvents.RaiseEnemyAttacked(data.Id);
        }

        public void SetFade(float alpha)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
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
        }

        private void Die()
        {
            foreach (Collider2D c in colliders)
                c.enabled = false;
            SetState(EnemyStateId.Dead);
            GameEvents.RaiseEnemyDefeated(data.Id);
            Died?.Invoke(this);
        }
    }
}
