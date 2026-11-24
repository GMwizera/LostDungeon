using LostDungeon.Core.World;

namespace LostDungeon.Game.Weather
{
    /// <summary>The active dungeon conditions, read by enemies and the level's lighting.</summary>
    public static class WorldModifiers
    {
        public static DungeonConditions Current { get; private set; } = DungeonConditions.Normal;

        public static float EnemySpeed => Current.EnemySpeedMultiplier;
        public static float EnemyDetection => Current.EnemyDetectionMultiplier;
        public static float LightIntensity => Current.LightIntensity;

        public static void Apply(DungeonConditions conditions) => Current = conditions;

        public static void Reset() => Current = DungeonConditions.Normal;
    }
}
