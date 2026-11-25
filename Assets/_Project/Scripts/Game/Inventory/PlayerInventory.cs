using System.Collections.Generic;
using LostDungeon.Core.Events;
using LostDungeon.Core.Inventory;
using LostDungeon.Core.Save;
using UnityEngine;
using CoreInventory = LostDungeon.Core.Inventory.Inventory;

namespace LostDungeon.Game.Inventory
{
    /// <summary>
    /// The player's items. It sends an event on every change,
    /// so the HUD, sound and saving update without it knowing them.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        private readonly CoreInventory items = new CoreInventory();

        public CoreInventory Items => items;

        private void Awake() => items.Changed += OnChanged;
        private void OnDestroy() => items.Changed -= OnChanged;

        public void Add(string id, int amount = 1)
        {
            items.Add(id, amount);
            GameEvents.RaiseItemCollected(id, amount);
        }

        public bool TryUse(string id, int amount = 1) => items.TryRemove(id, amount);

        /// <summary>Puts saved items back when a level starts.</summary>
        public void Load(IEnumerable<InventoryEntry> saved)
        {
            items.Clear();
            foreach (InventoryEntry entry in saved)
                if (entry.count > 0 && entry.id != ItemIds.Key) // a key only works in its own level
                    items.Add(entry.id, entry.count);
        }

        public List<InventoryEntry> ToSaveEntries()
        {
            var entries = new List<InventoryEntry>();
            foreach (ItemStack stack in items.GetSortedStacks())
                if (stack.Id != ItemIds.Key)
                    entries.Add(new InventoryEntry { id = stack.Id, count = stack.Count });
            return entries;
        }

        private void OnChanged(string id, int count) => GameEvents.RaiseInventoryChanged(id, count);
    }
}
