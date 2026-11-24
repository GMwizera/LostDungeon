using LostDungeon.Core.Save;
using UnityEngine;

namespace LostDungeon.Game.Save
{
    /// <summary>
    /// Save stored in PlayerPrefs. Used on WebGL, where PlayerPrefs is kept in the browser's IndexedDB
    /// and survives closing the tab, while a normal file write may not be flushed.
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
