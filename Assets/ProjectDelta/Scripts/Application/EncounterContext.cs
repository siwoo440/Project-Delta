using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 43일차: 현재 탐험 Encounter가 어디서 어떤 몬스터와 시작됐는지 보관한다.
    public sealed class EncounterContext
    {
        public string RoomId { get; }
        public string MonsterDefinitionId { get; }
        public GridPosition MonsterGridPosition { get; }

        public EncounterContext(
            string roomId,
            string monsterDefinitionId,
            GridPosition monsterGridPosition)
        {
            RoomId = roomId;
            MonsterDefinitionId = monsterDefinitionId;
            MonsterGridPosition = monsterGridPosition;
        }
    }
}
