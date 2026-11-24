using System.IO;
using LostDungeon.Game.Abilities;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Navigation;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Creates the Day 3 enemy assets (arrow, attacks, behaviours, EnemyData, prefabs)
    /// and adds pathfinding and an enemy spawner to the test room.
    /// Existing assets are kept, so Inspector tuning survives a rebuild.
    /// </summary>
    public static class EnemySetupBuilder
    {
        private const string ArrowSpritePath = "Assets/_Project/Art/Generated/Arrow.png";
        private const string ArrowPrefabPath = "Assets/_Project/Prefabs/Arrow.prefab";
        private const string EnemyFolder = "Assets/_Project/ScriptableObjects/Enemies";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
        private const string SpriteSetPath = "Assets/_Project/ScriptableObjects/MaleSpriteSet.asset";
        private const string IdleSpritePath = "Assets/_Project/Art/kenney_isometric-miniature-dungeon/Characters/Male/Male_3_Idle0.png";

        public static void SetUp(Grid grid, Tilemap floor, Tilemap walls, (string enemy, Vector3Int cell)[] spawns)
        {
            AddNavigation(grid, floor, walls);

            // Attacks reuse the player's ability classes: same code, different numbers.
            var warriorStrike = GetOrCreateAbility<MeleeAttackAbility>("WarriorStrike", a => SetValues(a,
                ("displayName", "WarriorStrike"), ("cooldownSeconds", 1.0f), ("damage", 1), ("reach", 0.3f),
                ("radius", 0.28f), ("critChance", 0f)));
            var guardianBash = GetOrCreateAbility<MeleeAttackAbility>("GuardianBash", a => SetValues(a,
                ("displayName", "GuardianBash"), ("cooldownSeconds", 1.6f), ("damage", 2), ("reach", 0.35f),
                ("radius", 0.35f), ("critChance", 0f)));
            var archerShot = GetOrCreateAbility<ProjectileAbility>("ArcherShot", a => SetValues(a,
                ("displayName", "ArcherShot"), ("cooldownSeconds", 1.6f), ("speed", 4.5f), ("damage", 1)));
            SetValues(archerShot, ("projectilePrefab", GetOrCreateArrowPrefab()));

            var melee = GetOrCreate<MeleeEnemyBehaviour>("MeleeBehaviour");
            var ranged = GetOrCreate<RangedEnemyBehaviour>("RangedBehaviour");
            var guardian = GetOrCreate<GuardianEnemyBehaviour>("GuardianBehaviour");

            var warriorData = GetOrCreate<EnemyData>("Warrior", d => SetValues(d,
                ("id", "Warrior"), ("tint", new Color(1f, 0.55f, 0.55f)), ("maxHealth", 3), ("moveSpeed", 1.6f),
                ("detectionRange", 3.5f), ("attackRange", 0.4f), ("behaviour", melee), ("attack", warriorStrike)));
            var archerData = GetOrCreate<EnemyData>("Archer", d => SetValues(d,
                ("id", "Archer"), ("tint", new Color(0.6f, 1f, 0.6f)), ("maxHealth", 2), ("moveSpeed", 1.4f),
                ("detectionRange", 4.5f), ("attackRange", 3f), ("preferredDistance", 2.2f),
                ("behaviour", ranged), ("attack", archerShot)));
            var guardianData = GetOrCreate<EnemyData>("Guardian", d => SetValues(d,
                ("id", "Guardian"), ("tint", new Color(0.55f, 0.7f, 1f)), ("scale", 1.2f), ("maxHealth", 8),
                ("armor", 1), ("moveSpeed", 1.1f), ("detectionRange", 3f), ("attackRange", 0.45f),
                ("guardRadius", 1.8f), ("behaviour", guardian), ("attack", guardianBash)));

            var prefabs = new System.Collections.Generic.Dictionary<string, EnemyController>
            {
                ["Warrior"] = GetOrCreateEnemyPrefab("Warrior", warriorData, 1f),
                ["Archer"] = GetOrCreateEnemyPrefab("Archer", archerData, 1f),
                ["Guardian"] = GetOrCreateEnemyPrefab("Guardian", guardianData, 3f),
            };

            CreateSpawner(grid, prefabs, spawns);
        }

        private static void AddNavigation(Grid grid, Tilemap floor, Tilemap walls)
        {
            var navigation = grid.gameObject.AddComponent<TilemapNavigationGrid>();
            SetValues(navigation, ("floor", floor), ("walls", walls));
        }

        private static void CreateSpawner(Grid grid, System.Collections.Generic.Dictionary<string, EnemyController> prefabs,
            (string enemy, Vector3Int cell)[] spawns)
        {
            var root = new GameObject("Enemies");
            var spawner = root.AddComponent<EnemySpawner>();

            var serialized = new SerializedObject(spawner);
            SerializedProperty list = serialized.FindProperty("spawns");
            list.arraySize = spawns.Length;

            for (int i = 0; i < spawns.Length; i++)
            {
                var point = new GameObject($"{spawns[i].enemy} Spawn").transform;
                point.SetParent(root.transform);
                point.position = grid.GetCellCenterWorld(spawns[i].cell);

                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prefab").objectReferenceValue = prefabs[spawns[i].enemy];
                element.FindPropertyRelative("point").objectReferenceValue = point;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyController GetOrCreateEnemyPrefab(string name, EnemyData data, float mass)
        {
            EnsureFolder(EnemyPrefabFolder);
            string path = $"{EnemyPrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyController>(path);
            if (existing != null)
            {
                ZeroColliderOffsets(path); // repairs prefabs made before the offset fix
                return AssetDatabase.LoadAssetAtPath<EnemyController>(path);
            }

            var root = new GameObject(name);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IdleSpritePath);
            renderer.sortingOrder = 0;
            renderer.color = data.Tint;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = mass;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var feet = root.AddComponent<CircleCollider2D>();
            feet.radius = 0.12f;
            feet.offset = Vector2.zero; // Unity centres a new collider on the sprite; keep it at the feet
            SetValues(root.AddComponent<Faction>(), ("team", (int)Team.Enemy));

            var flash = root.AddComponent<HitFlash>();
            SetValues(flash, ("spriteRenderer", renderer));

            var animator = root.AddComponent<DirectionalSpriteAnimator>();
            SetValues(animator,
                ("sprites", AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(SpriteSetPath)),
                ("body", body));

            var controller = root.AddComponent<EnemyController>();
            SetValues(controller, ("data", data), ("spriteRenderer", renderer), ("spriteAnimator", animator), ("hitFlash", flash));

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<EnemyController>(path);
        }

        private static Projectile GetOrCreateArrowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Projectile>(ArrowPrefabPath);
            if (existing != null)
                return existing;

            var root = new GameObject("Arrow");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.06f;

            root.AddComponent<SortingGroup>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            visual.AddComponent<SpriteRenderer>().sprite = GetOrCreateArrowSprite();

            var projectile = root.AddComponent<Projectile>();
            SetValues(projectile, ("visual", visual.transform), ("rotateVisualToDirection", true));

            PrefabUtility.SaveAsPrefabAsset(root, ArrowPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<Projectile>(ArrowPrefabPath);
        }

        private static Sprite GetOrCreateArrowSprite()
        {
            if (!File.Exists(ArrowSpritePath))
            {
                EnsureFolder(Path.GetDirectoryName(ArrowSpritePath)?.Replace('\\', '/'));
                const int width = 64, height = 12;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var clear = new Color(0f, 0f, 0f, 0f);
                var wood = new Color(0.55f, 0.36f, 0.18f);
                var metal = new Color(0.8f, 0.82f, 0.85f);
                var feather = new Color(0.95f, 0.95f, 0.9f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int fromCenter = Mathf.Abs(y * 2 + 1 - height) / 2; // 0 in the middle rows
                        Color color = clear;
                        if (x >= 50 && fromCenter <= (63 - x) / 2) color = metal;           // arrowhead pointing right
                        else if (x >= 8 && x < 50 && fromCenter == 0) color = wood;          // shaft
                        else if (x < 12 && fromCenter <= 3 && (x + fromCenter) % 3 != 0) color = feather; // fletching
                        texture.SetPixel(x, y, color);
                    }
                }

                File.WriteAllBytes(ArrowSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(ArrowSpritePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 200f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
        }

        private static T GetOrCreate<T>(string assetName, System.Action<T> configureNew = null) where T : ScriptableObject
        {
            EnsureFolder(EnemyFolder);
            string path = $"{EnemyFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            configureNew?.Invoke(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
