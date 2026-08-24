using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ExplorationEncounterSessionTests
    {
        [Test]
        public void TryBegin_SameRoomAndSamePosition_StartsEncounter()
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
            Assert.IsTrue(session.IsActive);
            Assert.AreEqual("MON_TEST", session.MonsterDefinitionId);
        }

        [Test]
        public void TryBegin_SameRoomDifferentPosition_DoesNotStart()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            bool started =
                session.TryBegin(
                    "ROOM_A",
                    new GridPosition(1, 0),
                    "ROOM_A",
                    new GridPosition(0, 0),
                    "MON_TEST");

            Assert.IsFalse(started);
            Assert.IsFalse(session.IsActive);
        }

        [Test]
        public void TryBegin_DifferentRoomSamePosition_DoesNotStart()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            bool started =
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_B",
                    GridPosition.Zero,
                    "MON_TEST");

            Assert.IsFalse(started);
            Assert.IsFalse(session.IsActive);
        }

        [Test]
        public void TryBegin_WhileAlreadyActive_DoesNotStartDuplicateEncounter()
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

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST"));
        }

        [Test]
        public void Complete_ClearsSessionAndAllowsAnotherEncounter()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession();

            Assert.IsTrue(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_A"));

            session.Complete();

            Assert.IsFalse(session.IsActive);
            Assert.IsNull(session.MonsterDefinitionId);

            Assert.IsTrue(
                session.TryBegin(
                    "ROOM_B",
                    new GridPosition(1, 1),
                    "ROOM_B",
                    new GridPosition(1, 1),
                    "MON_B"));
        }

        [Test]
        public void TryBegin_MissingRoomOrMonsterId_DoesNotStart()
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

            Assert.IsFalse(session.IsActive);
        }
    }
}
