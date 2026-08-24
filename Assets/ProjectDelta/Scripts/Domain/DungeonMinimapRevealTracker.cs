using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 37일차: 현재 방 주변 8칸에서 한 번 발견한 방을 현재 층 동안 기억한다.
    public sealed class DungeonMinimapRevealTracker
    {
        private readonly HashSet<string> revealedRoomIds =
            new HashSet<string>();

        private GeneratedDungeon trackedDungeon;

        public IReadOnlyCollection<string> RevealedRoomIds =>
            revealedRoomIds;

        public void Update(
            GeneratedDungeon dungeon,
            string currentRoomId)
        {
            if (!ReferenceEquals(trackedDungeon, dungeon))
            {
                trackedDungeon = dungeon;
                revealedRoomIds.Clear();
            }

            if (dungeon == null
                || dungeon.Layout == null
                || string.IsNullOrEmpty(currentRoomId)
                || !dungeon.Layout.TryGetRoom(
                    currentRoomId,
                    out RoomNode currentRoom))
            {
                return;
            }

            foreach (RoomNode room in dungeon.Layout.AllRooms)
            {
                int distanceX = Math.Abs(
                    room.MacroCoordinate.X
                    - currentRoom.MacroCoordinate.X);

                int distanceZ = Math.Abs(
                    room.MacroCoordinate.Z
                    - currentRoom.MacroCoordinate.Z);

                // 현재 방을 포함한 3x3 범위:
                // 현재 칸 + 상하좌우 + 대각선 4칸.
                if (distanceX <= 1 && distanceZ <= 1)
                {
                    revealedRoomIds.Add(room.RoomId);
                }
            }
        }

        public bool IsRevealed(string roomId)
        {
            return !string.IsNullOrEmpty(roomId)
                && revealedRoomIds.Contains(roomId);
        }
    }
}
