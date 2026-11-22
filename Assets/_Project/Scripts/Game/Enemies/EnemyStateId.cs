namespace LostDungeon.Game.Enemies
{
    /// <summary>The states an enemy can be in. Behaviours choose one of these each frame.</summary>
    public enum EnemyStateId
    {
        Idle,
        Chase,
        Attack,
        Retreat,
        ReturnHome,
        Dead,
    }
}
