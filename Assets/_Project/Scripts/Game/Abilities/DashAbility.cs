using LostDungeon.Game.Movement;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>Quick burst of speed in the facing direction. Works on anything that implements IDashable.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Abilities/Dash")]
    public class DashAbility : AbilityDefinition
    {
        [SerializeField] private float speed = 9f;
        [SerializeField] private float duration = 0.15f;

        public override void Activate(in AbilityContext context)
        {
            if (context.Owner.TryGetComponent(out IDashable dashable) && !dashable.IsDashing)
                dashable.Dash(context.Direction, speed, duration);
        }
    }
}
