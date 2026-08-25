using System;
using System.Collections;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
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

        // 54일차: 47일차 테스트 상수를 대체하는 몬스터 정의. 실제 조우 몬스터 연동
        // (DataRepository로 EncounterContext.MonsterDefinitionId를 조회하는 것)은 이후 일차로 미룬다.
        [SerializeField] private MonsterDefinition testMonsterDefinition;

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

        // 52일차: 전투 내부 방어 행동.
        private readonly IBattleCommand defendCommand =
            new DefendBattleCommand();

        // 59일차: 기획서 9.3 "CombatRng - 명중·피해·상태 이상". 더 이상 UnityEngine.Random을
        // 전투 핵심 판정에 직접 쓰지 않고 이 발생원 하나에서만 뽑는다.
        private readonly IRandomSource combatRng =
            new CombatRng();

        // 47일차: 승패 계산 전까지 사용하던 테스트 스탯. 54일차부터 플레이어의 체력·공격·방어·
        // 속도·매력·회피·저항은 PlayerRunState(기획서 6.1), 적은 MonsterDefinition에서 가져온다.
        // 명중은 능력치가 아니라 스킬별 기본값이라 56일차 명중 공식 정정 전까지는 임시 상수로 둔다.
        private const string TestPlayerInstanceId = "PLAYER";
        private const int TestPlayerAccuracy = 90;

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

        // 59일차: 기획서 4.2 용어에 맞춰 Turn → Round로 정정했다.
        public int BattleRoundNumber =>
            battleSession.RoundNumber;

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

        // 59일차: 문자열 메시지 하나뿐이던 BattleCommandResult 대신, 변화 목록·로그·저장 필요
        // 여부·전투 종료 결과를 담는 BattleActionResult로 바꿨다 (기획서 10.3).
        public BattleActionResult LastBattleActionResult { get; private set; }

        // 55일차: 마지막 공격에서 실제로 적용된 피해 공식·편차 난수를 디버그 창(BattleDamageDebugOverlay)에
        // 보여주기 위한 텍스트. 정식 UI가 아니라 디버그 전용이다.
        public string LastDamageFormulaDebugText { get; private set; }

        // 56일차: 적 턴이 버튼 없이 자동으로 진행되면서 행동이 눈에 안 보이는 문제를 보완하기 위한
        // 정보. 실제 행동(공격·방어)이 확정될 때마다 누가 행동했는지와 함께 증가하는 값을 남겨,
        // BattleHudController가 매 프레임 값이 바뀌었는지 확인해 해당 슬롯에 살짝 움직이는
        // 연출을 재생할 수 있게 한다.
        public BattleParticipant LastActingParticipant { get; private set; }
        public int LastActionSequence { get; private set; }

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
                || battleSession.State != BattleState.Idle
                || RunContext.Current == null
                || testMonsterDefinition == null)
            {
                return;
            }

            // 54일차: 플레이어 참가자는 PlayerRunState의 최종 능력치와 현재 자원(체력·마나·정력)을
            // 그대로 이어받는다. 전투가 끝나면 FinishBattle()에서 다시 PlayerRunState로 되돌린다.
            PlayerRunState playerRunState =
                RunContext.Current.Player;

            StatBlock finalStats =
                playerRunState.GetFinalStats();

            BattleParticipant player =
                new BattleParticipant(
                    TestPlayerInstanceId,
                    TestPlayerInstanceId,
                    BattleTeam.Player,
                    finalStats.MaxHealth,
                    finalStats.Speed,
                    finalStats.Attack,
                    finalStats.Defense,
                    TestPlayerAccuracy,
                    finalStats.Evasion,
                    finalStats.Charm,
                    finalStats.Resistance,
                    finalStats.MaxMana,
                    finalStats.MaxStamina,
                    playerRunState.CurrentHp,
                    playerRunState.CurrentMana,
                    playerRunState.CurrentStamina);

            // 47일차: 적 슬롯 4칸 레이아웃을 확인하기 위해 접촉한 몬스터를 1번 슬롯에 두고
            // 같은 정의로 4명을 채운다. 실제 적 구성(EncounterDefinition에 연결된 몬스터 조회)은
            // DataRepository가 도입되는 이후 일차에서 교체한다.
            BattleParticipant[] enemies =
                new BattleParticipant[BattleContext.MaxEnemySlots];

            for (int slotIndex = 0; slotIndex < enemies.Length; slotIndex++)
            {
                enemies[slotIndex] =
                    new BattleParticipant(
                        $"{session.Context.MonsterDefinitionId}#{slotIndex + 1}",
                        session.Context.MonsterDefinitionId,
                        BattleTeam.Enemy,
                        testMonsterDefinition.MaxHp,
                        testMonsterDefinition.Speed,
                        testMonsterDefinition.Attack,
                        testMonsterDefinition.Defense,
                        testMonsterDefinition.Accuracy,
                        testMonsterDefinition.Evasion,
                        testMonsterDefinition.Charm,
                        testMonsterDefinition.Resistance,
                        testMonsterDefinition.MaxMana);
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
                $"[Project Delta] 54일차 Battle Starting / Player {finalStats.MaxHealth}HP / Enemy {session.Context.MonsterDefinitionId} x{enemies.Length} {testMonsterDefinition.MaxHp}HP",
                this);

            if (!battleSession.TryStartRound())
            {
                Debug.LogError(
                    "[Project Delta] 47일차 Battle Starting → RoundStart 전환에 실패했습니다.",
                    this);

                return;
            }

            Debug.Log(
                $"[Project Delta] 47일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            // 56일차: "다음 턴" 버튼을 누르지 않아도 바로 행동할 수 있도록 첫 행동자를 자동으로 불러온다.
            TestAdvanceBattleTurn();
        }

        // 56일차: 여러 Enemy가 연속으로 행동할 때 한 프레임에 몰아서 처리하면 화면에서 아무것도
        // 안 보이므로, 행동자 한 명마다 짧게 대기해 각자의 행동(BattleDamageDebugOverlay·슬롯
        // 튀어오르는 연출)이 보이게 하는 간격이다.
        private const float EnemyActionVisibleDelaySeconds = 0.45f;

        // 이미 코루틴이 진행 중이면 다시 시작하지 않는다 (코루틴의 while 루프가 알아서 이어간다).
        private Coroutine autoAdvanceRoutine;

        // 49일차: 이번 턴의 다음 행동자를 AwaitingAction으로 불러온다.
        // Enemy 차례는 아직 AI가 없으므로 유일한 대상(Player)을 자동으로 미리 선택해 둔다.
        // 56일차: Enemy 차례는 대상 선택 후 버튼 입력 없이 바로 공격까지 자동으로 확정하고,
        // Player 차례가 오거나 전투가 끝날 때까지 코루틴으로 한 명씩 이어서 진행한다.
        public void TestAdvanceBattleTurn()
        {
            if (autoAdvanceRoutine != null)
            {
                return;
            }

            autoAdvanceRoutine =
                StartCoroutine(
                    AdvanceBattleTurnRoutine());
        }

        private IEnumerator AdvanceBattleTurnRoutine()
        {
            while (battleSession.Context != null
                && (battleSession.State == BattleState.RoundStart
                    || battleSession.State == BattleState.ResolvingAction))
            {
                if (!battleSession.TryEnterAwaitingAction())
                {
                    break;
                }

                BattleParticipant actor =
                    battleSession.CurrentActor;

                Debug.Log(
                    $"[Project Delta] 49일차 Battle AwaitingAction / Round {battleSession.RoundNumber} / Actor {actor.InstanceId} (Speed {actor.Speed})",
                    this);

                if (actor.Team != BattleTeam.Enemy)
                {
                    break; // Player 차례, 공격·방어 버튼 입력을 기다린다.
                }

                IReadOnlyList<BattleParticipant> validTargets =
                    BattleTargeting.GetValidTargets(
                        battleSession.Context,
                        actor);

                if (validTargets.Count > 0)
                {
                    battleSession.TrySelectTarget(
                        validTargets[0]);
                }

                ConfirmAttack();

                // 이 공격으로 전투가 끝났으면(승리·패배) 다음 행동자를 기다릴 필요 없이 바로 멈춘다.
                if (battleSession.State != BattleState.RoundStart
                    && battleSession.State != BattleState.ResolvingAction)
                {
                    break;
                }

                // 56일차: 방금 행동(슬롯이 튀어오르는 연출 포함)이 화면에 보이도록 잠깐 대기한 뒤
                // 다음 행동자로 넘어간다.
                yield return new WaitForSeconds(
                    EnemyActionVisibleDelaySeconds);
            }

            autoAdvanceRoutine = null;
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
        public BattleActionResult ConfirmAttack()
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
                BattleActionResult rejectedResult =
                    BattleActionResult.Reject(
                        declaration.CommandId,
                        declaration.Message);

                LastBattleActionResult =
                    rejectedResult;

                Debug.LogWarning(
                    $"[Project Delta] 49일차 공격 확정 실패 / {declaration.Message}",
                    this);

                return rejectedResult;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return BattleActionResult.Reject(
                    declaration.CommandId,
                    "행동을 처리할 수 없는 상태입니다.");
            }

            // 56일차: 이번에 실제로 행동한 참가자를 기록해, HUD가 살짝 움직이는 연출을 재생하게 한다.
            LastActingParticipant =
                actor;

            LastActionSequence++;

            // 50일차: 명중 판정에 쓰는 난수(0~99)는 여기(Presentation)에서 만들어
            // BattleDamageCalculator(Application, 엔진 비의존)에 넘긴다.
            // 55일차: 피해 편차(95~105%, 11단계)에 쓰는 난수(0~10)도 같은 방식으로 넘긴다.
            // 59일차: UnityEngine.Random 대신 목적별로 분리한 CombatRng(기획서 9.3)에서 뽑는다.
            int hitRoll =
                combatRng.NextInt(
                    0,
                    100);

            int varianceRoll =
                combatRng.NextInt(
                    0,
                    BattleDamageCalculator.DamageVarianceRollCount);

            BattleDamageResult damageResult =
                BattleDamageCalculator.Resolve(
                    actor,
                    target,
                    hitRoll,
                    varianceRoll);

            string resolutionMessage;
            int appliedDamage = 0;

            if (damageResult.IsHit)
            {
                appliedDamage =
                    target.ApplyDamage(
                        damageResult.Damage);

                resolutionMessage =
                    $"공격 적중 / {actor.InstanceId} → {target.InstanceId} / {appliedDamage} 데미지 (명중률 {damageResult.HitChancePercent}%)";

                // 55일차: varianceRoll(0~10)이 실제로 굴러가는지 눈으로 바로 확인하기 위한 디버그 텍스트.
                LastDamageFormulaDebugText =
                    $"{actor.InstanceId} → {target.InstanceId} / "
                    + $"{actor.Attack} × 100 ÷ (100 + {target.Defense}) = {damageResult.BaseDamage} → "
                    + $"× {damageResult.VariancePercent}% = {damageResult.Damage} (적용 {appliedDamage}) "
                    + $"({damageResult.VariancePercent}%)";
            }
            else
            {
                resolutionMessage =
                    $"공격 빗나감 / {actor.InstanceId} → {target.InstanceId} (명중률 {damageResult.HitChancePercent}%)";

                LastDamageFormulaDebugText =
                    $"{actor.InstanceId} → {target.InstanceId} / 빗나감 (명중률 {damageResult.HitChancePercent}%, 편차 미적용)";
            }

            Debug.Log(
                $"[Project Delta] 50일차 Battle 공격 판정 / {resolutionMessage}",
                this);

            // 59일차: 문자열 메시지 하나 대신 실제로 무엇이 바뀌었는지(피해 변화·제거된 참가자)를
            // 담아 BattleActionResult로 반환한다 (기획서 10.3).
            BattleDamageChange[] damageChanges =
            {
                new BattleDamageChange(
                    actor,
                    target,
                    damageResult,
                    appliedDamage)
            };

            BattleParticipant[] removedParticipants =
                target.IsAlive
                    ? Array.Empty<BattleParticipant>()
                    : new[] { target };

            // 51일차: 공격이 끝날 때마다 전멸 여부를 확인해, 결정됐으면 여기서 전투를 끝낸다.
            // 59일차: 승리 시 FinishBattle() 내부에서 battleSession.Result가 곧바로 지워지므로
            // (TryReset), 지워지기 전 값을 반환받아 BattleActionResult에 담는다.
            BattleResult battleEndResult = null;

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome outcome))
            {
                battleEndResult =
                    FinishBattle(
                        outcome);
            }

            BattleActionResult resolvedResult =
                BattleActionResult.Accept(
                    declaration.CommandId,
                    new[] { resolutionMessage },
                    damageChanges,
                    removedParticipants,
                    true,
                    battleEndResult);

            LastBattleActionResult =
                resolvedResult;

            if (battleEndResult != null)
            {
                return resolvedResult;
            }

            // 56일차: "다음 턴" 버튼 없이도 바로 이어서 행동할 수 있도록 다음 행동자를 자동으로 불러온다.
            // Enemy면 TestAdvanceBattleTurn()이 알아서 공격까지 이어서 처리하고, Player 차례가 되면
            // AwaitingAction에서 멈춰 공격·방어 버튼이 곧바로 활성화된다.
            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();

                return resolvedResult;
            }

            if (!battleSession.TryEndRound())
            {
                return resolvedResult;
            }

            if (!battleSession.TryStartRound())
            {
                return resolvedResult;
            }

            Debug.Log(
                $"[Project Delta] 49일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            TestAdvanceBattleTurn();

            return resolvedResult;
        }

        // 52일차: 방어를 확정한다. 공격과 달리 대상 선택이 필요 없어 확정하자마자 곧바로 해결한다.
        // 방어는 HP를 바꾸지 않으므로 승패 자동 판정(51일차)은 확인하지 않는다.
        public BattleActionResult ConfirmDefend()
        {
            if (battleSession.State != BattleState.AwaitingAction
                || battleSession.Context == null
                || battleSession.CurrentActor == null)
            {
                return null;
            }

            BattleParticipant actor =
                battleSession.CurrentActor;

            BattleCommandResult declaration =
                defendCommand.Execute(
                    battleSession.Context,
                    actor,
                    null);

            if (!declaration.Accepted)
            {
                BattleActionResult rejectedResult =
                    BattleActionResult.Reject(
                        declaration.CommandId,
                        declaration.Message);

                LastBattleActionResult =
                    rejectedResult;

                Debug.LogWarning(
                    $"[Project Delta] 52일차 방어 확정 실패 / {declaration.Message}",
                    this);

                return rejectedResult;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return BattleActionResult.Reject(
                    declaration.CommandId,
                    "행동을 처리할 수 없는 상태입니다.");
            }

            // 56일차: 방어도 공격과 같은 방식으로 행동자를 기록해 연출을 재생하게 한다.
            LastActingParticipant =
                actor;

            LastActionSequence++;

            Debug.Log(
                $"[Project Delta] 52일차 Battle 방어 확정 / {declaration.Message}",
                this);

            // 59일차: 방어는 피해 변화·제거된 참가자가 없으므로 로그만 담아 반환한다.
            BattleActionResult resolvedResult =
                BattleActionResult.Accept(
                    declaration.CommandId,
                    new[] { declaration.Message },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    true,
                    null);

            LastBattleActionResult =
                resolvedResult;

            // 56일차: "다음 턴" 버튼 없이도 바로 이어서 진행되도록 다음 행동자를 자동으로 불러온다.
            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();

                return resolvedResult;
            }

            if (!battleSession.TryEndRound())
            {
                return resolvedResult;
            }

            if (!battleSession.TryStartRound())
            {
                return resolvedResult;
            }

            Debug.Log(
                $"[Project Delta] 52일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            TestAdvanceBattleTurn();

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
        // 59일차: 승리 시 아래에서 battleSession.TryReset()을 호출해 Result가 null로 지워지므로,
        // BattleActionResult.BattleEndResult에 담을 수 있도록 지워지기 전 결과를 반환한다.
        private BattleResult FinishBattle(
            BattleOutcome outcome)
        {
            if (!battleSession.TryFinishBattle(
                    outcome))
            {
                return null;
            }

            BattleResult finishedResult =
                battleSession.Result;

            Debug.Log(
                $"[Project Delta] 51일차 Battle Finished / Outcome {finishedResult.Outcome} / Round {finishedResult.RoundCount}",
                this);

            // 54일차: 전투 후 자동 회복은 없다 (기획서 4.2). 참가자가 들고 있던 현재 체력·마나·
            // 정력을 PlayerRunState로 그대로 되돌린다. 다음 회복 시점은 층 이동(3.6.2)뿐이다.
            if (battleSession.Context != null
                && RunContext.Current != null)
            {
                BattleParticipant player =
                    battleSession.Context.Player;

                RunContext.Current.Player.CurrentHp =
                    player.CurrentHp;

                RunContext.Current.Player.CurrentMana =
                    player.CurrentMana;

                RunContext.Current.Player.CurrentStamina =
                    player.CurrentStamina;
            }

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

                    return finishedResult;
                }

                FinalizeActiveEncounter(
                    result);

                battleSession.TryReset();

                return finishedResult;
            }

            Debug.Log(
                "[Project Delta] 51일차 Battle Defeat / 메인 메뉴로 복귀",
                this);

            ApplicationFlow.Current?.ReturnToTitle();

            return finishedResult;
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
