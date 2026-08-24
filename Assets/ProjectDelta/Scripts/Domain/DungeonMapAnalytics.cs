using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public readonly struct DungeonMapProgress
    {
        public int ExploredRoomCount { get; }
        public int TotalRoomCount { get; }
        public float ExplorationPercent { get; }

        public DungeonMapProgress(
            int exploredRoomCount,
            int totalRoomCount)
        {
            ExploredRoomCount = exploredRoomCount;
            TotalRoomCount = totalRoomCount;
            ExplorationPercent = totalRoomCount > 0
                ? exploredRoomCount * 100f / totalRoomCount
                : 0f;
        }
    }

    public readonly struct DungeonMapBounds
    {
        public bool HasRooms { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public int MinZ { get; }
        public int MaxZ { get; }
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterZ => (MinZ + MaxZ) * 0.5f;

        public DungeonMapBounds(
            bool hasRooms,
            int minX,
            int maxX,
            int minZ,
            int maxZ)
        {
            HasRooms = hasRooms;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }
    }

    public readonly struct DungeonMapConnection
    {
        public string FromRoomId { get; }
        public string ToRoomId { get; }

        public DungeonMapConnection(
            string fromRoomId,
            string toRoomId)
        {
            FromRoomId = fromRoomId;
            ToRoomId = toRoomId;
        }
    }

    // 38일차: 전체 지도에서 사용하는 탐험률·중앙 정렬·연결선·최단 거리 계산.
    public static class DungeonMapAnalytics
    {
        public static DungeonMapProgress CalculateProgress(
            DungeonMinimapSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return new DungeonMapProgress(0, 0);
            }

            int exploredRoomCount = 0;

            for (int i = 0; i < snapshot.Rooms.Count; i++)
            {
                DungeonMinimapRoomState state =
                    snapshot.Rooms[i].State;

                if (state == DungeonMinimapRoomState.Visited
                    || state == DungeonMinimapRoomState.Current)
                {
                    exploredRoomCount++;
                }
            }

            return new DungeonMapProgress(
                exploredRoomCount,
                snapshot.Rooms.Count);
        }

        public static DungeonMapBounds CalculateRevealedBounds(
            DungeonMinimapSnapshot snapshot,
            IReadOnlyCollection<string> revealedRoomIds)
        {
            if (snapshot == null
                || revealedRoomIds == null
                || revealedRoomIds.Count == 0)
            {
                return new DungeonMapBounds(
                    false,
                    0,
                    0,
                    0,
                    0);
            }

            HashSet<string> revealed =
                new HashSet<string>(revealedRoomIds);

            bool hasRoom = false;
            int minX = 0;
            int maxX = 0;
            int minZ = 0;
            int maxZ = 0;

            for (int i = 0; i < snapshot.Rooms.Count; i++)
            {
                DungeonMinimapRoomEntry room =
                    snapshot.Rooms[i];

                if (!revealed.Contains(room.RoomId))
                {
                    continue;
                }

                int x = room.MacroCoordinate.X;
                int z = room.MacroCoordinate.Z;

                if (!hasRoom)
                {
                    minX = x;
                    maxX = x;
                    minZ = z;
                    maxZ = z;
                    hasRoom = true;
                    continue;
                }

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);
            }

            return new DungeonMapBounds(
                hasRoom,
                minX,
                maxX,
                minZ,
                maxZ);
        }

        public static IReadOnlyList<DungeonMapConnection> GetVisibleConnections(
            GeneratedDungeon dungeon,
            IReadOnlyCollection<string> revealedRoomIds)
        {
            if (dungeon == null
                || dungeon.Layout == null
                || revealedRoomIds == null
                || revealedRoomIds.Count == 0)
            {
                return Array.Empty<DungeonMapConnection>();
            }

            HashSet<string> revealed =
                new HashSet<string>(revealedRoomIds);

            HashSet<string> seenPairs =
                new HashSet<string>();

            List<DungeonMapConnection> connections =
                new List<DungeonMapConnection>();

            foreach (RoomNode room in dungeon.Layout.AllRooms)
            {
                if (room == null
                    || !revealed.Contains(room.RoomId))
                {
                    continue;
                }

                foreach (RoomConnectionEdge edge in room.Connections.Values)
                {
                    RoomNode neighbor = edge?.Neighbor;

                    if (neighbor == null
                        || !revealed.Contains(neighbor.RoomId))
                    {
                        continue;
                    }

                    string fromRoomId;
                    string toRoomId;

                    if (string.CompareOrdinal(
                            room.RoomId,
                            neighbor.RoomId) <= 0)
                    {
                        fromRoomId = room.RoomId;
                        toRoomId = neighbor.RoomId;
                    }
                    else
                    {
                        fromRoomId = neighbor.RoomId;
                        toRoomId = room.RoomId;
                    }

                    string pairKey =
                        fromRoomId + "\u001F" + toRoomId;

                    if (!seenPairs.Add(pairKey))
                    {
                        continue;
                    }

                    connections.Add(
                        new DungeonMapConnection(
                            fromRoomId,
                            toRoomId));
                }
            }

            connections.Sort(
                (left, right) =>
                {
                    int fromCompare =
                        string.CompareOrdinal(
                            left.FromRoomId,
                            right.FromRoomId);

                    return fromCompare != 0
                        ? fromCompare
                        : string.CompareOrdinal(
                            left.ToRoomId,
                            right.ToRoomId);
                });

            return connections;
        }

        public static bool TryGetShortestDistance(
            GeneratedDungeon dungeon,
            string fromRoomId,
            string toRoomId,
            out int distance)
        {
            distance = -1;

            if (dungeon == null
                || dungeon.Layout == null
                || string.IsNullOrEmpty(fromRoomId)
                || string.IsNullOrEmpty(toRoomId)
                || !dungeon.Layout.TryGetRoom(
                    fromRoomId,
                    out RoomNode startRoom)
                || !dungeon.Layout.TryGetRoom(
                    toRoomId,
                    out RoomNode targetRoom))
            {
                return false;
            }

            if (ReferenceEquals(startRoom, targetRoom)
                || startRoom.RoomId == targetRoom.RoomId)
            {
                distance = 0;
                return true;
            }

            Queue<RoomNode> queue =
                new Queue<RoomNode>();

            Dictionary<string, int> distanceByRoomId =
                new Dictionary<string, int>();

            queue.Enqueue(startRoom);
            distanceByRoomId[startRoom.RoomId] = 0;

            while (queue.Count > 0)
            {
                RoomNode current =
                    queue.Dequeue();

                int currentDistance =
                    distanceByRoomId[current.RoomId];

                foreach (RoomConnectionEdge edge in current.Connections.Values)
                {
                    RoomNode neighbor = edge?.Neighbor;

                    if (neighbor == null
                        || distanceByRoomId.ContainsKey(
                            neighbor.RoomId))
                    {
                        continue;
                    }

                    int nextDistance =
                        currentDistance + 1;

                    if (neighbor.RoomId == targetRoom.RoomId)
                    {
                        distance = nextDistance;
                        return true;
                    }

                    distanceByRoomId[neighbor.RoomId] =
                        nextDistance;

                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }
    }
}
