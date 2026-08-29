using System.Collections.Generic;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 112일차: 층의 방들 중 상자가 놓일 방을 결정론적으로 고른다.
    // RoomType(전투/함정/이벤트/일반)과 무관하게 어떤 방이든 상자를 가질 수 있다 -
    // 시작 방(Entry)과 계단 방(Stairs)만 제외한다.
    public sealed class RoomChestPlacementService
    {
        private const float ChestSpawnChance = 0.3f;

        public List<string> SelectRoomIds(
            GeneratedDungeon dungeon,
            int dungeonSeed)
        {
            List<string> selected =
                new List<string>();

            if (dungeon == null
                || dungeon.Layout == null)
            {
                return selected;
            }

            HashSet<string> excluded =
                new HashSet<string>();

            if (dungeon.EntryRoom != null)
            {
                excluded.Add(
                    dungeon.EntryRoom.RoomId);
            }

            if (dungeon.StairsRoom != null)
            {
                excluded.Add(
                    dungeon.StairsRoom.RoomId);
            }

            List<RoomNode> rooms =
                new List<RoomNode>(
                    dungeon.Layout.AllRooms);

            // 처리 순서를 고정해 같은 Seed면 항상 같은 결과가 나오게 한다.
            rooms.Sort(
                (left, right) =>
                    string.CompareOrdinal(
                        left.RoomId,
                        right.RoomId));

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomNode room =
                    rooms[i];

                if (room == null
                    || string.IsNullOrEmpty(room.RoomId)
                    || excluded.Contains(room.RoomId))
                {
                    continue;
                }

                float roll =
                    RoomEncounterPlacementService.CalculateStableRoll(
                        dungeonSeed,
                        room.RoomId,
                        "CHEST");

                if (roll < ChestSpawnChance)
                {
                    selected.Add(
                        room.RoomId);
                }
            }

            return selected;
        }
    }
}
