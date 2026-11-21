using LostDungeon.Core.Abilities;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class CooldownTests
    {
        [Test]
        public void NewCooldown_IsReady()
        {
            Assert.IsTrue(new Cooldown(2f).IsReady);
        }

        [Test]
        public void Start_MakesItNotReady_UntilDurationPasses()
        {
            var cooldown = new Cooldown(2f);
            cooldown.Start();

            cooldown.Tick(1.5f);
            Assert.IsFalse(cooldown.IsReady);

            cooldown.Tick(0.5f);
            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void Remaining_NeverGoesNegative()
        {
            var cooldown = new Cooldown(1f);
            cooldown.Start();

            cooldown.Tick(10f);

            Assert.AreEqual(0f, cooldown.Remaining);
        }

        [Test]
        public void Progress01_IsHalfwayAtHalfDuration()
        {
            var cooldown = new Cooldown(4f);
            cooldown.Start();

            cooldown.Tick(2f);

            Assert.AreEqual(0.5f, cooldown.Progress01, 0.0001f);
        }
    }
}
