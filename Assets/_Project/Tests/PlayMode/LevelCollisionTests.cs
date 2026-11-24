using System.Collections;
using LostDungeon.Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Loads every level and checks with real physics that walls block and floor doesn't,
    /// including the room corners where two wall cells only touch at a point.
    /// </summary>
    public class LevelCollisionTests
    {
        [UnityTest]
        public IEnumerator EveryLevel_WallsAndCornersAreSolid_FloorIsOpen()
        {
            for (int number = 1; number <= 5; number++)
            {
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync($"Level{number}");
                yield return handle;
                Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, $"Level{number} failed to load.");

                yield return new WaitForFixedUpdate(); // let the tilemap colliders build their shapes
                yield return new WaitForFixedUpdate();

                Tilemap floor = null, walls = null;
                foreach (Tilemap map in handle.Result.GetComponentsInChildren<Tilemap>())
                {
                    if (map.name == "Floor") floor = map;
                    if (map.name == "Walls") walls = map;
                }
                Assert.IsNotNull(floor, $"Level{number} has no Floor tilemap.");
                Assert.IsNotNull(walls, $"Level{number} has no Walls tilemap.");

                floor.CompressBounds();
                BoundsInt room = floor.cellBounds; // floor covers exactly the room
                int wallCells = 0;

                foreach (Vector3Int cell in walls.cellBounds.allPositionsWithin)
                {
                    if (!walls.HasTile(cell))
                        continue;
                    wallCells++;
                    Vector2 point = walls.GetCellCenterWorld(cell);
                    Assert.IsNotNull(Physics2D.OverlapPoint(point), $"Level{number}: wall cell {cell} has no collider.");
                }

                Assert.Greater(wallCells, 0, $"Level{number} has no walls.");
                Assert.IsNotNull(walls.GetTile(new Vector3Int(room.xMin - 1, room.yMin - 1, 0)), $"Level{number}: front corner is open.");

                Vector2 floorPoint = floor.GetCellCenterWorld(new Vector3Int(room.xMin, room.yMin, 0));
                Collider2D onFloor = Physics2D.OverlapPoint(floorPoint);
                Assert.IsTrue(onFloor == null || onFloor.isTrigger || onFloor.GetComponentInParent<Tilemap>() == null,
                    $"Level{number}: floor cell is blocked by a wall collider.");

                Addressables.Release(handle);
                yield return null;
            }
        }
    }
}
