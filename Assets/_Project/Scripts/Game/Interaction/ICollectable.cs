using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>Things you pick up by walking over them.</summary>
    public interface ICollectable
    {
        void Collect(GameObject collector);
    }
}
