using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class DungeonMinimapSnapshotTests
    {
        [Test]
        public void Build_UsesAllRoomsFromCurrentGeneratedDungeon()
        {
            GeneratedDungeon dungeon = CreateDungeon();
            DungeonRunState runState = new DungeonRunState();

            DungeonMinimapSnapshot snapshot =
                DungeonMinimapSnapshotBuilder.Build(dungeon, runState, "ROOM_A");

            Assert.AreEqual(3, snapshot.Rooms.Count);
            Assert.IsTrue(snapshot.TryGetRoom("ROOM_A", out _));
            Assert.IsTrue(snapshot.TryGetRoom("ROOM_B", out _));
            Assert.IsTrue(snapshot.TryGetRoom("ROOM_C", out _));
        }

        [Test]
        public void Build_UnregisteredRoom_IsUnvisited()
        {
            GeneratedDungeon dungeon = CreateDungeon();
            DungeonRunState runState = new DungeonRunState();

            DungeonMinimapSnapshot snapshot =
                DungeonMinimapSnapshotBuilder.Build(dungeon, runState, "ROOM_A");

            Assert.IsTrue(snapshot.TryGetRoom("ROOM_B", out DungeonMinimapRoomEntry room));
            Assert.AreEqual(DungeonMinimapRoomState.Unvisited, room.State);
        }

        [Test]
        public void Build_VisitedRoom_UsesVisitedState()
        {
            GeneratedDungeon dungeon = CreateDungeon();
            DungeonRunState runState = new DungeonRunState();
            RoomInstance visitedRoom = RoomInstance.Create("ROOM_B", "ROOM_TEST", null);
            visitedRoom.MarkVisited();
            runState.Register(visitedRoom);

            DungeonMinimapSnapshot snapshot =
                DungeonMinimapSnapshotBuilder.Build(dungeon, runState, "ROOM_A");

            Assert.IsTrue(snapshot.TryGetRoom("ROOM_B", out DungeonMinimapRoomEntry room));
            Assert.AreEqual(DungeonMinimapRoomState.Visited, room.State);
        }

        [Test]
        public void Build_CurrentRoom_OverridesVisitedState()
        {
            GeneratedDungeon dungeon = CreateDungeon();
            DungeonRunState runState = new DungeonRunState();
            RoomInstance currentRoom = RoomInstance.Create("ROOM_A", "ROOM_TEST", null);
            currentRoom.MarkVisited();
            runState.Register(currentRoom);

            DungeonMinimapSnapshot snapshot =
                DungeonMinimapSnapshotBuilder.Build(dungeon, runState, "ROOM_A");

            Assert.IsTrue(snapshot.TryGetRoom("ROOM_A", out DungeonMinimapRoomEntry room));
            Assert.AreEqual(DungeonMinimapRoomState.Current, room.State);
        }

        [Test]
        public void GetRelativeCoordinate_CentersCurrentRoomAndPreservesNorthAsPositiveZ()
        {
            GridPosition relative = DungeonMinimapSnapshotBuilder.GetRelativeCoordinate(
                new GridPosition(3, 2),
                new GridPosition(1, -1));

            Assert.AreEqual(2, relative.X);
            Assert.AreEqual(3, relative.Z);
        }

        [Test]
        public void Build_IgnoresRoomsThatAreNotInCurrentGeneratedDungeon()
        {
            GeneratedDungeon dungeon = CreateDungeon();
            DungeonRunState runState = new DungeonRunState();
            RoomInstance oldRoom = RoomInstance.Create("OLD_FLOOR_ROOM", "ROOM_TEST", null);
            oldRoom.MarkVisited();
            runState.Register(oldRoom);

            DungeonMinimapSnapshot snapshot =
                DungeonMinimapSnapshotBuilder.Build(dungeon, runState, "ROOM_A");

            Assert.AreEqual(3, snapshot.Rooms.Count);
            Assert.IsFalse(snapshot.TryGetRoom("OLD_FLOOR_ROOM", out _));
        }

        private static GeneratedDungeon CreateDungeon()
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph();
            RoomNode entry = graph.AddRoom(
                "ROOM_A",
                "ROOM_TEST",
                GridPosition.Zero);

            graph.AddRoom(
                "ROOM_B",
                "ROOM_TEST",
                new GridPosition(1, 0));

            RoomNode stairs = graph.AddRoom(
                "ROOM_C",
                "ROOM_TEST",
                new GridPosition(0, 1));

            return new GeneratedDungeon(graph, entry, stairs);
        }
    }
}
