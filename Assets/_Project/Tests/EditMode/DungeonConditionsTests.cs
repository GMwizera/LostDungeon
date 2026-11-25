using LostDungeon.Core.World;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class DungeonConditionsTests
    {
        [Test]
        public void Rain_SlowsEnemies()
        {
            DungeonConditions conditions = DungeonConditions.FromWeather(weatherCode: 61, temperatureC: 20f, isDay: true);

            Assert.AreEqual("Rain", conditions.Title);
            Assert.Less(conditions.EnemySpeedMultiplier, 1f);
        }

        [Test]
        public void Thunderstorm_IsSlowerThanRain()
        {
            DungeonConditions storm = DungeonConditions.FromWeather(95, 20f, true);
            DungeonConditions rain = DungeonConditions.FromWeather(61, 20f, true);

            Assert.Less(storm.EnemySpeedMultiplier, rain.EnemySpeedMultiplier);
        }

        [Test]
        public void Night_DarkensAndReducesDetection()
        {
            DungeonConditions conditions = DungeonConditions.FromWeather(0, 18f, isDay: false);

            Assert.Less(conditions.LightIntensity, 1f);
            Assert.Less(conditions.EnemyDetectionMultiplier, 1f);
        }

        [TestCase(0, "Clear sky")]
        [TestCase(61, "Overcast & Rain")]
        [TestCase(95, "Thunderstorm")]
        public void DescribeWeather_GivesReadableName(int code, string expected)
        {
            Assert.AreEqual(expected, DungeonConditions.DescribeWeather(code));
        }

        [Test]
        public void ClearDay_ChangesNothing()
        {
            DungeonConditions conditions = DungeonConditions.FromWeather(0, 22f, true);

            Assert.AreEqual(1f, conditions.EnemySpeedMultiplier);
            Assert.AreEqual(1f, conditions.LightIntensity);
        }
    }
}
