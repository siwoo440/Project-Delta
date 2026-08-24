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

        // 47일차: Encounter 내부에서 진행되는 실제 Battle 생명주기.
        private readonly BattleSession battleSession =
            new BattleSession();

        // 47일차: 승패 계산 전까지 사용하는 최소 테스트 스탯.
        private const string TestPlayerInstanceId = "PLAYER";
        private const int TestPlayerMaxHp = 20;
        private const int TestPlayerSpeed = 5;
        private const int TestEnemyMaxHp = 10;
        private const int TestEnemySpeed = 5;

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

        // 47일차: Battle 진행 상태를 UI에 노출한다.
        public BattleState CurrentBattleState =>
            battleSession.State;

        public BattleContext CurrentBattleContext =>
            battleSession.Context;

        public int BattleTurnNumber =>
            battleSession.TurnNumber;

        public BattleResult LastBattleResult =>
            battleSession.Result;

        // 48일차: 가장 최근에 행동을 진행한(또는 진행 중인) 참가자.
        public BattleParticipant CurrentBattleActor =>
            battleSession.CurrentActor;

        public bool IsBattleActive =>
            battleSession.IsActive;

        // 47일차: 종료 직후(Finished)에도 전투 화면을 유지하기 위해 Idle이 아닌 상태를 함께 본다.
        public bool HasBattle =>
            battleSession.State != BattleState.Idle;

        public bool IsBattleFinished =>
            battleSession.State == BattleState.Finished;

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
            battleSession.ForceReset();
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
            battleSession.ForceReset();

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

            if (actionSelectionGate.SelectedCommandId == battleCommand.Id)
            {
                // 47일차부터 전투 선택은 BattleSession 결과(TestWinBattle)로만 Encounter를 종료한다.
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

            FinalizeActiveEncounter(
                result);
        }

        // 47일차: Battle 선택이 확정되면 최소 테스트 참가자로 BattleContext를 만들고 Battle을 시작한다.
        private void BeginTestBattle()
        {
            if (session.State != EncounterState.Active
                || session.Context == null
                || battleSession.State != BattleState.Idle)
            {
                return;
            }

            BattleParticipant player =
                new BattleParticipant(
                    TestPlayerInstanceId,
                    TestPlayerInstanceId,
                    BattleTeam.Player,
                    TestPlayerMaxHp,
                    TestPlayerSpeed);

            // 47일차: 적 슬롯 4칸 레이아웃을 확인하기 위해 접촉한 몬스터를 1번 슬롯에 두고
            // 같은 정의로 4명을 채운다. 실제 적 구성은 EncounterDefinition 연동 시 교체한다.
            BattleParticipant[] enemies =
                new BattleParticipant[BattleContext.MaxEnemySlots];

            for (int slotIndex = 0; slotIndex < enemies.Length; slotIndex++)
            {
                enemies[slotIndex] =
                    new BattleParticipant(
                        $"{session.Context.MonsterDefinitionId}#{slotIndex + 1}",
                        session.Context.MonsterDefinitionId,
                        BattleTeam.Enemy,
                        TestEnemyMaxHp,
                        TestEnemySpeed);
            }

            BattleContext context =
                new BattleContext(
                    player,
                    enemies);

            if (!battleSession.TryBeginBattle(
                    context))
            {
                Debug.LogError(
                    "[Project Delta] 47일차 BattleContext 생성에 실패했습니다.",
                    this);

                return;
            }

            Debug.Log(
                $"[Project Delta] 47일차 Battle Starting / Player {TestPlayerMaxHp}HP / Enemy {session.Context.MonsterDefinitionId} x{enemies.Length} {TestEnemyMaxHp}HP",
                this);

            if (!battleSession.TryStartTurn())
            {
                Debug.LogError(
                    "[Project Delta] 47일차 Battle Starting → TurnStart 전환에 실패했습니다.",
                    this);

                return;
            }

            Debug.Log(
                $"[Project Delta] 47일차 Battle Turn {battleSession.TurnNumber} Start",
                this);
        }

        // 48일차: 이번 턴의 다음 행동자 한 명을 AwaitingAction → ResolvingAction까지 진행하는 테스트용 버튼.
        // 이번 턴의 마지막 행동자였다면 이어서 TurnEnd → 다음 TurnStart까지 자동으로 넘어간다.
        public bool TestAdvanceBattleTurn()
        {
            if (battleSession.Context == null
                || (battleSession.State != BattleState.TurnStart
                    && battleSession.State != BattleState.ResolvingAction))
            {
                return false;
            }

            if (!battleSession.TryEnterAwaitingAction())
            {
                return false;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return false;
            }

            BattleParticipant actor =
                battleSession.CurrentActor;

            Debug.Log(
                $"[Project Delta] 48일차 Battle Actor 진행 / Turn {battleSession.TurnNumber} / Actor {actor.InstanceId} (Speed {actor.Speed})",
                this);

            if (battleSession.HasPendingActorsThisTurn)
            {
                return true;
            }

            if (!battleSession.TryEndTurn())
            {
                return false;
            }

            if (!battleSession.TryStartTurn())
            {
                return false;
            }

            Debug.Log(
                $"[Project Delta] 48일차 Battle Turn {battleSession.TurnNumber} Start",
                this);

            return true;
        }

        // 47일차: 실제 승패 계산 전까지 Battle을 승리로 강제 종료하고, 46일차 Encounter 결과 처리로 이어준다.
        public void TestWinBattle()
        {
            if (!battleSession.IsActive)
            {
                return;
            }

            if (!battleSession.TryFinishBattle(
                    BattleOutcome.Victory))
            {
                return;
            }

            Debug.Log(
                $"[Project Delta] 47일차 Battle Finished / Outcome {battleSession.Result.Outcome} / Turn {battleSession.Result.TurnCount}",
                this);

            if (!EncounterResultResolver.TryCreateTestResult(
                    session.Context,
                    actionSelectionGate.SelectedCommandId,
                    out EncounterResult result))
            {
                Debug.LogError(
                    "[Project Delta] Battle 승리 결과를 Encounter 결과로 변환하지 못했습니다.",
                    this);

                return;
            }

            FinalizeActiveEncounter(
                result);

            battleSession.TryReset();
        }

        // 47일차: 실제 승패 계산 전까지 Battle을 패배로 강제 종료한다.
        // 패배를 Encounter 결과(EncounterOutcome)에 연결하는 것은 51·58일차에서 다룬다.
        public void TestLoseBattle()
        {
            if (!battleSession.IsActive)
            {
                return;
            }

            if (!battleSession.TryFinishBattle(
                    BattleOutcome.Defeat))
            {
                return;
            }

            Debug.Log(
                $"[Project Delta] 47일차 Battle Finished / Outcome {battleSession.Result.Outcome} / Turn {battleSession.Result.TurnCount} (패배 처리는 51일차 이후 연결)",
                this);
        }

        // 47일차: 패배 테스트로 종료된 전투를 닫고 Encounter 행동 선택으로 되돌린다.
        // 실제 패배 결과 처리(게임 오버·탐험 복귀)는 51·58일차에서 연결한다.
        public void TestDismissFinishedBattle()
        {
            if (battleSession.State != BattleState.Finished)
            {
                return;
            }

            if (!battleSession.TryReset())
            {
                battleSession.ForceReset();
            }

            // 전투가 결과 없이 닫혔으므로 행동을 다시 선택할 수 있게 되돌린다.
            actionSelectionGate.Reset();

            LastCommandResult =
                null;

            Debug.Log(
                "[Project Delta] 47일차 Battle 닫기 / Encounter 행동 선택으로 복귀",
                this);
        }

        private void FinalizeActiveEncounter(
            EncounterResult result)
        {
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

                if (result.CommandId == battleCommand.Id)
                {
                    BeginTestBattle();
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
            battleSession.ForceReset();
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
