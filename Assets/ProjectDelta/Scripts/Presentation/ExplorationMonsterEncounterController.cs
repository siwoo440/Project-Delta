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

        // 69일차: 전투 내부 도주 행동.
        private readonly IBattleCommand fleeCommand =
            new FleeBattleCommand();

        // 116일차: 전투 중 회유·유혹·관찰 - 이미 있던 공격·방어·도주와 같은 방식으로 붙는다.
        private readonly IBattleCommand persuadeCommand =
            new PersuadeBattleCommand();

        private readonly IBattleCommand seduceCommand =
            new SeduceBattleCommand();

        private readonly IBattleCommand observeCommand =
            new ObserveBattleCommand();

        // 116일차: 회유/유혹 기준 성공률 - EncounterPersuasionRule.CalculateSuccessPercent 참고.
        // 유혹이 회유보다 기준값이 낮다 - "더 위험한 시도"라는 차이를 능력치 없이 표현한다.
        private const int PersuadeBaseSuccessPercent = 50;
        private const int SeduceBaseSuccessPercent = 35;

        // 59일차: 기획서 9.3 "CombatRng - 명중·피해·상태 이상". 더 이상 UnityEngine.Random을
        // 전투 핵심 판정에 직접 쓰지 않고 이 발생원 하나에서만 뽑는다.
        private readonly IRandomSource combatRng =
            new CombatRng();

        // 80일차: 전투 RNG와 분리된 골드·아이템 드롭 전용 RNG.
        private readonly IRandomSource rewardRng =
            new RewardRng();

        // 47일차: 승패 계산 전까지 사용하던 테스트 스탯. 54일차부터 플레이어의 체력·공격·방어·
        // 속도·매력·회피·저항은 PlayerRunState(기획서 6.1), 적은 MonsterDefinition에서 가져온다.
        // 명중은 능력치가 아니라 스킬별 기본값이라 56일차 명중 공식 정정 전까지는 임시 상수로 둔다.
        private const string TestPlayerInstanceId = "PLAYER";
        private const int TestPlayerAccuracy = 90;

        // 117일차: 유혇 성공 시 넘어가는 별도 이벤트 전투. 씬에 직접 배치하지 않고
        // EventBattleRuntimeInstaller가 Player에 자동으로 붙이므로 여기서는 찾기만 한다.
        private EventBattleController eventBattleController;

        private ExplorationMonsterMarker activeMonster;
        private bool wasMoving;
        private bool ownsExplorationControlLock;
        private bool movementLockBeforeEncounter;
        private EncounterResult pendingVictoryEncounterResult; // 72일차 승리 후 보상 선택 전 Encounter 결과 보관
        private bool hasAppliedBattleDropRewards; // 81일차 자동 드롭 보상 중복 지급 방지

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

        // 79일차: 가장 최근 승리에서 적용된 경험치·레벨업 결과.
        // 81일차 정식 보상 화면이 이 값을 그대로 표시할 수 있도록 보존한다.
        public BattleGrowthResult LastBattleGrowthResult { get; private set; }

        // 80일차: 가장 최근 승리에서 한 번 판정된 골드·아이템 드롭 결과.
        // 81일차 정식 보상 화면이 재추첨 없이 이 결과를 그대로 표시한다.
        public BattleDropResult LastBattleDropResult { get; private set; }

        // 116일차: "관찰" 행동으로 확인한 대상 능력치 텍스트를 Battle HUD가 그대로 보여준다.
        public string LastObservationText { get; private set; }

        public bool IsBattleRewardPending =>
            pendingVictoryEncounterResult != null
            && BattleRewardState.IsPending; // 72일차 보상 선택 대기 여부

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

        // 117일차: EventBattleController는 EventBattleRuntimeInstaller가 씬 로드 시점에
        // 따로 붙이므로, Awake 순서 경쟁을 피하려고 실제로 필요한 순간(유혇 성공)에만 찾는다.
        private EventBattleController EnsureEventBattleController()
        {
            if (eventBattleController == null)
            {
                eventBattleController =
                    FindFirstObjectByType<EventBattleController>();
            }

            return eventBattleController;
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

            // Unity는 GameObject/컴포넌트가 비활성화되면 실행 중이던 코루틴을 그 자리에서
            // 즉시 중단시키고, 코루틴 본문에 남은 코드(끝부분의 autoAdvanceRoutine = null)는
            // 실행하지 않는다. 정리하지 않으면 이 필드가 죽은 코루틴 참조를 계속 들고 있게
            // 되어, 다음 전투에서 TestAdvanceBattleTurn()이 "이미 실행 중"이라고 착각하고
            // 아무것도 하지 않아 전투 조작이 영구히 먹통이 된다.
            if (autoAdvanceRoutine != null)
            {
                StopCoroutine(
                    autoAdvanceRoutine);

                autoAdvanceRoutine = null;
            }

            // 88일차: 탐험 → 전투 전환 도중 비활성화되어도 검은 화면과 죽은 코루틴 참조를 남기지 않는다.
            if (battleEntryRoutine != null)
            {
                StopCoroutine(
                    battleEntryRoutine);

                battleEntryRoutine = null;
            }

            if (BattleTransitionController.Current != null)
            {
                BattleTransitionController.Current.ForceReveal();
            }

            session.ForceReset();
            battleSession.ForceReset();
            actionSelectionGate.Reset();
            activeMonster = null;
            LastCommandResult = null;
            LastEncounterResult = null;
            pendingVictoryEncounterResult = null; // 72일차 대기 중 보상 결과 정리
            BattleRewardState.Clear(); // 72일차 보상 상태 정리
            LastBattleGrowthResult = null; // 79일차 성장 결과 정리
            LastBattleDropResult = null; // 80일차 드롭 결과 정리
            hasAppliedBattleDropRewards = false; // 81일차 자동 드롭 보상 상태 정리
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
                    monster.MonsterDefinitionId,
                    monster.MonsterGroupDefinitionIds)) // 76일차: 실제 전투에 쓸 그룹 전체 구성
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

            // 88일차: 별도 조우 선택 UI 없이 몬스터 접촉 직후 자동으로 전투 전환을 시작한다.
            StartAutomaticBattleEntry();

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

            // 76일차: 던전 생성 시 결정론적으로 뽑혀 있던 실제 그룹 구성(자리별 몬스터)을 그대로
            // 불러와 적을 만든다. 47일차부터 쓰던 "같은 정의로 4명 복제" 플레이스홀더를 대체한다 -
            // 각 자리의 몬스터 ID를 floorController에서 실제 MonsterDefinition으로 조회하고,
            // 찾지 못하면(테스트 씬 등 인카운터 데이터가 없는 상황) testMonsterDefinition으로
            // 대체해 기존 동작과 호환한다.
            IReadOnlyList<string> groupDefinitionIds =
                session.Context.MonsterGroupDefinitionIds;

            BattleParticipant[] enemies =
                new BattleParticipant[groupDefinitionIds.Count];

            for (int slotIndex = 0; slotIndex < enemies.Length; slotIndex++)
            {
                string slotMonsterDefinitionId =
                    groupDefinitionIds[slotIndex];

                MonsterDefinition slotMonster =
                    ResolveMonsterDefinition(
                        slotMonsterDefinitionId);

                // 121일차: 정예/보스는 층 보정 없이도(54일차 - 아직 그런 랜덤 편차가 없다)
                // 등급 배율만큼 능력치가 확정적으로 오른다 - "고정 능력치".
                float statMultiplier =
                    MonsterTierRules.GetStatMultiplier(
                        slotMonster.Tier);

                enemies[slotIndex] =
                    new BattleParticipant(
                        $"{slotMonsterDefinitionId}#{slotIndex + 1}",
                        slotMonsterDefinitionId,
                        BattleTeam.Enemy,
                        Mathf.RoundToInt(slotMonster.MaxHp * statMultiplier),
                        slotMonster.Speed,
                        Mathf.RoundToInt(slotMonster.Attack * statMultiplier),
                        Mathf.RoundToInt(slotMonster.Defense * statMultiplier),
                        slotMonster.Accuracy,
                        slotMonster.Evasion,
                        slotMonster.Charm,
                        Mathf.RoundToInt(slotMonster.Resistance * statMultiplier),
                        slotMonster.MaxMana);
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

            BattleRewardState.Clear(); // 72일차 이전 보상 상태 초기화
            pendingVictoryEncounterResult = null; // 72일차 이전 보상 결과 초기화
            LastBattleGrowthResult = null; // 79일차 이전 전투 성장 결과 초기화
            LastBattleDropResult = null; // 80일차 이전 전투 드롭 결과 초기화
            hasAppliedBattleDropRewards = false; // 81일차 새 전투 자동 드롭 지급 상태 초기화
            BattleDefeatService.BeginBattle(); // 70일차 패배 추적 정보 초기화

            Debug.Log(
                $"[Project Delta] 76일차 Battle Starting / Player {finalStats.MaxHealth}HP / Enemy Group {string.Join(",", groupDefinitionIds)} (대표 {session.Context.MonsterDefinitionId})",
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

            // 88일차: 첫 행동은 검은 화면에서 Battle HUD가 준비되고 Fade In까지 끝난 뒤 시작한다.
            // AutomaticBattleEntryRoutine()이 전환 완료 후 TestAdvanceBattleTurn()을 호출한다.
        }

        // 76일차: 그룹 슬롯의 몬스터 ID를 실제 MonsterDefinition으로 옮긴다. floorController가
        // 없거나(테스트 씬 등) 해당 ID를 찾지 못하면 testMonsterDefinition으로 대체해 기존
        // 단일 테스트 몬스터 흐름과 호환한다.
        private MonsterDefinition ResolveMonsterDefinition(
            string monsterDefinitionId)
        {
            if (floorController != null
                && floorController.TryFindMonsterDefinition(
                    monsterDefinitionId,
                    out MonsterDefinition resolved))
            {
                return resolved;
            }

            return testMonsterDefinition;
        }

        // 56일차: 여러 Enemy가 연속으로 행동할 때 한 프레임에 몰아서 처리하면 화면에서 아무것도
        // 안 보이므로, 행동자 한 명마다 짧게 대기해 각자의 행동(BattleDamageDebugOverlay·슬롯
        // 튀어오르는 연출)이 보이게 하는 간격이다.
        private const float EnemyActionVisibleDelaySeconds = 0.45f;

        // 이미 코루틴이 진행 중이면 다시 시작하지 않는다 (코루틴의 while 루프가 알아서 이어간다).
        private Coroutine autoAdvanceRoutine;

        // 88일차: 몬스터 접촉 후 암전 → Battle 준비 → 밝아짐 → 첫 행동 순서를 한 번만 실행한다.
        private Coroutine battleEntryRoutine;

        // 88일차: Encounter가 Active가 되면 기존 선택 UI 없이 자동 전투 전환 코루틴을 시작한다.
        private void StartAutomaticBattleEntry()
        {
            if (battleEntryRoutine != null)
            {
                return;
            }

            battleEntryRoutine =
                StartCoroutine(
                    AutomaticBattleEntryRoutine());
        }

        // 88일차: 화면이 완전히 검어진 뒤 Battle을 만들고, 화면이 다시 보인 뒤 첫 행동을 진행한다.
        private IEnumerator AutomaticBattleEntryRoutine()
        {
            BattleTransitionController transition =
                BattleTransitionController.GetOrCreate();

            yield return transition.FadeToBlack();

            EncounterCommandResult battleResult =
                SelectBattleCommand();

            bool battleReady =
                battleResult != null
                && battleResult.Accepted
                && HasBattle;

            if (!battleReady)
            {
                Debug.LogError(
                    "[Project Delta] 88일차 자동 전투 진입 실패 / Encounter를 중단하고 탐험 화면으로 복원합니다.",
                    this);

                yield return transition.FadeFromBlack();

                battleEntryRoutine =
                    null;

                AbortEncounter();

                yield break;
            }

            // 검은 화면 뒤에서 새 Battle HUD의 레이아웃과 텍스트가 갱신될 기회를 준다.
            Canvas.ForceUpdateCanvases();

            yield return transition.HoldBlack();
            yield return transition.FadeFromBlack();

            battleEntryRoutine =
                null;

            // Fade In이 끝난 뒤에만 첫 행동자를 호출해 Enemy가 검은 화면에서 먼저 공격하지 않게 한다.
            if (battleSession.State == BattleState.RoundStart)
            {
                TestAdvanceBattleTurn();
            }
        }

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

                if (!ExecuteEnemyIntent(
                        actor))
                {
                    IReadOnlyList<BattleParticipant> fallbackTargets =
                        BattleTargeting.GetValidTargets(
                            battleSession.Context,
                            actor);

                    if (fallbackTargets.Count > 0)
                    {
                        battleSession.TrySelectTarget(
                            fallbackTargets[0]);
                    }

                    ConfirmAttack();
                }

                // 이 공격으로 전투가 끝났으면(승리·패배) 다음 행동자를 기다릴 필요 없이 바로 멈춘다.
                if (battleSession.State != BattleState.RoundStart
                    && battleSession.State != BattleState.ResolvingAction)
                {
                    break;
                }

                // 56일차: 방금 행동(슬롯이 튀어오르는 연출 포함)이 화면에 보이도록 잠깐 대기한 뒤
                // 다음 행동자로 넘어간다.
                yield return new WaitForSeconds(
                    BattleSpeedState.ScaleDuration(
                        EnemyActionVisibleDelaySeconds));
            }

            autoAdvanceRoutine = null;
        }

        // 74일차: Enemy는 73일차에 미리 저장한 Intent를 실제 차례에서 그대로 실행한다.
        private bool ExecuteEnemyIntent(
            BattleParticipant actor)
        {
            if (actor == null
                || battleSession.Context == null)
            {
                return false;
            }

            if (!BattleIntentService.TryGet(
                    actor.InstanceId,
                    out BattleIntent intent))
            {
                // 이미 예고가 취소된 Enemy는 그 취소된 차례를 먼저 소비한다.
                // 여기서 새 AI Intent를 만들면 예고 취소 직후 공격/방어로 바뀌는 문제가 생긴다.
                if (BattleIntentService.HasPendingCancellation(
                        actor.InstanceId))
                {
                    BattleIntentCancelReason pendingReason =
                        BattleIntentService.GetLastCancelReason(
                            actor.InstanceId);

                    return ResolveCancelledEnemyIntent(
                        actor,
                        pendingReason);
                }

                // 76일차: 그룹에 여러 종이 섞일 수 있으므로, testMonsterDefinition 고정이 아니라
                // 이 actor가 실제로 어떤 몬스터인지(DefinitionId)로 AI Profile을 찾는다.
                MonsterDefinition actorMonsterDefinition =
                    ResolveMonsterDefinition(
                        actor.DefinitionId);

                MonsterAiProfile profile =
                    actorMonsterDefinition != null
                        ? actorMonsterDefinition.AiProfile
                        : null;

                bool skillsBlocked =
                    IsAiSkillBlocked(
                        actor);

                if (!MonsterAiDecisionService.TryCreateIntent(
                        actor,
                        battleSession.Context.Player,
                        profile,
                        skillsBlocked,
                        combatRng,
                        out intent))
                {
                    intent =
                        BattleIntent.CreateBasicAttack(
                            actor,
                            battleSession.Context.Player);
                }

                if (intent != null)
                {
                    BattleIntentService.TryRegister(
                        intent);
                }
            }

            if (intent == null)
            {
                return false;
            }

            // 74일차 수정: HUD Update가 아직 돌지 않은 같은 프레임이라도 실제 실행 직전에
            // 현재 상태를 다시 검사하여 오래된 Skill Intent가 침묵을 무시하지 못하게 한다.
            BattleIntentCancelReason cancelReason =
                BattleIntentExecutionPolicy.EvaluateCurrentCancelReason(
                    battleSession.Context,
                    actor,
                    intent);

            if (cancelReason != BattleIntentCancelReason.None)
            {
                BattleIntentService.Cancel(
                    actor.InstanceId,
                    cancelReason);

                return ResolveCancelledEnemyIntent(
                    actor,
                    cancelReason);
            }

            switch (intent.CommandId)
            {
                case "Attack":
                    if (!TrySelectIntentTarget(
                            intent))
                    {
                        return false;
                    }

                    return ConfirmAttack()
                        != null;

                case "Defend":
                    return ConfirmDefend()
                        != null;

                case "Skill":
                    if (intent.Skill == null)
                    {
                        return false;
                    }

                    if (intent.Skill.TargetType == SkillTargetType.Enemy
                        && !TrySelectIntentTarget(
                            intent))
                    {
                        return false;
                    }

                    return ConfirmSkill(
                        intent.Skill)
                        != null;

                default:
                    return false;
            }
        }

        private bool ResolveCancelledEnemyIntent(
            BattleParticipant actor,
            BattleIntentCancelReason cancelReason)
        {
            if (actor == null)
            {
                return true;
            }

            // 취소된 예고를 다른 공격으로 교체하지 않는다.
            // 해당 Enemy의 행동만 소비하고 정상적인 다음 행동자/다음 라운드 흐름으로 넘긴다.
            if (!battleSession.TryBeginResolveAction())
            {
                Debug.LogError(
                    $"[Project Delta] 74일차 Intent 취소 턴 소비 실패 / Actor {actor.InstanceId} / Reason {cancelReason}",
                    this);

                return true;
            }

            LastActingParticipant =
                actor;

            LastActionSequence++;

            string message =
                $"행동 예고 취소 / {actor.InstanceId} / {cancelReason}";

            LastBattleActionResult =
                BattleActionResult.Accept(
                    "IntentCancelled",
                    new[] { message },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    false,
                    null);

            Debug.Log(
                $"[Project Delta] 74일차 {message}",
                this);

            if (battleSession.HasPendingActorsThisRound)
            {
                return true;
            }

            if (!battleSession.TryEndRound())
            {
                return true;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return true;
            }

            if (!battleSession.TryStartRound())
            {
                return true;
            }

            Debug.Log(
                $"[Project Delta] 74일차 Battle Round {battleSession.RoundNumber} Start / Intent 취소 후 진행",
                this);

            return true;
        }

        private bool TrySelectIntentTarget(
            BattleIntent intent)
        {
            if (intent == null
                || string.IsNullOrEmpty(
                    intent.TargetInstanceId)
                || battleSession.Context == null
                || !battleSession.Context.TryGetParticipant(
                    intent.TargetInstanceId,
                    out BattleParticipant target)
                || target == null
                || !target.IsAlive)
            {
                return false;
            }

            return battleSession.TrySelectTarget(
                target);
        }

        private static bool IsAiSkillBlocked(
            BattleParticipant actor)
        {
            if (actor == null
                || actor.StatusEffects == null)
            {
                return false;
            }

            for (int index = 0;
                 index < actor.StatusEffects.Count;
                 index++)
            {
                StatusEffectInstance status =
                    actor.StatusEffects[index];

                if (status == null
                    || status.IsExpired
                    || string.IsNullOrEmpty(
                        status.DefinitionId))
                {
                    continue;
                }

                if (status.DefinitionId.IndexOf(
                        "SILENCE",
                        StringComparison.OrdinalIgnoreCase) >= 0
                    || status.DefinitionId.IndexOf(
                        "침묵",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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

                BattleDefeatService.RecordAppliedDamage(
                    actor,
                    target,
                    appliedDamage); // 70일차 마지막 실제 공격자 기록

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

            // 60일차: TryEndRound()가 방금 지속 피해·회복을 적용했으므로(기획서 4.2), 그
            // 결과로 전투가 끝났을 수 있다 — 다음 라운드로 넘어가기 전에 다시 확인한다.
            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

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

            // 60일차: TryEndRound()가 방금 지속 피해·회복을 적용했으므로(기획서 4.2), 그
            // 결과로 전투가 끝났을 수 있다 — 다음 라운드로 넘어가기 전에 다시 확인한다.
            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

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

        // 68일차: SkillDefinition 하나를 실제로 판정한다. ConfirmAttack()·ConfirmDefend()와
        // 같은 구조를 스킬 데이터 기반으로 일반화했다 - 특정 스킬 전용 메서드가 아니라 어떤
        // SkillDefinition을 넘겨도 동작한다. TargetType이 Self면 대상 선택 없이 시전자 자신을
        // 대상으로 삼고(피해 판정 없이 상태만 적용), Enemy면 지금 선택된 대상에게 공격과 같은
        // 방식으로 명중·피해를 판정한다.
        // 93일차: 인벤토리 소비 아이템을 정식 전투 행동 1회로 사용한다.
        public ItemUseResult ConfirmUseInventoryItem(
            int slotIndex,
            ItemDefinition definition)
        {
            if (RunContext.Current == null
                || battleSession.Context == null
                || battleSession.State
                    != BattleState.AwaitingAction)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
            }

            BattleParticipant actor =
                battleSession.CurrentActor;

            if (actor == null
                || actor.Team
                    != BattleTeam.Player
                || actor
                    != battleSession.Context.Player)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.NotPlayerTurn);
            }

            ItemUseResult preview =
                ItemUseService.PreviewBattle(
                    RunContext.Current.Inventory,
                    slotIndex,
                    actor,
                    definition);

            if (!preview.Success)
            {
                return preview;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
            }

            ItemUseResult resolved =
                ItemUseService.CommitBattle(
                    RunContext.Current.Inventory,
                    slotIndex,
                    actor,
                    definition);

            if (!resolved.Success)
            {
                TestAdvanceBattleTurn();
                return resolved;
            }

            string itemName =
                definition != null
                && !string.IsNullOrEmpty(
                    definition.DisplayName)
                    ? definition.DisplayName
                    : "아이템";

            string itemUseLog =
                $"PLAYER : {itemName} 사용 / HP +{resolved.HpRecovered} / MP +{resolved.ManaRecovered} / 정력 +{resolved.StaminaRecovered}";

            LastBattleActionResult =
                BattleActionResult.Accept(
                    "UseItem",
                    new[]
                    {
                        itemUseLog
                    },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    true,
                    null);

            LastActingParticipant =
                actor;

            LastActionSequence++;

            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();
                return resolved;
            }

            if (!battleSession.TryEndRound())
            {
                return resolved;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return resolved;
            }

            if (!battleSession.TryStartRound())
            {
                return resolved;
            }

            TestAdvanceBattleTurn();

            return resolved;
        }

        public BattleActionResult ConfirmSkill(
            SkillDefinition skill)
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
                skill != null
                && skill.TargetType == SkillTargetType.Self
                    ? actor
                    : battleSession.SelectedTarget;

            IBattleCommand skillCommand =
                new SkillBattleCommand(
                    skill);

            BattleCommandResult declaration =
                skillCommand.Execute(
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
                    $"[Project Delta] 68일차 스킬 확정 실패 / {declaration.Message}",
                    this);

                return rejectedResult;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return BattleActionResult.Reject(
                    declaration.CommandId,
                    "행동을 처리할 수 없는 상태입니다.");
            }

            // 66일차에 만든 자원 소모 API를 여기서 실제로 쓴다. Execute()는 충분한지 확인만
            // 했으므로, 선언이 확정된 지금(TryBeginResolveAction 이후) 실제로 차감한다.
            actor.TrySpendMana(
                skill.ManaCost);

            actor.TrySpendStamina(
                skill.StaminaCost);

            LastActingParticipant =
                actor;

            LastActionSequence++;

            string resolutionMessage;
            BattleDamageChange[] damageChanges;
            BattleParticipant[] removedParticipants =
                Array.Empty<BattleParticipant>();

            if (skill.TargetType == SkillTargetType.Self)
            {
                // 자기 자신 대상 스킬은 공격이 아니므로 명중 판정 없이 항상 적용된다.
                resolutionMessage =
                    $"스킬 사용 / {actor.InstanceId} / {skill.DisplayName}";

                resolutionMessage +=
                    ApplyGrantedStatusEffectIfAny(
                        skill,
                        actor,
                        actor);

                damageChanges =
                    Array.Empty<BattleDamageChange>();
            }
            else
            {
                int hitRoll =
                    combatRng.NextInt(
                        0,
                        100);

                int varianceRoll =
                    combatRng.NextInt(
                        0,
                        BattleDamageCalculator.DamageVarianceRollCount);

                int criticalRoll =
                    combatRng.NextInt(
                        0,
                        100);

                BattleDamageResult damageResult =
                    BattleDamageCalculator.Resolve(
                        actor,
                        target,
                        hitRoll,
                        varianceRoll,
                        SkillEffectMapping.ToDefenseInteraction(
                            skill.DefenseInteraction),
                        SkillEffectMapping.ToDamageType(
                            skill.DamageType),
                        skill.CriticalChancePercent,
                        skill.CriticalMultiplierPercent,
                        criticalRoll,
                        skill.AccuracyModifierPercent,
                        skill.DamageMultiplierPercent);

                int appliedDamage = 0;

                if (damageResult.IsHit)
                {
                    appliedDamage =
                        target.ApplyDamage(
                            damageResult.Damage);

                    BattleDefeatService.RecordAppliedDamage(
                        actor,
                        target,
                        appliedDamage); // 70일차 마지막 실제 공격자 기록

                    resolutionMessage =
                        $"스킬 적중 / {actor.InstanceId} → {target.InstanceId} / {skill.DisplayName} / {appliedDamage} 데미지 (명중률 {damageResult.HitChancePercent}%)";

                    resolutionMessage +=
                        ApplyGrantedStatusEffectIfAny(
                            skill,
                            actor,
                            target);
                }
                else
                {
                    resolutionMessage =
                        $"스킬 빗나감 / {actor.InstanceId} → {target.InstanceId} / {skill.DisplayName} (명중률 {damageResult.HitChancePercent}%)";
                }

                damageChanges =
                    new[]
                    {
                        new BattleDamageChange(
                            actor,
                            target,
                            damageResult,
                            appliedDamage)
                    };

                removedParticipants =
                    target.IsAlive
                        ? Array.Empty<BattleParticipant>()
                        : new[] { target };
            }

            // 64일차에 만든 추가 행동 부여를 여기서 처음 실전 투입한다. 명중 여부와 무관하게
            // "스킬 사용 자체가 성공"했으면(선언이 확정됐으면) 부여한다.
            if (skill.GrantsExtraAction)
            {
                battleSession.TryGrantExtraAction(
                    actor);
            }

            Debug.Log(
                $"[Project Delta] 68일차 Battle 스킬 판정 / {resolutionMessage}",
                this);

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

            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();

                return resolvedResult;
            }

            if (!battleSession.TryEndRound())
            {
                return resolvedResult;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return resolvedResult;
            }

            if (!battleSession.TryStartRound())
            {
                return resolvedResult;
            }

            Debug.Log(
                $"[Project Delta] 68일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            TestAdvanceBattleTurn();

            return resolvedResult;
        }

        // 68일차: 스킬이 상태를 부여하도록 지정돼 있으면 시도하고, 로그에 덧붙일 문구를 반환한다.
        // 상태가 지정돼 있지 않으면 빈 문자열을 반환해 호출부가 조건 없이 이어 붙일 수 있게 한다.
        private string ApplyGrantedStatusEffectIfAny(
            SkillDefinition skill,
            BattleParticipant source,
            BattleParticipant statusTarget)
        {
            if (skill.GrantedStatusEffect == null)
            {
                return string.Empty;
            }

            StatusEffectApplyResult statusResult =
                StatusEffectApplicationService.TryApply(
                    statusTarget,
                    skill.GrantedStatusEffect,
                    source.InstanceId,
                    skill.StatusEffectDurationRounds,
                    skill.StatusEffectAppliedValue,
                    skill.StatusEffectBaseChancePercent,
                    0,
                    0,
                    combatRng);

            return statusResult.Succeeded
                ? $" / {skill.GrantedStatusEffect.DisplayName} 부여 성공"
                : $" / {skill.GrantedStatusEffect.DisplayName} 부여 실패";
        }

        // 69일차: 도주를 확정한다. 성공하면 전투가 즉시 끝나고(EncounterOutcome.Escaped로
        // Encounter까지 정리됨), 실패하면 방어 실패와 같은 취급으로 그 턴만 소모하고 다음
        // 행동자로 넘어간다.
        public BattleActionResult ConfirmFlee()
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
                fleeCommand.Execute(
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
                    $"[Project Delta] 69일차 도주 확정 실패 / {declaration.Message}",
                    this);

                return rejectedResult;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return BattleActionResult.Reject(
                    declaration.CommandId,
                    "행동을 처리할 수 없는 상태입니다.");
            }

            LastActingParticipant =
                actor;

            LastActionSequence++;

            int escapeChancePercent =
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    battleSession.Context,
                    actor);

            int escapeRoll =
                combatRng.NextInt(
                    0,
                    100);

            bool escaped =
                escapeRoll < escapeChancePercent;

            string resolutionMessage =
                escaped
                    ? $"도주 성공 / {actor.InstanceId} (성공률 {escapeChancePercent}%)"
                    : $"도주 실패 / {actor.InstanceId} (성공률 {escapeChancePercent}%)";

            Debug.Log(
                $"[Project Delta] 69일차 Battle 도주 판정 / {resolutionMessage}",
                this);

            if (escaped)
            {
                BattleResult battleEndResult =
                    FinishBattle(
                        BattleOutcome.Escaped);

                BattleActionResult escapedResult =
                    BattleActionResult.Accept(
                        declaration.CommandId,
                        new[] { resolutionMessage },
                        Array.Empty<BattleDamageChange>(),
                        Array.Empty<BattleParticipant>(),
                        true,
                        battleEndResult);

                LastBattleActionResult =
                    escapedResult;

                return escapedResult;
            }

            // 도주 실패 - 방어 실패와 같은 방식으로 로그만 남기고 턴을 소모한다.
            BattleActionResult resolvedResult =
                BattleActionResult.Accept(
                    declaration.CommandId,
                    new[] { resolutionMessage },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    true,
                    null);

            LastBattleActionResult =
                resolvedResult;

            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();

                return resolvedResult;
            }

            if (!battleSession.TryEndRound())
            {
                return resolvedResult;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return resolvedResult;
            }

            if (!battleSession.TryStartRound())
            {
                return resolvedResult;
            }

            Debug.Log(
                $"[Project Delta] 69일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            TestAdvanceBattleTurn();

            return resolvedResult;
        }

        // 116일차: 회유 - 도주(ConfirmFlee)와 같은 구조지만, 판정은 BattleEscapeCalculator
        // 대신 매력/저항 기반 EncounterPersuasionRule을 쓰고, 대상은 현재 선택된 적 하나다.
        public BattleActionResult ConfirmPersuade()
        {
            return ConfirmInfluenceAttempt(
                persuadeCommand,
                PersuadeBaseSuccessPercent,
                "회유",
                triggersEventBattle: false);
        }

        // 116일차: 유혹 - 회유와 같은 구조지만 기준 성공률이 더 낮다.
        // 117일차: 성공하면 바로 평화롭게 끝나는 대신, 117일차에 만든 별도 이벤트 전투
        // (EventBattleController)로 넘어간다 - 기획서가 요구한 "조우의 유혇, 전용 Context 전환".
        public BattleActionResult ConfirmSeduce()
        {
            return ConfirmInfluenceAttempt(
                seduceCommand,
                SeduceBaseSuccessPercent,
                "유혹",
                triggersEventBattle: true);
        }

        private BattleActionResult ConfirmInfluenceAttempt(
            IBattleCommand command,
            int baseSuccessPercent,
            string logLabel,
            bool triggersEventBattle)
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
                command.Execute(
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
                    $"[Project Delta] 116일차 {logLabel} 확정 실패 / {declaration.Message}",
                    this);

                return rejectedResult;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return BattleActionResult.Reject(
                    declaration.CommandId,
                    "행동을 처리할 수 없는 상태입니다.");
            }

            LastActingParticipant =
                actor;

            LastActionSequence++;

            bool success =
                EncounterPersuasionRule.TryEvaluate(
                    baseSuccessPercent,
                    actor.Charm,
                    target.Resistance,
                    combatRng,
                    out int successPercent);

            string resolutionMessage =
                success
                    ? $"{logLabel} 성공 / {actor.InstanceId} → {target.InstanceId} (성공률 {successPercent}%)"
                    : $"{logLabel} 실패 / {actor.InstanceId} → {target.InstanceId} (성공률 {successPercent}%)";

            Debug.Log(
                $"[Project Delta] 116일차 Battle {logLabel} 판정 / {resolutionMessage}",
                this);

            if (success)
            {
                if (triggersEventBattle
                    && EnsureEventBattleController() != null
                    && eventBattleController.Begin(
                        actor,
                        target,
                        EventBattleEntrySource.Seduction,
                        () => FinishBattle(
                            BattleOutcome.Escaped),
                        () => ContinueBattleAfterFailedInfluence(
                            declaration.CommandId,
                            $"{logLabel} 성공 후 이벤트 전투 실패 / 전투로 복귀")))
                {
                    BattleActionResult eventStartedResult =
                        BattleActionResult.Accept(
                            declaration.CommandId,
                            new[] { $"{logLabel} 성공 / 별도 이벤트 전투 시작" },
                            Array.Empty<BattleDamageChange>(),
                            Array.Empty<BattleParticipant>(),
                            true,
                            null);

                    LastBattleActionResult =
                        eventStartedResult;

                    return eventStartedResult;
                }

                // eventBattleController가 없으면(테스트 씬 등) 117일차 이전처럼 곧바로
                // 평화롭게 끝낸다 - 회유(triggersEventBattle=false)도 항상 이 경로를 탄다.
                BattleResult battleEndResult =
                    FinishBattle(
                        BattleOutcome.Escaped);

                BattleActionResult succeededResult =
                    BattleActionResult.Accept(
                        declaration.CommandId,
                        new[] { resolutionMessage },
                        Array.Empty<BattleDamageChange>(),
                        Array.Empty<BattleParticipant>(),
                        true,
                        battleEndResult);

                LastBattleActionResult =
                    succeededResult;

                return succeededResult;
            }

            ContinueBattleAfterFailedInfluence(
                declaration.CommandId,
                resolutionMessage);

            return LastBattleActionResult;
        }

        // 116일차: 회유/유혇 실패 시 로그만 남기고 턴을 소모한다(도주 실패와 같은 방식).
        // 117일차: 유혇 성공 후 시작한 이벤트 전투가 패배·중단으로 끝났을 때도 똑같이
        // "전투로 복귀"해야 해서 이 메서드로 뽑아 재사용한다.
        private void ContinueBattleAfterFailedInfluence(
            string commandId,
            string message)
        {
            BattleActionResult resolvedResult =
                BattleActionResult.Accept(
                    commandId,
                    new[] { message },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    true,
                    null);

            LastBattleActionResult =
                resolvedResult;

            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();

                return;
            }

            if (!battleSession.TryEndRound())
            {
                return;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return;
            }

            if (!battleSession.TryStartRound())
            {
                return;
            }

            Debug.Log(
                $"[Project Delta] 116일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            TestAdvanceBattleTurn();
        }

        // 116일차: 관찰 - 방어(ConfirmDefend)와 같은 구조로 턴을 소모하지만, 상태 변화 대신
        // 선택된 대상의 능력치를 LastObservationText에 담아 HUD가 보여줄 수 있게 한다.
        public BattleActionResult ConfirmObserve()
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
                observeCommand.Execute(
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
                    $"[Project Delta] 116일차 관찰 확정 실패 / {declaration.Message}",
                    this);

                return rejectedResult;
            }

            if (!battleSession.TryBeginResolveAction())
            {
                return BattleActionResult.Reject(
                    declaration.CommandId,
                    "행동을 처리할 수 없는 상태입니다.");
            }

            LastActingParticipant =
                actor;

            LastActionSequence++;

            LastObservationText =
                $"{target.InstanceId} - HP {target.CurrentHp}/{target.MaxHp} / 공격 {target.Attack} / 방어 {target.Defense} / 속도 {target.Speed} / 매력 {target.Charm} / 저항 {target.Resistance}";

            string resolutionMessage =
                $"관찰 / {LastObservationText}";

            Debug.Log(
                $"[Project Delta] 116일차 Battle 관찰 확정 / {resolutionMessage}",
                this);

            BattleActionResult resolvedResult =
                BattleActionResult.Accept(
                    declaration.CommandId,
                    new[] { resolutionMessage },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    true,
                    null);

            LastBattleActionResult =
                resolvedResult;

            if (battleSession.HasPendingActorsThisRound)
            {
                TestAdvanceBattleTurn();

                return resolvedResult;
            }

            if (!battleSession.TryEndRound())
            {
                return resolvedResult;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return resolvedResult;
            }

            if (!battleSession.TryStartRound())
            {
                return resolvedResult;
            }

            Debug.Log(
                $"[Project Delta] 116일차 Battle Round {battleSession.RoundNumber} Start",
                this);

            TestAdvanceBattleTurn();

            return resolvedResult;
        }

        public bool ConfirmBattleReward(
            string rewardId)
        {
            if (pendingVictoryEncounterResult == null
                || RunContext.Current == null
                || !BattleRewardState.IsPending)
            {
                return false;
            }

            if (!BattleRewardState.TryClaim(
                    rewardId,
                    RunContext.Current.Player))
            {
                return false;
            }

            // 81일차: 80일차에서 이미 한 번 굴린 드롭 결과만 실제 런 상태에 지급한다.
            // 선택 보상 확정 성공 뒤 한 번만 실행되어 UI 재표시·중복 클릭으로 재지급되지 않는다.
            if (!hasAppliedBattleDropRewards)
            {
                BattleRewardPayoutService.ApplyAutomaticDrops(
                    RunContext.Current,
                    LastBattleDropResult);

                hasAppliedBattleDropRewards =
                    true;
            }

            EncounterResult result =
                pendingVictoryEncounterResult;

            pendingVictoryEncounterResult =
                null; // 72일차 중복 보상 수령 방지

            FinalizeActiveEncounter(
                result); // 보상 지급 후 기존 Encounter 완료 처리

            battleSession.TryReset(); // 보상 처리 후 Battle 세션 정리

            Debug.Log(
                $"[Project Delta] 72일차 Battle Reward Claimed / {rewardId} / 탐험 복귀",
                this);

            return true;
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
                ApplyVictoryGrowth(); // 79일차 경험치·레벨업
                ApplyVictoryDrops(); // 80일차 골드·아이템 드롭 판정

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

                pendingVictoryEncounterResult =
                    result; // 72일차 보상 선택 완료 전 Encounter 종료 보류

                BattleRewardState.BeginDefaultRewards(); // 72일차 기본 보상 후보 생성

                Debug.Log(
                    "[Project Delta] 72일차 Battle Victory / 보상 선택 대기",
                    this);

                return finishedResult;
            }

            // 69일차: 도주 성공 - 몬스터를 쓰러뜨린 것도 아니고 패배한 것도 아니므로, 승리와
            // 같은 방식으로 Encounter를 정리하되 결과만 EncounterOutcome.Escaped로 담는다
            // (46일차부터 있던 값 - 방 완료·몬스터 제거 둘 다 일어나지 않는다).
            if (outcome == BattleOutcome.Escaped)
            {
                EncounterResult escapedResult =
                    new EncounterResult(
                        session.Context.RoomId,
                        session.Context.MonsterDefinitionId,
                        EncounterOutcome.Escaped);

                FinalizeActiveEncounter(
                    escapedResult);

                battleSession.TryReset();

                return finishedResult;
            }

            Debug.Log(
                "[Project Delta] 51일차 Battle Defeat / 메인 메뉴로 복귀",
                this);

            BattleDefeatService.ReturnToTitleAfterDefeat(
                battleSession.Context,
                battleSession.RoundNumber); // 70일차 패배 기록 후 임시 타이틀 복귀

            return finishedResult;
        }

        // 80일차: 승리가 확정된 BattleContext의 실제 Enemy 구성 전체를 드롭 테이블로 환산한다.
        // FinishBattle()의 Victory 분기에서 한 번만 호출하므로 보상 UI를 열어도 재추첨하지 않는다.
        private void ApplyVictoryDrops()
        {
            if (battleSession.Context == null
                || battleSession.Context.Enemies == null)
            {
                LastBattleDropResult =
                    BattleDropResult.Empty;

                return;
            }

            System.Collections.Generic.List<MonsterDefinition> defeatedMonsters =
                new System.Collections.Generic.List<MonsterDefinition>();

            foreach (BattleParticipant enemy
                     in battleSession.Context.Enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                MonsterDefinition definition =
                    ResolveMonsterDefinition(
                        enemy.DefinitionId);

                if (definition != null)
                {
                    defeatedMonsters.Add(
                        definition);
                }
            }

            LastBattleDropResult =
                BattleDropService.RollBattleDrops(
                    defeatedMonsters,
                    rewardRng);

            Debug.Log(
                $"[Project Delta] 80일차 Battle Drop / Gold {LastBattleDropResult.Gold} / Item Type {LastBattleDropResult.Items.Count}",
                this);
        }

        // 79일차: 승리가 확정된 BattleContext의 실제 Enemy 구성 전체를 경험치로 환산한다.
        // FinishBattle()이 성공한 뒤 한 번만 호출되므로 보상 선택 버튼을 여러 번 눌러도 중복 지급되지 않는다.
        private void ApplyVictoryGrowth()
        {
            if (RunContext.Current == null
                || battleSession.Context == null
                || battleSession.Context.Enemies == null)
            {
                LastBattleGrowthResult =
                    null;

                return;
            }

            List<MonsterDefinition> defeatedMonsters =
                new List<MonsterDefinition>();

            foreach (BattleParticipant enemy
                     in battleSession.Context.Enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                MonsterDefinition definition =
                    ResolveMonsterDefinition(
                        enemy.DefinitionId);

                if (definition != null)
                {
                    defeatedMonsters.Add(
                        definition);
                }
            }

            PlayerGrowthDefinition growthDefinition =
                Resources.Load<PlayerGrowthDefinition>(
                    "PlayerGrowthDefinition");

            bool createdRuntimeFallback =
                false;

            if (growthDefinition == null)
            {
                growthDefinition =
                    PlayerGrowthDefinition.CreateDefaultRuntime();

                createdRuntimeFallback =
                    true;
            }

            LastBattleGrowthResult =
                PlayerGrowthService.ApplyBattleExperience(
                    RunContext.Current.Player,
                    defeatedMonsters,
                    growthDefinition);

            Debug.Log(
                $"[Project Delta] 79일차 Battle Growth / EXP +{LastBattleGrowthResult.EarnedExperience} / "
                + $"Lv.{LastBattleGrowthResult.PreviousLevel} → Lv.{LastBattleGrowthResult.CurrentLevel} / "
                + $"Stat Point +{LastBattleGrowthResult.GainedStatPoints}",
                this);

            if (createdRuntimeFallback)
            {
                Destroy(
                    growthDefinition);
            }
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
                // 122일차: "후퇴 후 재전투" - 보스(Tier == Boss, canRetreat)에게서 도망친
                // 경우에는 방을 완료 처리하지 않는다. 몬스터가 그대로 남아 다시 도전할 수
                // 있다 - 121일차에서 이미 "승리해야만" 계단이 열리므로, 승리 없이는 방이
                // 완료되지 않아야 재도전이 자연스럽게 보장된다.
                MonsterDefinition activeMonsterDefinition =
                    ResolveMonsterDefinition(
                        activeMonster.MonsterDefinitionId);

                bool isBossRetreat =
                    result.Outcome == EncounterOutcome.Escaped
                    && activeMonsterDefinition != null
                    && activeMonsterDefinition.Tier == MonsterTier.Boss
                    && activeMonsterDefinition.CanRetreat;

                if (isBossRetreat)
                {
                    Debug.Log(
                        $"[Project Delta] 122일차 보스 후퇴 / Room {result.RoomId} / Monster {result.MonsterDefinitionId} - 방을 비우지 않아 다시 도전할 수 있습니다.",
                        this);
                }
                else
                {
                    if (!activeMonster.TryMarkRoomEncounterCompleted())
                    {
                        return false;
                    }

                    // 121일차: 도망(Escaped)이 아니라 실제로 쓰러뜨렸을 때만 계단을 공개한다 -
                    // 보스 방에서 도망쳐 계단을 여는 우회를 막는다. 보스 방이 아닌 방이면
                    // DungeonFloorController가 알아서 아무 일도 하지 않는다.
                    if (result.Outcome == EncounterOutcome.MonsterDefeated)
                    {
                        floorController?.NotifyRoomEncounterCompleted(
                            result.RoomId);
                    }
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

            pendingVictoryEncounterResult =
                null; // 72일차 보상 대기 결과 제거

            BattleRewardState.Clear(); // 72일차 보상 상태 제거

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
