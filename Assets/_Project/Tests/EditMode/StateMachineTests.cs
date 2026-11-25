using System.Collections.Generic;
using LostDungeon.Core.AI;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class StateMachineTests
    {
        /// <summary>A test state that writes down what happens to it.</summary>
        private class LoggingState : IState<List<string>>
        {
            private readonly string name;
            public LoggingState(string name) => this.name = name;

            public void Enter(List<string> log) => log.Add($"{name}.Enter");
            public void Tick(List<string> log, float deltaTime) => log.Add($"{name}.Tick");
            public void Exit(List<string> log) => log.Add($"{name}.Exit");
        }

        private List<string> log;
        private StateMachine<List<string>> machine;
        private LoggingState idle;
        private LoggingState chase;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            machine = new StateMachine<List<string>>(log);
            idle = new LoggingState("Idle");
            chase = new LoggingState("Chase");
        }

        [Test]
        public void NewMachine_HasNoState()
        {
            Assert.IsNull(machine.Current);
            Assert.DoesNotThrow(() => machine.Tick(0.1f));
        }

        [Test]
        public void ChangeState_ExitsOldStateBeforeEnteringNew()
        {
            machine.ChangeState(idle);
            log.Clear();

            machine.ChangeState(chase);

            CollectionAssert.AreEqual(new[] { "Idle.Exit", "Chase.Enter" }, log);
            Assert.AreSame(chase, machine.Current);
        }

        [Test]
        public void ChangeState_ToSameState_DoesNothing()
        {
            machine.ChangeState(idle);
            log.Clear();

            machine.ChangeState(idle);

            Assert.IsEmpty(log);
        }

        [Test]
        public void Tick_OnlyRunsCurrentState()
        {
            machine.ChangeState(idle);
            machine.ChangeState(chase);
            log.Clear();

            machine.Tick(0.1f);

            CollectionAssert.AreEqual(new[] { "Chase.Tick" }, log);
        }

        [Test]
        public void StateChanged_ReportsPreviousAndNext()
        {
            IState<List<string>> from = null, to = null;
            machine.ChangeState(idle);
            machine.StateChanged += (previous, next) => { from = previous; to = next; };

            machine.ChangeState(chase);

            Assert.AreSame(idle, from);
            Assert.AreSame(chase, to);
        }
    }
}
