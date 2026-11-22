namespace LostDungeon.Core.Navigation
{
    /// <summary>
    /// What the pathfinder needs to know about a level: which cells can be walked on.
    /// The game answers this from the Unity tilemaps; tests answer it from a small text map.
    /// </summary>
    public interface INavigationGrid
    {
        bool IsWalkable(GridPoint cell);
    }
}
