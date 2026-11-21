namespace LostDungeon.Game.Pooling
{
    /// <summary>
    /// Implemented by components on pooled objects that need to reset themselves.
    /// PoolManager calls these instead of Start/OnDestroy, because pooled objects are reused, not recreated.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
