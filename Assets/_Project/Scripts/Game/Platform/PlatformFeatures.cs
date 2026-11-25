using UnityEngine;

namespace LostDungeon.Game.Platform
{
    /// <summary>
    /// All platform differences in one place, using conditional compilation.
    /// Each build only gets its own code, so no platform checks while playing
    /// and no calls to things that platform doesn't have.
    /// </summary>
    public static class PlatformFeatures
    {
        /// <summary>Phones and tablets show touch controls.</summary>
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

        /// <summary>A game can't close a browser tab, so WebGL hides Exit.</summary>
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
            // Phones: 60 fps, screen stays on, and the lighter mobile graphics.
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.SetQualityLevel(0, true);
#elif UNITY_WEBGL
            // Browser: let the browser set the frame rate.
            Application.targetFrameRate = -1;
#elif UNITY_STANDALONE
            // Desktop: match the screen's speed.
            QualitySettings.vSyncCount = 1;
#endif
        }

        /// <summary>Buzzes when the player is hurt. Only phones can vibrate.</summary>
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
