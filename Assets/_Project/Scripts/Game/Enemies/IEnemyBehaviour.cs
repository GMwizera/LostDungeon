namespace LostDungeon.Game.Enemies
{
    /// <summary>
    /// The "brain" of an enemy type (Strategy pattern): looks at the situation and picks a state.
    /// Warrior, Archer and Guardian differ only in their behaviour and their EnemyData numbers;
    /// the controller and the states are shared.
    /// </summary>
    public interface IEnemyBehaviour
    {
        EnemyStateId Decide(EnemyController enemy);
    }
}
