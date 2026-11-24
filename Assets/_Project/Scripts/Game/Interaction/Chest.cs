using System;
using System.Collections.Generic;
using LostDungeon.Game.Inventory;
using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>A treasure chest: opens once and gives its loot to whoever opened it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Chest : MonoBehaviour, IInteractable
    {
        [Serializable]
        public class Loot
        {
            public string itemId = ItemIds.Gold;
            public int amount = 5;
        }

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private List<Loot> loot = new List<Loot>();

        private bool opened;

        public string Prompt => "Open chest";

        public bool CanInteract(GameObject actor) => !opened && actor.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor))
                return;

            opened = true;
            if (spriteRenderer != null && openSprite != null)
                spriteRenderer.sprite = openSprite;

            var inventory = actor.GetComponent<PlayerInventory>();
            foreach (Loot item in loot)
                inventory.Add(item.itemId, item.amount);
        }
    }
}
