using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Navigation;
using LostDungeon.Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Runs and dashes the player into every wall and corner and checks it stays inside.
    /// Added after a bug where the player could walk out.
    /// </summary>
    public class PlayerContainmentTests
    {
        [UnityTest]
        public IEnumerator Player_PushingIntoWallsAndDashing_StaysInTheRoom()
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

            var input = Object.FindAnyObjectByType<PlayerInputReader>();
            var body = input.GetComponent<Rigidbody>();
            LevelNavigationGrid grid = LevelNavigationGrid.Current;
            Assert.IsNotNull(grid);

            Vector2[] pushes = { Vector2.down, Vector2.left, Vector2.right, Vector2.up, new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1) };
            foreach (Vector2 push in pushes)
            {
                input.SetVirtualMove(push.normalized);
                for (int i = 0; i < 4; i++)
                {
                    yield return new WaitForSeconds(0.4f);
                    input.PressDash();
                }

                Vector3 p = body.position;
                Assert.IsTrue(p.x > -0.05f && p.z > -0.05f && p.x < grid.Width + 0.05f && p.z < grid.Height + 0.05f,
                    $"Player left the room pushing {push}: at {p}.");
            }

            input.SetVirtualMove(Vector2.zero);
        }
    }
}
