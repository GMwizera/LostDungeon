using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// Abilities are ScriptableObject assets, so they can be tuned and swapped in the Inspector
    /// (drag a different asset into a slot to change what a button does).
    /// They hold settings only; per-use state such as cooldowns lives in AbilitySlot.
    /// </summary>
    public abstract class AbilityDefinition : ScriptableObject, IAbility
    {
        [SerializeField] private string displayName = "Ability";
        [SerializeField, Min(0f)] private float cooldownSeconds = 1f;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool playsActionAnimation;

        public string DisplayName => displayName;
        public float CooldownSeconds => cooldownSeconds;
        public Sprite Icon => icon;
        public bool PlaysActionAnimation => playsActionAnimation;

        public abstract void Activate(in AbilityContext context);
    }
}
