using System.Collections.Generic;
using LostDungeon.Game.Abilities;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Enemies;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEngine;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Creates the three enemy types as data (attacks, behaviours, EnemyData) and as 3D prefabs:
    /// a skeleton warrior, a skeleton archer with a crossbow, and a large armoured skeleton guardian.
    /// Their numbers are written every build so they match the 3D scale.
    /// </summary>
    public static class EnemySetupBuilder
    {
        private const string EnemyFolder = "Assets/_Project/ScriptableObjects/Enemies";
        public const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";

        public static Dictionary<string, EnemyController> BuildAll()
        {
            // Attacks reuse the player's ability classes: same code, different numbers.
            var warriorStrike = GetOrCreateAbility<MeleeAttackAbility>("WarriorStrike");
            SetValues(warriorStrike, ("displayName", "WarriorStrike"), ("cooldownSeconds", 1.1f), ("animationTrigger", "Attack"),
                ("damage", 1), ("reach", 0.45f), ("radius", 0.45f), ("critChance", 0f),
                ("castEffect", EffectsBuilder.EnemySlash), ("effectDistance", 0.35f), ("effectHeight", 0.55f));
            var guardianBash = GetOrCreateAbility<MeleeAttackAbility>("GuardianBash");
            SetValues(guardianBash, ("displayName", "GuardianBash"), ("cooldownSeconds", 1.7f), ("animationTrigger", "Attack"),
                ("damage", 2), ("reach", 0.6f), ("radius", 0.6f), ("critChance", 0f),
                ("castEffect", EffectsBuilder.EnemySlash), ("effectDistance", 0.5f), ("effectHeight", 0.6f));
            var archerShot = GetOrCreateAbility<ProjectileAbility>("ArcherShot");
            SetValues(archerShot, ("displayName", "ArcherShot"), ("cooldownSeconds", 1.8f), ("animationTrigger", "Attack"),
                ("projectilePrefab", AssetDatabase.LoadAssetAtPath<Projectile>(EffectsBuilder.ArrowPath)),
                ("speed", 6f), ("damage", 1), ("spawnDistance", 0.4f), ("castEffect", null));

            var melee = GetOrCreate<MeleeEnemyBehaviour>("MeleeBehaviour");
            var ranged = GetOrCreate<RangedEnemyBehaviour>("RangedBehaviour");
            var guardian = GetOrCreate<GuardianEnemyBehaviour>("GuardianBehaviour");

            var warriorData = GetOrCreate<EnemyData>("Warrior");
            SetValues(warriorData, ("id", "Warrior"), ("displayName", "Skeleton Warrior"), ("level", 2),
                ("tint", new Color(1f, 0.92f, 0.88f)), ("scale", 1f), ("maxHealth", 3), ("armor", 0), ("moveSpeed", 2f),
                ("detectionRange", 4.5f), ("attackRange", 0.75f), ("behaviour", melee), ("attack", warriorStrike));
            var archerData = GetOrCreate<EnemyData>("Archer");
            SetValues(archerData, ("id", "Archer"), ("displayName", "Bone Archer"), ("level", 3),
                ("tint", new Color(0.85f, 1f, 0.9f)), ("scale", 1f), ("maxHealth", 2), ("armor", 0), ("moveSpeed", 1.7f),
                ("detectionRange", 6f), ("attackRange", 4.2f), ("preferredDistance", 3f), ("behaviour", ranged), ("attack", archerShot));
            var guardianData = GetOrCreate<EnemyData>("Guardian");
            SetValues(guardianData, ("id", "Guardian"), ("displayName", "Crypt Guardian"), ("level", 5),
                ("tint", new Color(0.82f, 0.88f, 1f)), ("scale", 1.35f), ("maxHealth", 8), ("armor", 1), ("moveSpeed", 1.4f),
                ("detectionRange", 4f), ("attackRange", 0.95f), ("guardRadius", 2.6f), ("behaviour", guardian), ("attack", guardianBash));

            EnsureFolder(EnemyPrefabFolder);
            Material ring = EffectsBuilder.RingMaterial("FX_RingEnemy");
            return new Dictionary<string, EnemyController>
            {
                ["Warrior"] = BuildPrefab("Warrior", warriorData, 1f, new CharacterBuilder.Look
                {
                    ModelPath = $"{KayKitSetup.Root}/Skeletons/Skeleton_Minion.fbx",
                    Controller = KayKitSetup.BuildController("SkeletonWarrior", "Idle_B", "Walking_B", "Running_A",
                        "Melee_1H_Attack_Chop", null, "Hit_B", "Death_A", attackSpeed: 1.5f),
                    RightHand = $"{KayKitSetup.PropsFolder}/Skeleton_Blade.fbx",
                    LeftHand = $"{KayKitSetup.PropsFolder}/Skeleton_Shield_Small_A.fbx",
                    Ring = ring, Radius = 0.24f, RunSpeed = 2f,
                }),
                ["Archer"] = BuildPrefab("Archer", archerData, 1f, new CharacterBuilder.Look
                {
                    ModelPath = $"{KayKitSetup.Root}/Skeletons/Skeleton_Rogue.fbx",
                    Controller = KayKitSetup.BuildController("SkeletonArcher", "Idle_A", "Walking_A", "Running_A",
                        "Ranged_1H_Shoot", null, "Hit_A", "Death_A", attackSpeed: 1.4f),
                    RightHand = $"{KayKitSetup.PropsFolder}/Skeleton_Crossbow.fbx",
                    Ring = ring, Radius = 0.22f, RunSpeed = 1.7f,
                }),
                ["Guardian"] = BuildPrefab("Guardian", guardianData, 3f, new CharacterBuilder.Look
                {
                    ModelPath = $"{KayKitSetup.Root}/Skeletons/Skeleton_Warrior.fbx",
                    Controller = KayKitSetup.BuildController("SkeletonGuardian", "Melee_2H_Idle", "Walking_A", "Running_B",
                        "Melee_2H_Attack_Chop", null, "Hit_A", "Death_B", attackSpeed: 1.5f),
                    RightHand = $"{KayKitSetup.PropsFolder}/Skeleton_Axe.fbx",
                    LeftHand = $"{KayKitSetup.PropsFolder}/Skeleton_Shield_Large_A.fbx",
                    Ring = ring, Radius = 0.26f, RunSpeed = 1.4f,
                }),
            };
        }

        private static EnemyController BuildPrefab(string name, EnemyData data, float mass, CharacterBuilder.Look look)
        {
            GameObject root = CharacterBuilder.Create(name, look);
            root.GetComponent<Rigidbody>().mass = mass;
            SetValues(root.AddComponent<Faction>(), ("team", (int)Team.Enemy));

            var controller = root.AddComponent<EnemyController>();
            SetValues(controller, ("data", data), ("characterAnimator", root.GetComponent<CharacterAnimator>()),
                ("hitFlash", root.GetComponent<HitFlash>()));

            string path = $"{EnemyPrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<EnemyController>(path);
        }

        private static T GetOrCreate<T>(string assetName) where T : ScriptableObject
        {
            EnsureFolder(EnemyFolder);
            string path = $"{EnemyFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
