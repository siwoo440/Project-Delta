using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleSessionTests
    {
        [Test]
        public void TryBegin_ValidContext_TransitionsToActive()
        {
            EventBattleSession session =
                new EventBattleSession();

            bool began =
                session.TryBegin(
                    CreateContext());

            Assert.IsTrue(
                began);

            Assert.AreEqual(
                EventBattleState.Active,
                session.State);

            Assert.IsTrue(
                session.IsActive);
        }

        [Test]
        public void TryBegin_AlreadyActive_ReturnsFalse()
        {
            EventBattleSession session =
                new EventBattleSession();

            session.TryBegin(
                CreateContext());

            bool secondBegin =
                session.TryBegin(
                    CreateContext());

            Assert.IsFalse(
                secondBegin);
        }

        [Test]
        public void TryFinish_WhileActive_TransitionsToFinishedAndStoresResult()
        {
            EventBattleSession session =
                new EventBattleSession();

            EventBattleContext context =
                CreateContext();

            context.AddFavor(
                40);

            session.TryBegin(
                context);

            bool finished =
                session.TryFinish(
                    EventBattleOutcome.Won);

            Assert.IsTrue(
                finished);

            Assert.AreEqual(
                EventBattleState.Finished,
                session.State);

            Assert.AreEqual(
                EventBattleOutcome.Won,
                session.Result.Outcome);

            Assert.AreEqual(
                40,
                session.Result.FinalFavor);
        }

        [Test]
        public void TryFinish_WhileIdle_ReturnsFalse()
        {
            EventBattleSession session =
                new EventBattleSession();

            Assert.IsFalse(
                session.TryFinish(
                    EventBattleOutcome.Lost));
        }

        [Test]
        public void TryReset_AfterFinished_ReturnsToIdle()
        {
            EventBattleSession session =
                new EventBattleSession();

            session.TryBegin(
                CreateContext());

            session.TryFinish(
                EventBattleOutcome.Aborted);

            bool reset =
                session.TryReset();

            Assert.IsTrue(
                reset);

            Assert.AreEqual(
                EventBattleState.Idle,
                session.State);

            Assert.IsNull(
                session.Context);
        }

        [Test]
        public void ForceReset_FromAnyState_ReturnsToIdle()
        {
            EventBattleSession session =
                new EventBattleSession();

            session.TryBegin(
                CreateContext());

            session.ForceReset();

            Assert.AreEqual(
                EventBattleState.Idle,
                session.State);

            Assert.IsNull(
                session.Context);

            Assert.IsNull(
                session.Result);
        }

        private static EventBattleContext CreateContext()
        {
            BattleParticipant player =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    10,
                    5,
                    4,
                    2,
                    80,
                    5,
                    30,
                    10,
                    20,
                    20);

            BattleParticipant target =
                new BattleParticipant(
                    "MON_TEST",
                    "MON_TEST",
                    BattleTeam.Enemy,
                    10,
                    5,
                    3,
                    1,
                    80,
                    5,
                    10,
                    20);

            return new EventBattleContext(
                EventBattleEntrySource.Seduction,
                player,
                target);
        }
    }
}
