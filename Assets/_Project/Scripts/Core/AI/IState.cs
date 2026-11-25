namespace LostDungeon.Core.AI
{
    /// <summary>One state (State pattern). Each state has its own code, so no giant switch.</summary>
    public interface IState<in TContext>
    {
        void Enter(TContext context);
        void Tick(TContext context, float deltaTime);
        void Exit(TContext context);
    }
}
