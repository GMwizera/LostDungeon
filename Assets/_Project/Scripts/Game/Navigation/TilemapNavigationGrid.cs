using LostDungeon.Core.Navigation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LostDungeon.Game.Navigation
{
    /// <summary>
    /// Answers "can I walk here?" for the A* pathfinder, straight from the level's tilemaps:
    /// a cell is walkable when it has floor and no wall or solid prop.
    /// Put it on the level's Grid; enemies find it through Current.
    /// </summary>
    public class TilemapNavigationGrid : MonoBehaviour, INavigationGrid
    {
        [SerializeField] private Tilemap floor;
        [SerializeField] private Tilemap walls;

        public static TilemapNavigationGrid Current { get; private set; }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        public bool IsWalkable(GridPoint cell)
        {
            var position = new Vector3Int(cell.X, cell.Y, 0);
            return floor.HasTile(position) && (walls == null || !walls.HasTile(position));
        }

        public GridPoint WorldToCell(Vector2 world)
        {
            Vector3Int cell = floor.WorldToCell(world);
            return new GridPoint(cell.x, cell.y);
        }

        public Vector2 CellToWorld(GridPoint cell) => floor.GetCellCenterWorld(new Vector3Int(cell.X, cell.Y, 0));
    }
}
