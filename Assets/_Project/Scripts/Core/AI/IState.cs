namespace LostDungeon.Core.AI
{
    /// <summary>
    /// One state in a state machine (State pattern). Each state holds its own behaviour,
    /// so the owner doesn't need a big if/else or switch over "what am I doing now".
    /// </summary>
    public interface IState<in TContext>
    {
        void Enter(TContext context);
        void Tick(TContext context, float deltaTime);
        void Exit(TContext context);
    }
}
