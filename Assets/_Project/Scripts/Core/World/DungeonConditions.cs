namespace LostDungeon.Core.World
{
    /// <summary>
    /// Changes the dungeon based on real Kigali weather from the REST API.
    /// Rain slows enemies. Night is darker and enemies spot you later. Heat speeds them up.
    /// If the weather is unknown, nothing changes.
    /// </summary>
    public readonly struct DungeonConditions
    {
        public string Title { get; }
        public string Description { get; }
        public float EnemySpeedMultiplier { get; }
        public float EnemyDetectionMultiplier { get; }
        public float LightIntensity { get; }

        public DungeonConditions(string title, string description, float enemySpeed, float enemyDetection, float light)
        {
            Title = title;
            Description = description;
            EnemySpeedMultiplier = enemySpeed;
            EnemyDetectionMultiplier = enemyDetection;
            LightIntensity = light;
        }

        public static DungeonConditions Normal =>
            new DungeonConditions("Still air", "Nothing unusual in the dungeon today.", 1f, 1f, 1f);

        /// <summary>Turns a weather code into a name, like 61 -> "Rain".</summary>
        public static string DescribeWeather(int weatherCode)
        {
            if (weatherCode == 0) return "Clear sky";
            if (weatherCode <= 3) return "Partly cloudy";
            if (weatherCode <= 48) return "Fog";
            if (weatherCode <= 57) return "Drizzle";
            if (weatherCode <= 67) return "Overcast & Rain";
            if (weatherCode <= 77) return "Snow";
            if (weatherCode <= 82) return "Rain showers";
            if (weatherCode <= 86) return "Snow showers";
            return "Thunderstorm";
        }

        /// <param name="weatherCode">Open-Meteo weather code (0 clear to 99 storm).</param>
        /// <param name="temperatureC">Temperature in Celsius.</param>
        /// <param name="isDay">False after sunset.</param>
        public static DungeonConditions FromWeather(int weatherCode, float temperatureC, bool isDay)
        {
            bool storm = weatherCode >= 95;
            bool wet = storm || (weatherCode >= 51 && weatherCode <= 67) || (weatherCode >= 80 && weatherCode <= 82);
            float light = isDay ? 1f : 0.55f;
            float detection = isDay ? 1f : 0.75f;

            if (storm)
                return new DungeonConditions("Thunderstorm", "Water floods the halls: enemies move much slower.", 0.7f, detection, light * 0.8f);
            if (wet)
                return new DungeonConditions("Rain", "The stones are slippery: enemies move slower.", 0.85f, detection, light);
            if (temperatureC >= 28f)
                return new DungeonConditions("Heatwave", "The heat makes enemies restless and faster.", 1.15f, detection, light);
            if (!isDay)
                return new DungeonConditions("Night", "The torches are low: enemies see you later.", 1f, detection, light);
            return new DungeonConditions("Clear skies", "Light reaches deep into the dungeon.", 1f, 1f, 1f);
        }
    }
}
