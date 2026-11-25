namespace LostDungeon.Core.Combat
{
    /// <summary>Anything that can get hurt. Attackers only know this, not the real class.</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(int amount);
    }
}
