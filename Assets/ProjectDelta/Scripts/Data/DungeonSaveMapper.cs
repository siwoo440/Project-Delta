using System;
using System.Collections.Generic;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Data
{
    // RunContext(Domain)와 RunData(Data) 사이의 던전 저장·복원 변환기.
    public static class DungeonSaveMapper
    {
        private static Dictionary<string, RoomRunState>
            pendingRoomStates;

        public static RunData BuildFromRunContext(
            RunContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // 방 진입 직후 자동 저장에서도 주변 8칸 공개 정보가 빠지지 않게
            // 저장 직전에 현재 방 기준 발견 상태를 한 번 동기화한다.
            context.Dungeon.RevealAround(
                context.Player.CurrentRoomId);

            RunData data =
                new RunData();

            data.BasicInfo.RunId =
                context.Metadata.RunId;

            data.BasicInfo.StartedAtIso8601 =
                context.Metadata.StartedAtIso8601;

            data.BasicInfo.CurrentFloor =
                context.Dungeon.CurrentFloor;

            data.BasicInfo.CurrentRoomId =
                context.Player.CurrentRoomId;

            data.BasicInfo.CurrentGridPositionInRoom =
                new Vector2Int(
                    context.Player.CurrentGridPosition.X,
                    context.Player.CurrentGridPosition.Z);

            data.BasicInfo.DungeonSeed =
                context.Dungeon.CurrentDungeonSeed;

            // 79일차: 런타임 성장 상태를 기존 RunData.PlayerStats에 저장한다.
            data.PlayerStats.Level =
                Math.Max(
                    1,
                    Math.Min(
                        PlayerGrowthDefinition.DefaultMaxLevel,
                        context.Player.Level));

            data.PlayerStats.Experience =
                Math.Max(
                    0,
                    context.Player.Experience);

            data.PlayerStats.UnspentStatPoints =
                Math.Max(
                    0,
                    context.Player.UnusedStatPoints);

            if (context.Dungeon.CurrentLayoutSnapshot != null)
            {
                data.DungeonState.LayoutSnapshot =
                    context.Dungeon.CurrentLayoutSnapshot;
            }

            foreach (string roomId
                     in context.Dungeon.RevealedRoomIds)
            {
                data.DungeonState.RevealedRoomIds.Add(
                    roomId);
            }

            data.DungeonState.RevealedRoomIds.Sort(
                StringComparer.Ordinal);

            if (context.Dungeon.TryGetGeneratedFloor(
                    out GeneratedDungeon dungeon,
                    out _)
                && dungeon.Layout != null)
            {
                SaveGeneratedRooms(
                    context,
                    dungeon,
                    data);
            }
            else
            {
                SaveLegacyRooms(
                    context,
                    data);
            }

            foreach (InventoryItemStack item
                     in context.Inventory.Items)
            {
                data.Inventory.InventoryItemIds.Add(
                    item.ItemId);
            }

            return data;
        }

        public static void ApplyBasics(
            RunContext context,
            RunData savedRun)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (savedRun == null)
            {
                throw new ArgumentNullException(nameof(savedRun));
            }

            int savedFloor =
                savedRun.BasicInfo.CurrentFloor > 0
                    ? savedRun.BasicInfo.CurrentFloor
                    : 1;

            context.Dungeon.SetFloor(savedFloor);

            // 79일차: 구버전 저장의 Level=0도 Lv.1로 안전하게 복원한다.
            if (savedRun.PlayerStats != null)
            {
                context.Player.Level =
                    Math.Max(
                        1,
                        Math.Min(
                            PlayerGrowthDefinition.DefaultMaxLevel,
                            savedRun.PlayerStats.Level));

                context.Player.Experience =
                    Math.Max(
                        0,
                        savedRun.PlayerStats.Experience);

                context.Player.UnusedStatPoints =
                    Math.Max(
                        0,
                        savedRun.PlayerStats.UnspentStatPoints);
            }

            if (savedRun.DungeonState != null
                && savedRun.DungeonState.LayoutSnapshot != null)
            {
                context.Dungeon.RestoreGeneratedFloor(
                    savedRun.DungeonState.LayoutSnapshot,
                    savedRun.BasicInfo.DungeonSeed);
            }

            List<string> revealedRoomIds =
                new List<string>();

            if (savedRun.DungeonState?.RevealedRoomIds != null)
            {
                revealedRoomIds.AddRange(
                    savedRun.DungeonState.RevealedRoomIds);
            }

            // 구버전 저장 데이터가 RoomRunState.Discovered만 갖고 있어도
            // 발견 정보를 복원할 수 있도록 함께 병합한다.
            if (savedRun.DungeonState?.Rooms != null)
            {
                for (int i = 0;
                     i < savedRun.DungeonState.Rooms.Count;
                     i++)
                {
                    RoomRunState room =
                        savedRun.DungeonState.Rooms[i];

                    if (room != null
                        && room.Discovered
                        && !string.IsNullOrEmpty(room.RoomId))
                    {
                        revealedRoomIds.Add(
                            room.RoomId);
                    }
                }
            }

            context.Dungeon.RestoreRevealedRooms(
                revealedRoomIds);

            context.Player.CurrentRoomId =
                savedRun.BasicInfo.CurrentRoomId;

            Vector2Int savedGridPosition =
                savedRun.BasicInfo.CurrentGridPositionInRoom;

            context.Player.CurrentGridPosition =
                new GridPosition(
                    savedGridPosition.x,
                    savedGridPosition.y);

            if (savedRun.Inventory?.InventoryItemIds != null)
            {
                foreach (string itemId
                         in savedRun.Inventory.InventoryItemIds)
                {
                    context.Inventory.Add(
                        new InventoryItemStack(
                            itemId,
                            itemId));
                }
            }
        }

        public static void BeginRestore(
            RunData savedRun)
        {
            pendingRoomStates =
                new Dictionary<string, RoomRunState>();

            if (savedRun?.DungeonState?.Rooms == null)
            {
                return;
            }

            foreach (RoomRunState room
                     in savedRun.DungeonState.Rooms)
            {
                if (room != null
                    && !string.IsNullOrEmpty(room.RoomId))
                {
                    pendingRoomStates[room.RoomId] =
                        room;
                }
            }
        }

        public static bool TryGetRoomState(
            string roomId,
            out RoomRunState state)
        {
            if (pendingRoomStates != null
                && pendingRoomStates.TryGetValue(
                    roomId,
                    out state))
            {
                return true;
            }

            state = null;
            return false;
        }

        public static void ClearPendingRestore()
        {
            pendingRoomStates = null;
        }

        private static void SaveGeneratedRooms(
            RunContext context,
            GeneratedDungeon dungeon,
            RunData data)
        {
            List<RoomNode> rooms =
                new List<RoomNode>(
                    dungeon.Layout.AllRooms);

            rooms.Sort(
                (left, right) =>
                    string.CompareOrdinal(
                        left.RoomId,
                        right.RoomId));

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomNode node =
                    rooms[i];

                context.Dungeon.TryGetRoom(
                    node.RoomId,
                    out RoomInstance roomInstance);

                RoomRunState roomData =
                    new RoomRunState
                    {
                        RoomId = node.RoomId,
                        Coordinate =
                            new Vector2Int(
                                node.MacroCoordinate.X,
                                node.MacroCoordinate.Z),
                        Visited =
                            roomInstance != null
                            && roomInstance.Visited,
                        Discovered =
                            context.Dungeon.IsRoomRevealed(
                                node.RoomId),
                        Completed =
                            roomInstance != null
                            && roomInstance.Completed,
                        ChestOpened =
                            roomInstance != null
                            && roomInstance.ChestOpened,
                        IsStairs =
                            dungeon.StairsRoom != null
                            && dungeon.StairsRoom.RoomId
                                == node.RoomId
                    };

                foreach (CardinalDirection direction
                         in node.Connections.Keys)
                {
                    roomData.ConnectedDirections.Add(
                        (int)direction);
                }

                roomData.ConnectedDirections.Sort();

                roomData.StairsDiscovered =
                    roomData.IsStairs
                    && roomData.Discovered;

                data.DungeonState.Rooms.Add(
                    roomData);

                if (node.RoomId
                    == context.Player.CurrentRoomId)
                {
                    data.BasicInfo.CurrentRoomCoordinate =
                        roomData.Coordinate;
                }
            }
        }

        private static void SaveLegacyRooms(
            RunContext context,
            RunData data)
        {
            foreach (RoomInstance room
                     in context.Dungeon.AllRooms)
            {
                data.DungeonState.Rooms.Add(
                    new RoomRunState
                    {
                        RoomId = room.RoomId,
                        Visited = room.Visited,
                        Discovered =
                            context.Dungeon.IsRoomRevealed(
                                room.RoomId),
                        Completed = room.Completed,
                        ChestOpened = room.ChestOpened
                    });
            }
        }
    }
}
