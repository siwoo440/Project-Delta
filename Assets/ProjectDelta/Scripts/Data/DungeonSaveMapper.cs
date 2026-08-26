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
                throw new ArgumentNullException(
                    nameof(context));
            }

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

            data.PlayerStats.Gold =
                Math.Max(
                    0,
                    context.Player.Gold);

            // 83일차: 도주 후 받은 피해와 소모 자원을 그대로 이어하기 위해 현재 자원을 저장한다.
            StatBlock finalStats =
                context.Player.GetFinalStats();

            data.PlayerStats.MaxHealth =
                Math.Max(
                    0,
                    finalStats.MaxHealth);

            data.PlayerStats.CurrentHealth =
                Clamp(
                    context.Player.CurrentHp,
                    0,
                    data.PlayerStats.MaxHealth);

            data.PlayerStats.MaxMana =
                Math.Max(
                    0,
                    finalStats.MaxMana);

            data.PlayerStats.CurrentMana =
                Clamp(
                    context.Player.CurrentMana,
                    0,
                    data.PlayerStats.MaxMana);

            data.PlayerStats.MaxStamina =
                Math.Max(
                    0,
                    finalStats.MaxStamina);

            data.PlayerStats.CurrentStamina =
                Clamp(
                    context.Player.CurrentStamina,
                    0,
                    data.PlayerStats.MaxStamina);

            SavePersistentStatusEffects(
                context.Player,
                data.PlayerStats);

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

            // 89일차: 슬롯 위치와 확장 보너스를 저장한다.
            data.Inventory.PermanentSlotBonus =
                context.Inventory.PermanentSlotBonus;

            data.Inventory.BagSlotBonus =
                context.Inventory.BagSlotBonus;

            for (int slotIndex = 0;
                 slotIndex < context.Inventory.Slots.Count;
                 slotIndex++)
            {
                InventorySlotState slot =
                    context.Inventory.Slots[slotIndex];

                data.Inventory.Slots.Add(
                    new RunInventorySlotData
                    {
                        ItemId =
                            slot.IsEmpty
                                ? string.Empty
                                : slot.ItemId,
                        DisplayName =
                            slot.IsEmpty
                                ? string.Empty
                                : slot.DisplayName,
                        Quantity =
                            slot.IsEmpty
                                ? 0
                                : slot.Quantity,
                        // 95일차: HUD 초기화와 무관하게 저장 자체가 Stack 규칙을 보존한다.
                        MaxStackSize =
                            slot.IsEmpty
                                ? 0
                                : slot.MaxStackSize
                    });

                if (!slot.IsEmpty)
                {
                    // 89일차 이전 저장 형식과 기존 조회 코드의 호환 목록도 함께 유지한다.
                    data.Inventory.InventoryItemIds.Add(
                        slot.ItemId);
                }
            }

            return data;
        }

        public static void ApplyBasics(
            RunContext context,
            RunData savedRun)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            if (savedRun == null)
            {
                throw new ArgumentNullException(
                    nameof(savedRun));
            }

            int savedFloor =
                savedRun.BasicInfo.CurrentFloor > 0
                    ? savedRun.BasicInfo.CurrentFloor
                    : 1;

            context.Dungeon.SetFloor(
                savedFloor);

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

                context.Player.Gold =
                    Math.Max(
                        0,
                        savedRun.PlayerStats.Gold);

                RestorePlayerResources(
                    context.Player,
                    savedRun.PlayerStats);

                RestorePersistentStatusEffects(
                    context.Player,
                    savedRun.PlayerStats);
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
                        && !string.IsNullOrEmpty(
                            room.RoomId))
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

            // 89일차: 새 슬롯 저장이 있으면 위치를 보존해 복원한다.
            if (savedRun.Inventory?.Slots != null
                && savedRun.Inventory.Slots.Count > 0)
            {
                context.Inventory.ResetForRestore(
                    savedRun.Inventory.PermanentSlotBonus,
                    savedRun.Inventory.BagSlotBonus);

                int restoreCount =
                    Math.Min(
                        context.Inventory.Capacity,
                        savedRun.Inventory.Slots.Count);

                for (int slotIndex = 0;
                     slotIndex < restoreCount;
                     slotIndex++)
                {
                    RunInventorySlotData slot =
                        savedRun.Inventory.Slots[slotIndex];

                    if (slot == null
                        || string.IsNullOrEmpty(
                            slot.ItemId)
                        || slot.Quantity <= 0)
                    {
                        continue;
                    }

                    // 95일차 이후 저장은 저장된 Stack 상한을 직접 사용한다.
                    if (slot.MaxStackSize > 0)
                    {
                        context.Inventory.RestoreSlot(
                            slotIndex,
                            slot.ItemId,
                            slot.DisplayName,
                            slot.Quantity,
                            slot.MaxStackSize);
                    }
                    else
                    {
                        // 이전 저장은 기존 resolver 경로를 유지해 하위 호환한다.
                        context.Inventory.RestoreSlot(
                            slotIndex,
                            slot.ItemId,
                            slot.DisplayName,
                            slot.Quantity);
                    }
                }
            }
            else if (savedRun.Inventory?.InventoryItemIds != null)
            {
                // 89일차 이전 저장은 기존 ID 순서를 첫 빈 슬롯부터 옮겨 담는다.
                context.Inventory.ResetForRestore(
                    0,
                    0);

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
                    && !string.IsNullOrEmpty(
                        room.RoomId))
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

            state =
                null;

            return false;
        }

        public static void ClearPendingRestore()
        {
            pendingRoomStates =
                null;
        }

        private static void SavePersistentStatusEffects(
            PlayerRunState player,
            PlayerRunStats stats)
        {
            stats.ActiveStatusEffectIds.Clear();
            stats.ActiveStatusEffects.Clear();

            if (player.PersistentStatusEffects == null)
            {
                return;
            }

            for (int index = 0;
                 index < player.PersistentStatusEffects.Count;
                 index++)
            {
                PersistentStatusEffectState status =
                    player.PersistentStatusEffects[index];

                if (status == null
                    || status.RemainingDuration <= 0
                    || string.IsNullOrEmpty(
                        status.DefinitionId))
                {
                    continue;
                }

                stats.ActiveStatusEffectIds.Add(
                    status.DefinitionId);

                stats.ActiveStatusEffects.Add(
                    new PlayerStatusEffectRunState
                    {
                        DefinitionId = status.DefinitionId,
                        SourceInstanceId = status.SourceInstanceId,
                        RemainingDuration = status.RemainingDuration,
                        StackCount = Math.Max(1, status.StackCount),
                        AppliedValue = status.AppliedValue,
                        EffectKind = status.EffectKind,
                        TargetStat = status.TargetStat
                    });
            }
        }

        private static void RestorePlayerResources(
            PlayerRunState player,
            PlayerRunStats stats)
        {
            // 83일차 이전 저장은 MaxHealth/MaxMana가 0이므로 기본 자원을 그대로 유지한다.
            if (stats.MaxHealth <= 0)
            {
                return;
            }

            StatBlock finalStats =
                player.GetFinalStats();

            player.CurrentHp =
                Clamp(
                    stats.CurrentHealth,
                    0,
                    Math.Max(
                        0,
                        finalStats.MaxHealth));

            player.CurrentMana =
                Clamp(
                    stats.CurrentMana,
                    0,
                    Math.Max(
                        0,
                        finalStats.MaxMana));

            player.CurrentStamina =
                Clamp(
                    stats.CurrentStamina,
                    0,
                    Math.Max(
                        0,
                        finalStats.MaxStamina));
        }

        private static void RestorePersistentStatusEffects(
            PlayerRunState player,
            PlayerRunStats stats)
        {
            player.PersistentStatusEffects.Clear();
            player.StatusEffects.Clear();

            if (stats.ActiveStatusEffects == null)
            {
                return;
            }

            for (int index = 0;
                 index < stats.ActiveStatusEffects.Count;
                 index++)
            {
                PlayerStatusEffectRunState saved =
                    stats.ActiveStatusEffects[index];

                if (saved == null
                    || saved.RemainingDuration <= 0
                    || string.IsNullOrEmpty(
                        saved.DefinitionId))
                {
                    continue;
                }

                player.PersistentStatusEffects.Add(
                    new PersistentStatusEffectState
                    {
                        DefinitionId = saved.DefinitionId,
                        SourceInstanceId = saved.SourceInstanceId,
                        RemainingDuration = saved.RemainingDuration,
                        StackCount = Math.Max(1, saved.StackCount),
                        AppliedValue = saved.AppliedValue,
                        EffectKind = saved.EffectKind,
                        TargetStat = saved.TargetStat
                    });

                player.StatusEffects.Add(
                    saved.DefinitionId);
            }
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

            for (int i = 0;
                 i < rooms.Count;
                 i++)
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

                SaveChestContents(
                    roomInstance,
                    roomData);

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
                RoomRunState roomData =
                    new RoomRunState
                    {
                        RoomId = room.RoomId,
                        Visited = room.Visited,
                        Discovered =
                            context.Dungeon.IsRoomRevealed(
                                room.RoomId),
                        Completed = room.Completed,
                        ChestOpened = room.ChestOpened
                    };

                SaveChestContents(
                    room,
                    roomData);

                data.DungeonState.Rooms.Add(
                    roomData);
            }
        }

        private static void SaveChestContents(
            RoomInstance roomInstance,
            RoomRunState roomData)
        {
            if (roomInstance == null
                || roomData == null
                || !roomInstance.HasChestContentsSnapshot)
            {
                return;
            }

            roomData.HasChestContentsSnapshot =
                true;

            roomData.ChestRemainingItems.Clear();

            for (int index = 0;
                 index < roomInstance.ChestRemainingItems.Count;
                 index++)
            {
                roomData.ChestRemainingItems.Add(
                    roomInstance.ChestRemainingItems[index]);
            }
        }

        private static int Clamp(
            int value,
            int min,
            int max)
        {
            if (max < min)
            {
                return min;
            }

            return Math.Max(
                min,
                Math.Min(
                    max,
                    value));
        }
    }
}
