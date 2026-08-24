using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class MonsterSpawnPositionTests
    {
        [Test]
        public void BuildCandidates_ExcludesConnectedDoorAndInteriorSafetyCell()
        {
            MonsterSpawnPositionService service =
                new MonsterSpawnPositionService();

            RoomExit northDoor =
                new RoomExit(
                    new GridPosition(0, 2),
                    CardinalDirection.North);

            IReadOnlyList<GridPosition> candidates =
                service.BuildCandidates(
                    -2,
                    2,
                    -2,
                    2,
                    new[]
                    {
                        northDoor
                    },
                    null);

            CollectionAssert.DoesNotContain(
                candidates,
                new GridPosition(0, 2));

            CollectionAssert.DoesNotContain(
                candidates,
                new GridPosition(0, 1));

            CollectionAssert.Contains(
                candidates,
                new GridPosition(0, 0));
        }

        [Test]
        public void BuildCandidates_ExcludesOccupiedContentPositions()
        {
            MonsterSpawnPositionService service =
                new MonsterSpawnPositionService();

            GridPosition chest =
                new GridPosition(1, 0);

            GridPosition npc =
                new GridPosition(-1, -1);

            IReadOnlyList<GridPosition> candidates =
                service.BuildCandidates(
                    -2,
                    2,
                    -2,
                    2,
                    null,
                    new[]
                    {
                        chest,
                        npc
                    });

            CollectionAssert.DoesNotContain(
                candidates,
                chest);

            CollectionAssert.DoesNotContain(
                candidates,
                npc);
        }

        [Test]
        public void TryChoosePosition_SameSeedAndRoom_ReturnsSameGridPosition()
        {
            MonsterSpawnPositionService service =
                new MonsterSpawnPositionService();

            bool firstSuccess =
                service.TryChoosePosition(
                    -2,
                    2,
                    -2,
                    2,
                    new[]
                    {
                        new RoomExit(
                            new GridPosition(0, 2),
                            CardinalDirection.North)
                    },
                    new[]
                    {
                        new GridPosition(0, 0)
                    },
                    41001,
                    "ROOM_A",
                    "MON_TEST",
                    out GridPosition first);

            bool secondSuccess =
                service.TryChoosePosition(
                    -2,
                    2,
                    -2,
                    2,
                    new[]
                    {
                        new RoomExit(
                            new GridPosition(0, 2),
                            CardinalDirection.North)
                    },
                    new[]
                    {
                        new GridPosition(0, 0)
                    },
                    41001,
                    "ROOM_A",
                    "MON_TEST",
                    out GridPosition second);

            Assert.IsTrue(firstSuccess);
            Assert.IsTrue(secondSuccess);
            Assert.AreEqual(first, second);
        }

        [Test]
        public void TryChoosePosition_ReturnedPositionIsInsideRoomBounds()
        {
            MonsterSpawnPositionService service =
                new MonsterSpawnPositionService();

            bool success =
                service.TryChoosePosition(
                    -3,
                    3,
                    -1,
                    1,
                    null,
                    null,
                    41002,
                    "ROOM_B",
                    "MON_TEST",
                    out GridPosition position);

            Assert.IsTrue(success);
            Assert.That(position.X, Is.InRange(-3, 3));
            Assert.That(position.Z, Is.InRange(-1, 1));
        }

        [Test]
        public void TryChoosePosition_AllCellsReserved_ReturnsFalse()
        {
            MonsterSpawnPositionService service =
                new MonsterSpawnPositionService();

            bool success =
                service.TryChoosePosition(
                    0,
                    0,
                    0,
                    0,
                    null,
                    new[]
                    {
                        GridPosition.Zero
                    },
                    41003,
                    "ROOM_C",
                    "MON_TEST",
                    out _);

            Assert.IsFalse(success);
        }

        [Test]
        public void BuildCandidates_MultipleDoorsReserveTheirOwnSafeCellsOnlyOnce()
        {
            MonsterSpawnPositionService service =
                new MonsterSpawnPositionService();

            IReadOnlyList<GridPosition> candidates =
                service.BuildCandidates(
                    -2,
                    2,
                    -2,
                    2,
                    new[]
                    {
                        new RoomExit(
                            new GridPosition(0, 2),
                            CardinalDirection.North),
                        new RoomExit(
                            new GridPosition(2, 0),
                            CardinalDirection.East)
                    },
                    null);

            CollectionAssert.DoesNotContain(
                candidates,
                new GridPosition(0, 2));

            CollectionAssert.DoesNotContain(
                candidates,
                new GridPosition(0, 1));

            CollectionAssert.DoesNotContain(
                candidates,
                new GridPosition(2, 0));

            CollectionAssert.DoesNotContain(
                candidates,
                new GridPosition(1, 0));

            Assert.AreEqual(21, candidates.Count);
        }
    }
}
