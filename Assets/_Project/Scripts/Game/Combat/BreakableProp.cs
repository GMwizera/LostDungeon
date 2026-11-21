using LostDungeon.Core.Combat;
using LostDungeon.Game.Visuals;
using UnityEngine;

namespace LostDungeon.Game.Combat
{
    /// <summary>
    /// A barrel or crate that breaks after a few hits. Implements IDamageable,
    /// so the sword, fireballs and traps can all hurt it without knowing what it is.
    /// </summary>
    public class BreakableProp : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private HitFlash hitFlash;

        private Health health;

        public bool IsDead => health.IsDead;

        private void Awake()
        {
            health = new Health(maxHealth);
            health.Died += OnBroken;
            if (hitFlash == null)
                hitFlash = GetComponent<HitFlash>();
        }

        private void OnDestroy() => health.Died -= OnBroken;

        public void TakeDamage(int amount)
        {
            health.TakeDamage(amount);
            if (hitFlash != null)
                hitFlash.Flash();
        }

        private void OnBroken()
        {
            // Day 4: drop a collectible here.
            gameObject.SetActive(false);
        }
    }
}
