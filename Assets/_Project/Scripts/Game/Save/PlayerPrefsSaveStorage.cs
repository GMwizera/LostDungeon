using LostDungeon.Core.Save;
using UnityEngine;

namespace LostDungeon.Game.Save
{
    /// <summary>
    /// Saves in PlayerPrefs. WebGL uses this because the browser keeps it
    /// after the tab closes, but a file might be lost.
    /// </summary>
    public class PlayerPrefsSaveStorage : ISaveStorage
    {
        private readonly string key;

        public PlayerPrefsSaveStorage(string key) => this.key = key;

        public bool Exists() => PlayerPrefs.HasKey(key);
        public string Read() => PlayerPrefs.GetString(key);

        public void Write(string text)
        {
            PlayerPrefs.SetString(key, text);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
