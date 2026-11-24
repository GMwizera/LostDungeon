using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace LostDungeon.PlayModeTests
{
    /// <summary>
    /// Pushes the player into the walls and corners in every direction, dashing as it goes,
    /// and checks it never leaves the floor. Written after a real bug where the player could walk out.
    /// </summary>
    public class PlayerContainmentTests
    {
        [UnityTest]
        public IEnumerator Player_PushingIntoWallsAndDashing_StaysOnTheFloor()
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
            var body = input.GetComponent<Rigidbody2D>();
            Tilemap floor = null;
            foreach (Tilemap map in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
                if (map.name == "Floor")
                    floor = map;
            Assert.IsNotNull(floor);

            Vector2[] pushes = { Vector2.down, Vector2.left, Vector2.right, Vector2.up, new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1) };
            foreach (Vector2 push in pushes)
            {
                input.SetVirtualMove(push.normalized);
                for (int i = 0; i < 4; i++)
                {
                    yield return new WaitForSeconds(0.4f);
                    input.PressDash();
                }

                Vector3Int cell = floor.WorldToCell(body.position);
                Assert.IsTrue(floor.HasTile(cell), $"Player left the room pushing {push}: at {body.position}, cell {cell}.");
            }

            input.SetVirtualMove(Vector2.zero);
        }
    }
}
