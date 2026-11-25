using LostDungeon.Core.Combat;
using LostDungeon.Game.Visuals;
using LostDungeon.Game.Pooling;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Combat
{
    /// <summary>A pooled fireball or arrow. It flies straight and hurts the first thing it hits.</summary>
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
                trail.Clear(); // no old trail from its last use
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
                return; // skip traps, pickups and other shots

            GameObject hit = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (hit == owner || Faction.AreAllies(hit, owner))
                return; // don't hit the shooter or its team

            if (hit.TryGetComponent(out IDamageable target) && !target.IsDead)
            {
                target.TakeDamage(damage);
                CombatFeedback.RaiseHit(hit, damage, false);
            }

            if (impactEffect != null)
                PoolManager.Instance.Spawn(impactEffect, transform.position + Vector3.up * Ground.BodyHeight, Quaternion.identity);
            ReturnToPool(); // hit a wall, a prop or a character
        }

        private void ReturnToPool()
        {
            active = false; // so a hit and a timeout can't both happen
            PoolManager.Instance.Despawn(gameObject);
        }
    }
}
