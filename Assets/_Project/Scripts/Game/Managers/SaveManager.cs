using System.Collections.Generic;
using System.IO;
using LostDungeon.Core.Events;
using LostDungeon.Core.Progress;
using LostDungeon.Core.Save;
using LostDungeon.Game.Inventory;
using LostDungeon.Game.Save;
using UnityEngine;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// Keeps the save data as JSON (Singleton).
    /// It listens to events for stats, so gameplay code never calls it.
    /// </summary>
    [DefaultExecutionOrder(-100)] // runs before other scripts need it
    public class SaveManager : Singleton<SaveManager>
    {
        private const string FileName = "lostdungeon_save.json";

        [SerializeField] private int levelCount = 5;

        private SaveService service;
        private SaveData data;
        private LevelProgress progress;

        public SaveData Data { get { EnsureLoaded(); return data; } }
        public LevelProgress Progress { get { EnsureLoaded(); return progress; } }

        /// <summary>True if there is a game to continue.</summary>
        public bool HasProgress => Progress.CompletedLevels.Count > 0;

        protected override void Awake()
        {
            base.Awake();
            if (IsDuplicate)
                return;

            EnsureLoaded();
        }

        /// <summary>Loads the first time it is needed, even before Awake.</summary>
        private void EnsureLoaded()
        {
            if (service != null)
                return;
            service = new SaveService(CreateStorage(), d => JsonUtility.ToJson(d, true), JsonUtility.FromJson<SaveData>);
            Load();
        }

        /// <summary>
        /// Conditional compilation: WebGL saves in the browser, others save to a file.
        /// Each build only needs one, so we pick it at build time.
        /// </summary>
        private static ISaveStorage CreateStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new PlayerPrefsSaveStorage("lostdungeon.save");
#else
            return new FileSaveStorage(Path.Combine(Application.persistentDataPath, FileName));
#endif
        }

        private void OnEnable()
        {
            GameEvents.EnemyDefeated += OnEnemyDefeated;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDefeated -= OnEnemyDefeated;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.ItemCollected -= OnItemCollected;
        }

        public void Load()
        {
            EnsureLoaded();
            data = service.Load();
            progress = new LevelProgress(levelCount, data.completedLevels);
            if (service.LastLoadWasCorrupt)
                Debug.LogWarning("Save file could not be read; starting a fresh save.");
        }

        public void Save()
        {
            if (IsDuplicate)
                return;
            Data.completedLevels = new List<int>(Progress.CompletedLevels);
            Data.completedLevels.Sort();
            service.Save(Data);
        }

        public bool HasUnlock(string unlockId) => !string.IsNullOrEmpty(unlockId) && Data.unlocks.Contains(unlockId);

        public void RecordLevelComplete(int level, string unlockId, List<InventoryEntry> inventory, float seconds)
        {
            Progress.Complete(level);
            if (!string.IsNullOrEmpty(unlockId) && !Data.unlocks.Contains(unlockId))
                Data.unlocks.Add(unlockId);
            Data.inventory = inventory;
            Data.stats.secondsPlayed += seconds;
            Save();
        }

        /// <summary>Clears progress and items but keeps the settings.</summary>
        public void ResetProgress()
        {
            GameSettings settings = Data.settings;
            service.Delete();
            data = new SaveData { settings = settings };
            progress = new LevelProgress(levelCount);
            Save();
        }

        private void OnEnemyDefeated(string enemyId) => Data.stats.enemiesDefeated++;

        private void OnPlayerDied()
        {
            Data.stats.deaths++;
            Save();
        }

        private void OnItemCollected(string itemId, int amount)
        {
            if (itemId == ItemIds.Gold)
                Data.stats.goldCollected += amount;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Save(); // phones can close the app in the background
        }

        protected override void OnApplicationQuit()
        {
            Save();
            base.OnApplicationQuit();
        }
    }
}
