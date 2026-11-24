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
    /// REST API integration: gets the current weather in Kigali from Open-Meteo (free, no API key,
    /// HTTPS, and allows browser requests, so it also works in the WebGL build) and turns it into
    /// dungeon conditions. If the request fails or times out, the game keeps working with normal
    /// conditions and the HUD says the weather is unavailable.
    /// </summary>
    [DefaultExecutionOrder(-100)] // set up before menus and gameplay scripts ask for it
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
                }
                else
                {
                    Debug.LogWarning($"Weather request failed: {request.error}");
                    Conditions = DungeonConditions.Normal;
                    HasLiveData = false;
                    Summary = "Weather unavailable";
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
                // Malformed JSON: treated the same as no data.
            }
            return weather != null;
        }
    }
}
