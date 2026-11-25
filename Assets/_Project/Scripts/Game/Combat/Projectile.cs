using LostDungeon.Core.Combat;
using LostDungeon.Game.Visuals;
using LostDungeon.Game.Pooling;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Combat
{
    /// <summary>
    /// A pooled projectile (fireball, arrow). Flies straight across the floor at chest height,
    /// damages the first IDamageable it touches (never its owner or the owner's allies),
    /// and goes back to the pool on hit or when its lifetime runs out.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private PooledEffect impactEffect;

        private Rigidbody body;
        private GameObject owner;
        private int damage;
        private float despawnTime;
        private bool active;
        private TrailRenderer[] trails;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        public void Launch(GameObject shooter, Vector2 direction, float speed, int hitDamage)
        {
            owner = shooter;
            damage = hitDamage;
            transform.rotation = Ground.Facing(direction);
            body.linearVelocity = Ground.ToWorld(direction.normalized * speed);
            despawnTime = Time.time + lifetime;
        }

        public void OnSpawned()
        {
            active = true;
            foreach (TrailRenderer trail in trails)
                trail.Clear(); // don't draw a streak from where it was last used
        }

        public void OnDespawned()
        {
            active = false;
            owner = null;
            body.linearVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (active && Time.time >= despawnTime)
                ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!active || other.isTrigger)
                return; // ignore traps, pickups and other projectiles

            GameObject hit = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (hit == owner || Faction.AreAllies(hit, owner))
                return; // fly past the shooter and its allies

            if (hit.TryGetComponent(out IDamageable target) && !target.IsDead)
            {
                target.TakeDamage(damage);
                CombatFeedback.RaiseHit(hit, damage, false);
            }

            if (impactEffect != null)
                PoolManager.Instance.Spawn(impactEffect, transform.position + Vector3.up * Ground.BodyHeight, Quaternion.identity);
            ReturnToPool(); // hit something solid: a wall, a prop or a character
        }

        private void ReturnToPool()
        {
            active = false; // guards against a hit and a timeout in the same frame
            PoolManager.Instance.Despawn(gameObject);
        }
    }
}
