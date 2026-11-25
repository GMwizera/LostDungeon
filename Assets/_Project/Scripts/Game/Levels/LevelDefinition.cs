using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>
    /// One level: its number, name, Addressables key and reward.
    /// A new level is just a new prefab and a new one of these.
    /// </summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Levels/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] private int number = 1;
        [SerializeField] private string displayName = "Level";
        [SerializeField] private string addressKey = "Level1";
        [SerializeField] private string rewardUnlock;
        [SerializeField] private string rewardDescription;
        [SerializeField] private string rewardTag;
        [SerializeField] private string hostiles;

        public int Number => number;
        public string DisplayName => displayName;
        public string AddressKey => addressKey;

        /// <summary>What you unlock for beating it, or empty for nothing.</summary>
        public string RewardUnlock => rewardUnlock;

        public string RewardDescription => rewardDescription;

        /// <summary>Short reward label for the level card.</summary>
        public string RewardTag => rewardTag;

        /// <summary>The enemies in this level, for the level card.</summary>
        public string Hostiles => hostiles;
    }
}
