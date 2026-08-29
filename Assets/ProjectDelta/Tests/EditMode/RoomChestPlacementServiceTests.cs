using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 112일차: 상자 배치 방 선정(시작/계단 방 제외, 결정론적 재현, 중복 없음)을 검증한다.
    public sealed class RoomChestPlacementServiceTests
    {
        [Test]
        public void SelectRoomIds_NeverIncludesEntryOrStairs()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(10);

            List<string> selected =
                new RoomChestPlacementService().SelectRoomIds(
                    dungeon,
                    12345);

            Assert.That(
                selected,
                Does.Not.Contain(
                    dungeon.EntryRoom.RoomId));

            Assert.That(
                selected,
                Does.Not.Contain(
                    dungeon.StairsRoom.RoomId));
        }

        [Test]
        public void SelectRoomIds_SameSeed_ProducesSameResult()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(12);

            RoomChestPlacementService service =
                new RoomChestPlacementService();

            List<string> first =
                service.SelectRoomIds(
                    dungeon,
                    777);

            List<string> second =
                service.SelectRoomIds(
                    dungeon,
                    777);

            CollectionAssert.AreEqual(
                first,
                second);
        }

        [Test]
        public void SelectRoomIds_NoDuplicateRoomIds()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(20);

            List<string> selected =
                new RoomChestPlacementService().SelectRoomIds(
                    dungeon,
                    99);

            Assert.That(
                selected.Count,
                Is.EqualTo(
                    selected.Distinct().Count()));
        }

        [Test]
        public void SelectRoomIds_NullDungeon_ReturnsEmpty()
        {
            Assert.That(
                new RoomChestPlacementService().SelectRoomIds(
                    null,
                    1),
                Is.Empty);
        }

        private static GeneratedDungeon CreateLinearDungeon(
            int roomCount)
        {
            DungeonLayoutGraph graph =
                new DungeonLayoutGraph();

            RoomNode[] rooms =
                new RoomNode[roomCount];

            for (int i = 0; i < roomCount; i++)
            {
                rooms[i] =
                    graph.AddRoom(
                        $"ROOM_{i:00}",
                        "ROOM_TEST",
                        new GridPosition(i, 0));

                if (i > 0)
                {
                    graph.Connect(
                        rooms[i - 1],
                        CardinalDirection.East,
                        rooms[i]);
                }
            }

            return new GeneratedDungeon(
                graph,
                rooms[0],
                rooms[roomCount - 1]);
        }
    }
}
