using System.Collections;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Loads each level and uses real physics to check all 4 walls are solid
    /// and the player spawns on open floor.
    /// </summary>
    public class LevelCollisionTests
    {
        [UnityTest]
        public IEnumerator EveryLevel_RoomIsSealed_SpawnIsOpen()
        {
            for (int number = 1; number <= 5; number++)
            {
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync($"Level{number}");
                yield return handle;
                Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, $"Level{number} failed to load.");
                yield return new WaitForFixedUpdate();

                var grid = handle.Result.GetComponentInChildren<LevelNavigationGrid>();
                Assert.IsNotNull(grid, $"Level{number} has no navigation grid.");
                int w = grid.Width, h = grid.Height;

                for (int x = -1; x <= w; x++)
                {
                    AssertSolid(number, new Vector3(x + 0.5f, 1f, -0.25f));     // near side
                    AssertSolid(number, new Vector3(x + 0.5f, 1f, h + 0.25f));  // far side
                }
                for (int y = -1; y <= h; y++)
                {
                    AssertSolid(number, new Vector3(-0.25f, 1f, y + 0.5f));
                    AssertSolid(number, new Vector3(w + 0.25f, 1f, y + 0.5f));
                }

                Transform spawn = handle.Result.GetComponent<LevelRoot>().PlayerSpawn;
                // Ignore the hero, an earlier test may have left it here.
                Collider[] blockers = System.Array.FindAll(
                    Physics.OverlapSphere(spawn.position + Vector3.up * 0.6f, 0.2f, Physics.AllLayers, QueryTriggerInteraction.Ignore),
                    c => c.transform.IsChildOf(handle.Result.transform));
                Assert.IsEmpty(blockers, $"Level{number}: the player spawn at {spawn.position} is blocked by " +
                    string.Join(", ", System.Array.ConvertAll(blockers, c => $"{c.name} ({c.GetType().Name}) at {c.bounds.center}")));

                Addressables.Release(handle);
                yield return null;
            }
        }

        private static void AssertSolid(int level, Vector3 point)
        {
            Assert.IsTrue(Physics.CheckSphere(point, 0.1f, Physics.AllLayers, QueryTriggerInteraction.Ignore),
                $"Level{level}: the room edge at {point} has no wall.");
        }
    }
}
