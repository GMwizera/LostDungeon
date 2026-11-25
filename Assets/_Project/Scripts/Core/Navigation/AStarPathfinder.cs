using System.Collections.Generic;

namespace LostDungeon.Core.Navigation
{
    /// <summary>
    /// A* search so enemies can walk around walls to the player.
    /// Only 4 directions, so enemies never cut through wall corners.
    /// Manhattan distance never guesses too high, so we always get a shortest path.
    /// </summary>
    public static class AStarPathfinder
    {
        private static readonly GridPoint[] Steps =
        {
            new GridPoint(1, 0), new GridPoint(-1, 0), new GridPoint(0, 1), new GridPoint(0, -1),
        };

        /// <summary>
        /// Gives the steps from start to goal (start not included).
        /// Empty if already there, the goal is blocked, or no path is found in time.
        /// </summary>
        public static List<GridPoint> FindPath(INavigationGrid grid, GridPoint start, GridPoint goal, int maxSearchedCells = 2000)
        {
            var path = new List<GridPoint>();
            if (start.Equals(goal) || !grid.IsWalkable(goal))
                return path;

            var open = new MinHeap<GridPoint>();
            var cameFrom = new Dictionary<GridPoint, GridPoint>();
            var costSoFar = new Dictionary<GridPoint, int> { [start] = 0 };
            var closed = new HashSet<GridPoint>();

            open.Push(start, GridPoint.ManhattanDistance(start, goal));

            while (open.Count > 0)
            {
                GridPoint current = open.Pop();
                if (current.Equals(goal))
                    return BuildPath(cameFrom, start, goal);

                if (!closed.Add(current))
                    continue; // old, worse copy of a cell we already did
                if (closed.Count > maxSearchedCells)
                    break; // stop big searches so the game doesn't freeze

                foreach (GridPoint step in Steps)
                {
                    GridPoint next = current + step;
                    if (closed.Contains(next) || !grid.IsWalkable(next))
                        continue;

                    int newCost = costSoFar[current] + 1;
                    if (costSoFar.TryGetValue(next, out int oldCost) && newCost >= oldCost)
                        continue;

                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    open.Push(next, newCost + GridPoint.ManhattanDistance(next, goal));
                }
            }

            return path;
        }

        private static List<GridPoint> BuildPath(Dictionary<GridPoint, GridPoint> cameFrom, GridPoint start, GridPoint goal)
        {
            var path = new List<GridPoint>();
            GridPoint cell = goal;
            while (!cell.Equals(start))
            {
                path.Add(cell);
                cell = cameFrom[cell];
            }
            path.Reverse();
            return path;
        }
    }
}
