using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>
    /// Something the player can use, like a chest or door.
    /// The player only knows this interface, so new ones need no player changes.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Hint shown on screen, like "Open chest".</summary>
        string Prompt { get; }

        bool CanInteract(GameObject actor);
        void Interact(GameObject actor);
    }
}
