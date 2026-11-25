namespace LostDungeon.Game.Pooling
{
    /// <summary>
    /// For pooled objects that need to reset.
    /// Pooled things are reused, so these run instead of Start and OnDestroy.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
