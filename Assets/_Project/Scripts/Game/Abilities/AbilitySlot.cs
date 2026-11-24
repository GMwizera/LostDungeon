using LostDungeon.Core.Abilities;

namespace LostDungeon.Game.Abilities
{
    /// <summary>An ability plus its own cooldown timer. The HUD shows one icon per slot.</summary>
    public class AbilitySlot
    {
        public IAbility Ability { get; }
        public Cooldown Cooldown { get; }
        public bool Unlocked { get; set; } = true;
        public bool IsReady => Unlocked && Cooldown.IsReady;

        public AbilitySlot(IAbility ability)
        {
            Ability = ability;
            Cooldown = new Cooldown(ability.CooldownSeconds);
        }
    }
}
