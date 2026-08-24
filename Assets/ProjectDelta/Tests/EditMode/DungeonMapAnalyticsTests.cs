using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class DungeonMapAnalyticsTests
    {
        [Test]
        public void CalculateProgress_CountsVisitedAndCurrentRoomsAsExplored()
        {
            DungeonMinimapSnapshot snapshot =
                new DungeonMinimapSnapshot(
                    "CURRENT",
                    GridPosition.Zero,
                    new[]
                    {
                        new DungeonMinimapRoomEntry(
                            "CURRENT",
                            GridPosition.Zero,
                            DungeonMinimapRoomState.Current),
                        new DungeonMinimapRoomEntry(
                            "VISITED",
                            new GridPosition(1, 0),
                            DungeonMinimapRoomState.Visited),
                        new DungeonMinimapRoomEntry(
                            "UNVISITED_A",
                            new GridPosition(2, 0),
                            DungeonMinimapRoomState.Unvisited),
                        new DungeonMinimapRoomEntry(
                            "UNVISITED_B",
                            new GridPosition(3, 0),
                            DungeonMinimapRoomState.Unvisited)
                    });

            DungeonMapProgress progress =
                DungeonMapAnalytics.CalculateProgress(snapshot);

            Assert.AreEqual(2, progress.ExploredRoomCount);
            Assert.AreEqual(4, progress.TotalRoomCount);
            Assert.AreEqual(50f, progress.ExplorationPercent, 0.001f);
        }

        [Test]
        public void CalculateRevealedBounds_UsesOnlyRevealedRooms()
        {
            DungeonMinimapSnapshot snapshot =
                new DungeonMinimapSnapshot(
                    "A",
                    new GridPosition(-2, 1),
                    new[]
                    {
                        new DungeonMinimapRoomEntry(
                            "A",
                            new GridPosition(-2, 1),
                            DungeonMinimapRoomState.Current),
                        new DungeonMinimapRoomEntry(
                            "B",
                            new GridPosition(1, -3),
                            DungeonMinimapRoomState.Unvisited),
                        new DungeonMinimapRoomEntry(
                            "HIDDEN",
                            new GridPosition(20, 20),
                            DungeonMinimapRoomState.Unvisited)
                    });

            HashSet<string> revealed =
                new HashSet<string>
                {
                    "A",
                    "B"
                };

            DungeonMapBounds bounds =
                DungeonMapAnalytics.CalculateRevealedBounds(
                    snapshot,
                    revealed);

            Assert.IsTrue(bounds.HasRooms);
            Assert.AreEqual(-2, bounds.MinX);
            Assert.AreEqual(1, bounds.MaxX);
            Assert.AreEqual(-3, bounds.MinZ);
            Assert.AreEqual(1, bounds.MaxZ);
            Assert.AreEqual(-0.5f, bounds.CenterX, 0.001f);
            Assert.AreEqual(-1f, bounds.CenterZ, 0.001f);
        }

        [Test]
        public void GetVisibleConnections_HidesEdgesToUnrevealedRooms()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(
                    out RoomNode roomA,
                    out RoomNode roomB,
                    out RoomNode roomC);

            HashSet<string> revealed =
                new HashSet<string>
                {
                    roomA.RoomId,
                    roomB.RoomId
                };

            IReadOnlyList<DungeonMapConnection> connections =
                DungeonMapAnalytics.GetVisibleConnections(
                    dungeon,
                    revealed);

            Assert.AreEqual(1, connections.Count);
            Assert.AreEqual("A", connections[0].FromRoomId);
            Assert.AreEqual("B", connections[0].ToRoomId);
        }

        [Test]
        public void TryGetShortestDistance_ReturnsGraphEdgeCountToStairs()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(
                    out RoomNode roomA,
                    out _,
                    out RoomNode roomC);

            bool found =
                DungeonMapAnalytics.TryGetShortestDistance(
                    dungeon,
                    roomA.RoomId,
                    roomC.RoomId,
                    out int distance);

            Assert.IsTrue(found);
            Assert.AreEqual(2, distance);
        }

        [Test]
        public void TryGetShortestDistance_CurrentRoomIsTarget_ReturnsZero()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(
                    out RoomNode roomA,
                    out _,
                    out _);

            bool found =
                DungeonMapAnalytics.TryGetShortestDistance(
                    dungeon,
                    roomA.RoomId,
                    roomA.RoomId,
                    out int distance);

            Assert.IsTrue(found);
            Assert.AreEqual(0, distance);
        }

        private static GeneratedDungeon CreateLinearDungeon(
            out RoomNode roomA,
            out RoomNode roomB,
            out RoomNode roomC)
        {
            DungeonLayoutGraph graph =
                new DungeonLayoutGraph();

            roomA = graph.AddRoom(
                "A",
                "ROOM_TEST",
                new GridPosition(0, 0));

            roomB = graph.AddRoom(
                "B",
                "ROOM_TEST",
                new GridPosition(1, 0));

            roomC = graph.AddRoom(
                "C",
                "ROOM_TEST",
                new GridPosition(2, 0));

            graph.Connect(
                roomA,
                CardinalDirection.East,
                roomB);

            graph.Connect(
                roomB,
                CardinalDirection.East,
                roomC);

            return new GeneratedDungeon(
                graph,
                roomA,
                roomC);
        }
    }
}
