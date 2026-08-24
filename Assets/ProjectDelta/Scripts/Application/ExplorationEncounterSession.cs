using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 42일차: 탐험 중 플레이어와 몬스터의 논리 좌표가 겹쳤을 때
    // 테스트 Encounter가 한 번만 시작되도록 최소 세션 상태를 관리한다.
    public sealed class ExplorationEncounterSession
    {
        public bool IsActive { get; private set; }
        public string MonsterDefinitionId { get; private set; }

        public bool TryBegin(
            string playerRoomId,
            GridPosition playerPosition,
            string monsterRoomId,
            GridPosition monsterPosition,
            string monsterDefinitionId)
        {
            if (IsActive)
            {
                return false;
            }

            if (string.IsNullOrEmpty(playerRoomId)
                || string.IsNullOrEmpty(monsterRoomId)
                || string.IsNullOrEmpty(monsterDefinitionId))
            {
                return false;
            }

            if (playerRoomId != monsterRoomId
                || playerPosition != monsterPosition)
            {
                return false;
            }

            IsActive = true;
            MonsterDefinitionId = monsterDefinitionId;
            return true;
        }

        public void Complete()
        {
            IsActive = false;
            MonsterDefinitionId = null;
        }
    }
}
