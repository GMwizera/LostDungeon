using LostDungeon.Core.Combat;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class DamageCalculatorTests
    {
        [Test]
        public void Roll_BelowCritChance_IsCritical()
        {
            DamageResult result = DamageCalculator.Roll(baseDamage: 3, critChance: 0.2f, critMultiplier: 2f, roll: 0.1);

            Assert.IsTrue(result.IsCritical);
            Assert.AreEqual(6, result.Amount);
        }

        [Test]
        public void Roll_AboveCritChance_IsNormalHit()
        {
            DamageResult result = DamageCalculator.Roll(3, 0.2f, 2f, roll: 0.9);

            Assert.IsFalse(result.IsCritical);
            Assert.AreEqual(3, result.Amount);
        }

        [Test]
        public void Roll_ZeroCritChance_NeverCrits()
        {
            DamageResult result = DamageCalculator.Roll(3, 0f, 2f, roll: 0.0);

            Assert.IsFalse(result.IsCritical);
        }

        [TestCase(5, 2, 3)]
        [TestCase(2, 5, 1)]  // armour can't block a hit completely
        [TestCase(4, -3, 4)] // negative armour is treated as zero
        [TestCase(0, 0, 0)]  // no damage stays no damage
        public void ApplyArmor_ReducesDamage(int incoming, int armor, int expected)
        {
            Assert.AreEqual(expected, DamageCalculator.ApplyArmor(incoming, armor));
        }
    }
}
