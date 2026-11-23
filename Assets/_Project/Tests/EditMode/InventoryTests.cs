using LostDungeon.Core.Inventory;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class InventoryTests
    {
        [Test]
        public void Add_StacksSameItem()
        {
            var inventory = new Inventory();
            inventory.Add("gold", 3);
            inventory.Add("gold", 2);

            Assert.AreEqual(5, inventory.CountOf("gold"));
        }

        [Test]
        public void TryRemove_NotEnough_FailsAndKeepsItems()
        {
            var inventory = new Inventory();
            inventory.Add("key");

            Assert.IsFalse(inventory.TryRemove("key", 2));
            Assert.AreEqual(1, inventory.CountOf("key"));
        }

        [Test]
        public void TryRemove_Enough_Removes()
        {
            var inventory = new Inventory();
            inventory.Add("potion", 2);

            Assert.IsTrue(inventory.TryRemove("potion"));
            Assert.AreEqual(1, inventory.CountOf("potion"));
        }

        [Test]
        public void Changed_ReportsNewCount()
        {
            var inventory = new Inventory();
            string changedId = null;
            int changedCount = -1;
            inventory.Changed += (id, count) => { changedId = id; changedCount = count; };

            inventory.Add("gold", 4);

            Assert.AreEqual("gold", changedId);
            Assert.AreEqual(4, changedCount);
        }

        [Test]
        public void GetSortedStacks_LargestFirstThenByName()
        {
            var inventory = new Inventory();
            inventory.Add("key", 1);
            inventory.Add("gold", 9);
            inventory.Add("potion", 1);

            var stacks = inventory.GetSortedStacks();

            Assert.AreEqual("gold", stacks[0].Id);
            Assert.AreEqual("key", stacks[1].Id);
            Assert.AreEqual("potion", stacks[2].Id);
        }
    }
}
