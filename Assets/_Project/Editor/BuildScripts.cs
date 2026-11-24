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
    /// One-click builds for the three required platforms. Each build first builds the Addressables
    /// content for that platform (the level bundles), then the player, into SUMMATIVE/Builds/&lt;Platform&gt;.
    /// Also runnable from the command line with -executeMethod for unattended builds.
    /// </summary>
    public static class BuildScripts
    {
        private static string BuildRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));

        [MenuItem("Tools/Lost Dungeon/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Windows/LostDungeon.exe");

        [MenuItem("Tools/Lost Dungeon/Build/WebGL")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "WebGL");

        [MenuItem("Tools/Lost Dungeon/Build/Android")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Android/LostDungeon.apk");

        private static void Build(BuildTarget target, string relativePath)
        {
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

        /// <summary>Builds the level bundles for the active platform. Content must match the platform it runs on.</summary>
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

            // Landscape only on phones: the HUD and touch controls are laid out for it.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Android: 64-bit (required by most modern phones), so IL2CPP.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.giselemwizera.lostdungeon");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // WebGL: gzip with a fallback so it loads on hosts that don't send compression headers (Unity Play).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
        }
    }
}
