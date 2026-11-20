using LostDungeon.Core.Combat;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Unity wrapper around the plain C# Health class.
    /// Adds a short invulnerability window so one hit can't drain all health at once.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 6;
        [SerializeField] private float invulnerableSeconds = 0.5f;

        private float invulnerableUntil;

        /// <summary>The HUD will subscribe to Health.Changed (Day 5).</summary>
        public Health Health { get; private set; }

        public bool IsDead => Health.IsDead;

        private void Awake()
        {
            Health = new Health(maxHealth);
            Health.Died += OnDied;
        }

        private void OnDestroy()
        {
            Health.Died -= OnDied;
        }

        public void TakeDamage(int amount)
        {
            if (Time.time < invulnerableUntil)
                return;

            Health.TakeDamage(amount);
            invulnerableUntil = Time.time + invulnerableSeconds;
            Debug.Log($"Player health: {Health.Current}/{Health.Max}");
        }

        private void OnDied()
        {
            // Stop the player. Game over screen comes on Day 5.
            GetComponent<PlayerMovement>().enabled = false;
            GetComponent<PlayerInputReader>().enabled = false;
            Debug.Log("Player died");
        }
    }
}
