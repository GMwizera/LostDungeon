using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>
    /// One level: its number, name, the Addressables key of its content, and the reward for finishing it.
    /// Adding a sixth level = one new prefab marked Addressable + one new LevelDefinition in the catalog.
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

        /// <summary>Unlock id saved when the level is completed, e.g. "ability.fireball". Empty for none.</summary>
        public string RewardUnlock => rewardUnlock;

        public string RewardDescription => rewardDescription;

        /// <summary>Short label for the level card, e.g. "FIREBALL GRANTED".</summary>
        public string RewardTag => rewardTag;

        /// <summary>Which enemies the level has, shown on the level card.</summary>
        public string Hostiles => hostiles;
    }
}
