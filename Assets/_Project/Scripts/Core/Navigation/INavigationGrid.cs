namespace LostDungeon.Core.Navigation
{
    /// <summary>Tells the pathfinder which cells you can walk on. Tests use a text map.</summary>
    public interface INavigationGrid
    {
        bool IsWalkable(GridPoint cell);
    }
}
