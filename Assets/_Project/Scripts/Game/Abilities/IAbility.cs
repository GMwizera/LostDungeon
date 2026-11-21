using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// One interchangeable ability (Strategy pattern).
    /// The player (and later enemies) only call Activate; how the ability works is hidden inside it.
    /// Adding a new ability means writing one new class, with no changes to the player.
    /// </summary>
    public interface IAbility
    {
        string DisplayName { get; }
        float CooldownSeconds { get; }
        Sprite Icon { get; }
        bool PlaysActionAnimation { get; }
        void Activate(in AbilityContext context);
    }
}
