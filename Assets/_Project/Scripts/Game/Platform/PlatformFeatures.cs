using UnityEngine;

namespace LostDungeon.Game.Platform
{
    /// <summary>
    /// Platform-specific behaviour in one place, chosen with Unity conditional compilation.
    /// Each build only contains the code for its own platform, so there are no runtime platform checks
    /// spread around the game and no calls to APIs that don't exist on that platform.
    /// </summary>
    public static class PlatformFeatures
    {
        /// <summary>Phones and tablets: show touch controls by default.</summary>
        public static bool IsMobile
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>A browser tab can't be closed by the game, so WebGL hides the Exit button.</summary>
        public static bool CanQuit
        {
            get
            {
#if UNITY_WEBGL
                return false;
#else
                return true;
#endif
            }
        }

        public static string PlatformName
        {
            get
            {
#if UNITY_WEBGL
                return "Web";
#elif UNITY_STANDALONE_WIN
                return "Windows";
#elif UNITY_ANDROID
                return "Android";
#elif UNITY_IOS
                return "iOS";
#else
                return "Editor";
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyPerformanceSettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Phones: steady 60 fps, keep the screen on, and quality level 0, which uses the lighter
            // mobile render pipeline (no MSAA, 4 lights per object, hard shorter shadows, 85% resolution).
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.SetQualityLevel(0, true);
#elif UNITY_WEBGL
            // Browser: let requestAnimationFrame pace the game.
            Application.targetFrameRate = -1;
#elif UNITY_STANDALONE
            // Desktop: match the monitor's refresh rate.
            QualitySettings.vSyncCount = 1;
#endif
        }

        /// <summary>Short buzz when the player is hurt. Only phones have a vibration motor.</summary>
        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
            Application.Quit();
#endif
        }
    }
}
