using System;

namespace LostDungeon.Core.Abilities
{
    /// <summary>Counts down after an ability is used. The HUD reads Progress01 for its fill.</summary>
    public class Cooldown
    {
        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;

        /// <summary>0 right after use, 1 when ready again.</summary>
        public float Progress01 => Duration <= 0f ? 1f : 1f - Remaining / Duration;

        public Cooldown(float duration)
        {
            if (duration < 0f)
                throw new ArgumentException("Cooldown cannot be negative.", nameof(duration));
            Duration = duration;
        }

        public void Start() => Remaining = Duration;

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f)
                Remaining = Math.Max(0f, Remaining - deltaTime);
        }
    }
}
