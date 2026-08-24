using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public sealed class RoomEncounterAssignment
    {
        public string RoomId { get; }
        public RoomContentType ContentType { get; }
        public string EncounterDefinitionId { get; }
        public string MonsterDefinitionId { get; }

        public RoomEncounterAssignment(
            string roomId,
            RoomContentType contentType,
            string encounterDefinitionId,
            string monsterDefinitionId)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                throw new ArgumentException(
                    "roomId는 비어있을 수 없습니다.",
                    nameof(roomId));
            }

            RoomId = roomId;
            ContentType = contentType;
            EncounterDefinitionId = encounterDefinitionId;
            MonsterDefinitionId = monsterDefinitionId;
        }
    }

    // 40일차: 한 층에서 어떤 방에 어떤 인카운터가 배정됐는지 기록하는 논리 결과.
    // 실제 Monster GameObject와 GridPosition 배치는 41일차에서 연결한다.
    public sealed class DungeonEncounterLayout
    {
        private readonly Dictionary<string, RoomEncounterAssignment>
            assignmentsByRoomId =
                new Dictionary<string, RoomEncounterAssignment>();

        public IReadOnlyCollection<RoomEncounterAssignment> Assignments =>
            assignmentsByRoomId.Values;

        public int Count =>
            assignmentsByRoomId.Count;

        public bool TryAdd(
            RoomEncounterAssignment assignment)
        {
            if (assignment == null
                || assignmentsByRoomId.ContainsKey(
                    assignment.RoomId))
            {
                return false;
            }

            assignmentsByRoomId.Add(
                assignment.RoomId,
                assignment);

            return true;
        }

        public bool TryGet(
            string roomId,
            out RoomEncounterAssignment assignment)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                assignment = null;
                return false;
            }

            return assignmentsByRoomId.TryGetValue(
                roomId,
                out assignment);
        }
    }
}
