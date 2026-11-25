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

    /// <summary>All damage rules. The random roll is passed in so tests can pick it.</summary>
    public static class DamageCalculator
    {
        /// <summary>Damage before armour.</summary>
        /// <param name="roll">Random number from 0 up to (not including) 1.</param>
        public static DamageResult Roll(int baseDamage, float critChance, float critMultiplier, double roll)
        {
            if (baseDamage <= 0)
                return new DamageResult(0, false);

            bool critical = roll < critChance;
            int amount = critical ? (int)Math.Round(baseDamage * critMultiplier) : baseDamage;
            return new DamageResult(amount, critical);
        }

        /// <summary>Damage after armour. A hit always does at least 1.</summary>
        public static int ApplyArmor(int incoming, int armor)
        {
            if (incoming <= 0)
                return 0;
            return Math.Max(1, incoming - Math.Max(0, armor));
        }
    }
}
