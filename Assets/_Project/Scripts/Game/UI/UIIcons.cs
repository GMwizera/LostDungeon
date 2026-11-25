using UnityEngine;

namespace LostDungeon.Game.UI
{
    /// <summary>All the UI icons in one asset.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/UI/Icons")]
    public class UIIcons : ScriptableObject
    {
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Sprite locked;
        public Sprite completed;
        public Sprite available;
        public Sprite gold;
        public Sprite key;
        public Sprite potion;
        public Sprite sword;
        public Sprite dash;
        public Sprite fireball;
        public Sprite cloud;
        public Sprite flag;

        public Sprite ForItem(string itemId)
        {
            switch (itemId)
            {
                case Inventory.ItemIds.Gold: return gold;
                case Inventory.ItemIds.Key: return key;
                case Inventory.ItemIds.Potion: return potion;
                default: return null;
            }
        }
    }
}
