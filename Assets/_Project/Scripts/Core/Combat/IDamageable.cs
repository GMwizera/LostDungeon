namespace LostDungeon.Core.Combat
{
    /// <summary>
    /// Anything that can be hurt: the player, enemies, breakable objects.
    /// Attackers only know this interface, never the concrete class.
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(int amount);
    }
}
