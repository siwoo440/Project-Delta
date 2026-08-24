using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using ProjectDelta.Infrastructure;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class DungeonMapPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            DungeonSaveMapper.ClearPendingRestore();
            CleanSaveDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            DungeonSaveMapper.ClearPendingRestore();

            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            CleanSaveDirectory();
        }

        [Test]
        public void BuildFromRunContext_SavesSeedLayoutVisitAndRevealState()
        {
            RunContext context = CreateRuntimeState();

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(context);

            Assert.AreEqual(39001, saved.BasicInfo.DungeonSeed);
            Assert.AreEqual(3, saved.BasicInfo.CurrentFloor);
            Assert.AreEqual("ROOM_A", saved.BasicInfo.CurrentRoomId);
            Assert.AreEqual(1, saved.BasicInfo.CurrentGridPositionInRoom.x);
            Assert.AreEqual(-1, saved.BasicInfo.CurrentGridPositionInRoom.y);

            Assert.IsNotNull(saved.DungeonState.LayoutSnapshot);
            Assert.AreEqual(39001, saved.DungeonState.LayoutSnapshot.Seed);
            Assert.AreEqual(2, saved.DungeonState.LayoutSnapshot.Rooms.Count);
            Assert.AreEqual(1, saved.DungeonState.LayoutSnapshot.Connections.Count);

            CollectionAssert.Contains(
                saved.DungeonState.RevealedRoomIds,
                "ROOM_A");

            CollectionAssert.Contains(
                saved.DungeonState.RevealedRoomIds,
                "ROOM_B");

            RoomRunState roomA =
                saved.DungeonState.Rooms.Single(
                    room => room.RoomId == "ROOM_A");

            RoomRunState roomB =
                saved.DungeonState.Rooms.Single(
                    room => room.RoomId == "ROOM_B");

            Assert.IsTrue(roomA.Visited);
            Assert.IsTrue(roomA.Discovered);
            Assert.IsFalse(roomB.Visited);
            Assert.IsTrue(roomB.Discovered);

            Assert.AreEqual(0, roomA.Coordinate.x);
            Assert.AreEqual(0, roomA.Coordinate.y);
            Assert.AreEqual(1, roomB.Coordinate.x);
            Assert.AreEqual(0, roomB.Coordinate.y);
        }

        [Test]
        public void ApplyBasics_RestoresSameLayoutSeedCurrentRoomAndRevealState()
        {
            RunContext original =
                CreateRuntimeState();

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(original);

            RunContext.End();

            RunContext restored =
                RunContext.Begin(saved.BasicInfo.RunId);

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.AreEqual(3, restored.Dungeon.CurrentFloor);
            Assert.AreEqual("ROOM_A", restored.Player.CurrentRoomId);
            Assert.AreEqual(
                new GridPosition(1, -1),
                restored.Player.CurrentGridPosition);

            Assert.IsTrue(
                restored.Dungeon.TryGetGeneratedFloor(
                    out GeneratedDungeon dungeon,
                    out int seed));

            Assert.AreEqual(39001, seed);
            Assert.AreEqual(2, dungeon.Layout.AllRooms.Count);
            Assert.AreEqual("ROOM_A", dungeon.EntryRoom.RoomId);
            Assert.AreEqual("ROOM_B", dungeon.StairsRoom.RoomId);

            Assert.IsTrue(
                dungeon.Layout.TryGetRoom(
                    "ROOM_A",
                    out RoomNode roomA));

            Assert.IsTrue(
                dungeon.Layout.TryGetRoom(
                    "ROOM_B",
                    out RoomNode roomB));

            Assert.AreEqual(
                new GridPosition(0, 0),
                roomA.MacroCoordinate);

            Assert.AreEqual(
                new GridPosition(1, 0),
                roomB.MacroCoordinate);

            Assert.IsTrue(
                roomA.TryGetConnection(
                    CardinalDirection.East,
                    out RoomConnectionEdge connection));

            Assert.AreEqual("ROOM_B", connection.Neighbor.RoomId);

            Assert.IsTrue(restored.Dungeon.IsRoomRevealed("ROOM_A"));
            Assert.IsTrue(restored.Dungeon.IsRoomRevealed("ROOM_B"));
        }

        [Test]
        public void SaveServiceWriteRead_RoundTripsMapPersistenceData()
        {
            RunContext original =
                CreateRuntimeState();

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(original);

            SaveService saveService =
                new SaveService();

            saveService.WriteRun(
                saved,
                "InProgress");

            RunData loaded =
                saveService.ReadRun();

            Assert.IsNotNull(
                loaded.DungeonState.LayoutSnapshot);

            Assert.AreEqual(
                39001,
                loaded.BasicInfo.DungeonSeed);

            CollectionAssert.Contains(
                loaded.DungeonState.RevealedRoomIds,
                "ROOM_A");

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    loaded.BasicInfo.RunId);

            DungeonSaveMapper.ApplyBasics(
                restored,
                loaded);

            Assert.IsTrue(
                restored.Dungeon.TryGetGeneratedFloor(
                    out GeneratedDungeon restoredDungeon,
                    out int restoredSeed));

            Assert.AreEqual(39001, restoredSeed);
            Assert.AreEqual(
                2,
                restoredDungeon.Layout.AllRooms.Count);

            Assert.IsTrue(
                restored.Dungeon.IsRoomRevealed(
                    "ROOM_A"));
        }

        [Test]
        public void BeginRestore_PreservesVisitedRoomStateForRuntimeRoomCreation()
        {
            RunContext context =
                CreateRuntimeState();

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(context);

            DungeonSaveMapper.BeginRestore(saved);

            Assert.IsTrue(
                DungeonSaveMapper.TryGetRoomState(
                    "ROOM_A",
                    out RoomRunState roomA));

            Assert.IsTrue(roomA.Visited);
            Assert.IsTrue(roomA.Discovered);
        }

        private static void CleanSaveDirectory()
        {
            if (Directory.Exists(
                    SavePaths.SaveDirectory))
            {
                Directory.Delete(
                    SavePaths.SaveDirectory,
                    recursive: true);
            }
        }

        private static RunContext CreateRuntimeState()
        {
            RunContext context =
                RunContext.Begin("DAY39_TEST_RUN");

            context.Dungeon.SetFloor(3);

            DungeonLayoutGraph graph =
                new DungeonLayoutGraph();

            RoomNode roomA =
                graph.AddRoom(
                    "ROOM_A",
                    "ROOM_TEST",
                    new GridPosition(0, 0));

            RoomNode roomB =
                graph.AddRoom(
                    "ROOM_B",
                    "ROOM_TEST",
                    new GridPosition(1, 0));

            RoomExit roomAExit =
                new RoomExit(
                    new GridPosition(2, 0),
                    CardinalDirection.East);

            RoomExit roomBExit =
                new RoomExit(
                    new GridPosition(-2, 0),
                    CardinalDirection.West);

            graph.Connect(
                roomA,
                roomAExit,
                roomB,
                roomBExit);

            GeneratedDungeon dungeon =
                new GeneratedDungeon(
                    graph,
                    roomA,
                    roomB);

            context.Dungeon.SetGeneratedFloor(
                dungeon,
                39001);

            RoomInstance runtimeA =
                RoomInstance.Create(
                    "ROOM_A",
                    "ROOM_TEST",
                    null);

            RoomInstance runtimeB =
                RoomInstance.Create(
                    "ROOM_B",
                    "ROOM_TEST",
                    null);

            runtimeA.MarkVisited();

            context.Dungeon.Register(runtimeA);
            context.Dungeon.Register(runtimeB);

            context.Player.CurrentRoomId = "ROOM_A";
            context.Player.CurrentGridPosition =
                new GridPosition(1, -1);

            context.Dungeon.RevealAround(
                context.Player.CurrentRoomId);

            return context;
        }
    }
}
