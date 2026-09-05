using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 117일차: 일반 전투(ExplorationMonsterEncounterController)와 상호 배타적인 별도 이벤트
    // 전투 화면. EventBattleEntryService.TryEnter()로 시작해, 승리(대상 중 한 명 이상 공략
    // 또는 전원 만족 이탈)·패배(자원 고갈)·중단(포기) 중 하나로 끝나면 호출자가 넘겨준
    // 콜백으로 결과를 돌려준다 - 이 컨트롤러 자신은 "이겼을 때 무엇을 할지"를 모른다.
    // 118일차: 공통 행동 12종·주도권·종족 상성을 추가했다.
    // 119일차: 대상을 최대 3명까지, 각자 개별 게이지로 늘렸다(만족하면 먼저 이탈할 수 있다).
    // 몬스터 행동에 완전 무작위 대신 간단한 AI(EventBattleMonsterAiRule)를 붙였고, 행동별
    // 영구 숙련도(Lv.1~5, ProfileData)를 읽고 쌓는다.
    public sealed class EventBattleController : MonoBehaviour
    {
        // 120일차: 행동 그리드와 아이템 목록을 같은 자리에서 전환해 보여준다.
        private enum ServiceScreen
        {
            Actions,
            Items
        }

        // 120일차: 오른쪽 열(RightColumnWidth) 폭에 맞춰 3열로 줄였다(118일차엔 4열/128px).
        private const float ButtonWidth = 112f;
        private const float ButtonHeight = 34f;
        private const int ButtonsPerRow = 3;

        // 119일차: 호감도가 이 값 이상인 대상은 자기 차례가 될 때마다 이 확률(%)로 만족하고 떠난다.
        private const int SatisfiedDepartureChancePercent = 15;

        // 120일차: 전투 로그에 보여줄 최근 줄 수.
        private const int MaxLogLines = 6;

        [SerializeField] private DungeonFloorController floorController;

        private readonly EventBattleSession session =
            new EventBattleSession();

        private readonly IRandomSource rng =
            new CombatRng();

        private MonsterDefinition[] targetDefinitions;
        private string lastMonsterActionId;

        // 120일차: EventBattleCheckpointStore.Capture()에 넘길 방 ID - 저장/복원(120일차)에 쓴다.
        private PlayerGridMovementController movementController;

        // 120일차: "전투 로그" - 상태 텍스트 한 줄 대신 최근 몇 줄의 기록을 보여준다.
        private readonly List<string> battleLog =
            new List<string>();

        private Vector2 logScrollPosition;
        private ServiceScreen serviceScreen =
            ServiceScreen.Actions;

        // 119일차: Begin()에서 한 번 읽어 이벤트 전투가 끝날 때까지 들고 있다가 Finish()에서
        // 한 번에 저장한다 - 행동마다 디스크에 읽고 쓰지 않기 위해서다.
        private ProfileData profile;

        private Action onWon;
        private Action onLostOrAborted;

        // 118일차: 플레이어 정보 아래에 항상 보여주는 상태 텍스트.
        private string statusText;

        public bool IsActive =>
            session.IsActive;

        private void Awake()
        {
            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }

            if (movementController == null)
            {
                movementController =
                    GetComponent<PlayerGridMovementController>();
            }
        }

        // 117일차 호환용 - 대상 1명짜리 진입(유혇 성공 등).
        public bool Begin(
            BattleParticipant player,
            BattleParticipant target,
            EventBattleEntrySource source,
            Action beginOnWon,
            Action beginOnLostOrAborted)
        {
            return Begin(
                player,
                new[] { target },
                null,
                source,
                beginOnWon,
                beginOnLostOrAborted);
        }

        // 119일차: 다수 참가자(최대 3명) 진입. stageCounts는 상위 개체(보스) 2단계 게이지용 -
        // 생략하면 전원 1단계.
        public bool Begin(
            BattleParticipant player,
            IReadOnlyList<BattleParticipant> targets,
            IReadOnlyList<int> stageCounts,
            EventBattleEntrySource source,
            Action beginOnWon,
            Action beginOnLostOrAborted)
        {
            if (session.IsActive
                || !EventBattleEntryService.TryEnter(
                    source,
                    player,
                    targets,
                    stageCounts,
                    out EventBattleContext context))
            {
                return false;
            }

            if (!session.TryBegin(
                    context))
            {
                return false;
            }

            targetDefinitions =
                new MonsterDefinition[context.Targets.Count];

            for (int index = 0; index < context.Targets.Count; index++)
            {
                if (floorController != null)
                {
                    floorController.TryFindMonsterDefinition(
                        context.Targets[index].Participant.DefinitionId,
                        out targetDefinitions[index]);
                }
            }

            profile =
                ResolveProfile();

            onWon =
                beginOnWon;

            onLostOrAborted =
                beginOnLostOrAborted;

            lastMonsterActionId =
                null;

            serviceScreen =
                ServiceScreen.Actions;

            battleLog.Clear();

            AppendLog(
                "당신의 차례입니다.");

            CaptureCheckpoint();

            Debug.Log(
                $"[Project Delta] 117일차 이벤트 전투 시작 / Source {source} / 대상 {context.Targets.Count}명",
                this);

            return true;
        }

        // 118일차: 12종 공통 행동 중 하나를 플레이어 차례에 실행한다.
        public void ConfirmAction(
            IEventBattleCommand command)
        {
            if (!session.IsActive
                || session.Context == null
                || command == null
                || session.Context.InitiativeHolder != EventBattleInitiativeHolder.Player
                || session.Context.SelectedTarget == null)
            {
                return;
            }

            EventBattleContext context =
                session.Context;

            MonsterDefinition definition =
                ResolveDefinition(
                    context.SelectedTargetIndex);

            float affinityMultiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    definition != null
                        ? definition.EventBattleStrongActionIds
                        : null,
                    definition != null
                        ? definition.EventBattleWeakActionIds
                        : null,
                    command.Id);

            EventBattleActionProficiencyRecord proficiencyRecord =
                ResolveProficiencyRecord(
                    command.Id);

            float proficiencyMultiplier =
                EventBattleProficiencyRule.GetMultiplier(
                    proficiencyRecord.Level);

            context.PlayerActionFavorMultiplier =
                affinityMultiplier
                * proficiencyMultiplier;

            EventBattleCommandResult result =
                command.Execute(
                    context,
                    rng);

            context.PlayerActionFavorMultiplier =
                1f;

            context.RegisterAttempt();

            AppendLog(
                result.Message);

            if (!result.Accepted)
            {
                return;
            }

            context.LastPlayerActionId =
                command.Id;

            // 119일차: 실제로 쌓인 호감도만큼 숙련도 경험치로 쌓는다.
            EventBattleProficiencyRule.AddExperience(
                proficiencyRecord,
                result.FavorGained);

            if (context.HasWon)
            {
                Finish(
                    EventBattleOutcome.Won);

                return;
            }

            AdvanceInitiative(
                command.InitiativeModifier,
                0);
        }

        public void SelectTarget(
            int index)
        {
            session.Context?.TrySelectTarget(
                index);
        }

        public void ConfirmAbort()
        {
            if (!session.IsActive)
            {
                return;
            }

            Finish(
                EventBattleOutcome.Aborted);
        }

        // 120일차: 기존 93일차 ItemUseService.CommitBattle을 그대로 재사용한다 - 이벤트 전투도
        // Player가 진짜 BattleParticipant라 다른 코드 없이 바로 쓸 수 있었다. 호감도에는
        // 영향이 없고 자원 회복만 하지만, 턴은 그대로 소모한다(플레이어 차례가 아니면 실패).
        public ItemUseResult ConfirmUseItem(
            int slotIndex,
            ItemDefinition definition)
        {
            if (!session.IsActive
                || session.Context == null
                || session.Context.InitiativeHolder != EventBattleInitiativeHolder.Player)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
            }

            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            if (inventory == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.InvalidInventory);
            }

            EventBattleContext context =
                session.Context;

            ItemUseResult result =
                ItemUseService.CommitBattle(
                    inventory,
                    slotIndex,
                    context.Player,
                    definition);

            if (!result.Success)
            {
                return result;
            }

            context.RegisterAttempt();

            string itemName =
                definition != null
                && !string.IsNullOrEmpty(
                    definition.DisplayName)
                    ? definition.DisplayName
                    : "아이템";

            AppendLog(
                $"{itemName} 사용 / HP +{result.HpRecovered} MP +{result.ManaRecovered} 정력 +{result.StaminaRecovered}");

            serviceScreen =
                ServiceScreen.Actions;

            AdvanceInitiative(
                0,
                0);

            return result;
        }

        // 118일차: 행동 처리 후 다음 주도권을 굴린다 - 몬스터가 가져가면 곧바로 저항 행동을
        // 자동으로 처리하고, 다시 그 결과로 주도권을 굴린다(플레이어 차례가 될 때까지 반복).
        private void AdvanceInitiative(
            int playerActionInitiativeModifier,
            int targetActionInitiativeModifier)
        {
            EventBattleContext context =
                session.Context;

            EventBattleParticipantState activeTarget =
                context.SelectedTarget;

            EventBattleInitiativeHolder next =
                EventBattleInitiativeRule.RollNext(
                    context.Player.Charm,
                    playerActionInitiativeModifier,
                    activeTarget != null
                        ? activeTarget.Participant.Charm
                        : 0,
                    targetActionInitiativeModifier,
                    context.InitiativeHolder,
                    rng);

            context.SetInitiativeHolder(
                next);

            if (next == EventBattleInitiativeHolder.Target)
            {
                ResolveTargetTurn();

                return;
            }

            if (!TryReturnToPlayerTurn())
            {
                return;
            }
        }

        // 119일차: 플레이어 차례가 될 때 만족 이탈 판정을 먼저 처리하고, 그래도 게임이
        // 끝나지 않았으면 자원 확인 후 정상적으로 입력을 받는다.
        private bool TryReturnToPlayerTurn()
        {
            EventBattleContext context =
                session.Context;

            CheckSatisfiedDepartures();

            if (context.AllTargetsResolved)
            {
                Finish(
                    EventBattleOutcome.Won);

                return false;
            }

            if (context.SelectedTarget == null
                || !context.SelectedTarget.IsActive)
            {
                ReselectActiveTarget();
            }

            if (!context.PlayerCanAct(
                    EventBattleActionCatalog.All))
            {
                Finish(
                    EventBattleOutcome.Lost);

                return false;
            }

            AppendLog(
                "당신의 차례입니다.");

            return true;
        }

        private void CheckSatisfiedDepartures()
        {
            EventBattleContext context =
                session.Context;

            for (int index = 0; index < context.Targets.Count; index++)
            {
                EventBattleParticipantState state =
                    context.Targets[index];

                if (!state.IsActive
                    || state.Favor
                    < EventBattleParticipantState.SatisfiedFavorThreshold)
                {
                    continue;
                }

                if (rng.NextInt(
                        0,
                        100)
                    >= SatisfiedDepartureChancePercent)
                {
                    continue;
                }

                state.MarkSatisfiedDeparture();

                if (profile != null)
                {
                    profile.LifetimeStats.MonstersSatisfiedAway++;
                }

                string name =
                    ResolveTargetDisplayName(
                        index);

                AppendLog(
                    $"{name}이(가) 만족하여 떠났습니다.");

                Debug.Log(
                    $"[Project Delta] 119일차 만족 이탈 / {name}",
                    this);
            }
        }

        private void ReselectActiveTarget()
        {
            EventBattleContext context =
                session.Context;

            for (int index = 0; index < context.Targets.Count; index++)
            {
                if (context.Targets[index].IsActive)
                {
                    context.TrySelectTarget(
                        index);

                    return;
                }
            }
        }

        // 119일차: 몬스터도 같은 공통 행동 12종을 공유한다 - EventBattleMonsterAiRule로 하나를
        // 골라 "저항" 명목으로 지금 선택된 대상의 호감도를 깎는다. 몬스터는 마나·정력을 쓰지
        // 않으므로 항상 행동할 수 있다.
        private void ResolveTargetTurn()
        {
            if (!session.IsActive
                || session.Context == null)
            {
                return;
            }

            EventBattleContext context =
                session.Context;

            EventBattleParticipantState target =
                context.SelectedTarget;

            if (target == null
                || !target.IsActive)
            {
                ReselectActiveTarget();

                target =
                    context.SelectedTarget;
            }

            if (target == null)
            {
                Finish(
                    EventBattleOutcome.Won);

                return;
            }

            IReadOnlyList<IEventBattleCommand> catalog =
                EventBattleActionCatalog.All;

            IEventBattleCommand chosen =
                EventBattleMonsterAiRule.ChooseAction(
                    catalog,
                    lastMonsterActionId,
                    rng);

            lastMonsterActionId =
                chosen?.Id;

            int resistBase =
                5
                + Mathf.Max(
                    0,
                    target.Participant.Resistance
                    - context.Player.Charm)
                / 4;

            int variance =
                rng.NextInt(
                    -1,
                    4);

            int resistAmount =
                Mathf.Max(
                    0,
                    resistBase
                    + variance);

            target.AddFavor(
                -resistAmount);

            context.RegisterAttempt();

            string targetName =
                ResolveTargetDisplayName(
                    context.SelectedTargetIndex);

            string resistMessage =
                $"{targetName}이(가) {(chosen != null ? chosen.DisplayName : "저항")}에 저항했다! 호감도 -{resistAmount}";

            AppendLog(
                resistMessage);

            Debug.Log(
                $"[Project Delta] 119일차 이벤트 전투 몬스터 차례 / {resistMessage}",
                this);

            AdvanceInitiative(
                0,
                chosen != null
                    ? chosen.InitiativeModifier
                    : 0);
        }

        private void Finish(
            EventBattleOutcome outcome)
        {
            // 132일차: 기획서 7.3절 "이벤트 전투 정력 0" 패배 기록 - 그 순간 상대하던
            // 대상을 기준으로 남긴다(대상이 여럿이면 그중 지금 선택된 대상).
            if (outcome == EventBattleOutcome.Lost)
            {
                string opponentDefinitionId =
                    session.Context?.SelectedTarget?.Participant?.DefinitionId;

                ApplicationFlow.Current?.RecordDefeat(
                    opponentDefinitionId);
            }

            session.TryFinish(
                outcome);

            Debug.Log(
                $"[Project Delta] 117일차 이벤트 전투 종료 / Outcome {outcome} / 최종 호감도 {session.Result?.FinalFavor}",
                this);

            SaveProfile();

            // 120일차: 결과가 어떻든 이벤트 전투가 정상 종료됐으니 대기 중이던 체크포인트를 지운다.
            EventBattleCheckpointStore.Clear();

            Action callback =
                outcome == EventBattleOutcome.Won
                    ? onWon
                    : onLostOrAborted;

            session.TryReset();

            onWon =
                null;

            onLostOrAborted =
                null;

            statusText =
                string.Empty;

            battleLog.Clear();

            profile =
                null;

            callback?.Invoke();
        }

        // 120일차: 이벤트 전투가 시작되는 순간 저장 직전 상태를 체크포인트로 남긴다 -
        // 82일차 BattleEncounterCheckpointStore와 같은 원칙으로, 정확한 턴 재현은 하지 않고
        // "있었다"는 사실과 저장 시점 수치만 남긴다(ApplicationFlow.ContinueGame 참고).
        private void CaptureCheckpoint()
        {
            EventBattleContext context =
                session.Context;

            if (context == null)
            {
                return;
            }

            string roomId =
                movementController != null
                && movementController.PlayerState != null
                    ? movementController.PlayerState.CurrentRoomId
                    : null;

            if (string.IsNullOrEmpty(
                    roomId))
            {
                return;
            }

            List<string> definitionIds =
                new List<string>();

            List<int> favors =
                new List<int>();

            List<int> stages =
                new List<int>();

            for (int index = 0; index < context.Targets.Count; index++)
            {
                EventBattleParticipantState state =
                    context.Targets[index];

                definitionIds.Add(
                    state.Participant.DefinitionId);

                favors.Add(
                    state.Favor);

                stages.Add(
                    state.CurrentStage);
            }

            EventBattleCheckpointStore.Capture(
                roomId,
                context.Source.ToString(),
                context.AttemptCount,
                context.Player.CurrentMana,
                context.Player.CurrentStamina,
                definitionIds,
                favors,
                stages);
        }

        private void AppendLog(
            string message)
        {
            if (string.IsNullOrEmpty(
                    message))
            {
                return;
            }

            statusText =
                message;

            battleLog.Add(
                message);

            if (battleLog.Count > MaxLogLines)
            {
                battleLog.RemoveAt(
                    0);
            }
        }

        // 119일차: AppRoot가 아직 "지금 로드된 프로필"을 들고 있는 공식 자리가 없어서
        // (Infrastructure/AppRoot.cs의 66번째 줄 TODO 참고) ApplicationFlow(Application
        // 어셈블리, SaveService를 들고 있음)를 거쳐 다시 읽고/쓴다 - Presentation은 계층상
        // Infrastructure를 직접 참조하지 않는다. ProfileContext 같은 보관 지점이 생기면
        // 이 두 메서드만 바꾸면 된다.
        private ProfileData ResolveProfile()
        {
            return ApplicationFlow.Current != null
                ? ApplicationFlow.Current.ReadOrCreateProfile()
                : new ProfileData();
        }

        private void SaveProfile()
        {
            if (profile == null
                || ApplicationFlow.Current == null)
            {
                return;
            }

            ApplicationFlow.Current.WriteProfile(
                profile);
        }

        private EventBattleActionProficiencyRecord ResolveProficiencyRecord(
            string actionId)
        {
            if (profile == null)
            {
                return new EventBattleActionProficiencyRecord();
            }

            if (!profile.PermanentGrowth.EventBattleActionProficiency.TryGetValue(
                    actionId,
                    out EventBattleActionProficiencyRecord record)
                || record == null)
            {
                record =
                    new EventBattleActionProficiencyRecord();

                profile.PermanentGrowth.EventBattleActionProficiency[actionId] =
                    record;
            }

            return record;
        }

        private MonsterDefinition ResolveDefinition(
            int targetIndex)
        {
            if (targetDefinitions == null
                || targetIndex < 0
                || targetIndex >= targetDefinitions.Length)
            {
                return null;
            }

            return targetDefinitions[targetIndex];
        }

        // 120일차: "전반적으로 UI 배치를 재구성" 요청 - 왼쪽(대상 명단·게이지·자원·상태)과
        // 오른쪽(행동/아이템 탭 + 전투 로그)으로 나눈 2단 레이아웃으로 다시 짰다.
        private const float PanelWidth = 620f;
        private const float LeftColumnWidth = 220f;
        private const float RightColumnWidth = 360f;
        private const float ColumnGap = 12f;
        private const float LogHeight = 96f;

        private void OnGUI()
        {
            if (!IsActive
                || session.Context == null)
            {
                return;
            }

            EventBattleContext context =
                session.Context;

            IReadOnlyList<IEventBattleCommand> catalog =
                EventBattleActionCatalog.All;

            int rowCount =
                Mathf.CeilToInt(
                    catalog.Count
                    / (float)ButtonsPerRow);

            float actionsHeight =
                rowCount
                * (ButtonHeight + 6f);

            float height =
                Mathf.Max(
                    260f,
                    36f
                    + actionsHeight
                    + LogHeight
                    + 90f);

            Rect panelRect =
                new Rect(
                    (Screen.width - PanelWidth) * 0.5f,
                    Screen.height - height - 40f,
                    PanelWidth,
                    height);

            GUI.Box(
                panelRect,
                string.Empty);

            GUILayout.BeginArea(
                new Rect(
                    panelRect.x + 16f,
                    panelRect.y + 12f,
                    panelRect.width - 32f,
                    panelRect.height - 24f));

            GUIStyle headerStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };

            headerStyle.normal.textColor =
                Color.white;

            GUILayout.Label(
                "이벤트 전투",
                headerStyle);

            GUILayout.Space(
                4f);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(
                GUILayout.Width(LeftColumnWidth));

            DrawTargetRoster(
                context);

            DrawResourceBars(
                context);

            DrawInitiativeBadge(
                context);

            GUILayout.EndVertical();

            GUILayout.Space(
                ColumnGap);

            GUILayout.BeginVertical(
                GUILayout.Width(RightColumnWidth));

            DrawServiceTabs();

            if (serviceScreen == ServiceScreen.Items)
            {
                DrawItemsScreen(
                    context);
            }
            else
            {
                DrawActionsScreen(
                    context,
                    catalog,
                    rowCount);
            }

            GUILayout.FlexibleSpace();

            DrawLog();

            GUILayout.Space(
                4f);

            if (GUILayout.Button(
                    "포기",
                    GUILayout.Height(ButtonHeight)))
            {
                ConfirmAbort();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // 119일차에 만든 가로 탭 대신, 대상마다 상자 하나씩 세로로 쌓아 이름·게이지·상태를
        // 한눈에 보여준다.
        private void DrawTargetRoster(
            EventBattleContext context)
        {
            for (int index = 0; index < context.Targets.Count; index++)
            {
                EventBattleParticipantState state =
                    context.Targets[index];

                bool isSelected =
                    index == context.SelectedTargetIndex;

                Rect boxRect =
                    GUILayoutUtility.GetRect(
                        LeftColumnWidth,
                        isSelected
                            ? 46f
                            : 34f);

                GUI.Box(
                    boxRect,
                    string.Empty);

                if (isSelected)
                {
                    DrawBoxOutline(
                        boxRect,
                        new Color(0.85f, 0.35f, 0.55f, 1f));
                }

                string suffix =
                    state.HasLeftSatisfied
                        ? " (이탈)"
                        : state.HasWon
                            ? " (성공)"
                            : state.StageCount > 1
                                ? $" ({state.CurrentStage}/{state.StageCount}단계)"
                                : string.Empty;

                GUI.enabled =
                    state.IsActive;

                if (GUI.Button(
                        boxRect,
                        string.Empty,
                        GUIStyle.none))
                {
                    SelectTarget(
                        index);
                }

                GUI.enabled =
                    true;

                GUIStyle nameStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold
                    };

                nameStyle.normal.textColor =
                    state.IsActive
                        ? Color.white
                        : new Color(0.6f, 0.6f, 0.6f, 1f);

                GUI.Label(
                    new Rect(
                        boxRect.x + 6f,
                        boxRect.y + 2f,
                        boxRect.width - 12f,
                        16f),
                    $"{ResolveTargetDisplayName(index)}{suffix}",
                    nameStyle);

                Rect miniBarBg =
                    new Rect(
                        boxRect.x + 6f,
                        boxRect.y + 20f,
                        boxRect.width - 12f,
                        10f);

                GUI.Box(
                    miniBarBg,
                    string.Empty);

                DrawFillBar(
                    miniBarBg,
                    state.Favor
                    / (float)EventBattleContext.FavorToWin,
                    new Color(0.85f, 0.35f, 0.55f, 1f));

                if (isSelected)
                {
                    GUI.Label(
                        new Rect(
                            boxRect.x + 6f,
                            boxRect.y + 30f,
                            boxRect.width - 12f,
                            14f),
                        $"호감도 {state.Favor} / {EventBattleContext.FavorToWin}");
                }
            }

            GUILayout.Space(
                6f);
        }

        private void DrawResourceBars(
            EventBattleContext context)
        {
            GUILayout.Label(
                "플레이어 자원",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            DrawLabeledBar(
                $"MP {context.Player.CurrentMana}/{context.Player.MaxMana}",
                context.Player.MaxMana > 0
                    ? context.Player.CurrentMana / (float)context.Player.MaxMana
                    : 0f,
                new Color(0.35f, 0.55f, 0.9f, 1f));

            DrawLabeledBar(
                $"정력 {context.Player.CurrentStamina}/{context.Player.MaxStamina}",
                context.Player.MaxStamina > 0
                    ? context.Player.CurrentStamina / (float)context.Player.MaxStamina
                    : 0f,
                new Color(0.9f, 0.75f, 0.25f, 1f));

            GUILayout.Space(
                6f);
        }

        private void DrawLabeledBar(
            string label,
            float ratio,
            Color color)
        {
            GUILayout.Label(
                label);

            Rect barRect =
                GUILayoutUtility.GetRect(
                    LeftColumnWidth,
                    14f);

            GUI.Box(
                barRect,
                string.Empty);

            DrawFillBar(
                barRect,
                ratio,
                color);

            GUILayout.Space(
                4f);
        }

        private void DrawFillBar(
            Rect background,
            float ratio,
            Color color)
        {
            float clampedRatio =
                Mathf.Clamp01(
                    ratio);

            Rect fillRect =
                new Rect(
                    background.x,
                    background.y,
                    background.width
                    * clampedRatio,
                    background.height);

            Color previousColor =
                GUI.color;

            GUI.color =
                color;

            GUI.DrawTexture(
                fillRect,
                Texture2D.whiteTexture);

            GUI.color =
                previousColor;
        }

        private void DrawBoxOutline(
            Rect rect,
            Color color)
        {
            Color previousColor =
                GUI.color;

            GUI.color =
                color;

            GUI.DrawTexture(
                new Rect(rect.x, rect.y, rect.width, 2f),
                Texture2D.whiteTexture);

            GUI.DrawTexture(
                new Rect(rect.x, rect.yMax - 2f, rect.width, 2f),
                Texture2D.whiteTexture);

            GUI.DrawTexture(
                new Rect(rect.x, rect.y, 2f, rect.height),
                Texture2D.whiteTexture);

            GUI.DrawTexture(
                new Rect(rect.xMax - 2f, rect.y, 2f, rect.height),
                Texture2D.whiteTexture);

            GUI.color =
                previousColor;
        }

        // 118일차: 플레이어 칸(왼쪽 자원 아래)에 지금 상태를 글자로 보여준다.
        private void DrawInitiativeBadge(
            EventBattleContext context)
        {
            bool isPlayerTurn =
                context.InitiativeHolder
                == EventBattleInitiativeHolder.Player;

            GUIStyle statusStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };

            statusStyle.normal.textColor =
                isPlayerTurn
                    ? new Color(0.7f, 0.9f, 1f, 1f)
                    : new Color(1f, 0.7f, 0.7f, 1f);

            GUILayout.Label(
                isPlayerTurn
                    ? "지금 차례: 플레이어"
                    : "지금 차례: 상대",
                statusStyle);
        }

        private void DrawServiceTabs()
        {
            GUILayout.BeginHorizontal();

            GUI.enabled =
                serviceScreen != ServiceScreen.Actions;

            if (GUILayout.Button(
                    "행동",
                    GUILayout.Height(24f)))
            {
                serviceScreen =
                    ServiceScreen.Actions;
            }

            GUI.enabled =
                serviceScreen != ServiceScreen.Items;

            if (GUILayout.Button(
                    "아이템",
                    GUILayout.Height(24f)))
            {
                serviceScreen =
                    ServiceScreen.Items;
            }

            GUI.enabled =
                true;

            GUILayout.EndHorizontal();

            GUILayout.Space(
                4f);
        }

        // 120일차: 행동 상성 예고 - 선택된 대상이 강점/약점으로 알려진 행동이면 이름 옆에 표시한다
        // (133~135일차 전까지는 실제 몬스터 상성 데이터가 없어 대부분 아무 표시도 없다).
        private void DrawActionsScreen(
            EventBattleContext context,
            IReadOnlyList<IEventBattleCommand> catalog,
            int rowCount)
        {
            bool isPlayerTurn =
                context.InitiativeHolder
                == EventBattleInitiativeHolder.Player
                && context.SelectedTarget != null;

            MonsterDefinition selectedDefinition =
                ResolveDefinition(
                    context.SelectedTargetIndex);

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                GUILayout.BeginHorizontal();

                for (int column = 0; column < ButtonsPerRow; column++)
                {
                    int actionIndex =
                        rowIndex
                        * ButtonsPerRow
                        + column;

                    if (actionIndex >= catalog.Count)
                    {
                        break;
                    }

                    IEventBattleCommand action =
                        catalog[actionIndex];

                    bool affordable =
                        context.Player.CurrentMana
                        >= action.ManaCost
                        && context.Player.CurrentStamina
                        >= action.StaminaCost;

                    GUI.enabled =
                        isPlayerTurn
                        && affordable;

                    string costLabel =
                        action.ManaCost > 0
                            ? $"MP{action.ManaCost}"
                            : $"정력{action.StaminaCost}";

                    string affinityTag =
                        DescribeAffinityTag(
                            selectedDefinition,
                            action.Id);

                    if (GUILayout.Button(
                            $"{action.DisplayName}{affinityTag} ({costLabel})",
                            GUILayout.Width(ButtonWidth),
                            GUILayout.Height(ButtonHeight)))
                    {
                        ConfirmAction(
                            action);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUI.enabled =
                true;
        }

        private static string DescribeAffinityTag(
            MonsterDefinition definition,
            string actionId)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            float multiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    definition.EventBattleStrongActionIds,
                    definition.EventBattleWeakActionIds,
                    actionId);

            if (multiplier > EventBattleAffinityRule.NormalMultiplier)
            {
                return " [약점]";
            }

            if (multiplier < EventBattleAffinityRule.NormalMultiplier)
            {
                return " [강점]";
            }

            return string.Empty;
        }

        // 120일차: 93일차 인벤토리 아이템을 그대로 재사용한다 - 정력/마나를 채우는 소비 아이템.
        private void DrawItemsScreen(
            EventBattleContext context)
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            bool isPlayerTurn =
                context.InitiativeHolder
                == EventBattleInitiativeHolder.Player;

            bool hasUsableSlot =
                false;

            if (inventory != null)
            {
                for (int slotIndex = 0; slotIndex < inventory.Slots.Count; slotIndex++)
                {
                    InventorySlotState slot =
                        inventory.Slots[slotIndex];

                    if (slot == null
                        || slot.IsEmpty)
                    {
                        continue;
                    }

                    if (!RuntimeItemDefinitionLookup.TryFind(
                            slot.ItemId,
                            out ItemDefinition definition)
                        || !ItemCategoryRules.CanUse(
                            definition.Category)
                        || (definition.UseContext != ItemUseContext.Battle
                            && definition.UseContext != ItemUseContext.Both))
                    {
                        continue;
                    }

                    hasUsableSlot =
                        true;

                    GUI.enabled =
                        isPlayerTurn;

                    if (GUILayout.Button(
                            $"{slot.DisplayName} ×{slot.Quantity}  [사용]",
                            GUILayout.Height(28f)))
                    {
                        ConfirmUseItem(
                            slotIndex,
                            definition);
                    }

                    GUI.enabled =
                        true;
                }
            }

            if (!hasUsableSlot)
            {
                GUILayout.Label(
                    "쓸 수 있는 아이템이 없습니다.");
            }
        }

        private void DrawLog()
        {
            GUILayout.Label(
                "전투 로그",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            logScrollPosition =
                GUILayout.BeginScrollView(
                    logScrollPosition,
                    GUI.skin.box,
                    GUILayout.Height(LogHeight));

            for (int index = 0; index < battleLog.Count; index++)
            {
                GUILayout.Label(
                    battleLog[index]);
            }

            GUILayout.EndScrollView();
        }

        private string ResolveTargetDisplayName(
            int targetIndex)
        {
            EventBattleContext context =
                session.Context;

            if (context == null
                || targetIndex < 0
                || targetIndex >= context.Targets.Count)
            {
                return "대상";
            }

            MonsterDefinition definition =
                ResolveDefinition(
                    targetIndex);

            if (definition != null
                && !string.IsNullOrEmpty(
                    definition.DisplayName))
            {
                return definition.DisplayName;
            }

            return context.Targets[targetIndex].Participant.InstanceId;
        }
    }
}
