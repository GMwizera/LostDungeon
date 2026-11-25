using System;
using UnityEngine;

namespace LostDungeon.Game.Combat
{
    /// <summary>
    /// Observer channel for "something got hit here". Attacks raise it; the HUD shows a floating
    /// damage number and the effects presenter spawns sparks. Neither side knows about the other.
    /// Lives in the Game layer (not Core) because it carries a world position.
    /// </summary>
    public static class CombatFeedback
    {
        /// <summary>World position, damage, critical hit, and whether the player was the one hurt.</summary>
        public static event Action<Vector3, int, bool, bool> Hit;

        public static void RaiseHit(GameObject target, int amount, bool critical)
        {
            if (target == null || amount <= 0)
                return;
            bool playerHurt = target.TryGetComponent(out Faction faction) && faction.Team == Team.Player;
            Vector3 position = target.transform.position + Vector3.up * (World.Ground.BodyHeight + 0.5f);
            Hit?.Invoke(position, amount, critical, playerHurt);
        }

        /// <summary>Cleared when Play mode starts, because domain reload is off.</summary>
        public static void Reset() => Hit = null;
    }
}
