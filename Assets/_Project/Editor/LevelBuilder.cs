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
using UnityEngine.Tilemaps;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Builds the five level prefabs from the layouts below, marks each one Addressable
    /// (address "Level1" ... "Level5", one bundle per level) and creates the LevelCatalog.
    /// </summary>
    public static class LevelBuilder
    {
        private const string LevelFolder = "Assets/_Project/Levels";
        private const string DefinitionFolder = "Assets/_Project/ScriptableObjects/Levels";
        public const string CatalogPath = DefinitionFolder + "/LevelCatalog.asset";
        private const string AddressablesGroup = "Levels";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";

        private class Layout
        {
            public string Name;
            public int Width;
            public int Height;
            public string Floor;
            public string AccentFloor;
            public Vector2Int PlayerSpawn;
            public int DoorX;
            public (string enemy, Vector2Int cell)[] Enemies;
            public Vector2Int[] Obstacles = new Vector2Int[0];
            public string ObstacleTile = "stoneColumn_N";
            public Vector2Int Chest;
            public (string id, int amount)[] ChestLoot;
            public bool RequiresKey;
            public (string id, Vector2Int cell)[] Pickups = new (string, Vector2Int)[0];
            public Vector2Int[] Barrels = new Vector2Int[0];
            public Vector2Int[] Traps = new Vector2Int[0];
            public string RewardUnlock = "";
            public string RewardText;
        }

        private static readonly Layout[] Layouts =
        {
            new Layout
            {
                Name = "The Entrance", Width = 8, Height = 8, Floor = "stoneTile", AccentFloor = "stoneUneven",
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 4,
                Enemies = new[] { ("Warrior", new Vector2Int(6, 6)), ("Warrior", new Vector2Int(1, 6)) },
                Obstacles = new[] { new Vector2Int(3, 4), new Vector2Int(5, 3) },
                Chest = new Vector2Int(6, 1), ChestLoot = new[] { (ItemIds.Gold, 5) },
                Pickups = new[] { (ItemIds.Gold, new Vector2Int(2, 3)), (ItemIds.Potion, new Vector2Int(4, 6)) },
                Barrels = new[] { new Vector2Int(0, 7), new Vector2Int(7, 4) },
                RewardUnlock = LevelManager.UnlockFireball,
                RewardText = "Fireball unlocked! Press Q or right-click to throw it.",
            },
            new Layout
            {
                Name = "Flooded Cellar", Width = 10, Height = 8, Floor = "dirtTiles", AccentFloor = "dirt",
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 7,
                Enemies = new[] { ("Warrior", new Vector2Int(8, 6)), ("Archer", new Vector2Int(2, 6)), ("Warrior", new Vector2Int(8, 2)) },
                Obstacles = new[] { new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(5, 5), new Vector2Int(6, 5) },
                ObstacleTile = "woodenCrates_N",
                Chest = new Vector2Int(0, 7), ChestLoot = new[] { (ItemIds.Gold, 8) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(7, 3)), (ItemIds.Gold, new Vector2Int(2, 2)) },
                Barrels = new[] { new Vector2Int(9, 0), new Vector2Int(3, 7) },
                Traps = new[] { new Vector2Int(5, 2) },
                RewardUnlock = LevelManager.UnlockHealthBonus,
                RewardText = "Your maximum health went up by 2 hearts!",
            },
            new Layout
            {
                Name = "The Armory", Width = 10, Height = 10, Floor = "planks", AccentFloor = "planksBroken",
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 2, RequiresKey = true,
                Enemies = new[] { ("Archer", new Vector2Int(1, 8)), ("Archer", new Vector2Int(8, 8)), ("Guardian", new Vector2Int(7, 2)) },
                Obstacles = new[] { new Vector2Int(3, 5), new Vector2Int(6, 5), new Vector2Int(3, 6), new Vector2Int(6, 6) },
                ObstacleTile = "barrels_N",
                Chest = new Vector2Int(9, 1), ChestLoot = new[] { (ItemIds.Key, 1), (ItemIds.Gold, 10) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(5, 8)) },
                Barrels = new[] { new Vector2Int(0, 4), new Vector2Int(9, 5) },
                Traps = new[] { new Vector2Int(4, 3), new Vector2Int(5, 7) },
                RewardText = "You found the armory's treasure.",
            },
            new Layout
            {
                Name = "Collapsed Halls", Width = 12, Height = 10, Floor = "stoneUneven", AccentFloor = "stoneMissingTiles",
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
            },
            new Layout
            {
                Name = "Guardian's Vault", Width = 12, Height = 12, Floor = "stone", AccentFloor = "stoneTile",
                PlayerSpawn = new Vector2Int(1, 1), DoorX = 6, RequiresKey = true,
                Enemies = new[]
                {
                    ("Guardian", new Vector2Int(9, 9)), ("Guardian", new Vector2Int(6, 9)),
                    ("Warrior", new Vector2Int(3, 7)), ("Warrior", new Vector2Int(8, 3)),
                    ("Archer", new Vector2Int(1, 10)), ("Archer", new Vector2Int(10, 1)),
                },
                Obstacles = new[] { new Vector2Int(3, 3), new Vector2Int(8, 8), new Vector2Int(3, 8), new Vector2Int(8, 5) },
                Chest = new Vector2Int(10, 10), ChestLoot = new[] { (ItemIds.Key, 1), (ItemIds.Gold, 25) },
                Pickups = new[] { (ItemIds.Potion, new Vector2Int(5, 5)), (ItemIds.Potion, new Vector2Int(1, 7)), (ItemIds.Potion, new Vector2Int(10, 5)) },
                Barrels = new[] { new Vector2Int(0, 11), new Vector2Int(11, 0), new Vector2Int(5, 0) },
                Traps = new[] { new Vector2Int(6, 6), new Vector2Int(4, 10) },
                RewardText = "You escaped the Lost Dungeon. Well done!",
            },
        };

        public static LevelCatalog BuildAll(Dictionary<string, Tile> tiles, UIIconsRef icons)
        {
            EnsureFolder(LevelFolder);
            EnsureFolder(DefinitionFolder);
            tiles = TestRoomBuilder.ReloadTiles(tiles); // earlier scene changes may have unloaded them

            var enemyPrefabs = new Dictionary<string, EnemyController>
            {
                ["Warrior"] = AssetDatabase.LoadAssetAtPath<EnemyController>($"{EnemyPrefabFolder}/Warrior.prefab"),
                ["Archer"] = AssetDatabase.LoadAssetAtPath<EnemyController>($"{EnemyPrefabFolder}/Archer.prefab"),
                ["Guardian"] = AssetDatabase.LoadAssetAtPath<EnemyController>($"{EnemyPrefabFolder}/Guardian.prefab"),
            };

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
                BuildLevelPrefab(number, Layouts[i], tiles, enemyPrefabs, icons, prefabPath);
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

        /// <summary>The few icon sprites the levels need (pickups), passed in from the icon factory.</summary>
        public class UIIconsRef
        {
            public Sprite Gold;
            public Sprite Key;
            public Sprite Potion;
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
                ("rewardDescription", layout.RewardText ?? ""));
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

        private static void BuildLevelPrefab(int number, Layout layout, Dictionary<string, Tile> tiles,
            Dictionary<string, EnemyController> enemyPrefabs, UIIconsRef icons, string path)
        {
            var root = new GameObject($"Level{number}");
            var levelRoot = root.AddComponent<LevelRoot>();

            Grid grid = TestRoomBuilder.CreateGrid();
            grid.transform.SetParent(root.transform, false);
            // Individual mode: Chunk mode dropped tiles that come from different textures (black holes).
            Tilemap floor = TestRoomBuilder.CreateTilemap(grid, "Floor", TestRoomBuilder.FloorOrder, TilemapRenderer.Mode.Individual);
            Tilemap walls = TestRoomBuilder.CreateTilemap(grid, "Walls", TestRoomBuilder.ObjectOrder, TilemapRenderer.Mode.Individual);
            TestRoomBuilder.AddWallCollision(walls.gameObject);
            SetValues(grid.gameObject.AddComponent<TilemapNavigationGrid>(), ("floor", floor), ("walls", walls));

            PaintFloor(floor, layout, tiles, number);
            PaintWalls(walls, layout, tiles);
            TestRoomBuilder.BuildWallCollider(walls);

            Vector3 Cell(Vector2Int c) => grid.GetCellCenterWorld(new Vector3Int(c.x, c.y, 0));
            Transform Group(string name)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root.transform, false);
                return t;
            }

            EnemySpawner spawner = CreateSpawner(Group("Enemies"), layout, enemyPrefabs, Cell);

            var objective = Group("Objective").gameObject.AddComponent<LevelObjective>();
            SetValues(objective, ("spawner", spawner), ("requiresKey", layout.RequiresKey));

            Transform props = Group("Props");
            CreateDoor(props, Cell(new Vector2Int(layout.DoorX, layout.Height)), objective);
            CreateChest(props, Cell(layout.Chest), layout.ChestLoot);
            foreach ((string id, Vector2Int cell) in layout.Pickups)
                CreatePickup(props, Cell(cell), id, icons);
            foreach (Vector2Int cell in layout.Barrels)
                CreateBarrel(props, Cell(cell));
            foreach (Vector2Int cell in layout.Traps)
                CreateTrap(props, Cell(cell));

            Transform spawn = Group("PlayerSpawn");
            spawn.position = Cell(layout.PlayerSpawn);
            SetValues(levelRoot, ("playerSpawn", spawn), ("objective", objective));

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void PaintFloor(Tilemap floor, Layout layout, Dictionary<string, Tile> tiles, int seed)
        {
            var random = new System.Random(seed * 31);
            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    string name = random.Next(5) == 0 ? layout.AccentFloor : layout.Floor;
                    string facing = TestRoomBuilder.Facings[random.Next(4)];
                    floor.SetTile(new Vector3Int(x, y, 0), tiles[$"{name}_{facing}"]);
                }
            }
        }

        /// <summary>Walls sit in the ring of cells just outside the room (see TestRoomBuilder for the rule).</summary>
        private static void PaintWalls(Tilemap walls, Layout layout, Dictionary<string, Tile> tiles)
        {
            int w = layout.Width, h = layout.Height;
            for (int x = 0; x < w; x++)
            {
                walls.SetTile(new Vector3Int(x, h, 0), x == layout.DoorX
                    ? tiles[TestRoomBuilder.BlockerTile] // the door object is drawn here; this keeps the gap solid
                    : tiles[x % 4 == 1 ? "stoneWallWindow_W" : "stoneWall_W"]);
                walls.SetTile(new Vector3Int(x, -1, 0), tiles["stoneWallHalf_E"]);
            }

            for (int y = 0; y < h; y++)
            {
                walls.SetTile(new Vector3Int(w, y, 0), tiles["stoneWall_N"]);
                walls.SetTile(new Vector3Int(-1, y, 0), tiles["stoneWallHalf_S"]);
            }

            // Room corners: the wall cells on either side only touch at a point, so block the corner cell too.
            foreach (var corner in new[] { new Vector3Int(-1, -1, 0), new Vector3Int(w, -1, 0), new Vector3Int(-1, h, 0), new Vector3Int(w, h, 0) })
                walls.SetTile(corner, tiles[TestRoomBuilder.BlockerTile]);

            foreach (Vector2Int cell in layout.Obstacles)
                walls.SetTile(new Vector3Int(cell.x, cell.y, 0), tiles[layout.ObstacleTile]);
        }

        private static EnemySpawner CreateSpawner(Transform parent, Layout layout,
            Dictionary<string, EnemyController> prefabs, System.Func<Vector2Int, Vector3> cell)
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
                point.position = cell(at);

                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prefab").objectReferenceValue = prefabs[enemy];
                element.FindPropertyRelative("point").objectReferenceValue = point;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }

        private static SpriteRenderer CreateSprite(Transform parent, string name, Vector3 position, string isoSprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = IsoSprite(isoSprite);
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Sprite IsoSprite(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{TestRoomBuilder.IsoFolder}/{name}.png");

        private static void CreateDoor(Transform parent, Vector3 position, LevelObjective objective)
        {
            SpriteRenderer renderer = CreateSprite(parent, "ExitDoor", position, "stoneWallDoorClosed_W", TestRoomBuilder.ObjectOrder);
            var blocker = renderer.gameObject.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(0.6f, 0.3f);
            blocker.offset = Vector2.zero; // Unity centres a new collider on the sprite; keep it on the floor
            SetValues(renderer.gameObject.AddComponent<ExitDoor>(),
                ("objective", objective), ("spriteRenderer", renderer),
                ("closedSprite", IsoSprite("stoneWallDoorClosed_W")), ("openSprite", IsoSprite("stoneWallDoorOpen_W")));
        }

        private static void CreateChest(Transform parent, Vector3 position, (string id, int amount)[] loot)
        {
            SpriteRenderer renderer = CreateSprite(parent, "Chest", position, "chestClosed_N", TestRoomBuilder.ObjectOrder);
            var chestCollider = renderer.gameObject.AddComponent<CircleCollider2D>();
            chestCollider.radius = 0.2f;
            chestCollider.offset = Vector2.zero;
            var chest = renderer.gameObject.AddComponent<Chest>();
            SetValues(chest, ("spriteRenderer", renderer), ("openSprite", IsoSprite("chestOpen_N")));

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

        private static void CreatePickup(Transform parent, Vector3 position, string itemId, UIIconsRef icons)
        {
            var go = new GameObject($"Pickup ({itemId})");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.15f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = itemId == ItemIds.Potion ? icons.Potion : itemId == ItemIds.Key ? icons.Key : icons.Gold;
            go.AddComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = TestRoomBuilder.ObjectOrder;

            SetValues(go.AddComponent<Pickup>(),
                ("itemId", itemId), ("amount", itemId == ItemIds.Gold ? 3 : 1), ("visual", visual.transform));
        }

        private static void CreateBarrel(Transform parent, Vector3 position)
        {
            SpriteRenderer renderer = CreateSprite(parent, "Barrel", position, "barrel_N", TestRoomBuilder.ObjectOrder);
            var barrelCollider = renderer.gameObject.AddComponent<CircleCollider2D>();
            barrelCollider.radius = 0.18f;
            barrelCollider.offset = Vector2.zero;
            var flash = renderer.gameObject.AddComponent<HitFlash>();
            SetValues(renderer.gameObject.AddComponent<BreakableProp>(), ("hitFlash", flash));
        }

        private static void CreateTrap(Transform parent, Vector3 position)
        {
            SpriteRenderer renderer = CreateSprite(parent, "Trap", position, "planksHole_N", TestRoomBuilder.FloorDecalOrder);
            renderer.gameObject.AddComponent<PolygonCollider2D>().isTrigger = true;
            renderer.gameObject.AddComponent<SpikeTrap>();
        }
    }
}
