using LostDungeon.Game.Audio;
using LostDungeon.Game.Cameras;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using LostDungeon.Game.UI;
using LostDungeon.Game.Visuals;
using LostDungeon.Game.Weather;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Builds the whole game in one click, from the art to the scenes.
    /// You can run it again, it just remakes everything.
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
                EditorUtility.DisplayProgressBar("Lost Dungeon", "Setting up 3D rendering...", 0.05f);
                RenderSetup.Apply();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Importing KayKit models...", 0.15f);
                KayKitSetup.Prepare();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building effects...", 0.35f);
                EffectsBuilder.BuildAll();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Drawing icons...", 0.45f);
                IconFactory.Build();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building enemies...", 0.55f);
                var enemies = EnemySetupBuilder.BuildAll();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building levels...", 0.7f);
                LevelBuilder.BuildAll(enemies);

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
        /// Loads the assets after NewScene, or they save as None.
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
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; // looks the same on any screen size
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

            Camera camera = CreateCamera(new Color(0.02f, 0.02f, 0.035f));
            BuildMenuBackdrop(camera);
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

        /// <summary>
        /// The small 3D room you see behind the main menu.
        /// </summary>
        private static void BuildMenuBackdrop(Camera camera)
        {
            ApplyAtmosphere();
            AddMoonAndPostFX(out _);

            var root = new GameObject("Menu Backdrop").transform;
            var random = new System.Random(5);
            string[] tiles = { "floor_tile_small", "floor_tile_small", "floor_tile_small_weeds_A", "floor_tile_small_weeds_B" };
            for (int x = -6; x <= 6; x++)
                for (int z = -4; z <= 6; z++)
                    LevelBuilder.Place(root, tiles[random.Next(tiles.Length)], new Vector3(x + 0.5f, 0f, z + 0.5f), 90f * random.Next(4));

            for (int k = -3; k <= 2; k++)
            {
                LevelBuilder.Place(root, k == 0 ? "wall_doorway" : "wall", new Vector3(2 * k + 1, 0f, 5.25f));
                if (k % 2 != 0)
                    LevelBuilder.AddTorch(root, new Vector3(2 * k + 1, 0f, 5f), 180f);
            }
            for (int k = -2; k <= 2; k++)
            {
                LevelBuilder.Place(root, "wall", new Vector3(5.25f, 0f, 2 * k + 1), 90f);
                if (k % 2 == 0)
                    LevelBuilder.AddTorch(root, new Vector3(5f, 0f, 2 * k + 1), 270f);
            }
            LevelBuilder.Place(root, "pillar_decorated", new Vector3(-2.5f, 0f, 2.5f));
            LevelBuilder.Place(root, "pillar_decorated", new Vector3(3.5f, 0f, 3.5f));
            LevelBuilder.Place(root, "barrel_large", new Vector3(4.3f, 0f, -0.5f), 30f);
            GameObject chest = LevelBuilder.Place(root, "chest_gold", new Vector3(3.3f, 0f, 1.6f), 225f);
            var glow = new GameObject("Treasure Glow").AddComponent<Light>();
            glow.transform.SetParent(chest.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.78f, 0.3f);
            glow.intensity = 3f;
            glow.range = 3f;
            var doorGlow = new GameObject("Door Glow").AddComponent<Light>();
            doorGlow.transform.position = new Vector3(1f, 1.2f, 6.2f);
            doorGlow.type = LightType.Point;
            doorGlow.color = new Color(1f, 0.82f, 0.5f);
            doorGlow.intensity = 4f;
            doorGlow.range = 6f;

            GameObject hero = CharacterBuilder.Create("Menu Hero", new CharacterBuilder.Look
            {
                ModelPath = $"{KayKitSetup.Root}/Characters/Mage.fbx",
                Controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>($"{KayKitSetup.ControllerFolder}/Hero.controller"),
                RightHand = $"{KayKitSetup.PropsFolder}/staff.fbx",
                Ring = EffectsBuilder.RingMaterial("FX_RingHero"),
            });
            Transform hat = KayKitSetup.FindBone(hero.transform, "Mage_Hat");
            if (hat != null)
                hat.gameObject.SetActive(false);
            hero.transform.position = new Vector3(0.5f, 0f, 1.5f);
            hero.transform.Find("Model").rotation = Quaternion.Euler(0f, 205f, 0f);
            hero.GetComponent<Rigidbody>().isKinematic = true;
            EffectsBuilder.AddAura(hero.transform);
            var heroLight = new GameObject("Hero Light").AddComponent<Light>();
            heroLight.transform.SetParent(hero.transform, false);
            heroLight.transform.localPosition = new Vector3(-0.6f, 2.2f, -1.2f);
            heroLight.type = LightType.Point;
            heroLight.color = new Color(1f, 0.75f, 0.5f);
            heroLight.intensity = 2.8f;
            heroLight.range = 5f;

            foreach ((string prefab, Vector3 at, float yaw) in new[]
            {
                ("Warrior", new Vector3(-2.2f, 0f, 3.6f), 150f),
                ("Guardian", new Vector3(2.4f, 0f, 4.2f), 210f),
                ("Archer", new Vector3(-3.6f, 0f, 1.2f), 110f),
            })
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemySetupBuilder.EnemyPrefabFolder}/{prefab}.prefab");
                GameObject enemy = Object.Instantiate(source);
                enemy.name = $"Menu {prefab}";
                foreach (MonoBehaviour behaviour in enemy.GetComponents<MonoBehaviour>())
                    if (!(behaviour is CharacterAnimator))
                        Object.DestroyImmediate(behaviour); // just for show, so no AI
                enemy.GetComponent<Rigidbody>().isKinematic = true;
                enemy.transform.position = at;
                if (prefab == "Guardian")
                    enemy.transform.localScale = Vector3.one * 1.35f;
                enemy.transform.Find("Model").rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            camera.orthographic = false;
            camera.fieldOfView = 36f;
            camera.farClipPlane = 60f;
            camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            Quaternion view = Quaternion.Euler(24f, 30f, 0f);
            Vector3 focus = hero.transform.position + Vector3.up * 1.0f;
            camera.transform.rotation = view;
            // Move the camera so the hero shows between the two menus.
            camera.transform.position = focus - view * Vector3.forward * 7f + view * Vector3.right * 0.35f;
        }

        private static void ApplyAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.24f, 0.31f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.3f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.025f, 0.025f, 0.04f);
            RenderSettings.fogStartDistance = 15f;
            RenderSettings.fogEndDistance = 32f;
        }

        private static void AddMoonAndPostFX(out Light moon)
        {
            var moonObject = new GameObject("Moonlight");
            moonObject.transform.rotation = Quaternion.Euler(52f, 150f, 0f);
            moon = moonObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.58f, 0.68f, 1f);
            moon.intensity = 0.6f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.8f;

            var volume = new GameObject("Post Processing").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RenderSetup.ProfilePath);
        }

        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            (LevelCatalog catalog, UIIcons icons, PanelSettings panel) = LoadSharedAssets();

            // Dim light and fog to make it feel dark and spooky.
            ApplyAtmosphere();

            Camera camera = CreateCamera(new Color(0.02f, 0.02f, 0.035f));
            camera.orthographic = false;
            camera.fieldOfView = 36f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 60f;
            camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            CreateEventSystem();

            // Soft blue moonlight makes the shadows, torches add warm light.
            AddMoonAndPostFX(out Light moon);

            GameObject player = CreatePlayer(Vector3.zero);
            SetValues(follow, ("target", player.transform));
            player.SetActive(false); // turned on when the level finishes loading

            var levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
            SetValues(levelManager, ("catalog", catalog), ("player", player), ("cameraFollow", follow), ("moonLight", moon));

            SetValues(new GameObject("Combat Effects").AddComponent<CombatEffectsPresenter>(),
                ("hitSparks", EffectsBuilder.HitSparks), ("critSparks", EffectsBuilder.CritSparks), ("playerHurt", EffectsBuilder.HurtSparks));

            UIDocument document = CreateDocument("HUD", panel, $"{UIFolder}/Hud.uxml");
            SetValues(document.gameObject.AddComponent<HudController>(),
                ("icons", icons), ("playerInput", player.GetComponent<PlayerInputReader>()));
            document.gameObject.AddComponent<CombatOverlay>();

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 10f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<AudioListener>(); // without this there is no sound
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
