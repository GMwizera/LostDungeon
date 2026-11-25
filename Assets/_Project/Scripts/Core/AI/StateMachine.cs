using System;

namespace LostDungeon.Core.AI
{
    /// <summary>
    /// Runs one state at a time. On a switch it exits the old one, then enters the new one.
    /// Generic, so enemies and other things can reuse it.
    /// </summary>
    public class StateMachine<TContext>
    {
        private readonly TContext context;

        public IState<TContext> Current { get; private set; }

        /// <summary>(previous, next)</summary>
        public event Action<IState<TContext>, IState<TContext>> StateChanged;

        public StateMachine(TContext context)
        {
            this.context = context;
        }

        public void ChangeState(IState<TContext> next)
        {
            if (ReferenceEquals(next, Current))
                return;

            IState<TContext> previous = Current;
            previous?.Exit(context);
            Current = next;
            Current?.Enter(context);
            StateChanged?.Invoke(previous, next);
        }

        public void Tick(float deltaTime) => Current?.Tick(context, deltaTime);
    }
}
