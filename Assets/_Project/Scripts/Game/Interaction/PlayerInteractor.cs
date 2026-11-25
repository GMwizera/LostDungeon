using LostDungeon.Core.Events;
using LostDungeon.Game.Player;
using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>Finds the closest thing to use, shows its hint and uses it on Interact.</summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 0.9f;
        [SerializeField] private float scanInterval = 0.1f;

        private readonly Collider[] nearby = new Collider[16];
        private PlayerInputReader input;
        private IInteractable current;
        private string shownPrompt;
        private float nextScan;

        private void Awake() => input = GetComponent<PlayerInputReader>();
        private void OnEnable() => input.InteractPressed += OnInteract;

        private void OnDisable()
        {
            input.InteractPressed -= OnInteract;
            SetCurrent(null);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan)
                return;
            nextScan = Time.unscaledTime + scanInterval;
            SetCurrent(FindNearest());
        }

        private void OnInteract()
        {
            IInteractable target = FindNearest();
            if (target != null)
            {
                target.Interact(gameObject);
                SetCurrent(FindNearest()); // the hint may change, like an open chest
            }
        }

        /// <summary>
        /// Search: checks each nearby thing once and keeps the closest usable one.
        /// Squared distance skips a slow square root.
        /// </summary>
        private IInteractable FindNearest()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * World.Ground.BodyHeight, radius,
                nearby, Physics.AllLayers, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = nearby[i];
                if (!candidate.TryGetComponent(out IInteractable interactable) || !interactable.CanInteract(gameObject))
                    continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = interactable;
                }
            }

            return best;
        }

        private void SetCurrent(IInteractable interactable)
        {
            current = interactable;
            string prompt = current?.Prompt;
            if (prompt == shownPrompt)
                return;
            shownPrompt = prompt;
            GameEvents.RaiseInteractionPromptChanged(prompt);
        }
    }
}
