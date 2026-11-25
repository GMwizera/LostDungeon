using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>Goes on each level prefab and tells the LevelManager where things are.</summary>
    public class LevelRoot : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private LevelObjective objective;

        public Transform PlayerSpawn => playerSpawn;
        public LevelObjective Objective => objective;
    }
}
