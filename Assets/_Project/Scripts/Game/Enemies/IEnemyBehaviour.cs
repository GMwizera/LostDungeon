namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// The enemy's brain (Strategy pattern). It picks what state to be in.
    /// All enemy types share the same body and states.
    /// </summary>
    public interface IEnemyBehaviour
    {
        EnemyStateId Decide(EnemyController enemy);
    }
}
