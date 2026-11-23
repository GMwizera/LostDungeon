using LostDungeon.Core.Progress;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class LevelProgressTests
    {
        [Test]
        public void NewGame_OnlyLevelOneIsAvailable()
        {
            var progress = new LevelProgress(5);

            Assert.AreEqual(LevelStatus.Available, progress.GetStatus(1));
            Assert.AreEqual(LevelStatus.Locked, progress.GetStatus(2));
            Assert.AreEqual(LevelStatus.Locked, progress.GetStatus(5));
        }

        [Test]
        public void CompletingALevel_UnlocksTheNext()
        {
            var progress = new LevelProgress(5);

            progress.Complete(1);

            Assert.AreEqual(LevelStatus.Completed, progress.GetStatus(1));
            Assert.AreEqual(LevelStatus.Available, progress.GetStatus(2));
            Assert.AreEqual(LevelStatus.Locked, progress.GetStatus(3));
        }

        [Test]
        public void LockedLevel_CannotBeCompleted()
        {
            var progress = new LevelProgress(5);

            Assert.IsFalse(progress.Complete(3));
            Assert.AreEqual(LevelStatus.Locked, progress.GetStatus(3));
        }

        [Test]
        public void SavedProgress_IsRestored()
        {
            var progress = new LevelProgress(5, new[] { 1, 2 });

            Assert.AreEqual(LevelStatus.Available, progress.GetStatus(3));
            Assert.AreEqual(3, progress.NextLevelToPlay());
        }

        [Test]
        public void OutOfRangeLevels_AreLockedAndIgnored()
        {
            var progress = new LevelProgress(5, new[] { 0, 9 });

            Assert.AreEqual(LevelStatus.Locked, progress.GetStatus(9));
            Assert.IsEmpty(progress.CompletedLevels);
        }
    }
}
