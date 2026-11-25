using System.Collections.Generic;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Hazards;
using LostDungeon.Game.Interaction;
using LostDungeon.Game.Inventory;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Navigation;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Rendering;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Builds the five 3D level prefabs from the layouts below, marks each one Addressable
    /// (address "Level1" ... "Level5", one bundle per level) and creates the LevelCatalog.
    /// Every cell is one world unit. The room covers cells (0,0) to (Width-1, Height-1);
    /// tall walls stand on the far sides (north and east) and low barriers on the near sides,
    /// so the 45 degree camera can always see in.
    /// </summary>
    public static class LevelBuilder
    {
        private const string LevelFolder = "Assets/_Project/Levels";
        private const string DefinitionFolder = "Assets/_Project/ScriptableObjects/Levels";
        public const string CatalogPath = DefinitionFolder + "/LevelCatalog.asset";
        private const string AddressablesGroup = "Levels";
        private const float WallHeight = 2f;

        private enum Theme { Temple, Cellar, Armory, Ruins, Vault }

        private class Layout
        {
            public string Name;
            public int Width;
            public int Height;
            public Theme Theme;
            public Vector2Int PlayerSpawn;
            public int DoorX;
            public (string enemy, Vector2Int cell)[] Enemies;
            public Vector2Int[] Obstacles = new Vector2Int[0];
            public string Obstacle = "pillar";
            public Vector2Int Chest;
            public (string id, int amount)[] ChestLoot;
            public bool RequiresKey;
            public (string id, Vector2Int cell)[] Pickups = new (string, Vector2Int)[0];
            public Vector2Int[] Barrels = new Vector2Int[0];
            public Vector2Int[] Traps = new Vector2Int[0];
            public string RewardUnlock = "";
            public string RewardText;
            public string RewardTag;
            public string Hostiles;
        }

        private static readonly Layout[] Layouts =
        {
            new Layout
            {
                Name = "The Entrance", Width = 8, Height = 8, Theme = Theme.Temple,
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 4,
                Enemies = new[] { ("Warrior", new Vector2Int(6, 6)), ("Warrior", new Vector2Int(1, 6)) },
                Obstacles = new[] { new Vector2Int(3, 4), new Vector2Int(5, 3) },
                Chest = new Vector2Int(6, 1), ChestLoot = new[] { (ItemIds.Gold, 5) },
                Pickups = new[] { (ItemIds.Gold, new Vector2Int(2, 3)), (ItemIds.Potion, new Vector2Int(4, 6)) },
                Barrels = new[] { new Vector2Int(0, 7), new Vector2Int(7, 4) },
                RewardUnlock = LevelManager.UnlockFireball,
                RewardText = "Fireball unlocked! Press Q or right-click to throw it.",
                RewardTag = "FIREBALL GRANTED", Hostiles = "Skeleton Warriors",
            },
            new Layout
            {
                Name = "Flooded Cellar", Width = 10, Height = 8, Theme = Theme.Cellar,
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 7,
                Enemies = new[] { ("Warrior", new Vector2Int(8, 6)), ("Archer", new Vector2Int(2, 6)), ("Warrior", new Vector2Int(8, 2)) },
                Obstacles = new[] { new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(5, 5), new Vector2Int(6, 5) },
                Obstacle = "crates_stacked",
                Chest = new Vector2Int(0, 7), ChestLoot = new[] { (ItemIds.Gold, 8) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(7, 3)), (ItemIds.Gold, new Vector2Int(2, 2)) },
                Barrels = new[] { new Vector2Int(9, 0), new Vector2Int(3, 7) },
                Traps = new[] { new Vector2Int(5, 2) },
                RewardUnlock = LevelManager.UnlockHealthBonus,
                RewardText = "Your maximum health went up by 2 hearts!",
                RewardTag = "+2 EXTRA HEARTS", Hostiles = "Warriors and a Bone Archer",
            },
            new Layout
            {
                Name = "The Armory", Width = 10, Height = 10, Theme = Theme.Armory,
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 2, RequiresKey = true,
                Enemies = new[] { ("Archer", new Vector2Int(1, 8)), ("Archer", new Vector2Int(8, 8)), ("Guardian", new Vector2Int(7, 2)) },
                Obstacles = new[] { new Vector2Int(3, 5), new Vector2Int(6, 5), new Vector2Int(3, 6), new Vector2Int(6, 6) },
                Obstacle = "barrel_large",
                Chest = new Vector2Int(9, 1), ChestLoot = new[] { (ItemIds.Key, 1), (ItemIds.Gold, 10) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(5, 8)) },
                Barrels = new[] { new Vector2Int(0, 4), new Vector2Int(9, 5) },
                Traps = new[] { new Vector2Int(4, 3), new Vector2Int(5, 7) },
                RewardText = "You found the armory's treasure.",
                RewardTag = "KEY VAULT", Hostiles = "Crypt Guardian and Bone Archers",
            },
            new Layout
            {
                Name = "Collapsed Halls", Width = 12, Height = 10, Theme = Theme.Ruins,
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 10,
                Enemies = new[]
                {
                    ("Warrior", new Vector2Int(6, 8)), ("Warrior", new Vector2Int(10, 3)),
                    ("Archer", new Vector2Int(2, 8)), ("Archer", new Vector2Int(10, 8)),
                },
                Obstacles = new[]
                {
                    new Vector2Int(4, 0), new Vector2Int(4, 1), new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(4, 4), new Vector2Int(4, 5),
                    new Vector2Int(8, 4), new Vector2Int(8, 5), new Vector2Int(8, 6), new Vector2Int(8, 7), new Vector2Int(8, 8), new Vector2Int(8, 9),
                },
                Chest = new Vector2Int(6, 1), ChestLoot = new[] { (ItemIds.Gold, 12) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(2, 5)), (ItemIds.Potion, new Vector2Int(11, 1)), (ItemIds.Gold, new Vector2Int(6, 6)) },
                Barrels = new[] { new Vector2Int(0, 9), new Vector2Int(11, 6) },
                Traps = new[] { new Vector2Int(6, 4), new Vector2Int(2, 2) },
                RewardText = "The halls are clear. One vault remains.",
                RewardTag = "MAZE GOLD", Hostiles = "Warriors and Archers in a maze",
            },
            new Layout
            {
                Name = "Guardian's Vault", Width = 12, Height = 12, Theme = Theme.Vault,
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 6, RequiresKey = true,
                Enemies = new[]
                {
                    ("Guardian", new Vector2Int(9, 9)), ("Guardian", new Vector2Int(6, 9)),
                    ("Warrior", new Vector2Int(3, 7)), ("Warrior", new Vector2Int(8, 3)),
                    ("Archer", new Vector2Int(1, 10)), ("Archer", new Vector2Int(10, 1)),
                },
                Obstacles = new[] { new Vector2Int(3, 3), new Vector2Int(8, 8), new Vector2Int(3, 8), new Vector2Int(8, 5) },
                Obstacle = "pillar_decorated",
                Chest = new Vector2Int(10, 10), ChestLoot = new[] { (ItemIds.Key, 1), (ItemIds.Gold, 25) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(5, 5)), (ItemIds.Potion, new Vector2Int(1, 7)), (ItemIds.Potion, new Vector2Int(10, 5)) },
                Barrels = new[] { new Vector2Int(0, 11), new Vector2Int(11, 0), new Vector2Int(5, 0) },
                Traps = new[] { new Vector2Int(6, 6), new Vector2Int(4, 10) },
                RewardText = "You escaped the Lost Dungeon. Well done!",
                RewardTag = "FINAL BREACH", Hostiles = "Two Crypt Guardians and the full garrison",
            },
        };

        public static LevelCatalog BuildAll(Dictionary<string, EnemyController> enemyPrefabs)
        {
            EnsureFolder(LevelFolder);
            EnsureFolder(DefinitionFolder);

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var definitions = new List<LevelDefinition>();
            for (int i = 0; i < Layouts.Length; i++)
            {
                int number = i + 1;
                string prefabPath = $"{LevelFolder}/Level{number}.prefab";
                BuildLevelPrefab(number, Layouts[i], enemyPrefabs, prefabPath);
                MakeAddressable(prefabPath, $"Level{number}");
                definitions.Add(CreateDefinition(number, Layouts[i]));
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("levels");
            list.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static LevelDefinition CreateDefinition(int number, Layout layout)
        {
            string path = $"{DefinitionFolder}/Level{number}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            SetValues(definition,
                ("number", number),
                ("displayName", layout.Name),
                ("addressKey", $"Level{number}"),
                ("rewardUnlock", layout.RewardUnlock ?? ""),
                ("rewardDescription", layout.RewardText ?? ""),
                ("rewardTag", layout.RewardTag ?? ""),
                ("hostiles", layout.Hostiles ?? ""));
            return definition;
        }

        private static void MakeAddressable(string assetPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetGroup group = settings.FindGroup(AddressablesGroup);
            if (group == null)
            {
                group = settings.CreateGroup(AddressablesGroup, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            // One bundle per level: only the level being played is loaded into memory.
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel("level", true, true, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true, true);
        }

        // ---------- Level prefab ----------

        private static void BuildLevelPrefab(int number, Layout layout, Dictionary<string, EnemyController> enemyPrefabs, string path)
        {
            var random = new System.Random(number * 97);
            var root = new GameObject($"Level{number}");
            var levelRoot = root.AddComponent<LevelRoot>();

            Transform Group(string name)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root.transform, false);
                return t;
            }

            var blocked = new HashSet<Vector2Int>(layout.Obstacles) { layout.Chest };
            var trapCells = new HashSet<Vector2Int>(layout.Traps);

            BuildFloor(Group("Floor"), layout, trapCells, random);
            BuildWalls(Group("Walls"), layout, random);
            BuildCollision(Group("Collision"), layout);

            Transform obstacles = Group("Obstacles");
            foreach (Vector2Int cell in layout.Obstacles)
                CreateObstacle(obstacles, layout.Obstacle, cell, random);

            var navigation = Group("Navigation").gameObject.AddComponent<LevelNavigationGrid>();
            var open = new List<bool>();
            for (int y = 0; y < layout.Height; y++)
                for (int x = 0; x < layout.Width; x++)
                    open.Add(!blocked.Contains(new Vector2Int(x, y)));
            navigation.Configure(layout.Width, layout.Height, open);

            EnemySpawner spawner = CreateSpawner(Group("Enemies"), layout, enemyPrefabs);
            var objective = Group("Objective").gameObject.AddComponent<LevelObjective>();
            SetValues(objective, ("spawner", spawner), ("requiresKey", layout.RequiresKey));

            Transform props = Group("Props");
            CreateDoor(props, layout, objective);
            CreateChest(props, layout.Chest, layout.ChestLoot, layout);
            foreach ((string id, Vector2Int cell) in layout.Pickups)
                CreatePickup(props, cell, id);
            foreach (Vector2Int cell in layout.Barrels)
                CreateBarrel(props, cell);
            foreach (Vector2Int cell in layout.Traps)
                CreateTrap(props, cell);

            Transform spawn = Group("PlayerSpawn");
            spawn.position = Cell(layout.PlayerSpawn);
            SetValues(levelRoot, ("playerSpawn", spawn), ("objective", objective));

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static Vector3 Cell(Vector2Int cell) => new Vector3(cell.x + 0.5f, 0f, cell.y + 0.5f);

        /// <summary>Places a KayKit dungeon model (already imported at the 1 tile = 1 unit scale).</summary>
        internal static GameObject Place(Transform parent, string model, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(KayKitSetup.Model($"{KayKitSetup.DungeonFolder}/{model}.fbx"));
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        // ---------- Floor ----------

        private static string FloorTile(Theme theme, System.Random random)
        {
            int roll = random.Next(100);
            switch (theme)
            {
                case Theme.Cellar:
                    return roll < 30 ? "floor_dirt_small_A" : roll < 55 ? "floor_dirt_small_B" : roll < 80 ? "floor_dirt_small_C" : "floor_dirt_small_weeds";
                case Theme.Armory:
                    return roll < 70 ? "floor_wood_small" : "floor_wood_small_dark";
                case Theme.Ruins:
                    return roll < 45 ? "floor_tile_small" : roll < 65 ? "floor_tile_small_broken_A" : roll < 85 ? "floor_tile_small_broken_B" : "floor_tile_small_weeds_A";
                case Theme.Vault:
                    return roll < 80 ? "floor_tile_small" : "floor_tile_small_weeds_B";
                default: // Temple: mossy stone, like the reference art
                    return roll < 60 ? "floor_tile_small" : roll < 80 ? "floor_tile_small_weeds_A" : roll < 95 ? "floor_tile_small_weeds_B" : "floor_tile_small_decorated";
            }
        }

        private static void BuildFloor(Transform parent, Layout layout, HashSet<Vector2Int> trapCells, System.Random random)
        {
            // One ring of extra floor under the walls, so they never stand over the void.
            for (int x = -1; x <= layout.Width; x++)
            {
                for (int y = -1; y <= layout.Height; y++)
                {
                    if (trapCells.Contains(new Vector2Int(x, y)))
                        continue; // the trap has its own floor plate
                    GameObject tile = Place(parent, FloorTile(layout.Theme, random), Cell(new Vector2Int(x, y)), 90f * random.Next(4));
                    foreach (Renderer r in tile.GetComponentsInChildren<Renderer>())
                        r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        // ---------- Walls ----------

        private static void BuildWalls(Transform parent, Layout layout, System.Random random)
        {
            int w = layout.Width, h = layout.Height;
            int doorSegment = layout.DoorX / 2;

            // North (far) wall, along X. Segments are 2 cells long.
            for (int k = 0; k < w / 2; k++)
            {
                float x = 2 * k + 1;
                if (k == doorSegment)
                    continue; // the exit door is built separately
                Place(parent, WallPiece(random), new Vector3(x, 0f, h + 0.25f));
                if (k % 2 == 0)
                    AddTorch(parent, new Vector3(x, 0f, h), 180f, layout.Theme);
                else
                    AddBanner(parent, new Vector3(x, 0f, h), 180f, random);
            }

            // East (far) wall, along Z.
            for (int k = 0; k < h / 2; k++)
            {
                float z = 2 * k + 1;
                Place(parent, WallPiece(random), new Vector3(w + 0.25f, 0f, z), 90f);
                if (k % 2 == 1)
                    AddTorch(parent, new Vector3(w, 0f, z), 270f, layout.Theme);
            }

            // Corner and end pillars.
            Place(parent, "pillar", new Vector3(w + 0.25f, 0f, h + 0.25f));
            Place(parent, "pillar", new Vector3(-0.25f, 0f, h + 0.25f));
            Place(parent, "pillar", new Vector3(w + 0.25f, 0f, -0.25f));

            // South and west (near) sides: knee-high barriers, so the camera sees over them.
            for (int k = 0; k < w / 2; k++)
                Place(parent, "barrier", new Vector3(2 * k + 1, 0f, -0.15f));
            for (int k = 0; k < h / 2; k++)
                Place(parent, "barrier", new Vector3(-0.15f, 0f, 2 * k + 1), 90f);
            Place(parent, "barrier_column", new Vector3(-0.15f, 0f, -0.15f));
        }

        private static string WallPiece(System.Random random)
        {
            int roll = random.Next(100);
            return roll < 70 ? "wall" : roll < 85 ? "wall_cracked" : "wall_window_closed";
        }

        internal static void AddTorch(Transform parent, Vector3 wallFoot, float yaw) => AddTorch(parent, wallFoot, yaw, Theme.Temple);

        private static void AddTorch(Transform parent, Vector3 wallFoot, float yaw, Theme theme)
        {
            Vector3 inward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Place(parent, "torch_mounted", wallFoot + Vector3.up * 1.15f, yaw);

            var go = new GameObject("Torch Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = wallFoot + Vector3.up * 1.55f + inward * 0.45f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = theme == Theme.Vault ? new Color(1f, 0.62f, 0.32f) : new Color(1f, 0.58f, 0.26f);
            light.intensity = 2.4f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            go.AddComponent<TorchFlicker>();
        }

        private static void AddBanner(Transform parent, Vector3 wallFoot, float yaw, System.Random random)
        {
            string[] banners = { "banner_patternA_red", "banner_patternB_blue", "banner_shield_red", "banner_thin_red" };
            Place(parent, banners[random.Next(banners.Length)], wallFoot + Vector3.up * 0f, yaw);
        }

        /// <summary>
        /// Invisible solid boxes around the room. The visible barriers on the near side are low,
        /// but the collision is a full wall on every side, so nothing can leave the room.
        /// </summary>
        private static void BuildCollision(Transform parent, Layout layout)
        {
            float w = layout.Width, h = layout.Height;
            AddBox(parent, "North", new Vector3(w / 2f, WallHeight / 2f, h + 0.25f), new Vector3(w + 1f, WallHeight, 0.5f));
            AddBox(parent, "South", new Vector3(w / 2f, WallHeight / 2f, -0.25f), new Vector3(w + 1f, WallHeight, 0.5f));
            AddBox(parent, "East", new Vector3(w + 0.25f, WallHeight / 2f, h / 2f), new Vector3(0.5f, WallHeight, h + 1f));
            AddBox(parent, "West", new Vector3(-0.25f, WallHeight / 2f, h / 2f), new Vector3(0.5f, WallHeight, h + 1f));
        }

        private static BoxCollider AddBox(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            return box;
        }

        // ---------- Obstacles and props ----------

        private static void CreateObstacle(Transform parent, string model, Vector2Int cell, System.Random random)
        {
            float scale = model == "crates_stacked" ? 0.85f : model == "barrel_large" ? 0.95f : 1f;
            GameObject go = Place(parent, model, Cell(cell), 90f * random.Next(4), scale);
            go.name = $"Obstacle {cell}";
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, WallHeight / 2f, 0f) / scale;
            box.size = new Vector3(0.82f, WallHeight, 0.82f) / scale;
        }

        private static void CreateDoor(Transform parent, Layout layout, LevelObjective objective)
        {
            int segment = layout.DoorX / 2;
            Vector3 position = new Vector3(2 * segment + 1, 0f, layout.Height + 0.25f);
            GameObject doorway = Place(parent, "wall_doorway", position);
            doorway.name = "ExitDoor";

            var blocker = doorway.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, WallHeight / 2f, 0f);
            blocker.size = new Vector3(2f, WallHeight, 0.5f);

            // Warm light beyond the doorway: faint while sealed, bright once the way out is open.
            var glow = new GameObject("Exit Glow");
            glow.transform.SetParent(doorway.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.2f, 0.9f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.82f, 0.5f);
            light.intensity = 0.6f;
            light.range = 6f;
            light.shadows = LightShadows.None;

            Transform door = doorway.transform.Find("wall_doorway_door");
            SetValues(doorway.AddComponent<ExitDoor>(), ("objective", objective), ("door", door), ("glow", light));
        }

        private static void CreateChest(Transform parent, Vector2Int cell, (string id, int amount)[] loot, Layout layout)
        {
            // Face the middle of the room.
            Vector2 toCentre = new Vector2(layout.Width / 2f - cell.x, layout.Height / 2f - cell.y);
            float yaw = Mathf.Round(Mathf.Atan2(toCentre.x, toCentre.y) * Mathf.Rad2Deg / 90f) * 90f;
            GameObject chestObject = Place(parent, "chest", Cell(cell), yaw);
            chestObject.name = "Chest";

            var box = chestObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.35f, 0f);
            box.size = new Vector3(0.85f, 0.7f, 0.75f);

            var glowObject = new GameObject("Treasure Glow");
            glowObject.transform.SetParent(chestObject.transform, false);
            glowObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var glow = glowObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.78f, 0.3f);
            glow.intensity = 2.5f;
            glow.range = 2.5f;

            var chest = chestObject.AddComponent<Chest>();
            SetValues(chest, ("lid", chestObject.transform.Find("chest_lid")), ("glow", glow));

            var serialized = new SerializedObject(chest);
            SerializedProperty list = serialized.FindProperty("loot");
            list.arraySize = loot.Length;
            for (int i = 0; i < loot.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("itemId").stringValue = loot[i].id;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = loot[i].amount;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePickup(Transform parent, Vector2Int cell, string itemId)
        {
            var go = new GameObject($"Pickup ({itemId})");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Cell(cell);

            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.35f;
            trigger.center = new Vector3(0f, 0.4f, 0f);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            visual.localPosition = new Vector3(0f, 0.3f, 0f);
            string model = itemId == ItemIds.Potion ? "bottle_A_green" : itemId == ItemIds.Key ? "key" : "coin_stack_small";
            float scale = itemId == ItemIds.Potion ? 1.4f : itemId == ItemIds.Key ? 1.2f : 1f;
            Place(visual, model, Vector3.zero, 0f, scale);

            var glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(go.transform, false);
            glowObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var glow = glowObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = itemId == ItemIds.Potion ? new Color(0.35f, 1f, 0.5f) : new Color(1f, 0.8f, 0.3f);
            glow.intensity = 1.6f;
            glow.range = 1.6f;

            SetValues(go.AddComponent<Pickup>(),
                ("itemId", itemId), ("amount", itemId == ItemIds.Gold ? 3 : 1), ("visual", visual));
        }

        private static void CreateBarrel(Transform parent, Vector2Int cell)
        {
            GameObject barrel = Place(parent, "barrel_small", Cell(cell), Random.Range(0f, 360f), 1.1f);
            barrel.name = "Barrel";
            var collider = barrel.AddComponent<CapsuleCollider>();
            collider.radius = 0.25f;
            collider.height = 0.6f;
            collider.center = new Vector3(0f, 0.3f, 0f);
            var flash = barrel.AddComponent<HitFlash>();
            SetValues(barrel.AddComponent<BreakableProp>(), ("hitFlash", flash), ("breakEffect", EffectsBuilder.BreakDust));
        }

        private static void CreateTrap(Transform parent, Vector2Int cell)
        {
            GameObject trap = Place(parent, "floor_tile_big_spikes", Cell(cell), 0f, 0.5f); // the plate is 2x2 cells: halve it
            trap.name = "Spike Trap";
            var trigger = trap.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.5f, 0f);
            trigger.size = new Vector3(1.7f, 1f, 1.7f); // in the trap's own (halved) scale: about one cell
            SetValues(trap.AddComponent<SpikeTrap>(), ("spikes", trap.transform.Find("spikes")), ("hiddenDepth", 0.9f));
        }

        private static EnemySpawner CreateSpawner(Transform parent, Layout layout, Dictionary<string, EnemyController> prefabs)
        {
            var spawner = parent.gameObject.AddComponent<EnemySpawner>();
            var serialized = new SerializedObject(spawner);
            SerializedProperty list = serialized.FindProperty("spawns");
            list.arraySize = layout.Enemies.Length;

            for (int i = 0; i < layout.Enemies.Length; i++)
            {
                (string enemy, Vector2Int at) = layout.Enemies[i];
                var point = new GameObject($"{enemy} Spawn").transform;
                point.SetParent(parent, false);
                point.position = Cell(at);

                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prefab").objectReferenceValue = prefabs[enemy];
                element.FindPropertyRelative("point").objectReferenceValue = point;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }
    }
}
