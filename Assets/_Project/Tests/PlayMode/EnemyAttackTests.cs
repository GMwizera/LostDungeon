using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using LostDungeon.Game.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Puts each enemy next to the player and checks it does damage.
    /// Added after a bug where a cornered archer kept backing up and never shot.
    /// </summary>
    public class EnemyAttackTests
    {
        [UnityTest]
        public IEnumerator Archer_PointBlank_ShootsThePlayer() => EnemyHurtsPlayer("Archer", 0.7f);

        [UnityTest]
        public IEnumerator Warrior_UpClose_HitsThePlayer() => EnemyHurtsPlayer("Warrior", 0.7f);

        [UnityTest]
        public IEnumerator Guardian_UpClose_HitsThePlayer() => EnemyHurtsPlayer("Guardian", 0.8f);

        [UnityTest]
        public IEnumerator Warrior_SeesPlayer_WalksTowardThem() => EnemyApproaches("Warrior");

        [UnityTest]
        public IEnumerator Archer_SeesPlayerFarAway_WalksIntoRange() => EnemyApproaches("Archer", 5.5f);

        /// <summary>Spawns an enemy a bit away and checks it walks closer.</summary>
        private static IEnumerator EnemyApproaches(string enemyName, float distance = 3f)
        {
#if UNITY_EDITOR
            yield return StartLevelWithoutEnemies();
            var player = Object.FindAnyObjectByType<PlayerHealth>();
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyController>($"Assets/_Project/Prefabs/Enemies/{enemyName}.prefab");
            Vector3 start = player.transform.position + new Vector3(distance * 0.6f, 0f, distance * 0.8f);
            EnemyController enemy = PoolManager.Instance.Spawn(prefab, start, Quaternion.identity);
            float before = Vector3.Distance(enemy.transform.position, player.transform.position);

            yield return new WaitForSeconds(1.5f);
            float after = Vector3.Distance(enemy.transform.position, player.transform.position);
            Assert.Less(after, before - 0.5f, $"{enemyName} saw the player {before:0.0} units away but did not walk toward them (now {after:0.0}, state {enemy.CurrentState}).");
#else
            Assert.Ignore("Needs the Editor to load the enemy prefab.");
            yield break;
#endif
        }

        private static IEnumerator StartLevelWithoutEnemies()
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
            yield return new WaitForSeconds(0.3f);
            foreach (EnemyController other in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
                other.TakeDamage(999);
            yield return new WaitForSeconds(2.2f);
        }

        private static IEnumerator EnemyHurtsPlayer(string enemyName, float distance)
        {
#if UNITY_EDITOR
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
            yield return new WaitForSeconds(0.3f);

            // Only the test enemy should be fighting.
            foreach (EnemyController other in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
                other.TakeDamage(999);
            yield return new WaitForSeconds(2.2f);

            var player = Object.FindAnyObjectByType<PlayerHealth>();
            int before = player.Health.Current;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyController>($"Assets/_Project/Prefabs/Enemies/{enemyName}.prefab");
            Vector3 at = player.transform.position + new Vector3(distance, 0f, 0f);
            PoolManager.Instance.Spawn(prefab, at, Quaternion.identity);

            waited = 0f;
            while (player.Health.Current >= before && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.Less(player.Health.Current, before, $"{enemyName} stood next to the player for 5 seconds without hurting them.");
#else
            Assert.Ignore("Needs the Editor to load the enemy prefab.");
            yield break;
#endif
        }
    }
}
