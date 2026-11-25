using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LostDungeon.PlayModeTests
{
    /// <summary>Runs the real game. Any error in the log fails the test.</summary>
    public class GameFlowTests
    {
        [UnityTest]
        public IEnumerator MainMenu_StartGame_LoadsLevelOneAndSpawnsEnemies()
        {
            SceneManager.LoadScene(GameManager.MenuScene);
            yield return null;
            yield return null;

            GameManager.Instance.StartGame();

            float waited = 0f;
            while (GameManager.Instance.State != GameState.Playing && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(GameState.Playing, GameManager.Instance.State, "Level 1 did not finish loading.");
            Assert.IsNotNull(Object.FindAnyObjectByType<LevelRoot>(), "Level content was not instantiated.");
            Assert.IsNotNull(Object.FindAnyObjectByType<PlayerHealth>(), "Player is not active.");

            yield return new WaitForSeconds(3f); // give enemies time to move and attack
            Assert.Greater(Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude).Length, 0, "No enemies spawned.");
        }

        [UnityTest]
        public IEnumerator EveryLevelAddress_LoadsThroughAddressables()
        {
            for (int number = 1; number <= 5; number++)
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>($"Level{number}");
                yield return handle;

                Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, $"Level{number} failed to load.");
                Assert.IsNotNull(handle.Result.GetComponent<LevelRoot>(), $"Level{number} has no LevelRoot.");
                Addressables.Release(handle);
            }
        }
    }
}
