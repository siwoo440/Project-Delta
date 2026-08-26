using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 40일차: 생성된 방들에 0~1개의 Monster Encounter를 결정적으로 배정한다.
    public sealed class RoomEncounterPlacementService
    {
        public DungeonEncounterLayout Build(
            GeneratedDungeon dungeon,
            int dungeonSeed,
            EncounterDefinition encounter,
            IEnumerable<string> excludedRoomIds = null)
        {
            if (dungeon == null)
            {
                throw new ArgumentNullException(
                    nameof(dungeon));
            }

            DungeonEncounterLayout result =
                new DungeonEncounterLayout();

            if (encounter == null
                || !encounter.IsValidForPlacement)
            {
                return result;
            }

            float spawnChance =
                encounter.RoomSpawnChance;

            if (spawnChance <= 0f)
            {
                return result;
            }

            if (spawnChance > 1f)
            {
                spawnChance = 1f;
            }

            HashSet<string> excluded =
                new HashSet<string>(
                    StringComparer.Ordinal);

            if (excludedRoomIds != null)
            {
                foreach (string roomId
                         in excludedRoomIds)
                {
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        excluded.Add(roomId);
                    }
                }
            }

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

            for (int i = 0;
                 i < dungeon.SpecialRoomCandidates.Count;
                 i++)
            {
                RoomNode specialRoom =
                    dungeon.SpecialRoomCandidates[i];

                if (specialRoom != null
                    && !string.IsNullOrEmpty(
                        specialRoom.RoomId))
                {
                    excluded.Add(
                        specialRoom.RoomId);
                }
            }

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
                RoomNode room =
                    rooms[i];

                if (room == null
                    || excluded.Contains(room.RoomId))
                {
                    continue;
                }

                float roll =
                    CalculateStableRoll(
                        dungeonSeed,
                        room.RoomId,
                        encounter.Id);

                if (roll >= spawnChance)
                {
                    continue;
                }

                // 76일차: 이 방에 실제로 몇 마리의 어떤 몬스터가 배치되는지 결정론적으로 뽑는다.
                MonsterGroupCompositionService.Result group =
                    MonsterGroupCompositionService.Build(
                        encounter,
                        dungeonSeed,
                        room.RoomId);

                string[] monsterDefinitionIds =
                    new string[group.Slots.Count];

                for (int slotIndex = 0; slotIndex < group.Slots.Count; slotIndex++)
                {
                    monsterDefinitionIds[slotIndex] =
                        group.Slots[slotIndex].Id;
                }

                result.TryAdd(
                    new RoomEncounterAssignment(
                        room.RoomId,
                        RoomContentType.Monster,
                        encounter.Id,
                        monsterDefinitionIds,
                        group.Representative.Id));
            }

            return result;
        }

        // string.GetHashCode()는 런타임/플랫폼마다 값이 달라질 수 있으므로 사용하지 않는다.
        // Seed + RoomId + EncounterId만으로 항상 같은 0~1 난수를 만든다.
        // 76일차: 해시 혼합 로직 자체는 DeterministicRollHash로 옮겼다 - 결과값은 그대로다.
        public static float CalculateStableRoll(
            int dungeonSeed,
            string roomId,
            string encounterDefinitionId)
        {
            return DeterministicRollHash.ComputeRoll01(
                dungeonSeed,
                roomId,
                encounterDefinitionId);
        }
    }
}
