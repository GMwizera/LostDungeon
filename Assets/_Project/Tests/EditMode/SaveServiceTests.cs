using LostDungeon.Core.Save;
using NUnit.Framework;
using UnityEngine;

namespace LostDungeon.Tests
{
    public class SaveServiceTests
    {
        /// <summary>Keeps the "file" in memory so tests don't touch the disk.</summary>
        private class MemoryStorage : ISaveStorage
        {
            public string Text;
            public bool Exists() => Text != null;
            public string Read() => Text;
            public void Write(string text) => Text = text;
            public void Delete() => Text = null;
        }

        private MemoryStorage storage;
        private SaveService service;

        [SetUp]
        public void SetUp()
        {
            storage = new MemoryStorage();
            service = new SaveService(storage, data => JsonUtility.ToJson(data), JsonUtility.FromJson<SaveData>);
        }

        [Test]
        public void SaveThenLoad_RestoresEverything()
        {
            var data = new SaveData();
            data.completedLevels.Add(1);
            data.completedLevels.Add(2);
            data.stats.enemiesDefeated = 7;
            data.inventory.Add(new InventoryEntry { id = "gold", count = 12 });
            data.settings.musicVolume = 0.25f;
            data.unlocks.Add("ability.fireball");

            service.Save(data);
            SaveData loaded = service.Load();

            CollectionAssert.AreEqual(new[] { 1, 2 }, loaded.completedLevels);
            Assert.AreEqual(7, loaded.stats.enemiesDefeated);
            Assert.AreEqual("gold", loaded.inventory[0].id);
            Assert.AreEqual(12, loaded.inventory[0].count);
            Assert.AreEqual(0.25f, loaded.settings.musicVolume, 0.0001f);
            CollectionAssert.Contains(loaded.unlocks, "ability.fireball");
        }

        [Test]
        public void Load_WithNoSave_ReturnsFreshData()
        {
            SaveData loaded = service.Load();

            Assert.IsEmpty(loaded.completedLevels);
            Assert.IsFalse(service.LastLoadWasCorrupt);
        }

        [Test]
        public void Load_CorruptFile_ReturnsFreshDataInsteadOfCrashing()
        {
            storage.Text = "{ this is not json";

            SaveData loaded = service.Load();

            Assert.IsNotNull(loaded);
            Assert.IsTrue(service.LastLoadWasCorrupt);
        }

        [Test]
        public void Load_OldSaveWithMissingParts_IsRepaired()
        {
            storage.Text = "{\"version\":0}";

            SaveData loaded = service.Load();

            Assert.IsNotNull(loaded.stats);
            Assert.IsNotNull(loaded.settings);
            Assert.IsNotNull(loaded.unlocks);
        }

        [Test]
        public void Delete_RemovesSave()
        {
            service.Save(new SaveData());

            service.Delete();

            Assert.IsFalse(service.HasSave());
        }
    }
}
