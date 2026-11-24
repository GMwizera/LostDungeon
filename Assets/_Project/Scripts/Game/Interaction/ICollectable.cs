using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>Picked up automatically by walking over it (coins, keys, potions).</summary>
    public interface ICollectable
    {
        void Collect(GameObject collector);
    }
}
