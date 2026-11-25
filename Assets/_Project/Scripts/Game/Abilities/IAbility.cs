using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// One swappable ability (Strategy pattern).
    /// Users just call Activate, so a new ability is one new class.
    /// </summary>
    public interface IAbility
    {
        string DisplayName { get; }
        float CooldownSeconds { get; }
        Sprite Icon { get; }

        /// <summary>Animation to play, like "Attack", or empty for none.</summary>
        string AnimationTrigger { get; }

        void Activate(in AbilityContext context);
    }
}
