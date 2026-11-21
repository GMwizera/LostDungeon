using LostDungeon.Core.Events;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class GameEventsTests
    {
        [SetUp]
        public void SetUp() => GameEvents.ResetAll();

        [TearDown]
        public void TearDown() => GameEvents.ResetAll();

        [Test]
        public void EnemyDefeated_ReachesEveryListener()
        {
            int heard = 0;
            GameEvents.EnemyDefeated += _ => heard++; // e.g. score
            GameEvents.EnemyDefeated += _ => heard++; // e.g. audio

            GameEvents.RaiseEnemyDefeated("Warrior");

            Assert.AreEqual(2, heard);
        }

        [Test]
        public void ItemCollected_PassesItsData()
        {
            string itemId = null;
            int amount = 0;
            GameEvents.ItemCollected += (id, count) => { itemId = id; amount = count; };

            GameEvents.RaiseItemCollected("gold", 5);

            Assert.AreEqual("gold", itemId);
            Assert.AreEqual(5, amount);
        }

        [Test]
        public void Raise_WithNoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => GameEvents.RaiseLevelCompleted(1));
        }

        [Test]
        public void ResetAll_RemovesListeners()
        {
            bool heard = false;
            GameEvents.PlayerDied += () => heard = true;

            GameEvents.ResetAll();
            GameEvents.RaisePlayerDied();

            Assert.IsFalse(heard);
        }
    }
}
