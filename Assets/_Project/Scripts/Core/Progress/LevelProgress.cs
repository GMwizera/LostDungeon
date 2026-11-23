using System;
using System.Collections.Generic;

namespace LostDungeon.Core.Progress
{
    public enum LevelStatus
    {
        Locked,
        Available,
        Completed,
    }

    /// <summary>
    /// Level unlocking rules: level 1 is always open, and finishing a level opens the next one.
    /// Levels are numbered from 1.
    /// </summary>
    public class LevelProgress
    {
        private readonly HashSet<int> completed = new HashSet<int>();

        public int LevelCount { get; }

        public LevelProgress(int levelCount, IEnumerable<int> completedLevels = null)
        {
            if (levelCount <= 0)
                throw new ArgumentException("There must be at least one level.", nameof(levelCount));
            LevelCount = levelCount;

            if (completedLevels == null)
                return;
            foreach (int level in completedLevels)
                if (IsValid(level))
                    completed.Add(level);
        }

        public IReadOnlyCollection<int> CompletedLevels => completed;

        public LevelStatus GetStatus(int level)
        {
            if (!IsValid(level))
                return LevelStatus.Locked;
            if (completed.Contains(level))
                return LevelStatus.Completed;
            return level == 1 || completed.Contains(level - 1) ? LevelStatus.Available : LevelStatus.Locked;
        }

        public bool IsPlayable(int level) => GetStatus(level) != LevelStatus.Locked;

        /// <summary>Marks a level done. Returns false if the level was locked or doesn't exist.</summary>
        public bool Complete(int level)
        {
            if (!IsPlayable(level))
                return false;
            completed.Add(level);
            return true;
        }

        /// <summary>The level "Continue" should start: the first one not yet completed, or the last level.</summary>
        public int NextLevelToPlay()
        {
            for (int level = 1; level <= LevelCount; level++)
                if (GetStatus(level) == LevelStatus.Available)
                    return level;
            return LevelCount;
        }

        public bool AllCompleted => completed.Count == LevelCount;

        private bool IsValid(int level) => level >= 1 && level <= LevelCount;
    }
}
