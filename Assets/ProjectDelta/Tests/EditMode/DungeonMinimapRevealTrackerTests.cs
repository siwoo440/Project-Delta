using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class DungeonMinimapRevealTrackerTests
    {
        [Test]
        public void Update_RevealsCurrentRoomAndExistingRoomsInSurroundingEightCells()
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph();
            RoomNode center = graph.AddRoom("CENTER", "ROOM_TEST", new GridPosition(0, 0));

            string[] nearbyRoomIds =
            {
                "NW", "N", "NE",
                "W",       "E",
                "SW", "S", "SE"
            };

            GridPosition[] nearbyCoordinates =
            {
                new GridPosition(-1, 1),
                new GridPosition(0, 1),
                new GridPosition(1, 1),
                new GridPosition(-1, 0),
                new GridPosition(1, 0),
                new GridPosition(-1, -1),
                new GridPosition(0, -1),
                new GridPosition(1, -1)
            };

            for (int i = 0; i < nearbyRoomIds.Length; i++)
            {
                graph.AddRoom(
                    nearbyRoomIds[i],
                    "ROOM_TEST",
                    nearbyCoordinates[i]);
            }

            graph.AddRoom(
                "FAR",
                "ROOM_TEST",
                new GridPosition(2, 0));

            GeneratedDungeon dungeon =
                new GeneratedDungeon(graph, center, center);

            DungeonMinimapRevealTracker tracker =
                new DungeonMinimapRevealTracker();

            tracker.Update(dungeon, "CENTER");

            Assert.IsTrue(tracker.IsRevealed("CENTER"));

            for (int i = 0; i < nearbyRoomIds.Length; i++)
            {
                Assert.IsTrue(
                    tracker.IsRevealed(nearbyRoomIds[i]),
                    nearbyRoomIds[i]);
            }

            Assert.IsFalse(tracker.IsRevealed("FAR"));
        }

        [Test]
        public void Update_PreviouslyRevealedRoomRemainsRevealedAfterPlayerMovesAway()
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph();

            RoomNode roomA =
                graph.AddRoom(
                    "A",
                    "ROOM_TEST",
                    new GridPosition(0, 0));

            graph.AddRoom(
                "B",
                "ROOM_TEST",
                new GridPosition(1, 0));

            graph.AddRoom(
                "C",
                "ROOM_TEST",
                new GridPosition(2, 0));

            graph.AddRoom(
                "D",
                "ROOM_TEST",
                new GridPosition(3, 0));

            GeneratedDungeon dungeon =
                new GeneratedDungeon(graph, roomA, roomA);

            DungeonMinimapRevealTracker tracker =
                new DungeonMinimapRevealTracker();

            tracker.Update(dungeon, "A");

            Assert.IsTrue(tracker.IsRevealed("A"));
            Assert.IsTrue(tracker.IsRevealed("B"));
            Assert.IsFalse(tracker.IsRevealed("D"));

            tracker.Update(dungeon, "D");

            Assert.IsTrue(tracker.IsRevealed("A"));
            Assert.IsTrue(tracker.IsRevealed("B"));
            Assert.IsTrue(tracker.IsRevealed("C"));
            Assert.IsTrue(tracker.IsRevealed("D"));
        }

        [Test]
        public void Update_WhenDungeonChanges_ClearsPreviousFloorDiscovery()
        {
            DungeonLayoutGraph firstGraph =
                new DungeonLayoutGraph();

            RoomNode firstEntry =
                firstGraph.AddRoom(
                    "FIRST_A",
                    "ROOM_TEST",
                    new GridPosition(0, 0));

            firstGraph.AddRoom(
                "FIRST_B",
                "ROOM_TEST",
                new GridPosition(1, 0));

            GeneratedDungeon firstDungeon =
                new GeneratedDungeon(
                    firstGraph,
                    firstEntry,
                    firstEntry);

            DungeonLayoutGraph secondGraph =
                new DungeonLayoutGraph();

            RoomNode secondEntry =
                secondGraph.AddRoom(
                    "SECOND_A",
                    "ROOM_TEST",
                    new GridPosition(0, 0));

            GeneratedDungeon secondDungeon =
                new GeneratedDungeon(
                    secondGraph,
                    secondEntry,
                    secondEntry);

            DungeonMinimapRevealTracker tracker =
                new DungeonMinimapRevealTracker();

            tracker.Update(firstDungeon, "FIRST_A");

            Assert.IsTrue(tracker.IsRevealed("FIRST_B"));

            tracker.Update(secondDungeon, "SECOND_A");

            Assert.IsFalse(tracker.IsRevealed("FIRST_A"));
            Assert.IsFalse(tracker.IsRevealed("FIRST_B"));
            Assert.IsTrue(tracker.IsRevealed("SECOND_A"));
        }

        [Test]
        public void IsRevealed_UnknownRoom_ReturnsFalse()
        {
            DungeonMinimapRevealTracker tracker =
                new DungeonMinimapRevealTracker();

            Assert.IsFalse(tracker.IsRevealed("UNKNOWN"));
        }
    }
}
