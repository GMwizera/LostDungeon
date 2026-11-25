using System;
using System.Collections.Generic;
using LostDungeon.Game.Inventory;
using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>
    /// A treasure chest: its lid swings open once and it gives its loot to whoever opened it.
    /// Chests holding a key glow from the start; the others light up when opened.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Chest : MonoBehaviour, IInteractable
    {
        [Serializable]
        public class Loot
        {
            public string itemId = ItemIds.Gold;
            public int amount = 5;
        }

        [SerializeField] private Transform lid;
        [SerializeField] private float openAngle = -110f;
        [SerializeField] private Light glow;
        [SerializeField] private List<Loot> loot = new List<Loot>();

        private bool opened;
        private Quaternion closedRotation;
        private float openProgress;

        public string Prompt => "Open chest";

        private void Awake()
        {
            if (lid != null)
                closedRotation = lid.localRotation;
            // A chest that holds the key glows gold before it is opened, so the player can find it.
            if (glow != null)
                glow.enabled = loot.Exists(item => item.itemId == ItemIds.Key);
        }

        public bool CanInteract(GameObject actor) => !opened && actor.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor))
                return;

            opened = true;
            if (glow != null)
                glow.enabled = true;

            var inventory = actor.GetComponent<PlayerInventory>();
            foreach (Loot item in loot)
                inventory.Add(item.itemId, item.amount);
        }

        private void Update()
        {
            if (!opened || lid == null || openProgress >= 1f)
                return;
            openProgress = Mathf.Min(1f, openProgress + Time.deltaTime * 2.5f);
            float eased = 1f - (1f - openProgress) * (1f - openProgress);
            lid.localRotation = closedRotation * Quaternion.Euler(openAngle * eased, 0f, 0f);
        }
    }
}
