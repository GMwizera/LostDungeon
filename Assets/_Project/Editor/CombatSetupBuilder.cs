using System.IO;
using LostDungeon.Game.Abilities;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Interaction;
using LostDungeon.Game.Inventory;
using LostDungeon.Game.Player;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEngine;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Makes the hero, their 3 attacks, and small helpers the other builders use.
    /// </summary>
    public static class CombatSetupBuilder
    {
        private const string AbilityFolder = "Assets/_Project/ScriptableObjects/Abilities";

        public static GameObject CreatePlayer(Vector3 position)
        {
            GameObject player = CharacterBuilder.Create("Player", new CharacterBuilder.Look
            {
                ModelPath = $"{KayKitSetup.Root}/Characters/Mage.fbx",
                Controller = KayKitSetup.BuildController("Hero", "Idle_A", "Walking_A", "Running_A",
                    "Melee_2H_Attack_Spinning", "Ranged_Magic_Shoot", "Hit_A", "Death_A", attackSpeed: 1.7f),
                RightHand = $"{KayKitSetup.PropsFolder}/staff.fbx",
                Ring = EffectsBuilder.RingMaterial("FX_RingHero"),
                Radius = 0.24f,
                RunSpeed = 2.8f,
            });
            player.transform.position = position;

            // Hide the hat so you can see the hero's face from above.
            Transform hat = KayKitSetup.FindBone(player.transform, "Mage_Hat");
            if (hat != null)
                hat.gameObject.SetActive(false);

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerInteractor>();
            SetValues(player.AddComponent<Faction>(), ("team", (int)Team.Player));
            SetValues(player.GetComponent<PlayerHealth>(),
                ("hitFlash", player.GetComponent<HitFlash>()), ("characterAnimator", player.GetComponent<CharacterAnimator>()));

            (AbilityDefinition sword, AbilityDefinition dash, AbilityDefinition fireball) = CreateHeroAbilities();
            SetValues(player.AddComponent<PlayerAbilities>(),
                ("attack", sword), ("dash", dash), ("special", fireball),
                ("characterAnimator", player.GetComponent<CharacterAnimator>()));

            // A light that follows the hero so the dark dungeon is not too dark.
            var torch = new GameObject("Torch");
            torch.transform.SetParent(player.transform, false);
            torch.transform.localPosition = new Vector3(0f, 2.4f, -0.4f);
            var light = torch.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.45f);
            light.intensity = 2.6f;
            light.range = 6.5f;
            light.shadows = LightShadows.None;
            torch.AddComponent<TorchFlicker>();
            return player;
        }

        /// <summary>Makes the hero's 3 attacks and sets their numbers.</summary>
        public static (AbilityDefinition, AbilityDefinition, AbilityDefinition) CreateHeroAbilities()
        {
            var sword = GetOrCreateAbility<MeleeAttackAbility>("Sword");
            SetValues(sword, ("displayName", "Sword"), ("cooldownSeconds", 0.45f), ("animationTrigger", "Attack"),
                ("damage", 1), ("reach", 0.5f), ("radius", 0.55f), ("critChance", 0.2f), ("critMultiplier", 2f),
                ("castEffect", EffectsBuilder.HeroSlash), ("effectDistance", 0.35f), ("effectHeight", 0.55f));

            var dash = GetOrCreateAbility<DashAbility>("Dash");
            SetValues(dash, ("displayName", "Dash"), ("cooldownSeconds", 1.2f), ("animationTrigger", ""),
                ("speed", 10f), ("duration", 0.16f),
                ("castEffect", EffectsBuilder.WindBurst), ("effectDistance", 0f), ("effectHeight", 0.05f));

            var fireball = GetOrCreateAbility<ProjectileAbility>("Fireball");
            SetValues(fireball, ("displayName", "Fireball"), ("cooldownSeconds", 0.8f), ("animationTrigger", "Cast"),
                ("projectilePrefab", AssetDatabase.LoadAssetAtPath<Projectile>(EffectsBuilder.FireballPath)),
                ("speed", 7.5f), ("damage", 2), ("spawnDistance", 0.45f),
                ("castEffect", EffectsBuilder.CastFlash), ("effectDistance", 0.4f), ("effectHeight", 0.7f));

            return (sword, dash, fireball);
        }

        // ---------- Helpers ----------

        internal static T GetOrCreateAbility<T>(string assetName) where T : AbilityDefinition
        {
            EnsureFolder(AbilityFolder);
            string path = $"{AbilityFolder}/{assetName}.asset";
            var ability = AssetDatabase.LoadAssetAtPath<T>(path);
            if (ability != null)
                return ability;

            ability = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ability, path);
            return ability;
        }

        /// <summary>Sets private fields so Unity saves them.</summary>
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
