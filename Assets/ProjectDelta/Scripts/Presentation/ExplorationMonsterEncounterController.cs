using ProjectDelta.Application;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGridMovementController))]
    public sealed class ExplorationMonsterEncounterController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovementController movementController;
        [SerializeField] private PlayerLookController lookController;
        [SerializeField] private DungeonFloorController floorController;

        private readonly ExplorationEncounterSession session =
            new ExplorationEncounterSession();

        private readonly IEncounterCommand battleCommand =
            new BattleEncounterCommand();

        private readonly IEncounterCommand escapeCommand =
            new EscapeEncounterCommand();

        private readonly EncounterActionSelectionGate actionSelectionGate =
            new EncounterActionSelectionGate();

        private ExplorationMonsterMarker activeMonster;
        private bool wasMoving;
        private bool ownsExplorationControlLock;
        private bool movementLockBeforeEncounter;

        public bool IsEncounterActive =>
            session.State != EncounterState.Idle;

        public EncounterState CurrentState =>
            session.State;

        public EncounterContext CurrentContext =>
            session.Context;

        public string ActiveMonsterDefinitionId =>
            session.MonsterDefinitionId;

        public EncounterCommandResult LastCommandResult { get; private set; }

        public EncounterResult LastEncounterResult { get; private set; }

        public bool HasSelectedEncounterAction =>
            actionSelectionGate.HasSelection;

        public string SelectedEncounterCommandId =>
            actionSelectionGate.SelectedCommandId;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController =
                    GetComponent<PlayerGridMovementController>();
            }

            if (lookController == null)
            {
                lookController =
                    GetComponent<PlayerLookController>();
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }
        }

        private void OnEnable()
        {
            wasMoving =
                movementController != null
                && movementController.IsMoving;
        }

        private void OnDisable()
        {
            RestoreExplorationControl();

            session.ForceReset();
            actionSelectionGate.Reset();
            activeMonster = null;
            LastCommandResult = null;
            LastEncounterResult = null;
            wasMoving = false;
        }

        private void Update()
        {
            if (movementController == null)
            {
                return;
            }

            bool isMovingNow =
                movementController.IsMoving;

            if (wasMoving && !isMovingNow)
            {
                TryBeginEncounterAtCurrentPosition();
            }

            wasMoving =
                isMovingNow;
        }

        public bool TryBeginEncounterAtCurrentPosition()
        {
            if (session.State != EncounterState.Idle
                || movementController == null
                || movementController.PlayerState == null)
            {
                return false;
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }

            if (floorController == null)
            {
                return false;
            }

            PlayerRunState playerState =
                movementController.PlayerState;

            if (!floorController.SpawnedMonsters.TryGetValue(
                    playerState.CurrentRoomId,
                    out ExplorationMonsterMarker monster)
                || monster == null
                || !monster.gameObject.activeInHierarchy
                || monster.IsRoomEncounterCompleted)
            {
                return false;
            }

            if (!session.TryBegin(
                    playerState.CurrentRoomId,
                    playerState.CurrentGridPosition,
                    monster.RoomId,
                    monster.GridPosition,
                    monster.MonsterDefinitionId))
            {
                return false;
            }

            activeMonster =
                monster;

            LastCommandResult =
                null;

            LastEncounterResult =
                null;

            actionSelectionGate.Reset();

            LockExplorationControl();

            Debug.Log(
                $"[Project Delta] 46일차 Encounter Starting / Room {monster.RoomId} / Player {playerState.CurrentGridPosition} / Monster {monster.GridPosition} / Target {monster.MonsterDefinitionId}",
                this);

            if (!session.TryActivate())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Starting → Active 전환에 실패했습니다.",
                    this);

                AbortEncounter();
                return false;
            }

            Debug.Log(
                $"[Project Delta] 46일차 Encounter Active / Monster {monster.MonsterDefinitionId}",
                this);

            return true;
        }

        public EncounterActionAvailability GetActionAvailability()
        {
            return actionSelectionGate.Evaluate(
                session.State,
                session.Context);
        }

        public EncounterCommandResult SelectBattleCommand()
        {
            return ExecuteEncounterCommand(
                battleCommand);
        }

        public EncounterCommandResult SelectEscapeCommand()
        {
            return ExecuteEncounterCommand(
                escapeCommand);
        }

        public void CompleteTestEncounter()
        {
            if (session.State != EncounterState.Active
                || !actionSelectionGate.HasSelection)
            {
                return;
            }

            if (!EncounterResultResolver.TryCreateTestResult(
                    session.Context,
                    actionSelectionGate.SelectedCommandId,
                    out EncounterResult result))
            {
                Debug.LogError(
                    "[Project Delta] 선택된 Command를 Encounter 결과로 변환하지 못했습니다.",
                    this);

                return;
            }

            if (!session.TryBeginResolve())
            {
                return;
            }

            Debug.Log(
                $"[Project Delta] 46일차 Encounter Resolving / Outcome {result.Outcome}",
                this);

            if (!TryApplyEncounterResult(
                    result))
            {
                Debug.LogError(
                    "[Project Delta] Encounter 결과를 방·몬스터 상태에 반영하지 못했습니다.",
                    this);

                AbortEncounter();
                return;
            }

            if (!session.TryFinish())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Resolving → Finished 전환에 실패했습니다.",
                    this);

                AbortEncounter();
                return;
            }

            LastEncounterResult =
                result;

            Debug.Log(
                $"[Project Delta] 46일차 Encounter Finished / Outcome {result.Outcome}",
                this);

            activeMonster =
                null;

            LastCommandResult =
                null;

            actionSelectionGate.Reset();

            RestoreExplorationControl();

            if (!session.TryReset())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Finished → Idle 전환에 실패했습니다.",
                    this);

                session.ForceReset();
            }

            Debug.Log(
                "[Project Delta] 46일차 Encounter Idle 복귀 / 탐험 재개",
                this);
        }

        private bool TryApplyEncounterResult(
            EncounterResult result)
        {
            if (result == null
                || activeMonster == null
                || result.RoomId != activeMonster.RoomId
                || result.MonsterDefinitionId != activeMonster.MonsterDefinitionId)
            {
                return false;
            }

            if (result.CompletesRoom)
            {
                if (!activeMonster.TryMarkRoomEncounterCompleted())
                {
                    return false;
                }

                ApplicationFlow.Current?.SaveDungeonProgress();

                Debug.Log(
                    $"[Project Delta] 46일차 Encounter 완료 저장 / Room {result.RoomId} / Monster {result.MonsterDefinitionId}",
                    this);
            }

            return true;
        }

        private EncounterCommandResult ExecuteEncounterCommand(
            IEncounterCommand command)
        {
            if (command == null)
            {
                return null;
            }

            EncounterActionAvailability availability =
                GetActionAvailability();

            if (!availability.CanSelect)
            {
                EncounterCommandResult rejected =
                    EncounterCommandResult.Reject(
                        command.Id,
                        availability.Reason);

                LastCommandResult =
                    rejected;

                return rejected;
            }

            EncounterCommandResult result =
                command.Execute(
                    session.Context);

            if (result == null)
            {
                EncounterCommandResult rejected =
                    EncounterCommandResult.Reject(
                        command.Id,
                        "행동 처리 결과를 확인할 수 없습니다.");

                LastCommandResult =
                    rejected;

                return rejected;
            }

            if (result.Accepted)
            {
                if (!actionSelectionGate.TryCommit(
                        result.CommandId))
                {
                    EncounterCommandResult rejected =
                        EncounterCommandResult.Reject(
                            command.Id,
                            "이미 행동을 선택했습니다.");

                    LastCommandResult =
                        rejected;

                    return rejected;
                }
            }

            LastCommandResult =
                result;

            Debug.Log(
                $"[Project Delta] 46일차 Encounter Command / Id {result.CommandId} / Accepted {result.Accepted} / {result.Message}",
                this);

            return result;
        }

        private void AbortEncounter()
        {
            activeMonster =
                null;

            LastCommandResult =
                null;

            actionSelectionGate.Reset();

            RestoreExplorationControl();
            session.ForceReset();
        }

        private void LockExplorationControl()
        {
            if (ownsExplorationControlLock)
            {
                return;
            }

            if (movementController != null)
            {
                movementLockBeforeEncounter =
                    movementController.IsInputLocked;

                movementController.IsInputLocked =
                    true;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(
                    true);
            }

            ownsExplorationControlLock =
                true;
        }

        private void RestoreExplorationControl()
        {
            if (!ownsExplorationControlLock)
            {
                return;
            }

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    movementLockBeforeEncounter;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(
                    false);
            }

            movementLockBeforeEncounter =
                false;

            ownsExplorationControlLock =
                false;
        }
    }
}
