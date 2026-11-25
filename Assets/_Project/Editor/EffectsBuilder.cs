using System.IO;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Builds every visual effect from code: glow, ring and slash textures, additive URP materials,
    /// particle effects (sword slash, wind burst, sparks, dust), the fireball and the arrow.
    /// Everything is regenerated on each build, so tweaks belong here.
    /// </summary>
    public static class EffectsBuilder
    {
        private const string TextureFolder = "Assets/_Project/Art/Generated";
        private const string MaterialFolder = "Assets/_Project/Art/Effects";
        public const string EffectFolder = "Assets/_Project/Prefabs/Effects";
        public const string FireballPath = "Assets/_Project/Prefabs/Fireball.prefab";
        public const string ArrowPath = "Assets/_Project/Prefabs/Arrow.prefab";

        public static PooledEffect HeroSlash => Load("HeroSlash");
        public static PooledEffect EnemySlash => Load("EnemySlash");
        public static PooledEffect WindBurst => Load("WindBurst");
        public static PooledEffect CastFlash => Load("CastFlash");
        public static PooledEffect HitSparks => Load("HitSparks");
        public static PooledEffect CritSparks => Load("CritSparks");
        public static PooledEffect HurtSparks => Load("HurtSparks");
        public static PooledEffect BreakDust => Load("BreakDust");

        public static Material RingMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

        private static PooledEffect Load(string name) => AssetDatabase.LoadAssetAtPath<PooledEffect>($"{EffectFolder}/{name}.prefab");

        public static void BuildAll()
        {
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(EffectFolder);

            Texture2D dot = MakeTexture("FX_Dot", 64, (u, v) => { float d = Radius(u, v); float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f); return new Color(1, 1, 1, a); });
            Texture2D ring = MakeTexture("FX_Ring", 128, (u, v) =>
            {
                float d = Radius(u, v);
                float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.07f);
                float angle = Mathf.Atan2(v - 0.5f, u - 0.5f) * Mathf.Rad2Deg + 180f;
                float dash = Mathf.Repeat(angle, 22.5f) < 14f ? 1f : 0.15f;
                float inner = Mathf.Clamp01(1f - d / 0.8f) * 0.18f;
                return new Color(1, 1, 1, Mathf.Max(band * dash, inner));
            });
            Texture2D slash = MakeTexture("FX_Slash", 128, (u, v) =>
            {
                float d = Radius(u, v);
                float angle = Mathf.Atan2(v - 0.5f, u - 0.5f); // crescent on the far side (+v)
                float sweep = Mathf.Clamp01(Mathf.Sin(angle));
                float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.78f) / 0.16f);
                return new Color(1, 1, 1, Mathf.Pow(band * sweep, 1.4f));
            });

            Material additive = Particle("FX_Additive", dot, additive: true);
            Material ringHero = Transparent("FX_RingHero", ring, new Color(0.25f, 0.85f, 1f, 0.9f), additive: true);
            Material ringEnemy = Transparent("FX_RingEnemy", ring, new Color(1f, 0.22f, 0.18f, 0.85f), additive: true);
            Material slashGold = Transparent("FX_SlashGold", slash, new Color(1f, 0.85f, 0.45f, 1f) * 2.2f, additive: true);
            Material slashRed = Transparent("FX_SlashRed", slash, new Color(1f, 0.3f, 0.2f, 1f) * 1.8f, additive: true);
            _ = ringHero; _ = ringEnemy;

            SaveEffect("HeroSlash", 0.24f, -520f, root =>
            {
                AddQuad(root, slashGold, 1.5f, new Vector3(0f, 0f, -0.1f));
                AddBurst(root, additive, 10, new Color(1f, 0.8f, 0.4f), 0.06f, 2.5f, 0.25f, ParticleSystemShapeType.Sphere, 0.3f);
            });
            SaveEffect("EnemySlash", 0.22f, 480f, root => AddQuad(root, slashRed, 1.1f, new Vector3(0f, 0f, -0.1f)));
            SaveEffect("WindBurst", 0.9f, 0f, root =>
            {
                ParticleSystem swirl = AddBurst(root, additive, 90, new Color(0.45f, 0.95f, 1f), 0.11f, 0.6f, 0.85f, ParticleSystemShapeType.Circle, 0.5f);
                var velocity = swirl.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.orbitalY = new ParticleSystem.MinMaxCurve(11f);
                velocity.y = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
                velocity.radial = new ParticleSystem.MinMaxCurve(0.4f);
                velocity.x = velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f); // same mode as y (random between two constants)
                AddLight(root, new Color(0.4f, 0.9f, 1f), 1.8f, 2.5f);
            });
            SaveEffect("CastFlash", 0.35f, 0f, root =>
            {
                AddBurst(root, additive, 18, new Color(1f, 0.55f, 0.15f), 0.08f, 2f, 0.35f, ParticleSystemShapeType.Sphere, 0.15f);
                AddLight(root, new Color(1f, 0.55f, 0.2f), 3f, 3f);
            });
            SaveEffect("HitSparks", 0.4f, 0f, root =>
            {
                AddBurst(root, additive, 14, new Color(1f, 0.72f, 0.3f), 0.06f, 3.5f, 0.35f, ParticleSystemShapeType.Sphere, 0.1f);
                AddLight(root, new Color(1f, 0.7f, 0.35f), 1.5f, 2f);
            });
            SaveEffect("CritSparks", 0.55f, 0f, root =>
            {
                AddBurst(root, additive, 30, new Color(1f, 0.85f, 0.35f), 0.09f, 5f, 0.5f, ParticleSystemShapeType.Sphere, 0.15f);
                AddBurst(root, additive, 1, new Color(1f, 0.95f, 0.8f), 1.1f, 0f, 0.18f, ParticleSystemShapeType.Sphere, 0.01f);
                AddLight(root, new Color(1f, 0.8f, 0.4f), 4f, 3f);
            });
            SaveEffect("HurtSparks", 0.4f, 0f, root =>
            {
                AddBurst(root, additive, 16, new Color(1f, 0.15f, 0.1f), 0.07f, 3f, 0.4f, ParticleSystemShapeType.Sphere, 0.1f);
                AddLight(root, new Color(1f, 0.2f, 0.1f), 2f, 2f);
            });
            SaveEffect("BreakDust", 0.7f, 0f, root =>
            {
                ParticleSystem dust = AddBurst(root, additive, 22, new Color(0.65f, 0.5f, 0.35f, 0.6f), 0.22f, 1.6f, 0.7f, ParticleSystemShapeType.Sphere, 0.3f);
                var main = dust.main;
                main.gravityModifier = 0.6f;
            });

            BuildFireball(additive);
            BuildArrow(additive);
            AssetDatabase.SaveAssets();
        }

        /// <summary>A looping elemental aura: cyan wind circling upward and a few rising embers (menu hero).</summary>
        public static void AddAura(Transform parent)
        {
            Material additive = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/FX_Additive.mat");
            var go = new GameObject("Wind Aura");
            go.transform.SetParent(parent, false);
            ParticleSystem wind = AddContinuous(go.transform, additive, new Color(0.45f, 0.95f, 1f, 0.8f), 0.12f, 1.4f, 70f);
            var main = wind.main;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = wind.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.9f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var velocity = wind.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(3.5f);
            velocity.radial = new ParticleSystem.MinMaxCurve(-0.25f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            velocity.x = velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f); // same mode as y (random between two constants)

            ParticleSystem embers = AddContinuous(go.transform, additive, new Color(1f, 0.55f, 0.2f), 0.06f, 1.8f, 12f);
            var emberMain = embers.main;
            emberMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            var emberShape = embers.shape;
            emberShape.shapeType = ParticleSystemShapeType.Circle;
            emberShape.radius = 0.7f;
            emberShape.rotation = new Vector3(-90f, 0f, 0f);
        }

        // ---------- Projectiles ----------

        private static void BuildFireball(Material additive)
        {
            var root = new GameObject("Fireball");
            AddProjectileBody(root, 0.18f);

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(core.GetComponent<Collider>());
            core.name = "Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            core.transform.localScale = Vector3.one * 0.26f;
            var coreRenderer = core.GetComponent<MeshRenderer>();
            coreRenderer.sharedMaterial = Unlit("FX_FireCore", new Color(1f, 0.6f, 0.2f) * 6f);
            coreRenderer.shadowCastingMode = ShadowCastingMode.Off;

            ParticleSystem flames = AddContinuous(root.transform, additive, new Color(1f, 0.45f, 0.1f), 0.32f, 0.35f, 45f);
            flames.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var flameMain = flames.main;
            flameMain.startSpeed = 0.3f;
            AddTrail(core, additive, new Color(1f, 0.55f, 0.15f), 0.22f, 0.25f);
            AddLight(core, new Color(1f, 0.55f, 0.2f), 3.5f, 3.5f);

            SaveEffect("FireballImpact", 0.5f, 0f, impact =>
            {
                AddBurst(impact, additive, 34, new Color(1f, 0.5f, 0.12f), 0.12f, 3.5f, 0.45f, ParticleSystemShapeType.Sphere, 0.15f);
                AddLight(impact, new Color(1f, 0.5f, 0.15f), 5f, 4f);
            });

            Projectile projectile = root.AddComponent<Projectile>();
            SetValues(projectile, ("lifetime", 1.6f), ("impactEffect", Load("FireballImpact")));
            SavePrefab(root, FireballPath);
        }

        private static void BuildArrow(Material additive)
        {
            var root = new GameObject("Arrow");
            AddProjectileBody(root, 0.1f);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(KayKitSetup.Model($"{KayKitSetup.PropsFolder}/Skeleton_Arrow.fbx"));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Visual";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // the model is modelled pointing up
            model.transform.localScale = Vector3.one * 1.4f;
            AddTrail(model, additive, new Color(0.5f, 1f, 0.9f), 0.05f, 0.18f);

            Projectile projectile = root.AddComponent<Projectile>();
            SetValues(projectile, ("lifetime", 2f), ("impactEffect", HitSparks));
            SavePrefab(root, ArrowPath);
        }

        private static void AddProjectileBody(GameObject root, float radius)
        {
            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            trigger.center = new Vector3(0f, 0.6f, 0f);
        }

        // ---------- Building blocks ----------

        private static void SaveEffect(string name, float lifetime, float spin, System.Action<GameObject> build)
        {
            var root = new GameObject(name);
            build(root);
            PooledEffect effect = root.AddComponent<PooledEffect>();
            SetValues(effect, ("lifetime", lifetime), ("spin", spin));
            SavePrefab(root, $"{EffectFolder}/{name}.prefab");
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void AddQuad(GameObject root, Material material, float size, Vector3 offset)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Slash";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = offset;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // flat, arc facing forward
            quad.transform.localScale = Vector3.one * size;
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void AddLight(GameObject parent, Color color, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent.transform, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static ParticleSystem AddBurst(GameObject root, Material material, int count, Color color, float size,
            float speed, float lifetime, ParticleSystemShapeType shape, float radius)
        {
            var go = new GameObject("Particles");
            go.transform.SetParent(root.transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.duration = lifetime;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.3f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count * 2;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shapeModule = system.shape;
            shapeModule.shapeType = shape;
            shapeModule.radius = radius;
            if (shape == ParticleSystemShapeType.Circle)
                shapeModule.rotation = new Vector3(90f, 0f, 0f);

            FadeOut(system);
            SetRenderer(system, material);
            return system;
        }

        private static ParticleSystem AddContinuous(Transform parent, Material material, Color color, float size, float lifetime, float rate)
        {
            var go = new GameObject("Flames");
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            FadeOut(system);
            SetRenderer(system, material);
            return system;
        }

        private static void FadeOut(ParticleSystem system)
        {
            var colour = system.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
        }

        private static void SetRenderer(ParticleSystem system, Material material)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void AddTrail(GameObject parent, Material material, Color color, float width, float time)
        {
            var trail = parent.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = time;
            trail.minVertexDistance = 0.05f;
            trail.widthCurve = AnimationCurve.Linear(0f, width, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Textures and materials ----------

        private static float Radius(float u, float v) => Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;

        private static Texture2D MakeTexture(string name, int size, System.Func<float, float, Color> pixel)
        {
            string path = $"{TextureFolder}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, pixel((x + 0.5f) / size, (y + 0.5f) / size));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material GetMaterial(string name, string shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find(shader);
            return material;
        }

        /// <summary>Soft additive particles (sparks, flames, wind). Additive blending makes overlaps glow.</summary>
        private static Material Particle(string name, Texture2D texture, bool additive)
        {
            Material material = GetMaterial(name, "Universal Render Pipeline/Particles/Unlit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            MakeTransparent(material, additive);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A transparent unlit surface (ground rings, the slash arc).</summary>
        private static Material Transparent(string name, Texture2D texture, Color color, bool additive)
        {
            Material material = GetMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            MakeTransparent(material, additive);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Bright solid colour that bloom turns into a glow (the fireball's core).</summary>
        private static Material Unlit(string name, Color color)
        {
            Material material = GetMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void MakeTransparent(Material material, bool additive)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
        }
    }
}
