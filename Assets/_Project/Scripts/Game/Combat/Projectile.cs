using LostDungeon.Core.Combat;
using LostDungeon.Game.Pooling;
using UnityEngine;

namespace LostDungeon.Game.Combat
{
    /// <summary>
    /// A pooled projectile. Flies in a straight line, damages the first IDamageable it touches
    /// (never its owner), and goes back to the pool on hit or when its lifetime runs out.
    /// The player's fireball uses it now; the Archer enemy will reuse it for arrows.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 2f;

        private Rigidbody2D body;
        private GameObject owner;
        private int damage;
        private float despawnTime;
        private bool active;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        public void Launch(GameObject shooter, Vector2 direction, float speed, int hitDamage)
        {
            owner = shooter;
            damage = hitDamage;
            body.linearVelocity = direction.normalized * speed;
            despawnTime = Time.time + lifetime;
        }

        public void OnSpawned() => active = true;

        public void OnDespawned()
        {
            active = false;
            owner = null;
            body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            if (active && Time.time >= despawnTime)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!active || other.isTrigger)
                return; // ignore traps, pickups and other projectiles

            GameObject hit = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (hit == owner)
                return;

            if (hit.TryGetComponent(out IDamageable target) && !target.IsDead)
                target.TakeDamage(damage);

            ReturnToPool(); // hit something solid: a wall, a prop or a character
        }

        private void ReturnToPool()
        {
            active = false; // guards against a hit and a timeout in the same frame
            PoolManager.Instance.Despawn(gameObject);
        }
    }
}
