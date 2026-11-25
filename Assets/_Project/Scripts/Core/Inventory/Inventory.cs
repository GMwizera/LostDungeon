using System;
using System.Collections.Generic;

namespace LostDungeon.Core.Inventory
{
    /// <summary>An item and how many the player has.</summary>
    public readonly struct ItemStack
    {
        public string Id { get; }
        public int Count { get; }

        public ItemStack(string id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    /// <summary>Counts each item, like gold, keys and potions. Easy to test and save.</summary>
    public class Inventory
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        /// <summary>(item id, new count)</summary>
        public event Action<string, int> Changed;

        public int CountOf(string id) => counts.TryGetValue(id, out int count) ? count : 0;

        public bool Has(string id, int amount = 1) => CountOf(id) >= amount;

        public void Add(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Item id is required.", nameof(id));
            if (amount <= 0)
                return;

            counts[id] = CountOf(id) + amount;
            Changed?.Invoke(id, counts[id]);
        }

        /// <summary>Takes items if there are enough. If not, returns false.</summary>
        public bool TryRemove(string id, int amount = 1)
        {
            if (amount <= 0 || !Has(id, amount))
                return false;

            int left = CountOf(id) - amount;
            if (left == 0)
                counts.Remove(id);
            else
                counts[id] = left;

            Changed?.Invoke(id, left);
            return true;
        }

        public void Clear()
        {
            var ids = new List<string>(counts.Keys);
            counts.Clear();
            foreach (string id in ids)
                Changed?.Invoke(id, 0);
        }

        /// <summary>All items, biggest count first, ties A to Z.</summary>
        public List<ItemStack> GetSortedStacks()
        {
            var stacks = new List<ItemStack>(counts.Count);
            foreach (KeyValuePair<string, int> pair in counts)
                stacks.Add(new ItemStack(pair.Key, pair.Value));

            // Insertion sort is fine because there are only a few item types.
            for (int i = 1; i < stacks.Count; i++)
            {
                ItemStack current = stacks[i];
                int j = i - 1;
                while (j >= 0 && ComesBefore(current, stacks[j]))
                {
                    stacks[j + 1] = stacks[j];
                    j--;
                }
                stacks[j + 1] = current;
            }

            return stacks;
        }

        private static bool ComesBefore(ItemStack a, ItemStack b) =>
            a.Count > b.Count || (a.Count == b.Count && string.CompareOrdinal(a.Id, b.Id) < 0);
    }
}
