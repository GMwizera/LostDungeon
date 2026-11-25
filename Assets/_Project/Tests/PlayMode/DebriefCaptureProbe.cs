using System.Collections;
using System.IO;
using LostDungeon.Core.Events;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LostDungeon.PlayModeTests
{
    // Temporary: saves a picture of the level complete screen.
    public class DebriefCaptureProbe
    {
        [UnityTest]
        public IEnumerator Capture()
        {
            string save = Path.Combine(Application.persistentDataPath, "lostdungeon_save.json");
            string backup = File.Exists(save) ? File.ReadAllText(save) : null;
            SceneManager.LoadScene(GameManager.MenuScene);
            yield return null;
            SaveManager.Instance.ResetProgress();
            GameManager.Instance.StartGame();
            while (GameManager.Instance.State != GameState.Playing) yield return null;
            yield return new WaitForSeconds(0.5f);
            foreach (EnemyController e in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude)) e.TakeDamage(999);
            GameEvents.RaiseItemCollected("potion", 1);
            yield return new WaitForSeconds(0.3f);
            var door = Object.FindAnyObjectByType<ExitDoor>();
            door.Interact(Object.FindAnyObjectByType<PlayerHealth>().gameObject);
            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes("/private/tmp/claude-501/debrief.png", shot.EncodeToPNG());
            Debug.Log($"DEBRIEF CAPTURED {shot.width}x{shot.height}");
            if (backup != null) File.WriteAllText(save, backup);
            SaveManager.Instance.Load();
        }
    }
}
