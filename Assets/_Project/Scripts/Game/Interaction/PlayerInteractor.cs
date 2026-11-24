using System.Collections.Generic;
using LostDungeon.Core.Events;
using LostDungeon.Game.Player;
using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>
    /// Finds the nearest usable IInteractable around the player, shows its prompt on the HUD,
    /// and uses it when Interact is pressed.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 0.7f;
        [SerializeField] private float scanInterval = 0.1f;

        private readonly List<Collider2D> nearby = new List<Collider2D>();
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
                SetCurrent(FindNearest()); // its prompt may have changed (e.g. chest now open)
            }
        }

        /// <summary>
        /// Searching algorithm: one pass over the colliders in range, keeping the closest
        /// interactable that can be used right now (squared distance avoids a square root per item).
        /// </summary>
        private IInteractable FindNearest()
        {
            Physics2D.OverlapCircle(transform.position, radius, ContactFilter2D.noFilter, nearby);

            IInteractable best = null;
            float bestDistance = float.MaxValue;
            foreach (Collider2D candidate in nearby)
            {
                if (!candidate.TryGetComponent(out IInteractable interactable) || !interactable.CanInteract(gameObject))
                    continue;

                float distance = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
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
