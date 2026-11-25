using LostDungeon.Game.Movement;
using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>A quick dash forward. Works on anything that is IDashable.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Abilities/Dash")]
    public class DashAbility : AbilityDefinition
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float duration = 0.16f;

        protected override void Perform(in AbilityContext context)
        {
            if (context.Owner.TryGetComponent(out IDashable dashable) && !dashable.IsDashing)
                dashable.Dash(context.Direction, speed, duration);
        }
    }
}
