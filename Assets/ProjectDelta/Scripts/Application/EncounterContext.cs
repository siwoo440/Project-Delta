using System.Collections.Generic;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 43일차: 현재 탐험 Encounter가 어디서 어떤 몬스터와 시작됐는지 보관한다.
    public sealed class EncounterContext
    {
        public string RoomId { get; }

        // 대표(탐험 화면에 보이던) 몬스터 ID. 76일차 이전에는 이게 유일한 몬스터였다.
        public string MonsterDefinitionId { get; }

        public GridPosition MonsterGridPosition { get; }

        // 76일차: 실제 전투를 구성할 그룹 전체 - 자리 순서대로 정렬돼 있다.
        public IReadOnlyList<string> MonsterGroupDefinitionIds { get; }

        public EncounterContext(
            string roomId,
            string monsterDefinitionId,
            GridPosition monsterGridPosition,
            IReadOnlyList<string> monsterGroupDefinitionIds = null)
        {
            RoomId = roomId;
            MonsterDefinitionId = monsterDefinitionId;
            MonsterGridPosition = monsterGridPosition;

            MonsterGroupDefinitionIds =
                monsterGroupDefinitionIds != null
                && monsterGroupDefinitionIds.Count > 0
                    ? monsterGroupDefinitionIds
                    : new[] { monsterDefinitionId }; // 그룹 정보가 없으면 대표 하나로 취급 (호환)
        }
    }
}
