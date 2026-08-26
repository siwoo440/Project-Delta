using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public sealed class RoomEncounterAssignment
    {
        public string RoomId { get; }
        public RoomContentType ContentType { get; }
        public string EncounterDefinitionId { get; }

        // 76일차: 이 방에 실제로 배치된 몬스터 그룹 - 자리(0번이 1번 자리) 순서대로 정렬돼 있다.
        // MonsterDefinitionId(대표 외형)는 이 목록 중 하나를 가리킨다.
        public IReadOnlyList<string> MonsterDefinitionIds { get; }

        // 47일차부터 쓰던 이름을 유지한다 - 탐험 화면에 보여줄 "대표" 몬스터 ID
        // (76일차: 그룹 중 등급이 가장 높은 몬스터, 동률이면 가장 앞 자리).
        public string MonsterDefinitionId { get; }

        public RoomEncounterAssignment(
            string roomId,
            RoomContentType contentType,
            string encounterDefinitionId,
            IReadOnlyList<string> monsterDefinitionIds,
            string representativeMonsterDefinitionId)
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
            MonsterDefinitionIds = monsterDefinitionIds ?? Array.Empty<string>();
            MonsterDefinitionId = representativeMonsterDefinitionId;
        }

        // 47일차부터의 단일 몬스터 호출부(테스트 등) 호환용 - 대표 = 유일한 몬스터로 취급한다.
        public RoomEncounterAssignment(
            string roomId,
            RoomContentType contentType,
            string encounterDefinitionId,
            string monsterDefinitionId)
            : this(
                roomId,
                contentType,
                encounterDefinitionId,
                new[] { monsterDefinitionId },
                monsterDefinitionId)
        {
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
