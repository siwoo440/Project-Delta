using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 43일차: 탐험 Encounter 생명주기를 명시적인 상태 머신으로 관리한다.
    public sealed class ExplorationEncounterSession
    {
        public EncounterState State { get; private set; } =
            EncounterState.Idle;

        public EncounterContext Context { get; private set; }

        public bool IsActive =>
            State != EncounterState.Idle;

        public string MonsterDefinitionId =>
            Context != null
                ? Context.MonsterDefinitionId
                : null;

        public bool TryBegin(
            string playerRoomId,
            GridPosition playerPosition,
            string monsterRoomId,
            GridPosition monsterPosition,
            string monsterDefinitionId)
        {
            if (State != EncounterState.Idle)
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

            Context =
                new EncounterContext(
                    monsterRoomId,
                    monsterDefinitionId,
                    monsterPosition);

            State =
                EncounterState.Starting;

            return true;
        }

        public bool TryActivate()
        {
            if (State != EncounterState.Starting)
            {
                return false;
            }

            State =
                EncounterState.Active;

            return true;
        }

        public bool TryBeginResolve()
        {
            if (State != EncounterState.Active)
            {
                return false;
            }

            State =
                EncounterState.Resolving;

            return true;
        }

        public bool TryFinish()
        {
            if (State != EncounterState.Resolving)
            {
                return false;
            }

            State =
                EncounterState.Finished;

            return true;
        }

        public bool TryReset()
        {
            if (State != EncounterState.Finished)
            {
                return false;
            }

            Context =
                null;

            State =
                EncounterState.Idle;

            return true;
        }

        // 씬 비활성화·오브젝트 종료 같은 비정상 중단 시 잠금 상태를 남기지 않기 위한 안전 초기화.
        public void ForceReset()
        {
            Context =
                null;

            State =
                EncounterState.Idle;
        }
    }
}
