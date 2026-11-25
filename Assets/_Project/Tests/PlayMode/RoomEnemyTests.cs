using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Uses the enemies the room spawns itself (not ones a test adds later). Written after a bug where
    /// a room's enemies looked for the player before the player had been placed, found nobody,
    /// and then ignored the player for the whole level.
    /// </summary>
    public class RoomEnemyTests
    {
        [UnityTest]
        public IEnumerator RoomEnemies_PlayerWalksUpToOne_ItFightsBack()
        {
            SceneManager.LoadScene(GameManager.MenuScene);
            yield return null;
            GameManager.Instance.StartGame();
            float waited = 0f;
            while (GameManager.Instance.State != GameState.Playing && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            yield return new WaitForSeconds(0.5f);

            var player = Object.FindAnyObjectByType<PlayerHealth>();
            EnemyController enemy = Object.FindAnyObjectByType<EnemyController>();
            Assert.IsNotNull(enemy, "The room spawned no enemies.");
            Assert.IsTrue(enemy.HasTarget, "The room's enemy does not know where the player is.");

            // Stand right next to it.
            var body = player.GetComponent<Rigidbody>();
            body.position = enemy.transform.position + new Vector3(0.7f, 0f, 0f);
            player.transform.position = body.position;
            int before = player.Health.Current;

            waited = 0f;
            while (player.Health.Current >= before && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.Less(player.Health.Current, before, $"The room's {enemy.Data.Id} stood next to the player for 5 seconds without attacking (state {enemy.CurrentState}).");
        }
    }
}
