using System.Collections.Generic;
using LostDungeon.Core.Navigation;
using UnityEngine;

namespace LostDungeon.Game.Navigation
{
    /// <summary>
    /// Tells the A* pathfinder which floor cells you can walk on.
    /// Each cell is one unit wide. Enemies use Current to find it.
    /// </summary>
    public class LevelNavigationGrid : MonoBehaviour, INavigationGrid
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private List<bool> walkable = new List<bool>();

        public static LevelNavigationGrid Current { get; private set; }

        public int Width => width;
        public int Height => height;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        /// <summary>The level builder calls this in the Editor.</summary>
        public void Configure(int columns, int rows, IList<bool> open)
        {
            width = columns;
            height = rows;
            walkable = new List<bool>(open);
        }

        public bool IsWalkable(GridPoint cell) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height && walkable[cell.Y * width + cell.X];

        public GridPoint WorldToCell(Vector2 floorPoint)
        {
            Vector3 origin = transform.position;
            return new GridPoint(Mathf.FloorToInt(floorPoint.x - origin.x), Mathf.FloorToInt(floorPoint.y - origin.z));
        }

        public Vector2 CellToWorld(GridPoint cell)
        {
            Vector3 origin = transform.position;
            return new Vector2(origin.x + cell.X + 0.5f, origin.z + cell.Y + 0.5f);
        }
    }
}
