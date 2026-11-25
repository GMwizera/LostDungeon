using LostDungeon.Game.Pooling;
using LostDungeon.Game.Visuals;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// Abilities are ScriptableObject assets, so they can be tuned and swapped in the Inspector
    /// (drag a different asset into a slot to change what a button does).
    /// They hold settings only; per-use state such as cooldowns lives in AbilitySlot.
    /// Activate is a template method: it shows the ability's visual effect, then runs Perform,
    /// which each ability class writes for itself.
    /// </summary>
    public abstract class AbilityDefinition : ScriptableObject, IAbility
    {
        [SerializeField] private string displayName = "Ability";
        [SerializeField, Min(0f)] private float cooldownSeconds = 1f;
        [SerializeField] private Sprite icon;
        [SerializeField] private string animationTrigger = "";

        [Header("Visual effect")]
        [SerializeField] private PooledEffect castEffect;
        [SerializeField] private float effectDistance;
        [SerializeField] private float effectHeight = Ground.BodyHeight;

        public string DisplayName => displayName;
        public float CooldownSeconds => cooldownSeconds;
        public Sprite Icon => icon;
        public string AnimationTrigger => animationTrigger;

        public void Activate(in AbilityContext context)
        {
            if (castEffect != null && PoolManager.HasInstance)
            {
                Vector2 at = context.Origin + context.Direction * effectDistance;
                PoolManager.Instance.Spawn(castEffect, Ground.ToWorld(at, effectHeight), Ground.Facing(context.Direction));
            }
            Perform(context);
        }

        protected abstract void Perform(in AbilityContext context);
    }
}
