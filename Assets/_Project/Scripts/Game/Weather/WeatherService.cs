using System;
using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Core.World;
using LostDungeon.Game.Managers;
using UnityEngine;
using UnityEngine.Networking;

namespace LostDungeon.Game.Weather
{
    /// <summary>
    /// Gets Kigali's weather from the Open-Meteo REST API and turns it into dungeon conditions.
    /// It needs no key and works in WebGL too.
    /// If the request fails, the game just uses normal conditions.
    /// </summary>
    [DefaultExecutionOrder(-100)] // runs before other scripts need it
    public class WeatherService : Singleton<WeatherService>
    {
        private const string Url =
            "https://api.open-meteo.com/v1/forecast?latitude=-1.9441&longitude=30.0619&current=temperature_2m,weather_code,is_day";

        [SerializeField] private int timeoutSeconds = 6;

        [Serializable]
        private class Response
        {
            public CurrentWeather current;
        }

        [Serializable]
        private class CurrentWeather
        {
            public float temperature_2m;
            public int weather_code;
            public int is_day;
        }

        public DungeonConditions Conditions { get; private set; } = DungeonConditions.Normal;
        public bool HasLiveData { get; private set; }
        public bool IsFetching { get; private set; }
        public string Summary { get; private set; } = "Checking Kigali weather...";

        /// <summary>Temperature in Celsius, or null if we have no data.</summary>
        public float? Temperature { get; private set; }

        /// <summary>The weather in words, like "Overcast".</summary>
        public string WeatherName { get; private set; } = "Checking...";

        public bool IsNight { get; private set; }

        private void Start()
        {
            if (!IsDuplicate)
                Refresh();
        }

        public void Refresh()
        {
            if (!IsFetching)
                StartCoroutine(Fetch());
        }

        private IEnumerator Fetch()
        {
            IsFetching = true;
            using (UnityWebRequest request = UnityWebRequest.Get(Url))
            {
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success && TryParse(request.downloadHandler.text, out CurrentWeather weather))
                {
                    Conditions = DungeonConditions.FromWeather(weather.weather_code, weather.temperature_2m, weather.is_day == 1);
                    HasLiveData = true;
                    Summary = $"Kigali {weather.temperature_2m:0}°C";
                    Temperature = weather.temperature_2m;
                    WeatherName = DungeonConditions.DescribeWeather(weather.weather_code);
                    IsNight = weather.is_day == 0;
                }
                else
                {
                    Debug.LogWarning($"Weather request failed: {request.error}");
                    Conditions = DungeonConditions.Normal;
                    HasLiveData = false;
                    Summary = "Weather unavailable";
                    Temperature = null;
                    WeatherName = "Signal lost";
                    IsNight = false;
                }
            }

            IsFetching = false;
            WorldModifiers.Apply(Conditions);
            GameEvents.RaiseConditionsChanged(Conditions.Title, $"{Summary}. {Conditions.Description}");
        }

        private static bool TryParse(string json, out CurrentWeather weather)
        {
            weather = null;
            try
            {
                weather = JsonUtility.FromJson<Response>(json)?.current;
            }
            catch (ArgumentException)
            {
                // Bad JSON counts as no data.
            }
            return weather != null;
        }
    }
}
