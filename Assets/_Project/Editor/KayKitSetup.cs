using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Sets up the KayKit 3D art (CC0, by Kay Lousberg) and the character animations.
    /// Models are half size so one floor tile is one unit.
    /// </summary>
    public static class KayKitSetup
    {
        public const string Root = "Assets/_Project/Art/KayKit";
        public const string DungeonFolder = Root + "/Dungeon";
        public const string PropsFolder = Root + "/Props";
        private const string MaterialFolder = Root + "/Materials";
        private const string AnimationFolder = Root + "/Animations";
        public const string ControllerFolder = "Assets/_Project/Animation";
        public const float ImportScale = 0.5f;

        private static readonly string[] LoopingClips =
        {
            "Idle_A", "Idle_B", "Running_A", "Running_B", "Walking_A", "Walking_B", "Walking_C",
            "Melee_2H_Idle", "Melee_Unarmed_Idle", "Ranged_Bow_Idle", "Ranged_1H_Aiming", "Ranged_Magic_Spellcasting",
        };

        public static Material Dungeon => LoadMaterial("KayKit_Dungeon");
        public static Material Mage => LoadMaterial("KayKit_Mage");
        public static Material Skeleton => LoadMaterial("KayKit_Skeleton");
        public static Material Glow => LoadMaterial("KayKit_Glow");

        public static void Prepare()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(ControllerFolder);
            CreateMaterials();

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root }))
                ConfigureModel(AssetDatabase.GUIDToAssetPath(guid));

            AssetDatabase.SaveAssets();
        }

        // ---------- Materials ----------

        private static void CreateMaterials()
        {
            MakeLit("KayKit_Dungeon", Texture($"{DungeonFolder}/dungeon_texture.png"), new Color(0.9f, 0.93f, 0.9f), 0.12f);
            MakeLit("KayKit_Mage", Texture($"{Root}/Characters/mage_texture.png"), Color.white, 0.25f);
            MakeLit("KayKit_Knight", Texture($"{Root}/Characters/knight_texture.png"), Color.white, 0.3f);
            MakeLit("KayKit_Skeleton", Texture($"{Root}/Skeletons/skeleton_texture.png"), new Color(0.92f, 0.9f, 0.86f), 0.2f);
            Material glow = MakeLit("KayKit_Glow", null, new Color(0.3f, 0.9f, 1f), 0.5f);
            glow.SetColor("_EmissionColor", new Color(0.3f, 0.9f, 1f) * 4f); // makes skeleton eyes glow blue
            EditorUtility.SetDirty(glow);
        }

        private static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        public static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

        /// <summary>A normal material with glow on, so hits can flash it.</summary>
        public static Material MakeLit(string name, Texture2D texture, Color tint, float smoothness)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material MaterialFor(string sourceName)
        {
            string n = sourceName.ToLowerInvariant();
            if (n.Contains("glow")) return Glow;
            if (n.Contains("skeleton")) return Skeleton;
            if (n.Contains("mage")) return Mage;
            if (n.Contains("knight")) return LoadMaterial("KayKit_Knight");
            return Dungeon; // everything else uses the dungeon texture
        }

        // ---------- Import settings ----------

        private static void ConfigureModel(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            bool character = path.Contains("/Characters/") || path.Contains("/Skeletons/");
            bool animations = path.StartsWith(AnimationFolder);

            importer.globalScale = ImportScale;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            if (character || animations)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = animations;
                importer.optimizeGameObjects = false; // keep the hand bones so we can add weapons
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }

            importer.SaveAndReimport();

            // Swap the model's own materials for ours.
            foreach (Material embedded in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), MaterialFor(embedded.name));

            if (animations)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                foreach (ModelImporterClipAnimation clip in clips)
                    clip.loopTime = LoopingClips.Contains(clip.name);
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
        }

        // ---------- Animation ----------

        public static AnimationClip Clip(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AnimationFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .FirstOrDefault(c => c.name == name);
                if (clip != null)
                    return clip;
            }
            throw new System.InvalidOperationException($"Animation clip {name} not found.");
        }

        /// <summary>
        /// Makes the animation setup for one kind of character: move, attack, cast, hit and die.
        /// </summary>
        public static AnimatorController BuildController(string name, string idle, string walk, string run,
            string attack, string cast, string hit, string death, float attackSpeed = 1.6f)
        {
            string path = $"{ControllerFolder}/{name}.controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(idle), 0f);
            tree.AddChild(Clip(walk), 0.35f);
            tree.AddChild(Clip(run), 1f);
            machine.defaultState = locomotion;

            AddOneShot(machine, locomotion, "Attack", Clip(attack), attackSpeed);
            if (!string.IsNullOrEmpty(cast))
                AddOneShot(machine, locomotion, "Cast", Clip(cast), 1.5f);
            AnimatorState hitState = AddOneShot(machine, locomotion, "Hit", Clip(hit), 1.4f);
            hitState.name = "Hit";

            AnimatorState dead = machine.AddState("Dead");
            dead.motion = Clip(death);
            AnimatorStateTransition die = machine.AddAnyStateTransition(dead);
            die.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            die.duration = 0.1f;
            die.canTransitionToSelf = false;
            AnimatorStateTransition revive = dead.AddTransition(locomotion);
            revive.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            revive.duration = 0f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorState AddOneShot(AnimatorStateMachine machine, AnimatorState back, string trigger, AnimationClip clip, float speed)
        {
            AnimatorState state = machine.AddState(trigger);
            state.motion = clip;
            state.speed = speed;

            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            enter.duration = 0.05f;
            enter.canTransitionToSelf = true;

            AnimatorStateTransition exit = state.AddTransition(back);
            exit.hasExitTime = true;
            exit.exitTime = 0.85f;
            exit.duration = 0.12f;
            return state;
        }

        /// <summary>Finds a bone in the model by its name.</summary>
        public static Transform FindBone(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static GameObject Model(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
                throw new System.InvalidOperationException($"Model {path} is missing.");
            return model;
        }

        public static IEnumerable<string> BoneNames(Transform root) => root.GetComponentsInChildren<Transform>(true).Select(t => t.name);
    }
}
