using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// One click builds for Windows, WebGL and Android.
    /// Each one builds the level bundles first, then the game.
    /// </summary>
    public static class BuildScripts
    {
        private static string BuildRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));

        [MenuItem("Tools/Lost Dungeon/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Windows/LostDungeon.exe");

        [MenuItem("Tools/Lost Dungeon/Build/WebGL")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "WebGL");

        // For showing desktop play on a Mac. Same desktop code as the Windows build.
        [MenuItem("Tools/Lost Dungeon/Build/macOS")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "macOS/LostDungeon.app");

        [MenuItem("Tools/Lost Dungeon/Build/Android")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Android/LostDungeon.apk");

        private static void Build(BuildTarget target, string relativePath)
        {
            // Addressables content is built for the active platform, so switch to the target first.
            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);

            ConfigurePlayer();
            BuildAddressables();

            string path = Path.Combine(BuildRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? BuildRoot);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });

            BuildSummary summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime}, errors {summary.totalErrors}");
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"{target} build failed ({summary.totalErrors} errors).");
        }

        /// <summary>Builds the level bundles for the platform we are building.</summary>
        private static void BuildAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
                throw new BuildFailedException($"Addressables build failed: {result.Error}");
            Debug.Log($"Addressables content built in {result.Duration:0.0}s");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Gisele Mwizera";
            PlayerSettings.productName = "Chronicles of the Lost Dungeon";

            // Phones stay sideways because the buttons are made for that.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Most new phones need 64 bit, and that needs IL2CPP.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.giselemwizera.lostdungeon");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // OpenGL ES 3 first, Vulkan as backup. Every modern phone has GLES 3,
            // and Android emulators crash on Vulkan, so this works on both.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3, UnityEngine.Rendering.GraphicsDeviceType.Vulkan });

            // The fallback lets the web build load on sites like Unity Play.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
        }
    }
}
