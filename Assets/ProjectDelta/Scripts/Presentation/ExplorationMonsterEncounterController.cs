using System;
using System.Collections.Generic;
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

        // 49일차: 전투 내부 공격 행동.
        private readonly IBattleCommand attackCommand =
            new AttackBattleCommand();

        // 47일차: 승패 계산 전까지 사용하는 최소 테스트 스탯.
        private const string TestPlayerInstanceId = "PLAYER";
        private const int TestPlayerMaxHp = 20;
        private const int TestPlayerSpeed = 5;
        private const int TestPlayerAttack = 6;
        private const int TestPlayerDefense = 3;
        private const int TestPlayerAccuracy = 90;
        private const int TestPlayerEvasion = 10;
        private const int TestPlayerPenetration = 0;
        private const int TestEnemyMaxHp = 10;
        private const int TestEnemySpeed = 5;
        private const int TestEnemyAttack = 4;
        private const int TestEnemyDefense = 2;
        private const int TestEnemyAccuracy = 80;
        private const int TestEnemyEvasion = 5;
        private const int TestEnemyPenetration = 0;

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

        // 49일차: 현재 행동자가 지정한(재지정 가능한) 대상.
        public BattleParticipant SelectedBattleTarget =>
            battleSession.SelectedTarget;

        public BattleCommandResult LastBattleCommandResult { get; private set; }

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
                    TestPlayerSpeed,
                    TestPlayerAttack,
                    TestPlayerDefense,
                    TestPlayerAccuracy,
                    TestPlayerEvasion,
                    TestPlayerPenetration);

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
                        TestEnemySpeed,
                        TestEnemyAttack,
                        TestEnemyDefense,
                        TestEnemyAccuracy,
                        TestEnemyEvasion,
                        TestEnemyPenetration);
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

        // 49일차: 이번 턴의 다음 행동자를 AwaitingAction으로 불러온다.
        // 실제 행동 확정(공격 등)은 별도이므로 여기서는 대상 선택을 기다리는 상태까지만 진행한다.
        // Enemy 차례는 아직 AI가 없으므로 유일한 대상(Player)을 자동으로 미리 선택해 둔다.
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

            BattleParticipant actor =
                battleSession.CurrentActor;

            Debug.Log(
                $"[Project Delta] 49일차 Battle AwaitingAction / Turn {battleSession.TurnNumber} / Actor {actor.InstanceId} (Speed {actor.Speed})",
                this);

            if (actor.Team == BattleTeam.Enemy)
            {
                IReadOnlyList<BattleParticipant> validTargets =
                    BattleTargeting.GetValidTargets(
                        battleSession.Context,
                        actor);

                if (validTargets.Count > 0)
                {
                    battleSession.TrySelectTarget(
                        validTargets[0]);
                }
            }

            return true;
        }

        // 49일차: AwaitingAction 상태에서 CurrentActor의 공격 대상을 지정·재지정한다.
        public bool TrySelectBattleTarget(
            BattleParticipant target)
        {
            return battleSession.TrySelectTarget(
                target);
        }

        // 49일차: AwaitingAction 상태에서 CurrentActor가 선택할 수 있는 대상 목록.
        public IReadOnlyList<BattleParticipant> GetValidBattleTargets()
        {
            if (battleSession.State != BattleState.AwaitingAction
                || battleSession.Context == null
                || battleSession.CurrentActor == null)
            {
                return Array.Empty<BattleParticipant>();
            }

            return BattleTargeting.GetValidTargets(
                battleSession.Context,
                battleSession.CurrentActor);
        }

        // 49일차: 대상 유효성을 검증하고 확정한다.
        // 50일차: 확정된 공격의 명중·피해를 실제로 계산해 적용한다.
        public BattleCommandResult ConfirmAttack()
        {
            if (battleSession.State != BattleState.AwaitingAction
                || battleSession.Context == null
                || battleSession.CurrentActor == null)
            {
                return null;
            }

            BattleParticipant actor =
                battleSession.CurrentActor;

            BattleParticipant target =
                battleSession.SelectedTarget;

            BattleCommandResult declaration =
                attackCommand.Execute(
                    battleSession.Context,
                    actor,
                    target);

            if (!declaration.Accepted)
            {
                LastBattleCommandResult =
                    declaration;

                Debug.LogWarning(
                    $"[Project Delta] 49일차 공격 확정 실패 / {declaration.Message}",
                    this);

                return declaration;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return declaration;
            }

            // 50일차: 명중 판정에 쓰는 난수(0~99)는 여기(Presentation)에서 만들어
            // BattleDamageCalculator(Application, 엔진 비의존)에 넘긴다.
            int roll =
                UnityEngine.Random.Range(
                    0,
                    100);

            BattleDamageResult damageResult =
                BattleDamageCalculator.Resolve(
                    actor,
                    target,
                    roll);

            string resolutionMessage;

            if (damageResult.IsHit)
            {
                int appliedDamage =
                    target.ApplyDamage(
                        damageResult.Damage);

                resolutionMessage =
                    $"공격 적중 / {actor.InstanceId} → {target.InstanceId} / {appliedDamage} 데미지 (명중률 {damageResult.HitChancePercent}%)";
            }
            else
            {
                resolutionMessage =
                    $"공격 빗나감 / {actor.InstanceId} → {target.InstanceId} (명중률 {damageResult.HitChancePercent}%)";
            }

            BattleCommandResult resolvedResult =
                BattleCommandResult.Accept(
                    declaration.CommandId,
                    resolutionMessage);

            LastBattleCommandResult =
                resolvedResult;

            Debug.Log(
                $"[Project Delta] 50일차 Battle 공격 판정 / {resolutionMessage}",
                this);

            // 51일차: 공격이 끝날 때마다 전멸 여부를 확인해, 결정됐으면 여기서 전투를 끝낸다.
            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome outcome))
            {
                FinishBattle(
                    outcome);

                return resolvedResult;
            }

            if (battleSession.HasPendingActorsThisTurn)
            {
                return resolvedResult;
            }

            if (!battleSession.TryEndTurn())
            {
                return resolvedResult;
            }

            if (!battleSession.TryStartTurn())
            {
                return resolvedResult;
            }

            Debug.Log(
                $"[Project Delta] 49일차 Battle Turn {battleSession.TurnNumber} Start",
                this);

            return resolvedResult;
        }

        // 47일차: 실제 승패 계산 전까지 Battle을 승리로 강제 종료하는 테스트용 버튼.
        // 51일차부터는 ConfirmAttack()의 자동 판정도 이 메서드가 감싼 FinishBattle()을 그대로 탄다.
        public void TestWinBattle()
        {
            if (!battleSession.IsActive)
            {
                return;
            }

            FinishBattle(
                BattleOutcome.Victory);
        }

        // 47일차: 실제 승패 계산 전까지 Battle을 패배로 강제 종료하는 테스트용 버튼.
        public void TestLoseBattle()
        {
            if (!battleSession.IsActive)
            {
                return;
            }

            FinishBattle(
                BattleOutcome.Defeat);
        }

        // 51일차: 승리·패배를 실제로 마무리한다.
        // 승리 → 46일차 Encounter 결과 처리(방 완료·저장)로 이어진다.
        // 패배 → 게임 오버 연출 없이 일단 타이틀(메인 메뉴)로 돌아간다.
        //         보상·재도전 같은 더 나은 패배 경험은 58일차에서 다룬다.
        private void FinishBattle(
            BattleOutcome outcome)
        {
            if (!battleSession.TryFinishBattle(
                    outcome))
            {
                return;
            }

            Debug.Log(
                $"[Project Delta] 51일차 Battle Finished / Outcome {battleSession.Result.Outcome} / Turn {battleSession.Result.TurnCount}",
                this);

            if (outcome == BattleOutcome.Victory)
            {
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

                return;
            }

            Debug.Log(
                "[Project Delta] 51일차 Battle Defeat / 메인 메뉴로 복귀",
                this);

            ApplicationFlow.Current?.ReturnToTitle();
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
