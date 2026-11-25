using LostDungeon.Game.Interaction;
using LostDungeon.Game.Inventory;
using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>
    /// The way out. The door stays shut until the objective is done, then swings open and light pours in;
    /// using it finishes the level. It reports "exit reached" to the LevelManager through an event, not by calling it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private LevelObjective objective;
        [SerializeField] private Transform door;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private Light glow;
        [SerializeField] private float openGlow = 4f;
        [SerializeField] private float closedGlow = 0.6f;

        private Quaternion closedRotation;
        private float openProgress;

        public static event System.Action ExitReached;

        public bool IsOpen => objective == null || objective.ExitUnlocked;

        public string Prompt => IsOpen ? "Leave the dungeon" : "Locked";

        private void Awake()
        {
            if (door != null)
                closedRotation = door.localRotation;
        }

        public bool CanInteract(GameObject actor) => actor.TryGetComponent(out PlayerInventory _);

        private void Update()
        {
            float target = IsOpen ? 1f : 0f;
            openProgress = Mathf.MoveTowards(openProgress, target, Time.deltaTime * 1.5f);
            if (door != null)
                door.localRotation = closedRotation * Quaternion.Euler(0f, openAngle * openProgress, 0f);
            if (glow != null)
                glow.intensity = Mathf.Lerp(closedGlow, openGlow, openProgress);
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
