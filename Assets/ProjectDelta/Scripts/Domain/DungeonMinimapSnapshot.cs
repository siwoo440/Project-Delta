using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public enum DungeonMinimapRoomState
    {
        Unvisited,
        Visited,
        Current
    }

    public readonly struct DungeonMinimapRoomEntry
    {
        public string RoomId { get; }
        public GridPosition MacroCoordinate { get; }
        public DungeonMinimapRoomState State { get; }

        public DungeonMinimapRoomEntry(
            string roomId,
            GridPosition macroCoordinate,
            DungeonMinimapRoomState state)
        {
            RoomId = roomId;
            MacroCoordinate = macroCoordinate;
            State = state;
        }
    }

    public sealed class DungeonMinimapSnapshot
    {
        private readonly List<DungeonMinimapRoomEntry> rooms;
        private readonly Dictionary<string, DungeonMinimapRoomEntry> roomsById;

        public string CurrentRoomId { get; }
        public GridPosition CurrentMacroCoordinate { get; }
        public IReadOnlyList<DungeonMinimapRoomEntry> Rooms => rooms;
        public bool HasCurrentRoom => !string.IsNullOrEmpty(CurrentRoomId);

        public DungeonMinimapSnapshot(
            string currentRoomId,
            GridPosition currentMacroCoordinate,
            IReadOnlyList<DungeonMinimapRoomEntry> roomEntries)
        {
            CurrentRoomId = currentRoomId;
            CurrentMacroCoordinate = currentMacroCoordinate;
            rooms = roomEntries != null
                ? new List<DungeonMinimapRoomEntry>(roomEntries)
                : new List<DungeonMinimapRoomEntry>();

            roomsById = new Dictionary<string, DungeonMinimapRoomEntry>();

            for (int i = 0; i < rooms.Count; i++)
            {
                DungeonMinimapRoomEntry room = rooms[i];

                if (!string.IsNullOrEmpty(room.RoomId))
                {
                    roomsById[room.RoomId] = room;
                }
            }
        }

        public bool TryGetRoom(string roomId, out DungeonMinimapRoomEntry room)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                room = default;
                return false;
            }

            return roomsById.TryGetValue(roomId, out room);
        }
    }

    public static class DungeonMinimapSnapshotBuilder
    {
        public static DungeonMinimapSnapshot Build(
            GeneratedDungeon dungeon,
            DungeonRunState runState,
            string currentRoomId)
        {
            if (dungeon == null
                || dungeon.Layout == null
                || string.IsNullOrEmpty(currentRoomId)
                || !dungeon.Layout.TryGetRoom(currentRoomId, out RoomNode currentRoom))
            {
                return new DungeonMinimapSnapshot(
                    null,
                    GridPosition.Zero,
                    Array.Empty<DungeonMinimapRoomEntry>());
            }

            List<RoomNode> orderedRooms =
                new List<RoomNode>(dungeon.Layout.AllRooms);

            orderedRooms.Sort(
                (left, right) => string.CompareOrdinal(left.RoomId, right.RoomId));

            List<DungeonMinimapRoomEntry> entries =
                new List<DungeonMinimapRoomEntry>(orderedRooms.Count);

            for (int i = 0; i < orderedRooms.Count; i++)
            {
                RoomNode room = orderedRooms[i];
                DungeonMinimapRoomState state =
                    ResolveRoomState(room, runState, currentRoomId);

                entries.Add(new DungeonMinimapRoomEntry(
                    room.RoomId,
                    room.MacroCoordinate,
                    state));
            }

            return new DungeonMinimapSnapshot(
                currentRoomId,
                currentRoom.MacroCoordinate,
                entries);
        }

        public static GridPosition GetRelativeCoordinate(
            GridPosition roomCoordinate,
            GridPosition currentRoomCoordinate)
        {
            return new GridPosition(
                roomCoordinate.X - currentRoomCoordinate.X,
                roomCoordinate.Z - currentRoomCoordinate.Z);
        }

        private static DungeonMinimapRoomState ResolveRoomState(
            RoomNode room,
            DungeonRunState runState,
            string currentRoomId)
        {
            if (room.RoomId == currentRoomId)
            {
                return DungeonMinimapRoomState.Current;
            }

            if (runState != null
                && runState.TryGetRoom(room.RoomId, out RoomInstance roomInstance)
                && roomInstance != null
                && roomInstance.Visited)
            {
                return DungeonMinimapRoomState.Visited;
            }

            return DungeonMinimapRoomState.Unvisited;
        }
    }
}
