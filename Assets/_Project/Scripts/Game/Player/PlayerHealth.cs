using LostDungeon.Core.Combat;
using LostDungeon.Core.Events;
using LostDungeon.Game.Visuals;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// Unity wrapper around the plain C# Health class.
    /// Adds a short invulnerability window so one hit can't drain all health at once,
    /// and forwards health changes to GameEvents so the HUD and audio can react without knowing the player.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 6;
        [SerializeField] private float invulnerableSeconds = 0.5f;
        [SerializeField] private HitFlash hitFlash;

        private float invulnerableUntil;

        public Health Health { get; private set; }

        public bool IsDead => Health.IsDead;

        private void Awake()
        {
            Health = new Health(maxHealth);
            Health.Changed += OnHealthChanged;
            Health.Died += OnDied;
            if (hitFlash == null)
                hitFlash = GetComponent<HitFlash>();
        }

        private void Start()
        {
            // Let listeners (the HUD) show full health at the start.
            GameEvents.RaisePlayerHealthChanged(Health.Current, Health.Max);
        }

        private void OnDestroy()
        {
            Health.Changed -= OnHealthChanged;
            Health.Died -= OnDied;
        }

        public void TakeDamage(int amount)
        {
            if (Time.time < invulnerableUntil || IsDead)
                return;

            Health.TakeDamage(amount);
            invulnerableUntil = Time.time + invulnerableSeconds;
            if (hitFlash != null)
                hitFlash.Flash();
        }

        public void Heal(int amount) => Health.Heal(amount);

        private void OnHealthChanged(int current, int max)
        {
            Debug.Log($"Player health: {current}/{max}");
            GameEvents.RaisePlayerHealthChanged(current, max);
        }

        private void OnDied()
        {
            // Stop the player. The game over screen will listen to PlayerDied (Day 5).
            GetComponent<PlayerMovement>().enabled = false;
            GetComponent<PlayerInputReader>().enabled = false;
            Debug.Log("Player died");
            GameEvents.RaisePlayerDied();
        }
    }
}
