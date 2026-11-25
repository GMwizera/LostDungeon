using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LostDungeon.EditorTools.CombatSetupBuilder;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Switches the project from the 2D renderer to a 3D Universal renderer with real-time shadows,
    /// HDR and anti-aliasing, and builds the post-processing profile (bloom, vignette, tone mapping,
    /// colour grading) that gives the dungeon its cinematic look.
    /// </summary>
    public static class RenderSetup
    {
        private const string Folder = "Assets/_Project/Settings";
        private const string RendererPath = Folder + "/Dungeon3DRenderer.asset";
        private const string PipelinePath = Folder + "/Dungeon3DPipeline.asset";
        public const string ProfilePath = Folder + "/DungeonPostFX.asset";
        private const string PostProcessDataGuid = "41439944d30ece34e96484bdb6645b55"; // URP's default PostProcessData

        public static void Apply()
        {
            EnsureFolder(Folder);
            UniversalRenderPipelineAsset pipeline = GetOrCreatePipeline();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            GetOrCreateProfile();
            AssetDatabase.SaveAssets();
        }

        private static UniversalRenderPipelineAsset GetOrCreatePipeline()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var rendererSettings = new SerializedObject(renderer);
            SerializedProperty postData = rendererSettings.FindProperty("postProcessData");
            if (postData != null && postData.objectReferenceValue == null)
            {
                postData.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(AssetDatabase.GUIDToAssetPath(PostProcessDataGuid));
                rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            var settings = new SerializedObject(pipeline);
            Set(settings, "m_SupportsHDR", 1);
            Set(settings, "m_MSAA", 4);
            Set(settings, "m_RenderScale", 1f);
            Set(settings, "m_MainLightRenderingMode", 1);
            Set(settings, "m_MainLightShadowsSupported", 1);
            Set(settings, "m_MainLightShadowmapResolution", 2048);
            Set(settings, "m_AdditionalLightsRenderingMode", 1);
            Set(settings, "m_AdditionalLightsPerObjectLimit", 8);
            Set(settings, "m_AdditionalLightShadowsSupported", 0);
            Set(settings, "m_ShadowDistance", 30f);
            Set(settings, "m_ShadowCascadeCount", 1);
            Set(settings, "m_SoftShadowsSupported", 1);
            Set(settings, "m_ShadowDepthBias", 1f);
            Set(settings, "m_ShadowNormalBias", 0.6f);
            Set(settings, "m_ColorGradingMode", 1); // HDR grading
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void Set(SerializedObject target, string field, float value)
        {
            SerializedProperty property = target.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning($"URP asset has no field {field}");
                return;
            }
            if (property.propertyType == SerializedPropertyType.Float)
                property.floatValue = value;
            else
                property.intValue = (int)value;
        }

        private static VolumeProfile GetOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            Bloom bloom = Get<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.7f));

            Tonemapping tonemapping = Get<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = Get<ColorAdjustments>(profile);
            color.postExposure.Override(0.75f);
            color.contrast.Override(18f);
            color.saturation.Override(12f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            Vignette vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.02f, 0.01f, 0.03f));

            ShadowsMidtonesHighlights tones = Get<ShadowsMidtonesHighlights>(profile);
            tones.shadows.Override(new Vector4(0.86f, 0.95f, 1.08f, 0f)); // cool shadows
            tones.highlights.Override(new Vector4(1.08f, 1f, 0.9f, 0f));  // warm torchlight

            foreach (VolumeComponent component in profile.components)
            {
                component.active = true;
                EditorUtility.SetDirty(component);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing))
                return existing;
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
