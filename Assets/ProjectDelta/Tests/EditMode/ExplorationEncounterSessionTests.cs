using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ExplorationEncounterSessionTests
    {
        [Test]
        public void NewSession_StartsIdleWithoutContext()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            Assert.AreEqual(
                EncounterState.Idle,
                session.State);

            Assert.IsFalse(
                session.IsActive);

            Assert.IsNull(
                session.Context);
        }

        [Test]
        public void TryBegin_SameRoomAndPosition_MovesIdleToStartingAndCreatesContext()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            bool started =
                session.TryBegin(
                    "ROOM_A",
                    new GridPosition(1, 0),
                    "ROOM_A",
                    new GridPosition(1, 0),
                    "MON_TEST");

            Assert.IsTrue(started);

            Assert.AreEqual(
                EncounterState.Starting,
                session.State);

            Assert.IsTrue(
                session.IsActive);

            Assert.IsNotNull(
                session.Context);

            Assert.AreEqual(
                "ROOM_A",
                session.Context.RoomId);

            Assert.AreEqual(
                "MON_TEST",
                session.Context.MonsterDefinitionId);

            Assert.AreEqual(
                new GridPosition(1, 0),
                session.Context.MonsterGridPosition);
        }

        [Test]
        public void TryBegin_DifferentRoomOrPosition_StaysIdle()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_B",
                    GridPosition.Zero,
                    "MON_TEST"));

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    new GridPosition(1, 0),
                    "MON_TEST"));

            Assert.AreEqual(
                EncounterState.Idle,
                session.State);
        }

        [Test]
        public void TryActivate_OnlyAllowsStartingToActive()
        {
            ExplorationEncounterSession session =
                CreateStartingSession();

            Assert.IsTrue(
                session.TryActivate());

            Assert.AreEqual(
                EncounterState.Active,
                session.State);

            Assert.IsFalse(
                session.TryActivate());
        }

        [Test]
        public void TryBeginResolve_OnlyAllowsActiveToResolving()
        {
            ExplorationEncounterSession session =
                CreateStartingSession();

            Assert.IsFalse(
                session.TryBeginResolve());

            Assert.IsTrue(
                session.TryActivate());

            Assert.IsTrue(
                session.TryBeginResolve());

            Assert.AreEqual(
                EncounterState.Resolving,
                session.State);
        }

        [Test]
        public void TryFinish_OnlyAllowsResolvingToFinished()
        {
            ExplorationEncounterSession session =
                CreateActiveSession();

            Assert.IsFalse(
                session.TryFinish());

            Assert.IsTrue(
                session.TryBeginResolve());

            Assert.IsTrue(
                session.TryFinish());

            Assert.AreEqual(
                EncounterState.Finished,
                session.State);
        }

        [Test]
        public void TryReset_OnlyAllowsFinishedToIdleAndClearsContext()
        {
            ExplorationEncounterSession session =
                CreateActiveSession();

            Assert.IsFalse(
                session.TryReset());

            Assert.IsTrue(
                session.TryBeginResolve());

            Assert.IsTrue(
                session.TryFinish());

            Assert.IsTrue(
                session.TryReset());

            Assert.AreEqual(
                EncounterState.Idle,
                session.State);

            Assert.IsNull(
                session.Context);

            Assert.IsNull(
                session.MonsterDefinitionId);

            Assert.IsFalse(
                session.IsActive);
        }

        [Test]
        public void TryBegin_WhileLifecycleIsNotIdle_BlocksDuplicateEncounter()
        {
            ExplorationEncounterSession session =
                CreateStartingSession();

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST"));

            Assert.AreEqual(
                EncounterState.Starting,
                session.State);
        }

        [Test]
        public void ForceReset_ReturnsAnyStateToIdleForControllerShutdown()
        {
            ExplorationEncounterSession session =
                CreateActiveSession();

            session.ForceReset();

            Assert.AreEqual(
                EncounterState.Idle,
                session.State);

            Assert.IsNull(
                session.Context);

            Assert.IsFalse(
                session.IsActive);
        }

        [Test]
        public void TryBegin_MissingRequiredId_StaysIdle()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            Assert.IsFalse(
                session.TryBegin(
                    null,
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST"));

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    null));

            Assert.AreEqual(
                EncounterState.Idle,
                session.State);
        }

        private static ExplorationEncounterSession CreateStartingSession()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            Assert.IsTrue(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST"));

            return session;
        }

        private static ExplorationEncounterSession CreateActiveSession()
        {
            ExplorationEncounterSession session =
                CreateStartingSession();

            Assert.IsTrue(
                session.TryActivate());

            return session;
        }
    }
}
