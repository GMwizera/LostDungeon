using System.Collections.Generic;
using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>The list of all levels, in order.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Levels/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public int Count => levels.Count;
        public IReadOnlyList<LevelDefinition> Levels => levels;

        /// <summary>Gets a level by number, starting at 1, or null.</summary>
        public LevelDefinition Get(int number) =>
            number >= 1 && number <= levels.Count ? levels[number - 1] : null;
    }
}
