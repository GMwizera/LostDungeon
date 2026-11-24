using System.Collections.Generic;
using UnityEngine;

namespace LostDungeon.Game.Levels
{
    /// <summary>The ordered list of levels. The menu, progress and level loader all read it.</summary>
    [CreateAssetMenu(menuName = "Lost Dungeon/Levels/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public int Count => levels.Count;
        public IReadOnlyList<LevelDefinition> Levels => levels;

        /// <summary>Level by number (1-based), or null if there is no such level.</summary>
        public LevelDefinition Get(int number) =>
            number >= 1 && number <= levels.Count ? levels[number - 1] : null;
    }
}
