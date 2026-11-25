using System.Collections.Generic;
using LostDungeon.Core.Navigation;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class AStarPathfinderTests
    {
        /// <summary>Makes a grid from text. '#' is a wall. Row 0 is the bottom.</summary>
        private class TextGrid : INavigationGrid
        {
            private readonly string[] rows;

            public TextGrid(params string[] topToBottom)
            {
                rows = (string[])topToBottom.Clone();
                System.Array.Reverse(rows);
            }

            public bool IsWalkable(GridPoint cell) =>
                cell.Y >= 0 && cell.Y < rows.Length &&
                cell.X >= 0 && cell.X < rows[cell.Y].Length &&
                rows[cell.Y][cell.X] != '#';
        }

        [Test]
        public void OpenRoom_PathLengthIsManhattanDistance()
        {
            var grid = new TextGrid(
                ".....",
                ".....",
                ".....");

            List<GridPoint> path = AStarPathfinder.FindPath(grid, new GridPoint(0, 0), new GridPoint(4, 2));

            Assert.AreEqual(6, path.Count);
            Assert.AreEqual(new GridPoint(4, 2), path[path.Count - 1]);
        }

        [Test]
        public void Wall_PathGoesAroundIt()
        {
            var grid = new TextGrid(
                ".....",
                ".###.",
                ".....");

            List<GridPoint> path = AStarPathfinder.FindPath(grid, new GridPoint(2, 0), new GridPoint(2, 2));

            Assert.AreEqual(6, path.Count); // 2 sideways, 2 up, 2 back
            foreach (GridPoint cell in path)
                Assert.IsTrue(grid.IsWalkable(cell), $"Path walks through wall at {cell}");
        }

        [Test]
        public void EachStep_MovesExactlyOneCell()
        {
            var grid = new TextGrid(
                "......",
                ".####.",
                "......");
            var start = new GridPoint(0, 0);

            List<GridPoint> path = AStarPathfinder.FindPath(grid, start, new GridPoint(5, 2));

            GridPoint previous = start;
            foreach (GridPoint cell in path)
            {
                Assert.AreEqual(1, GridPoint.ManhattanDistance(previous, cell));
                previous = cell;
            }
        }

        [Test]
        public void GoalSealedOff_ReturnsEmpty()
        {
            var grid = new TextGrid(
                "..#.",
                "..#.",
                "..#.");

            Assert.IsEmpty(AStarPathfinder.FindPath(grid, new GridPoint(0, 0), new GridPoint(3, 0)));
        }

        [Test]
        public void GoalIsWall_ReturnsEmpty()
        {
            var grid = new TextGrid("..#");

            Assert.IsEmpty(AStarPathfinder.FindPath(grid, new GridPoint(0, 0), new GridPoint(2, 0)));
        }

        [Test]
        public void StartEqualsGoal_ReturnsEmpty()
        {
            var grid = new TextGrid("...");

            Assert.IsEmpty(AStarPathfinder.FindPath(grid, new GridPoint(1, 0), new GridPoint(1, 0)));
        }

        [Test]
        public void MinHeap_PopsLowestPriorityFirst()
        {
            var heap = new MinHeap<string>();
            heap.Push("c", 3);
            heap.Push("a", 1);
            heap.Push("d", 4);
            heap.Push("b", 2);

            Assert.AreEqual("a", heap.Pop());
            Assert.AreEqual("b", heap.Pop());
            Assert.AreEqual("c", heap.Pop());
            Assert.AreEqual("d", heap.Pop());
        }
    }
}
