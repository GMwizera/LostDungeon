using LostDungeon.Game.Pooling;
using LostDungeon.Game.Visuals;
using LostDungeon.Game.World;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>
    /// Base for ability assets, so I can swap abilities in the Inspector.
    /// It only holds settings. Cooldowns live in AbilitySlot.
    /// Template Method: Activate shows the effect, then calls each ability's own Perform.
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
