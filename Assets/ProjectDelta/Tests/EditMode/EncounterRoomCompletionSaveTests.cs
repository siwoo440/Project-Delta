using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EncounterRoomCompletionSaveTests
    {
        [TearDown]
        public void TearDown()
        {
            DungeonSaveMapper.ClearPendingRestore();

            if (RunContext.Current != null)
            {
                RunContext.End();
            }
        }

        [Test]
        public void BuildFromRunContext_CompletedRoom_SavesCompletedFlag()
        {
            RunContext context =
                BeginTestRun();

            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_A",
                    "ROOM_DEF",
                    null);

            context.Dungeon.Register(
                room);

            room.MarkCompleted();

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    context);

            Assert.AreEqual(
                1,
                saved.DungeonState.Rooms.Count);

            Assert.IsTrue(
                saved.DungeonState.Rooms[0].Completed);
        }

        [Test]
        public void BeginRestore_CompletedRoom_PreservesCompletedFlag()
        {
            RunContext context =
                BeginTestRun();

            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_A",
                    "ROOM_DEF",
                    null);

            context.Dungeon.Register(
                room);

            room.MarkCompleted();

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    context);

            DungeonSaveMapper.BeginRestore(
                saved);

            Assert.IsTrue(
                DungeonSaveMapper.TryGetRoomState(
                    "ROOM_A",
                    out RoomRunState restored));

            Assert.IsTrue(
                restored.Completed);
        }

        private static RunContext BeginTestRun()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            RunContext context =
                RunContext.Begin(
                    "DAY46_TEST");

            context.Player.CurrentRoomId =
                "ROOM_A";

            context.Player.CurrentGridPosition =
                GridPosition.Zero;

            return context;
        }
    }
}
