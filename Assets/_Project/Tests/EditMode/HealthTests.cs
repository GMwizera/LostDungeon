using System;
using LostDungeon.Core.Combat;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class HealthTests
    {
        [Test]
        public void NewHealth_StartsFull()
        {
            var health = new Health(10);

            Assert.AreEqual(10, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Constructor_ZeroMax_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Health(0));
        }

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            var health = new Health(10);

            health.TakeDamage(3);

            Assert.AreEqual(7, health.Current);
        }

        [Test]
        public void TakeDamage_NeverGoesBelowZero()
        {
            var health = new Health(5);

            health.TakeDamage(50);

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void TakeDamage_NegativeAmount_IsIgnored()
        {
            var health = new Health(10);

            health.TakeDamage(-4);

            Assert.AreEqual(10, health.Current);
        }

        [Test]
        public void Died_FiresOnlyOnce()
        {
            var health = new Health(3);
            int deaths = 0;
            health.Died += () => deaths++;

            health.TakeDamage(3);
            health.TakeDamage(3);

            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Changed_ReportsCurrentAndMax()
        {
            var health = new Health(10);
            int reportedCurrent = -1, reportedMax = -1;
            health.Changed += (current, max) => { reportedCurrent = current; reportedMax = max; };

            health.TakeDamage(4);

            Assert.AreEqual(6, reportedCurrent);
            Assert.AreEqual(10, reportedMax);
        }

        [Test]
        public void Heal_NeverGoesAboveMax()
        {
            var health = new Health(10);
            health.TakeDamage(2);

            health.Heal(100);

            Assert.AreEqual(10, health.Current);
        }

        [Test]
        public void Heal_WhenDead_DoesNothing()
        {
            var health = new Health(5);
            health.TakeDamage(5);

            health.Heal(3);

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
        }
    }
}
