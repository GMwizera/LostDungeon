using UnityEngine;

namespace LostDungeon.Game.Movement
{
    /// <summary>Anything that can be pushed into a quick burst of movement.</summary>
    public interface IDashable
    {
        bool IsDashing { get; }
        void Dash(Vector2 direction, float speed, float duration);
    }
}
