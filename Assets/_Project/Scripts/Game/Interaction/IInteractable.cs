using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>
    /// Something the player uses with the Interact button: chests, doors, levers, NPCs.
    /// PlayerInteractor only knows this interface, so new interactables need no player changes.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short hint shown on the HUD, e.g. "Open chest".</summary>
        string Prompt { get; }

        bool CanInteract(GameObject actor);
        void Interact(GameObject actor);
    }
}
