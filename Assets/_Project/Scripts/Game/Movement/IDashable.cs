using UnityEngine;

namespace LostDungeon.Game.Movement
{
    /// <summary>Anything that can do a quick dash.</summary>
    public interface IDashable
    {
        bool IsDashing { get; }
        void Dash(Vector2 direction, float speed, float duration);
    }
}
