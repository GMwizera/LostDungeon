using LostDungeon.Core.Events;
using UnityEngine;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// Domain reload is off to make Play faster, so static values stay between runs.
    /// This clears them before a scene loads.
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
