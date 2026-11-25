using LostDungeon.Core.Movement;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class EightWayDirectionTests
    {
        [TestCase(-1f, 1f, 0)]  // up-left
        [TestCase(-1f, 0f, 1)]  // left
        [TestCase(-1f, -1f, 2)] // down-left
        [TestCase(0f, -1f, 3)]  // down
        [TestCase(1f, -1f, 4)]  // down-right
        [TestCase(1f, 0f, 5)]   // right
        [TestCase(1f, 1f, 6)]   // up-right
        [TestCase(0f, 1f, 7)]   // up
        public void FromVector_ReturnsMatchingSpriteDirection(float x, float y, int expected)
        {
            Assert.AreEqual(expected, EightWayDirection.FromVector(x, y));
        }

        [Test]
        public void FromVector_SnapsToNearestDirection()
        {
            // Mostly right and a bit up still counts as "right".
            Assert.AreEqual(5, EightWayDirection.FromVector(1f, 0.2f));
        }
    }
}
