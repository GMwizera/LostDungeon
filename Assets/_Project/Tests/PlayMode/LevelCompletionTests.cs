using System.Collections;
using System.IO;
using LostDungeon.Core.Events;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using LostDungeon.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Plays level 1 to the end: defeat every enemy, use the exit door, and check that the level is saved
    /// as completed, the next level unlocks, and the Expedition Debrief screen is shown and filled in.
    /// The player's real save file is backed up and restored, so running tests never changes their progress.
    /// </summary>
    public class LevelCompletionTests
    {
        private string savePath;
        private string backup;

        [SetUp]
        public void BackUpSave()
        {
            savePath = Path.Combine(Application.persistentDataPath, "lostdungeon_save.json");
            backup = File.Exists(savePath) ? File.ReadAllText(savePath) : null;
        }

        [TearDown]
        public void RestoreSave()
        {
            if (backup != null)
                File.WriteAllText(savePath, backup);
            else if (File.Exists(savePath))
                File.Delete(savePath);
            if (SaveManager.HasInstance)
                SaveManager.Instance.Load();
        }

        [UnityTest]
        public IEnumerator Level1_DefeatEnemiesAndExit_CompletesAndShowsDebrief()
        {
            SceneManager.LoadScene(GameManager.MenuScene);
            yield return null;
            SaveManager.Instance.ResetProgress(); // start from a clean save for the test
            GameManager.Instance.StartGame();

            float waited = 0f;
            while (GameManager.Instance.State != GameState.Playing && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            yield return new WaitForSeconds(0.5f); // let the spawner place the enemies

            foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
                enemy.TakeDamage(999);
            yield return null;

            var door = Object.FindAnyObjectByType<ExitDoor>();
            var player = Object.FindAnyObjectByType<PlayerHealth>().gameObject;
            Assert.IsTrue(door.CanInteract(player));
            door.Interact(player);
            yield return null;

            Assert.AreEqual(GameState.LevelComplete, GameManager.Instance.State, "Exit door did not complete the level.");
            Assert.AreEqual(Core.Progress.LevelStatus.Completed, SaveManager.Instance.Progress.GetStatus(1));
            Assert.AreEqual(Core.Progress.LevelStatus.Available, SaveManager.Instance.Progress.GetStatus(2));
            Assert.IsTrue(SaveManager.Instance.HasUnlock(LevelManager.UnlockFireball), "Fireball reward was not granted.");

            VisualElement hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
            Assert.AreEqual(DisplayStyle.Flex, hud.Q("complete-overlay").resolvedStyle.display, "Debrief screen is not showing.");
            StringAssert.Contains("ROOM 1 CLEARED", hud.Q<Label>("complete-title").text);
            Assert.Greater(hud.Q("spoils").childCount, 0, "Debrief has no spoils listed.");
        }
    }
}
