using System;
using System.Collections.Generic;

namespace LostDungeon.Core.Save
{
    /// <summary>What goes in the save file. Public fields so JsonUtility can write it.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public List<int> completedLevels = new List<int>();
        public PlayerStats stats = new PlayerStats();
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public GameSettings settings = new GameSettings();

        /// <summary>Rewards from levels, like "ability.fireball".</summary>
        public List<string> unlocks = new List<string>();
    }

    [Serializable]
    public class PlayerStats
    {
        public int enemiesDefeated;
        public int deaths;
        public int goldCollected;
        public float secondsPlayed;
    }

    [Serializable]
    public class InventoryEntry
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class GameSettings
    {
        public float musicVolume = 0.6f;
        public float effectsVolume = 0.8f;
        public bool showTouchControls = true;
    }
}
