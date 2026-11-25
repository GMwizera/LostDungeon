using System;

namespace LostDungeon.Core.Save
{
    /// <summary>
    /// Loads and saves the game using the storage and JSON tool it is given.
    /// A missing or broken save just starts a new one, it never crashes.
    /// </summary>
    public class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly Func<SaveData, string> toJson;
        private readonly Func<string, SaveData> fromJson;

        public SaveService(ISaveStorage storage, Func<SaveData, string> toJson, Func<string, SaveData> fromJson)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.toJson = toJson ?? throw new ArgumentNullException(nameof(toJson));
            this.fromJson = fromJson ?? throw new ArgumentNullException(nameof(fromJson));
        }

        /// <summary>True if the last load found a broken save.</summary>
        public bool LastLoadWasCorrupt { get; private set; }

        public bool HasSave() => storage.Exists();

        public SaveData Load()
        {
            LastLoadWasCorrupt = false;
            if (!storage.Exists())
                return new SaveData();

            try
            {
                SaveData data = fromJson(storage.Read());
                return Repair(data);
            }
            catch (Exception)
            {
                LastLoadWasCorrupt = true;
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.version = SaveData.CurrentVersion;
            storage.Write(toJson(data));
        }

        public void Delete() => storage.Delete();

        /// <summary>Fills in missing parts, like in a save from an older version.</summary>
        private static SaveData Repair(SaveData data)
        {
            if (data == null)
                throw new FormatException("Save file is empty.");

            data.completedLevels ??= new System.Collections.Generic.List<int>();
            data.stats ??= new PlayerStats();
            data.inventory ??= new System.Collections.Generic.List<InventoryEntry>();
            data.settings ??= new GameSettings();
            data.unlocks ??= new System.Collections.Generic.List<string>();
            return data;
        }
    }
}
