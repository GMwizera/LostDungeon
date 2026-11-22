using UnityEngine;

namespace LostDungeon.Game.Combat
{
    public enum Team
    {
        Neutral,
        Player,
        Enemy,
    }

    /// <summary>Marks which side something is on, so attacks don't hurt allies.</summary>
    public class Faction : MonoBehaviour
    {
        [SerializeField] private Team team = Team.Neutral;

        public Team Team => team;

        /// <summary>True when both objects are on the same (non-neutral) team.</summary>
        public static bool AreAllies(GameObject a, GameObject b)
        {
            if (a == null || b == null)
                return false;
            return a.TryGetComponent(out Faction first) && b.TryGetComponent(out Faction second)
                && first.team != Team.Neutral && first.team == second.team;
        }
    }
}
