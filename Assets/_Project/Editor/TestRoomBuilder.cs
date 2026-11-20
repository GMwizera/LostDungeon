using System.Collections.Generic;
using LostDungeon.Game.Cameras;
using LostDungeon.Game.Hazards;
using LostDungeon.Game.Player;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Sets up the isometric art and builds the TestRoom scene in one click:
    /// reimports sprites, builds the character sprite set, creates tile assets and a
    /// tile palette, then paints a walled room with the player, a trap and a following camera.
    /// Safe to run again: it replaces the Grid, Player and Trap it made before.
    ///
    /// Wall sprites hug one edge of their cell (measured from the art):
    /// _N = lower-left edge, _E = upper-left, _S = upper-right, _W = lower-right.
    /// Walls go in the ring of cells just outside the room, using the variant whose edge touches the room,
    /// so the wall's Grid collider blocks only outside cells.
    /// </summary>
    public static class TestRoomBuilder
    {
        private const string ArtRoot = "Assets/_Project/Art/kenney_isometric-miniature-dungeon";
        private const string IsoFolder = ArtRoot + "/Isometric";
        private const string TilesFolder = "Assets/_Project/Art/Tiles";
        private const string PaletteFolder = "Assets/_Project/Art";
        private const string PaletteName = "Dungeon";
        private const string ScenePath = "Assets/_Project/Scenes/TestRoom.unity";
        private const string SpriteSetPath = "Assets/_Project/ScriptableObjects/MaleSpriteSet.asset";

        private static readonly Vector3 CellSize = new Vector3(1f, 0.5f, 1f);
        private const int RoomWidth = 10;
        private const int RoomHeight = 10;

        private const int FloorOrder = -2;
        private const int FloorDecalOrder = -1;
        private const int ObjectOrder = 0; // walls, props, characters sort against each other by Y

        // Walkable art: no collider.
        private static readonly string[] FloorNames =
        {
            "stoneTile", "stone", "stoneUneven", "stoneMissingTiles", "dirt", "dirtTiles", "planks", "planksBroken", "planksHole",
        };

        // Solid art: Grid collider (the whole cell diamond).
        private static readonly string[] SolidNames =
        {
            "stoneWall", "stoneWallHalf", "stoneWallWindow", "stoneWallDoor", "stoneWallDoorClosed", "stoneWallDoorOpen",
            "stoneWallArchway", "stoneWallCorner", "stoneColumn", "barrel", "barrels", "woodenCrate", "woodenCrates",
            "chestClosed", "chestOpen", "tableRound", "tableShortChairs",
        };

        private static readonly string[] Facings = { "N", "E", "S", "W" };

        [MenuItem("Tools/Lost Dungeon/Build Test Room")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                EditorUtility.DisplayProgressBar("Lost Dungeon", "Reimporting sprites...", 0.1f);
                AssetDatabase.ImportAsset(IsoFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(ArtRoot + "/Characters", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building character sprite set...", 0.4f);
                CharacterSpriteSetBuilder.Build();

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Creating tiles and palette...", 0.6f);
                Dictionary<string, Tile> tiles = CreateTiles();
                CreatePalette(tiles);

                EditorUtility.DisplayProgressBar("Lost Dungeon", "Building TestRoom...", 0.85f);
                BuildScene(tiles);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("Test room built. Press Play to try it.");
        }

        // ---------- Tiles ----------

        private static Dictionary<string, Tile> CreateTiles()
        {
            EnsureFolder(TilesFolder);
            var tiles = new Dictionary<string, Tile>();

            foreach (string name in FloorNames)
                CreateTileSet(name, Tile.ColliderType.None, tiles);
            foreach (string name in SolidNames)
                CreateTileSet(name, Tile.ColliderType.Grid, tiles);

            AssetDatabase.SaveAssets();
            return tiles;
        }

        private static void CreateTileSet(string baseName, Tile.ColliderType collider, Dictionary<string, Tile> tiles)
        {
            foreach (string facing in Facings)
            {
                string key = $"{baseName}_{facing}";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IsoFolder}/{key}.png");
                if (sprite == null)
                {
                    Debug.LogWarning($"Sprite {key} not found, skipping.");
                    continue;
                }

                string tilePath = $"{TilesFolder}/{key}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }

                tile.sprite = sprite;
                tile.colliderType = collider;
                EditorUtility.SetDirty(tile);
                tiles[key] = tile;
            }
        }

        private static void CreatePalette(Dictionary<string, Tile> tiles)
        {
            string path = $"{PaletteFolder}/{PaletteName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                GridPaletteUtility.CreateNewPalette(PaletteFolder, PaletteName, GridLayout.CellLayout.Isometric,
                    GridPalette.CellSizing.Manual, CellSize, GridLayout.CellSwizzle.XYZ,
                    TransparencySortMode.CustomAxis, new Vector3(0f, 1f, 0f));
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
                tilemap.ClearAllTiles();

                // One row per art piece, its four facings side by side.
                var names = new List<string>(FloorNames);
                names.AddRange(SolidNames);
                for (int row = 0; row < names.Count; row++)
                {
                    for (int col = 0; col < Facings.Length; col++)
                    {
                        if (tiles.TryGetValue($"{names[row]}_{Facings[col]}", out Tile tile))
                            tilemap.SetTile(new Vector3Int(col * 2, -row * 2, 0), tile);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Scene ----------

        private static void BuildScene(Dictionary<string, Tile> tiles)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "Grid" || go.name == "Player" || go.name == "Trap")
                    Object.DestroyImmediate(go);
            }

            Grid grid = CreateGrid();
            Tilemap floor = CreateTilemap(grid, "Floor", FloorOrder, TilemapRenderer.Mode.Chunk);
            Tilemap walls = CreateTilemap(grid, "Walls", ObjectOrder, TilemapRenderer.Mode.Individual);
            AddWallCollision(walls.gameObject);

            PaintFloor(floor, tiles);
            PaintWalls(walls, tiles);

            GameObject player = CreatePlayer(grid.GetCellCenterWorld(new Vector3Int(RoomWidth / 2, RoomHeight / 2, 0)));
            CreateTrap(grid.GetCellCenterWorld(new Vector3Int(3, 4, 0)));
            SetUpCamera(player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = player;
        }

        private static Grid CreateGrid()
        {
            var go = new GameObject("Grid");
            var grid = go.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = CellSize;
            return grid;
        }

        private static Tilemap CreateTilemap(Grid grid, string name, int order, TilemapRenderer.Mode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = mode;
            return tilemap;
        }

        private static void AddWallCollision(GameObject walls)
        {
            var body = walls.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var tileCollider = walls.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            walls.AddComponent<CompositeCollider2D>();
        }

        private static void PaintFloor(Tilemap floor, Dictionary<string, Tile> tiles)
        {
            var random = new System.Random(7); // fixed seed: same room every time
            for (int x = 0; x < RoomWidth; x++)
            {
                for (int y = 0; y < RoomHeight; y++)
                {
                    string baseName = random.Next(6) == 0 ? "stoneUneven" : "stoneTile";
                    string facing = Facings[random.Next(Facings.Length)];
                    floor.SetTile(new Vector3Int(x, y, 0), tiles[$"{baseName}_{facing}"]);
                }
            }
        }

        private static void PaintWalls(Tilemap walls, Dictionary<string, Tile> tiles)
        {
            for (int x = 0; x < RoomWidth; x++)
            {
                // Back-left side: full walls, two with windows.
                string back = (x == 2 || x == 7) ? "stoneWallWindow_W" : "stoneWall_W";
                walls.SetTile(new Vector3Int(x, RoomHeight, 0), tiles[back]);
                // Front-right side: half walls so the player stays visible.
                walls.SetTile(new Vector3Int(x, -1, 0), tiles["stoneWallHalf_E"]);
            }

            for (int y = 0; y < RoomHeight; y++)
            {
                walls.SetTile(new Vector3Int(RoomWidth, y, 0), tiles["stoneWall_N"]);  // back-right
                walls.SetTile(new Vector3Int(-1, y, 0), tiles["stoneWallHalf_S"]);     // front-left
            }

            // A couple of props so depth sorting is easy to see.
            walls.SetTile(new Vector3Int(1, 8, 0), tiles["barrel_N"]);
            walls.SetTile(new Vector3Int(8, 8, 0), tiles["woodenCrates_N"]);
        }

        private static GameObject CreatePlayer(Vector3 position)
        {
            var player = new GameObject("Player");
            player.transform.position = position;

            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Characters/Male/Male_3_Idle0.png");
            renderer.sortingOrder = ObjectOrder;

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circle = player.AddComponent<CircleCollider2D>();
            circle.radius = 0.12f;

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerHealth>();

            var animator = player.AddComponent<DirectionalSpriteAnimator>();
            SetField(animator, "sprites", AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(SpriteSetPath));
            SetField(animator, "body", body);

            return player;
        }

        private static void CreateTrap(Vector3 position)
        {
            var trap = new GameObject("Trap");
            trap.transform.position = position;

            var renderer = trap.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IsoFolder}/planksHole_N.png");
            renderer.sortingOrder = FloorDecalOrder;

            var collider = trap.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;

            trap.AddComponent<SpikeTrap>();
        }

        private static void SetUpCamera(Transform target)
        {
            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("No camera in TestRoom; add one and give it CameraFollow.");
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.transform.position = new Vector3(target.position.x, target.position.y, -10f);

            var follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
                follow = camera.gameObject.AddComponent<CameraFollow>();
            SetField(follow, "target", target);
        }

        // ---------- Helpers ----------

        /// <summary>Assigns a private [SerializeField] so the value is saved with the scene.</summary>
        private static void SetField(Object component, string fieldName, Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
