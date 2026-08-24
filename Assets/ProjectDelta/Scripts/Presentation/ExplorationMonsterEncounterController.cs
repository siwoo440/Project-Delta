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

        private ExplorationMonsterMarker activeMonster;
        private bool wasMoving;

        public bool IsEncounterActive =>
            session.State != EncounterState.Idle;

        public EncounterState CurrentState =>
            session.State;

        public EncounterContext CurrentContext =>
            session.Context;

        public string ActiveMonsterDefinitionId =>
            session.MonsterDefinitionId;

        public EncounterCommandResult LastCommandResult { get; private set; }

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
            activeMonster = null;
            LastCommandResult = null;
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
                || !monster.gameObject.activeInHierarchy)
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

            LockExplorationControl();

            Debug.Log(
                $"[Project Delta] 44일차 Encounter Starting / Room {monster.RoomId} / Grid {monster.GridPosition} / Monster {monster.MonsterDefinitionId}",
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
                $"[Project Delta] 44일차 Encounter Active / Monster {monster.MonsterDefinitionId}",
                this);

            return true;
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
            if (session.State != EncounterState.Active)
            {
                return;
            }

            if (!session.TryBeginResolve())
            {
                return;
            }

            Debug.Log(
                "[Project Delta] 44일차 Encounter Resolving",
                this);

            if (activeMonster != null)
            {
                activeMonster.gameObject.SetActive(false);
            }

            if (!session.TryFinish())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Resolving → Finished 전환에 실패했습니다.",
                    this);

                AbortEncounter();
                return;
            }

            Debug.Log(
                "[Project Delta] 44일차 Encounter Finished",
                this);

            activeMonster =
                null;

            LastCommandResult =
                null;

            RestoreExplorationControl();

            if (!session.TryReset())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Finished → Idle 전환에 실패했습니다.",
                    this);

                session.ForceReset();
            }

            Debug.Log(
                "[Project Delta] 44일차 Encounter Idle 복귀 / 탐험 재개",
                this);
        }

        private EncounterCommandResult ExecuteEncounterCommand(
            IEncounterCommand command)
        {
            if (command == null)
            {
                return null;
            }

            if (session.State != EncounterState.Active
                || session.Context == null)
            {
                EncounterCommandResult rejected =
                    EncounterCommandResult.Reject(
                        command.Id,
                        "현재 행동을 선택할 수 있는 Encounter가 없습니다.");

                LastCommandResult =
                    rejected;

                return rejected;
            }

            EncounterCommandResult result =
                command.Execute(
                    session.Context);

            LastCommandResult =
                result;

            Debug.Log(
                $"[Project Delta] 44일차 Encounter Command / Id {result.CommandId} / Accepted {result.Accepted} / {result.Message}",
                this);

            return result;
        }

        private void AbortEncounter()
        {
            activeMonster =
                null;

            LastCommandResult =
                null;

            RestoreExplorationControl();
            session.ForceReset();
        }

        private void LockExplorationControl()
        {
            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(true);
            }
        }

        private void RestoreExplorationControl()
        {
            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(false);
            }
        }
    }
}
