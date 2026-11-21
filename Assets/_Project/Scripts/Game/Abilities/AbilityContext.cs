using UnityEngine;

namespace LostDungeon.Game.Abilities
{
    /// <summary>Everything an ability needs to know about who used it and where they are aiming.</summary>
    public readonly struct AbilityContext
    {
        public GameObject Owner { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }

        public AbilityContext(GameObject owner, Vector2 origin, Vector2 direction)
        {
            Owner = owner;
            Origin = origin;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.down;
        }
    }
}
