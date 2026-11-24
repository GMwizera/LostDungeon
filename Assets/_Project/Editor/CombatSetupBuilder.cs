using System.IO;
using LostDungeon.Game.Abilities;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Player;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Creates the Day 2 combat assets (fireball sprite + prefab, three ability assets),
    /// gives the player its abilities, and places breakable barrels to practise on.
    /// Existing assets are kept, so tuning done in the Inspector is not overwritten.
    /// </summary>
    public static class CombatSetupBuilder
    {
        private const string GeneratedArtFolder = "Assets/_Project/Art/Generated";
        private const string FireballSpritePath = GeneratedArtFolder + "/Fireball.png";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string FireballPrefabPath = PrefabFolder + "/Fireball.prefab";
        private const string AbilityFolder = "Assets/_Project/ScriptableObjects/Abilities";
        private const string BarrelSpritePath = "Assets/_Project/Art/kenney_isometric-miniature-dungeon/Isometric/barrel_N.png";

        public static void SetUpPlayer(GameObject player)
        {
            Projectile fireballPrefab = GetOrCreateFireballPrefab();

            var sword = GetOrCreateAbility<MeleeAttackAbility>("Sword", ability =>
                SetValues(ability, ("displayName", "Sword"), ("cooldownSeconds", 0.4f), ("playsActionAnimation", true)));
            var dash = GetOrCreateAbility<DashAbility>("Dash", ability =>
                SetValues(ability, ("displayName", "Dash"), ("cooldownSeconds", 1.2f)));
            var fireball = GetOrCreateAbility<ProjectileAbility>("Fireball", ability =>
                SetValues(ability, ("displayName", "Fireball"), ("cooldownSeconds", 0.8f), ("playsActionAnimation", true)));
            SetValues(fireball, ("projectilePrefab", fireballPrefab));

            // TryGetComponent, not "?? AddComponent": in the Editor a missing component is a fake null that ?? doesn't catch.
            if (!player.TryGetComponent(out HitFlash flash))
                flash = player.AddComponent<HitFlash>();
            SetValues(player.GetComponent<PlayerHealth>(), ("hitFlash", flash));

            if (!player.TryGetComponent(out Faction faction))
                faction = player.AddComponent<Faction>();
            SetValues(faction, ("team", (int)Team.Player));

            if (!player.TryGetComponent(out PlayerAbilities abilities))
                abilities = player.AddComponent<PlayerAbilities>();
            SetValues(abilities,
                ("attack", sword),
                ("dash", dash),
                ("special", fireball),
                ("spriteAnimator", player.GetComponent<DirectionalSpriteAnimator>()));
        }

        public static void CreateBreakables(Grid grid, Vector3Int[] cells)
        {
            var parent = new GameObject("Breakables").transform;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarrelSpritePath);

            foreach (Vector3Int cell in cells)
            {
                var barrel = new GameObject("Barrel");
                barrel.transform.SetParent(parent);
                barrel.transform.position = grid.GetCellCenterWorld(cell);

                var renderer = barrel.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 0;

                var collider = barrel.AddComponent<CircleCollider2D>();
                collider.radius = 0.18f;
                collider.offset = Vector2.zero; // Unity centres a new collider on the sprite; keep it on the floor

                var flash = barrel.AddComponent<HitFlash>();
                var prop = barrel.AddComponent<BreakableProp>();
                SetValues(prop, ("hitFlash", flash));
            }
        }

        // ---------- Fireball ----------

        private static Projectile GetOrCreateFireballPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Projectile>(FireballPrefabPath);
            if (existing != null)
                return existing;

            Sprite sprite = GetOrCreateFireballSprite();
            EnsureFolder(PrefabFolder);

            // Root sits at ground level so it hits things at their feet; the glowing visual floats above it.
            var root = new GameObject("Fireball");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.08f;

            root.AddComponent<SortingGroup>(); // sort by the root's ground position, not the floating visual
            root.AddComponent<Projectile>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            visual.AddComponent<SpriteRenderer>().sprite = sprite;

            var light = visual.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.intensity = 1.2f;
            light.pointLightOuterRadius = 0.9f;
            light.pointLightInnerRadius = 0.1f;

            PrefabUtility.SaveAsPrefabAsset(root, FireballPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<Projectile>(FireballPrefabPath);
        }

        private static Sprite GetOrCreateFireballSprite()
        {
            if (!File.Exists(FireballSpritePath))
            {
                EnsureFolder(GeneratedArtFolder);
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var core = new Color(1f, 0.95f, 0.6f);
                var edge = new Color(1f, 0.35f, 0.05f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                        float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.4f);
                        Color color = Color.Lerp(core, edge, Mathf.Clamp01(distance * 1.6f));
                        color.a = alpha;
                        texture.SetPixel(x, y, color);
                    }
                }

                File.WriteAllBytes(FireballSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(FireballSpritePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(FireballSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 200f; // about 0.3 units across
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(FireballSpritePath);
        }

        // ---------- Helpers ----------

        internal static T GetOrCreateAbility<T>(string assetName, System.Action<T> configureNew) where T : AbilityDefinition
        {
            EnsureFolder(AbilityFolder);
            string path = $"{AbilityFolder}/{assetName}.asset";
            var ability = AssetDatabase.LoadAssetAtPath<T>(path);
            if (ability != null)
                return ability;

            ability = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ability, path);
            configureNew(ability);
            AssetDatabase.SaveAssets();
            return ability;
        }

        /// <summary>Sets private [SerializeField] values so they are saved with the asset or scene.</summary>
        internal static void SetValues(Object target, params (string field, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.");
                    continue;
                }

                switch (value)
                {
                    case Object reference: property.objectReferenceValue = reference; break;
                    case float number: property.floatValue = number; break;
                    case int whole: property.intValue = whole; break;
                    case Color color: property.colorValue = color; break;
                    case bool flag: property.boolValue = flag; break;
                    case string text: property.stringValue = text; break;
                    case null: property.objectReferenceValue = null; break;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Puts every collider in a prefab back at the object's origin (its feet). Adding a collider to an
        /// object with a sprite makes Unity centre it on the sprite, which for these tall sprites is far above the floor.
        /// </summary>
        internal static void ZeroColliderOffsets(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (Collider2D collider in root.GetComponents<Collider2D>())
                    collider.offset = Vector2.zero;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
