using System.Collections.Generic; // IReadOnlyList 사용
using ProjectDelta.Domain; // GridPosition 사용

namespace ProjectDelta.Application // 애플리케이션 네임스페이스
{
    // 43일차: 탐험 Encounter 생명주기를 명시적인 상태 머신으로 관리한다.
    public sealed class ExplorationEncounterSession
    {
        public EncounterState State { get; private set; } =
            EncounterState.Idle; // 현재 Encounter 상태

        public EncounterContext Context { get; private set; } // 현재 Encounter 정보

        public bool IsActive =>
            State != EncounterState.Idle; // Encounter 진행 여부

        public string MonsterDefinitionId =>
            Context != null
                ? Context.MonsterDefinitionId
                : null; // 현재 대상 몬스터 ID

        public bool TryBegin(
            string playerRoomId,
            GridPosition playerPosition,
            string monsterRoomId,
            GridPosition monsterPosition,
            string monsterDefinitionId,
            IReadOnlyList<string> monsterGroupDefinitionIds = null)
        {
            if (State != EncounterState.Idle) // 중복 Encounter 확인
            {
                return false; // 이미 진행 중이면 시작 거부
            }

            if (string.IsNullOrEmpty(playerRoomId)
                || string.IsNullOrEmpty(monsterRoomId)
                || string.IsNullOrEmpty(monsterDefinitionId)) // 필수 ID 확인
            {
                return false; // 필수 정보 누락 거부
            }

            if (playerRoomId != monsterRoomId) // 같은 방 여부 확인
            {
                return false; // 다른 방이면 Encounter 거부
            }

            if (!EncounterRangeRule.IsWithinRange(
                    playerPosition,
                    monsterPosition)) // 몬스터 주변 8방향 1칸 범위 확인
            {
                return false; // 포착 범위 밖이면 Encounter 거부
            }

            Context =
                new EncounterContext(
                    monsterRoomId,
                    monsterDefinitionId,
                    monsterPosition,
                    monsterGroupDefinitionIds); // Encounter Context 생성 (76일차: 그룹 구성 포함)

            State =
                EncounterState.Starting; // 시작 상태 전환

            return true; // Encounter 시작 성공
        }

        public bool TryActivate()
        {
            if (State != EncounterState.Starting) // Starting 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            State =
                EncounterState.Active; // Active 상태 전환

            return true; // 전환 성공
        }

        public bool TryBeginResolve()
        {
            if (State != EncounterState.Active) // Active 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            State =
                EncounterState.Resolving; // Resolving 상태 전환

            return true; // 전환 성공
        }

        public bool TryFinish()
        {
            if (State != EncounterState.Resolving) // Resolving 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            State =
                EncounterState.Finished; // Finished 상태 전환

            return true; // 전환 성공
        }

        public bool TryReset()
        {
            if (State != EncounterState.Finished) // Finished 상태 확인
            {
                return false; // 잘못된 초기화 거부
            }

            Context =
                null; // Encounter Context 제거

            State =
                EncounterState.Idle; // Idle 상태 복귀

            return true; // 초기화 성공
        }

        // 씬 비활성화·오브젝트 종료 같은 비정상 중단 시 잠금 상태를 남기지 않기 위한 안전 초기화.
        public void ForceReset()
        {
            Context =
                null; // Encounter Context 강제 제거

            State =
                EncounterState.Idle; // Idle 강제 복귀
        }
    }
}
