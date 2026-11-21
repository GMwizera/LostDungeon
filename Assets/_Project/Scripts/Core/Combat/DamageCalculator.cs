using System;

namespace LostDungeon.Core.Combat
{
    public readonly struct DamageResult
    {
        public int Amount { get; }
        public bool IsCritical { get; }

        public DamageResult(int amount, bool isCritical)
        {
            Amount = amount;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// Damage rules in one place. The random roll is passed in instead of generated here,
    /// so tests can force a critical hit or a normal hit.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>Damage an attacker deals before the target's armour.</summary>
        /// <param name="roll">A random number from 0 (inclusive) to 1 (exclusive).</param>
        public static DamageResult Roll(int baseDamage, float critChance, float critMultiplier, double roll)
        {
            if (baseDamage <= 0)
                return new DamageResult(0, false);

            bool critical = roll < critChance;
            int amount = critical ? (int)Math.Round(baseDamage * critMultiplier) : baseDamage;
            return new DamageResult(amount, critical);
        }

        /// <summary>Damage after armour. Any hit that lands does at least 1 damage.</summary>
        public static int ApplyArmor(int incoming, int armor)
        {
            if (incoming <= 0)
                return 0;
            return Math.Max(1, incoming - Math.Max(0, armor));
        }
    }
}
