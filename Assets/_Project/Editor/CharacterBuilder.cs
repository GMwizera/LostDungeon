using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Puts a 3D character together: a body for physics, the model, a weapon and a floor ring.
    /// </summary>
    public static class CharacterBuilder
    {
        public class Look
        {
            public string ModelPath;
            public AnimatorController Controller;
            public string RightHand;   // weapon, can be empty
            public string LeftHand;    // shield or book, can be empty
            public Material Ring;
            public float Radius = 0.25f;
            public float RunSpeed = 2.8f;
        }

        public static GameObject Create(string name, Look look)
        {
            var root = new GameObject(name);

            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 1f;
            body.linearDamping = 0f;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = look.Radius;
            capsule.height = 1.2f;
            capsule.center = new Vector3(0f, 0.6f, 0f);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(KayKitSetup.Model(look.ModelPath));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * 1.15f; // a bit bigger so characters are easy to see

            if (!model.TryGetComponent(out Animator animator))
                animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = look.Controller;
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(look.ModelPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true; // stops the model vanishing at the screen edge
            }

            Attach(model.transform, look.RightHand, "handslot.r", "hand.r", "wrist.r");
            Attach(model.transform, look.LeftHand, "handslot.l", "hand.l", "wrist.l");

            if (look.Ring != null)
                AddRing(root.transform, look.Ring, look.Radius * 4.2f);

            root.AddComponent<HitFlash>();
            var characterAnimator = root.AddComponent<CharacterAnimator>();
            SetValues(characterAnimator, ("animator", animator), ("model", model.transform), ("body", body), ("runSpeed", look.RunSpeed));
            return root;
        }

        private static void Attach(Transform model, string itemPath, params string[] bones)
        {
            if (string.IsNullOrEmpty(itemPath))
                return;
            Transform bone = KayKitSetup.FindBone(model, bones);
            if (bone == null)
            {
                Debug.LogWarning($"No hand bone ({string.Join("/", bones)}) on {model.name}; bones: {string.Join(", ", KayKitSetup.BoneNames(model))}");
                return;
            }

            var item = (GameObject)PrefabUtility.InstantiatePrefab(KayKitSetup.Model(itemPath));
            PrefabUtility.UnpackPrefabInstance(item, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            item.transform.SetParent(bone, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
        }

        /// <summary>A glowing ring on the floor under a character.</summary>
        public static void AddRing(Transform parent, Material material, float size)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.name = "Ring";
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = Vector3.one * size;
            var renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
