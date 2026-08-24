using ProjectDelta.Application; // Encounter 애플리케이션 로직 사용
using ProjectDelta.Domain; // 플레이어 런타임 상태 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    [RequireComponent(typeof(PlayerGridMovementController))] // 이동 컨트롤러 필수
    public sealed class ExplorationMonsterEncounterController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovementController movementController; // 플레이어 이동 컨트롤러
        [SerializeField] private PlayerLookController lookController; // 플레이어 시점 컨트롤러
        [SerializeField] private DungeonFloorController floorController; // 현재 층 컨트롤러

        private readonly ExplorationEncounterSession session =
            new ExplorationEncounterSession(); // Encounter 생명주기 상태

        private readonly IEncounterCommand battleCommand =
            new BattleEncounterCommand(); // 전투 행동 Command

        private readonly IEncounterCommand escapeCommand =
            new EscapeEncounterCommand(); // 회피 행동 Command

        private readonly EncounterActionSelectionGate actionSelectionGate =
            new EncounterActionSelectionGate(); // 행동 중복 선택 방지 Gate

        private ExplorationMonsterMarker activeMonster; // 현재 Encounter 몬스터
        private bool wasMoving; // 이전 프레임 이동 여부
        private bool ownsExplorationControlLock; // Encounter가 탐험 잠금을 소유하는지 여부
        private bool movementLockBeforeEncounter; // Encounter 시작 전 이동 잠금 상태

        public bool IsEncounterActive =>
            session.State != EncounterState.Idle; // Encounter 진행 여부

        public EncounterState CurrentState =>
            session.State; // 현재 Encounter 상태

        public EncounterContext CurrentContext =>
            session.Context; // 현재 Encounter Context

        public string ActiveMonsterDefinitionId =>
            session.MonsterDefinitionId; // 현재 몬스터 ID

        public EncounterCommandResult LastCommandResult { get; private set; } // 마지막 행동 결과

        public bool HasSelectedEncounterAction =>
            actionSelectionGate.HasSelection; // 현재 Encounter 행동 확정 여부

        public string SelectedEncounterCommandId =>
            actionSelectionGate.SelectedCommandId; // 현재 확정 Command ID

        private void Awake()
        {
            if (movementController == null) // 이동 컨트롤러 참조 확인
            {
                movementController =
                    GetComponent<PlayerGridMovementController>(); // 같은 오브젝트에서 자동 연결
            }

            if (lookController == null) // 시점 컨트롤러 참조 확인
            {
                lookController =
                    GetComponent<PlayerLookController>(); // 같은 오브젝트에서 자동 연결
            }

            if (floorController == null) // 층 컨트롤러 참조 확인
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>(); // 씬에서 자동 검색
            }
        }

        private void OnEnable()
        {
            wasMoving =
                movementController != null
                && movementController.IsMoving; // 현재 이동 상태 저장
        }

        private void OnDisable()
        {
            RestoreExplorationControl(); // Encounter가 소유한 탐험 잠금만 복구

            session.ForceReset(); // Encounter 생명주기 초기화
            actionSelectionGate.Reset(); // 행동 선택 상태 초기화
            activeMonster = null; // 대상 몬스터 참조 제거
            LastCommandResult = null; // 마지막 행동 결과 제거
            wasMoving = false; // 이동 감지 상태 초기화
        }

        private void Update()
        {
            if (movementController == null) // 이동 컨트롤러 확인
            {
                return; // 감지 처리 중단
            }

            bool isMovingNow =
                movementController.IsMoving; // 현재 이동 여부 읽기

            if (wasMoving && !isMovingNow) // 한 칸 이동 완료 시점 확인
            {
                TryBeginEncounterAtCurrentPosition(); // 현재 위치 기준 Encounter 검사
            }

            wasMoving =
                isMovingNow; // 다음 프레임 비교 상태 저장
        }

        public bool TryBeginEncounterAtCurrentPosition()
        {
            if (session.State != EncounterState.Idle
                || movementController == null
                || movementController.PlayerState == null) // Encounter 시작 조건 확인
            {
                return false; // 시작 불가
            }

            if (floorController == null) // 층 컨트롤러 재확인
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>(); // 씬에서 다시 검색
            }

            if (floorController == null) // 층 컨트롤러 누락 확인
            {
                return false; // 몬스터 조회 불가
            }

            PlayerRunState playerState =
                movementController.PlayerState; // 현재 플레이어 상태 읽기

            if (!floorController.SpawnedMonsters.TryGetValue(
                    playerState.CurrentRoomId,
                    out ExplorationMonsterMarker monster)
                || monster == null
                || !monster.gameObject.activeInHierarchy) // 현재 방 활성 몬스터 확인
            {
                return false; // 대상 몬스터 없음
            }

            if (!session.TryBegin(
                    playerState.CurrentRoomId,
                    playerState.CurrentGridPosition,
                    monster.RoomId,
                    monster.GridPosition,
                    monster.MonsterDefinitionId)) // 같은 방·8방향 1칸 이내 Encounter 시작 요청
            {
                return false; // 포착 조건 불충족
            }

            activeMonster =
                monster; // 현재 Encounter 몬스터 저장

            LastCommandResult =
                null; // 이전 행동 결과 초기화

            actionSelectionGate.Reset(); // 새 Encounter 행동 선택 상태 초기화

            LockExplorationControl(); // 탐험 이동 잠금 및 UI 커서 전환

            Debug.Log(
                $"[Project Delta] 45일차 Encounter Starting / Room {monster.RoomId} / Player {playerState.CurrentGridPosition} / Monster {monster.GridPosition} / Target {monster.MonsterDefinitionId}",
                this); // Encounter 시작 로그

            if (!session.TryActivate()) // Starting → Active 전환 시도
            {
                Debug.LogError(
                    "[Project Delta] Encounter Starting → Active 전환에 실패했습니다.",
                    this); // 전환 실패 로그

                AbortEncounter(); // 안전 중단
                return false; // 시작 실패
            }

            Debug.Log(
                $"[Project Delta] 45일차 Encounter Active / Monster {monster.MonsterDefinitionId}",
                this); // Active 로그

            return true; // Encounter 진입 성공
        }

        public EncounterActionAvailability GetActionAvailability()
        {
            return actionSelectionGate.Evaluate(
                session.State,
                session.Context); // 현재 행동 선택 가능 여부 계산
        }

        public EncounterCommandResult SelectBattleCommand()
        {
            return ExecuteEncounterCommand(
                battleCommand); // 전투 Command 실행 요청
        }

        public EncounterCommandResult SelectEscapeCommand()
        {
            return ExecuteEncounterCommand(
                escapeCommand); // 회피 Command 실행 요청
        }

        public void CompleteTestEncounter()
        {
            if (session.State != EncounterState.Active) // Active 상태 확인
            {
                return; // 종료 처리 생략
            }

            if (!session.TryBeginResolve()) // Active → Resolving 전환 시도
            {
                return; // 전환 실패
            }

            Debug.Log(
                "[Project Delta] 45일차 Encounter Resolving",
                this); // Resolving 로그

            if (activeMonster != null) // 현재 몬스터 참조 확인
            {
                activeMonster.gameObject.SetActive(false); // 테스트 몬스터 비활성화
            }

            if (!session.TryFinish()) // Resolving → Finished 전환 시도
            {
                Debug.LogError(
                    "[Project Delta] Encounter Resolving → Finished 전환에 실패했습니다.",
                    this); // 전환 실패 로그

                AbortEncounter(); // 안전 중단
                return;
            }

            Debug.Log(
                "[Project Delta] 45일차 Encounter Finished",
                this); // Finished 로그

            activeMonster =
                null; // 현재 몬스터 참조 제거

            LastCommandResult =
                null; // 행동 결과 초기화

            actionSelectionGate.Reset(); // 행동 선택 상태 초기화

            RestoreExplorationControl(); // Encounter가 소유한 탐험 잠금 복구

            if (!session.TryReset()) // Finished → Idle 전환 시도
            {
                Debug.LogError(
                    "[Project Delta] Encounter Finished → Idle 전환에 실패했습니다.",
                    this); // 전환 실패 로그

                session.ForceReset(); // 상태 강제 초기화
            }

            Debug.Log(
                "[Project Delta] 45일차 Encounter Idle 복귀 / 탐험 재개",
                this); // 탐험 복귀 로그
        }

        private EncounterCommandResult ExecuteEncounterCommand(
            IEncounterCommand command)
        {
            if (command == null) // Command 참조 확인
            {
                return null; // 잘못된 호출 거부
            }

            EncounterActionAvailability availability =
                GetActionAvailability(); // 현재 선택 가능 조건 계산

            if (!availability.CanSelect) // 행동 선택 불가 확인
            {
                EncounterCommandResult rejected =
                    EncounterCommandResult.Reject(
                        command.Id,
                        availability.Reason); // 선택 불가 결과 생성

                LastCommandResult =
                    rejected; // 실패 결과 저장

                return rejected; // 실패 결과 반환
            }

            EncounterCommandResult result =
                command.Execute(
                    session.Context); // 실제 Command 실행

            if (result == null) // Command 결과 확인
            {
                EncounterCommandResult rejected =
                    EncounterCommandResult.Reject(
                        command.Id,
                        "행동 처리 결과를 확인할 수 없습니다."); // 결과 누락 실패 생성

                LastCommandResult =
                    rejected; // 실패 결과 저장

                return rejected; // 실패 결과 반환
            }

            if (result.Accepted) // 정상 선택 결과 확인
            {
                if (!actionSelectionGate.TryCommit(
                        result.CommandId)) // 첫 행동 선택 확정
                {
                    EncounterCommandResult rejected =
                        EncounterCommandResult.Reject(
                            command.Id,
                            "이미 행동을 선택했습니다."); // 예상치 못한 중복 확정 차단

                    LastCommandResult =
                        rejected; // 중복 실패 결과 저장

                    return rejected; // 중복 실패 반환
                }
            }

            LastCommandResult =
                result; // Command 결과 저장

            Debug.Log(
                $"[Project Delta] 45일차 Encounter Command / Id {result.CommandId} / Accepted {result.Accepted} / {result.Message}",
                this); // Command 실행 로그

            return result; // Command 결과 반환
        }

        private void AbortEncounter()
        {
            activeMonster =
                null; // 대상 몬스터 참조 제거

            LastCommandResult =
                null; // 행동 결과 제거

            actionSelectionGate.Reset(); // 행동 선택 상태 초기화

            RestoreExplorationControl(); // Encounter가 소유한 탐험 잠금 복구
            session.ForceReset(); // Encounter 상태 강제 초기화
        }

        private void LockExplorationControl()
        {
            if (ownsExplorationControlLock) // 중복 잠금 확인
            {
                return; // 기존 잠금 유지
            }

            if (movementController != null) // 이동 컨트롤러 확인
            {
                movementLockBeforeEncounter =
                    movementController.IsInputLocked; // 기존 이동 잠금 상태 보관

                movementController.IsInputLocked =
                    true; // Encounter 중 이동 입력 잠금
            }

            if (lookController != null) // 시점 컨트롤러 확인
            {
                lookController.SetCursorFreeForUi(
                    true); // UI 클릭용 커서 해제
            }

            ownsExplorationControlLock =
                true; // Encounter 잠금 소유 상태 저장
        }

        private void RestoreExplorationControl()
        {
            if (!ownsExplorationControlLock) // Encounter 잠금 소유 여부 확인
            {
                return; // 다른 시스템의 잠금 상태는 변경하지 않음
            }

            if (movementController != null) // 이동 컨트롤러 확인
            {
                movementController.IsInputLocked =
                    movementLockBeforeEncounter; // Encounter 이전 잠금 상태로 복구
            }

            if (lookController != null) // 시점 컨트롤러 확인
            {
                lookController.SetCursorFreeForUi(
                    false); // Encounter UI 커서 요청 해제
            }

            movementLockBeforeEncounter =
                false; // 보관 잠금 상태 초기화

            ownsExplorationControlLock =
                false; // Encounter 잠금 소유 해제
        }
    }
}
