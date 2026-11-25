using LostDungeon.Core.Combat;
using LostDungeon.Core.Events;
using LostDungeon.Game.Platform;
using LostDungeon.Game.Visuals;
using UnityEngine;

namespace LostDungeon.Game.Player
{
    /// <summary>
    /// The player's health. After a hit the player is safe for a moment.
    /// Changes go out as events, so the HUD and sound don't need the player.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 6;
        [SerializeField] private float invulnerableSeconds = 0.5f;
        [SerializeField] private HitFlash hitFlash;
        [SerializeField] private CharacterAnimator characterAnimator;

        private float invulnerableUntil;
        private int bonusHealth;

        public Health Health { get; private set; }

        public bool IsDead => Health.IsDead;

        private void Awake()
        {
            Health = new Health(maxHealth + bonusHealth);
            Health.Changed += OnHealthChanged;
            Health.Died += OnDied;
            if (hitFlash == null)
                hitFlash = GetComponent<HitFlash>();
            if (characterAnimator == null)
                characterAnimator = GetComponent<CharacterAnimator>();
        }

        private void Start()
        {
            // So the HUD shows full health at the start.
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
            if (characterAnimator != null && !IsDead)
                characterAnimator.PlayHit();
            PlatformFeatures.Vibrate();
        }

        public void Heal(int amount) => Health.Heal(amount);

        /// <summary>Bonus health from rewards. Call before the player is turned on.</summary>
        public void ConfigureMaxHealth(int bonus) => bonusHealth = Mathf.Max(0, bonus);

        private void OnHealthChanged(int current, int max)
        {
            Debug.Log($"Player health: {current}/{max}");
            GameEvents.RaisePlayerHealthChanged(current, max);
        }

        private void OnDied()
        {
            // Stop and fall. The game over screen hears the event.
            if (characterAnimator != null)
                characterAnimator.SetDead(true);
            GetComponent<PlayerMovement>().enabled = false;
            GetComponent<PlayerInputReader>().enabled = false;
            Debug.Log("Player died");
            GameEvents.RaisePlayerDied();
        }
    }
}
