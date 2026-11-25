using System.IO;
using System.Linq;
using LostDungeon.Game.Cameras;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Saves a picture of each level, so you can check it without pressing Play.
    /// </summary>
    public static class PreviewCapture
    {
        [MenuItem("Tools/Lost Dungeon/Capture Level Previews")]
        public static void CaptureAll()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Previews"));
            Directory.CreateDirectory(folder);
            for (int number = 1; number <= 5; number++)
                Capture(number, Path.Combine(folder, $"Level{number}.png"));
            Debug.Log($"Previews saved to {folder}");
        }

        private static void Capture(int number, string file)
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);
            var levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Levels/Level{number}.prefab");
            var level = (GameObject)PrefabUtility.InstantiatePrefab(levelPrefab);
            var root = level.GetComponent<LevelRoot>();

            GameObject player = Object.FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include).First().gameObject;
            player.SetActive(true);
            player.transform.position = root.PlayerSpawn.position + new Vector3(2.2f, 0f, 2.6f);
            Pose(player, "Idle_A", 0.3f, new Vector3(0.6f, 0f, 1f));

            foreach (EnemySpawner spawner in level.GetComponentsInChildren<EnemySpawner>())
            {
                var spawns = new SerializedObject(spawner).FindProperty("spawns");
                for (int i = 0; i < spawns.arraySize; i++)
                {
                    var prefab = (EnemyController)spawns.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue;
                    var point = (Transform)spawns.GetArrayElementAtIndex(i).FindPropertyRelative("point").objectReferenceValue;
                    var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
                    enemy.transform.position = point.position;
                    enemy.transform.localScale = Vector3.one * prefab.Data.Scale;
                    Pose(enemy, "Idle_A", 0.5f, player.transform.position - point.position);
                }
            }

            Camera camera = Camera.main;
            var follow = camera.GetComponent<CameraFollow>();
            var serialized = new SerializedObject(follow);
            float yaw = serialized.FindProperty("yaw").floatValue, pitch = serialized.FindProperty("pitch").floatValue;
            float distance = serialized.FindProperty("distance").floatValue;
            // Point the camera at the hero, like in the game.
            Vector3 focus = player.transform.position + Vector3.up * serialized.FindProperty("lookHeight").floatValue;
            camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            camera.transform.position = focus - camera.transform.forward * distance;

            const int width = 1280, height = 720;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = target;
            camera.aspect = width / (float)height;
            camera.Render();
            camera.Render(); // render twice so the effects look right

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            target.Release();
        }

        private static void Pose(GameObject character, string clipName, float time, Vector3 facing)
        {
            var animator = character.GetComponentInChildren<Animator>();
            if (animator == null)
                return;
            KayKitSetup.Clip(clipName).SampleAnimation(animator.gameObject, time);
            if (facing.sqrMagnitude > 0.01f)
                animator.transform.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z));
        }
    }
}
