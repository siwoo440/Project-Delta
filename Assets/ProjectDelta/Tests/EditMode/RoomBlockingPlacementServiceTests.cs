using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class RoomBlockingPlacementServiceTests
    {
        [Test]
        public void TryChoosePosition_OneTileWideDoorCorridor_ReturnsFalse()
        {
            RoomBlockingPlacementService service =
                new RoomBlockingPlacementService();

            RoomExit[] exits =
            {
                new RoomExit(
                    new GridPosition(-2, 0),
                    CardinalDirection.West),
                new RoomExit(
                    new GridPosition(2, 0),
                    CardinalDirection.East)
            };

            bool success =
                service.TryChoosePosition(
                    -2,
                    2,
                    0,
                    0,
                    exits,
                    null,
                    (position, direction) => true,
                    11301,
                    "ROOM_CORRIDOR",
                    "CHEST",
                    out _);

            Assert.IsFalse(success);
        }

        [Test]
        public void TryChoosePosition_OpenRoomWithAlternativeRoute_ReturnsTrue()
        {
            RoomBlockingPlacementService service =
                new RoomBlockingPlacementService();

            RoomExit[] exits =
            {
                new RoomExit(
                    new GridPosition(-2, 0),
                    CardinalDirection.West),
                new RoomExit(
                    new GridPosition(2, 0),
                    CardinalDirection.East)
            };

            bool success =
                service.TryChoosePosition(
                    -2,
                    2,
                    -1,
                    1,
                    exits,
                    null,
                    (position, direction) => true,
                    11302,
                    "ROOM_OPEN",
                    "CHEST",
                    out GridPosition position);

            Assert.IsTrue(success);
            Assert.That(position.X, Is.InRange(-2, 2));
            Assert.That(position.Z, Is.InRange(-1, 1));
        }

        [Test]
        public void TryChoosePosition_SameSeedAndRoom_ReturnsSameSafePosition()
        {
            RoomBlockingPlacementService service =
                new RoomBlockingPlacementService();

            bool firstSuccess =
                service.TryChoosePosition(
                    -2,
                    2,
                    -2,
                    2,
                    null,
                    null,
                    (position, direction) => true,
                    11303,
                    "ROOM_STABLE",
                    "CHEST",
                    out GridPosition first);

            bool secondSuccess =
                service.TryChoosePosition(
                    -2,
                    2,
                    -2,
                    2,
                    null,
                    null,
                    (position, direction) => true,
                    11303,
                    "ROOM_STABLE",
                    "CHEST",
                    out GridPosition second);

            Assert.IsTrue(firstSuccess);
            Assert.IsTrue(secondSuccess);
            Assert.AreEqual(first, second);
        }

        [Test]
        public void PreservesTraversableArea_ArticulationCell_ReturnsFalse()
        {
            RoomBlockingPlacementService service =
                new RoomBlockingPlacementService();

            bool safe =
                service.PreservesTraversableArea(
                    -1,
                    1,
                    0,
                    0,
                    GridPosition.Zero,
                    (position, direction) => true);

            Assert.IsFalse(safe);
        }
    }
}
