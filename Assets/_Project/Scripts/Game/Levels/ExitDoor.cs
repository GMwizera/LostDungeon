using LostDungeon.Game.Interaction;
using LostDungeon.Game.Inventory;
using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>
    /// The way out. Looks closed until the objective is done, then opens; using it finishes the level.
    /// It reports "exit reached" to the LevelManager through an event, not by calling it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private LevelObjective objective;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;

        public static event System.Action ExitReached;

        public string Prompt => objective == null || objective.ExitUnlocked ? "Leave the dungeon" : "Locked";

        public bool CanInteract(GameObject actor) => actor.TryGetComponent(out PlayerInventory _);

        private void Update()
        {
            if (spriteRenderer == null)
                return;
            bool open = objective == null || objective.ExitUnlocked;
            Sprite wanted = open ? openSprite : closedSprite;
            if (wanted != null && spriteRenderer.sprite != wanted)
                spriteRenderer.sprite = wanted;
        }

        public void Interact(GameObject actor)
        {
            if (objective != null && !objective.ExitUnlocked)
            {
                objective.Publish(); // remind the player what is still missing
                return;
            }

            if (objective != null && objective.RequiresKey)
                actor.GetComponent<PlayerInventory>().TryUse(ItemIds.Key);

            ExitReached?.Invoke();
        }

        /// <summary>Static event: cleared when Play mode starts, because domain reload is off.</summary>
        internal static void ResetStatics() => ExitReached = null;
    }
}
