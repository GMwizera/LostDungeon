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
    /// The body every enemy shares. Each frame the brain picks a state and the state machine runs it.
    /// New enemy types don't need changes here. Enemies come from the pool.
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
        private Vector2 wantedVelocity;
        private float windUpLeft = -1f; // below zero means no attack coming
        private float stuckTime;
        private float retargetTimer;

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

        /// <summary>True when a wall keeps the enemy from moving, like an archer in a corner.</summary>
        public bool IsStuck => stuckTime > 0.35f;

        /// <summary>Health from 0 to 1, for the bar over its head.</summary>
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

        // For enemies put in the scene by hand, not from the pool.
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

            FindTarget();

            ClearPath();
            windUpLeft = -1f;
            stuckTime = 0f;
            wantedVelocity = Vector2.zero;
            CurrentState = EnemyStateId.Idle;
            stateMachine.ChangeState(states[EnemyStateId.Idle]);
        }

        /// <summary>
        /// Finds the player. Enemies spawn before the player is placed, so we keep looking.
        /// </summary>
        private void FindTarget()
        {
            PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
            target = player != null ? player.transform : null;
            targetHealth = player;
        }

        private void Update()
        {
            if (!initialised)
                return;

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f)
                {
                    retargetTimer = 0.25f;
                    FindTarget();
                }
            }

            if (!IsDead)
            {
                attackCooldown.Tick(Time.deltaTime);
                if (windUpLeft >= 0f)
                {
                    windUpLeft -= Time.deltaTime;
                    if (windUpLeft < 0f)
                        ReleaseAttack();
                }
                SetState(data.Behaviour.Decide(this));
            }

            stateMachine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!initialised || IsDead)
                return;
            bool trying = wantedVelocity.sqrMagnitude > 0.25f;
            bool held = Ground.ToFloor(body.linearVelocity).sqrMagnitude < wantedVelocity.sqrMagnitude * 0.1f;
            stuckTime = trying && held ? stuckTime + Time.fixedDeltaTime : Mathf.Max(0f, stuckTime - Time.fixedDeltaTime * 2f);
        }

        private void SetState(EnemyStateId next)
        {
            if (next == CurrentState)
                return;
            CurrentState = next;
            stateMachine.ChangeState(states[next]);
        }

        // ---------- Actions the states use ----------

        public void Stop()
        {
            wantedVelocity = Vector2.zero;
            if (!body.isKinematic)
                body.linearVelocity = Vector3.zero;
        }

        public void MoveTowards(Vector2 point, float speedMultiplier = 1f)
        {
            Vector2 offset = point - Position;
            Vector2 velocity = offset.sqrMagnitude < 0.0004f
                ? Vector2.zero
                : offset.normalized * (data.MoveSpeed * speedMultiplier * WorldModifiers.EnemySpeed);
            wantedVelocity = velocity;
            body.linearVelocity = Ground.ToWorld(velocity);
        }

        public void ClearPath()
        {
            path.Clear();
            pathIndex = 0;
            repathTimer = 0f;
        }

        /// <summary>Walks an A* path to a spot, redoing the path often.</summary>
        public void FollowPathTo(Vector2 destination, float deltaTime)
        {
            LevelNavigationGrid grid = LevelNavigationGrid.Current;
            if (grid == null)
            {
                MoveTowards(destination); // no grid, so walk straight
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

            // Already close or no path, so go straight there.
            Vector2 next = pathIndex < path.Count ? grid.CellToWorld(path[pathIndex]) : destination;
            MoveTowards(next);
        }

        public void FaceTarget()
        {
            if (characterAnimator != null && HasTarget)
                characterAnimator.FaceDirection(TargetPosition - Position);
        }

        /// <summary>
        /// Starts a swing. The hit lands a moment later, so the player can dash away.
        /// </summary>
        public void PerformAttack()
        {
            if (data.Attack == null || !HasTarget || windUpLeft >= 0f)
                return;

            attackCooldown.Start();
            if (characterAnimator != null)
                characterAnimator.PlayAction(data.Attack.AnimationTrigger);
            windUpLeft = data.WindUpSeconds;
            if (windUpLeft <= 0f)
                ReleaseAttack();
        }

        private void ReleaseAttack()
        {
            windUpLeft = -1f;
            if (IsDead || !HasTarget)
                return;
            data.Attack.Activate(new AbilityContext(gameObject, Position, TargetPosition - Position));
            GameEvents.RaiseEnemyAttacked(data.Id);
        }

        /// <summary>Sinks the dead body into the floor, from 0 to 1.</summary>
        public void SetSink(float amount)
        {
            if (characterAnimator != null && characterAnimator.Model != null)
                characterAnimator.Model.localPosition = modelRestPosition + Vector3.down * (Mathf.Clamp01(amount) * 1.2f);
        }

        /// <summary>Sends it back to the pool.</summary>
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
            windUpLeft = -1f; // dead enemies don't finish a swing
            foreach (Collider c in colliders)
                c.enabled = false;
            Stop();
            body.isKinematic = true; // stay where it fell
            if (characterAnimator != null)
                characterAnimator.SetDead(true);
            SetState(EnemyStateId.Dead);
            GameEvents.RaiseEnemyDefeated(data.Id);
            Died?.Invoke(this);
        }
    }
}
