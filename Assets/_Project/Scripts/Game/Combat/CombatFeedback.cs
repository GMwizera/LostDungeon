using System;
using UnityEngine;

namespace LostDungeon.Game.Combat
{
    /// <summary>
    /// An event for "something got hit here" (Observer).
    /// Attacks fire it, and the HUD and sparks listen without knowing each other.
    /// </summary>
    public static class CombatFeedback
    {
        /// <summary>Where, how much, was it a crit, and was it the player.</summary>
        public static event Action<Vector3, int, bool, bool> Hit;

        public static void RaiseHit(GameObject target, int amount, bool critical)
        {
            if (target == null || amount <= 0)
                return;
            bool playerHurt = target.TryGetComponent(out Faction faction) && faction.Team == Team.Player;
            Vector3 position = target.transform.position + Vector3.up * (World.Ground.BodyHeight + 0.5f);
            Hit?.Invoke(position, amount, critical, playerHurt);
        }

        /// <summary>Cleared on Play because domain reload is off.</summary>
        public static void Reset() => Hit = null;
    }
}
