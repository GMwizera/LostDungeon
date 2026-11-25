using LostDungeon.Core.Events;
using UnityEngine;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// The project has "Reload Domain" turned off for fast Play mode, so static state survives
    /// between Play sessions. This clears it before any scene loads.
    /// </summary>
    internal static class PlayModeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            GameEvents.ResetAll();
            Levels.ExitDoor.ResetStatics();
            Weather.WorldModifiers.Reset();
            Combat.CombatFeedback.Reset();
        }
    }
}
