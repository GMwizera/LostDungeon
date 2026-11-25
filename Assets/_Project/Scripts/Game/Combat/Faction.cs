using UnityEngine;

namespace LostDungeon.Game.Combat
{
    public enum Team
    {
        Neutral,
        Player,
        Enemy,
    }

    /// <summary>Says which team something is on, so friends don't hurt friends.</summary>
    public class Faction : MonoBehaviour
    {
        [SerializeField] private Team team = Team.Neutral;

        public Team Team => team;

        /// <summary>True if both are on the same team (not neutral).</summary>
        public static bool AreAllies(GameObject a, GameObject b)
        {
            if (a == null || b == null)
                return false;
            return a.TryGetComponent(out Faction first) && b.TryGetComponent(out Faction second)
                && first.team != Team.Neutral && first.team == second.team;
        }
    }
}
