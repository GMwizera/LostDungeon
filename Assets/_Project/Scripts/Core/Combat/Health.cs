using System;

namespace LostDungeon.Core.Combat
{
    /// <summary>
    /// Plain C# health model shared by the player and enemies.
    /// It has no Unity code, so it can be unit tested directly.
    /// </summary>
    public class Health : IDamageable
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>Raised with (current, max) whenever health changes.</summary>
        public event Action<int, int> Changed;

        /// <summary>Raised once, the moment health reaches zero.</summary>
        public event Action Died;

        public Health(int max)
        {
            if (max <= 0)
                throw new ArgumentException("Max health must be greater than zero.", nameof(max));

            Max = max;
            Current = max;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
                return;

            Current = Math.Max(0, Current - amount);
            Changed?.Invoke(Current, Max);

            if (IsDead)
                Died?.Invoke();
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
                return;

            Current = Math.Min(Max, Current + amount);
            Changed?.Invoke(Current, Max);
        }
    }
}
