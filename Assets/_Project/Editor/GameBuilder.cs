using System.Collections.Generic;
using LostDungeon.Game.Audio;
using LostDungeon.Game.Cameras;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using LostDungeon.Game.UI;
using LostDungeon.Game.Weather;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Tools > Lost Dungeon > Build Game: builds every generated asset, the five Addressable levels,
    /// the MainMenu and Game scenes, and the build settings, in one go. Safe to run again.
    /// </summary>
    public static class GameBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string MenuScenePath = SceneFolder + "/MainMenu.unity";
        private const string GameScenePath = SceneFolder + "/Game.unity";
        private const string UIFolder = "Assets/_Project/UI";
        private const string PanelSettingsPath = UIFolder + "/PanelSettings.asset";
        private const string SoundLibraryPath = "Assets/_Project/ScriptableObjects/Audio/SoundLibrary.asset";

        [MenuItem("Tools/Lost Dungeon/Build Game")]
        public static void BuildGame()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                EditorUtility.DisplayProgressBar("Lost Dungeon", "Preparing art and tiles...", 0.1f);
                Dictionary<string, Tile> tiles = TestRoomBuilder.PrepareAssets();

                // The test room also creates the combat, enemy and audio assets the levels use.
                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building test room and shared assets...", 0.3f);
                TestRoomBuilder.BuildSceneOnly(tiles);

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Drawing icons...", 0.45f);
                UIIcons icons = IconFactory.Build();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building levels...", 0.6f);
                LevelCatalog catalog = LevelBuilder.BuildAll(tiles, new LevelBuilder.UIIconsRef
                {
                    Gold = icons.gold, Key = icons.key, Potion = icons.potion,
                });

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building scenes...", 0.85f);
                GetOrCreatePanelSettings();
                AssetDatabase.SaveAssets();

                BuildMenuScene();
                BuildGameScene();

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MenuScenePath, true),
                    new EditorBuildSettingsScene(GameScenePath, true),
                };
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!Application.isBatchMode)
                EditorSceneManager.OpenScene(MenuScenePath);
            Debug.Log("Game built. Open MainMenu and press Play.");
        }

        /// <summary>
        /// Loads the assets the scenes reference. Must run AFTER NewScene: creating a scene unloads assets
        /// nothing is using, which turns earlier references into dead ones that save as "None".
        /// </summary>
        private static (LevelCatalog, UIIcons, PanelSettings) LoadSharedAssets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelBuilder.CatalogPath);
            var icons = AssetDatabase.LoadAssetAtPath<UIIcons>(IconFactory.IconsAssetPath);
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (catalog == null || icons == null || panel == null)
                throw new System.InvalidOperationException("Catalog, icons or panel settings asset is missing.");
            return (catalog, icons, panel);
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel != null)
                return panel;

            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{UIFolder}/LostDungeonTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; // same layout on phones, browsers and monitors
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            return panel;
        }

        private static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            (LevelCatalog catalog, UIIcons icons, PanelSettings panel) = LoadSharedAssets();

            CreateCamera(new Color(0.07f, 0.05f, 0.09f));
            CreateEventSystem();

            SetValues(new GameObject("GameManager").AddComponent<GameManager>(), ("catalog", catalog));
            new GameObject("SaveManager").AddComponent<SaveManager>();
            new GameObject("WeatherService").AddComponent<WeatherService>();
            SetValues(new GameObject("AudioManager").AddComponent<AudioManager>(),
                ("library", AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath)));

            UIDocument document = CreateDocument("MainMenuUI", panel, $"{UIFolder}/MainMenu.uxml");
            SetValues(document.gameObject.AddComponent<MainMenuController>(), ("icons", icons));

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            (LevelCatalog catalog, UIIcons icons, PanelSettings panel) = LoadSharedAssets();

            Camera camera = CreateCamera(Color.black);
            camera.orthographicSize = 3f;
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            CreateEventSystem();

            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            GameObject player = TestRoomBuilder.CreatePlayer(Vector3.zero);
            SetUpPlayer(player);
            SetValues(follow, ("target", player.transform));
            player.SetActive(false); // the LevelManager enables it once the level has loaded

            var levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
            SetValues(levelManager, ("catalog", catalog), ("player", player), ("cameraFollow", follow), ("globalLight", light));

            UIDocument document = CreateDocument("HUD", panel, $"{UIFolder}/Hud.uxml");
            SetValues(document.gameObject.AddComponent<HudController>(),
                ("icons", icons), ("playerInput", player.GetComponent<PlayerInputReader>()));

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<AudioListener>(); // without one, nothing is heard
            return camera;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static UIDocument CreateDocument(string name, PanelSettings panel, string uxmlPath)
        {
            var document = new GameObject(name).AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            return document;
        }
    }
}
