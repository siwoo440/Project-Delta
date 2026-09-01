using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
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
        private const float ButtonWidth = 128f;
        private const float ButtonHeight = 34f;
        private const int ButtonsPerRow = 4;

        // 119일차: 호감도가 이 값 이상인 대상은 자기 차례가 될 때마다 이 확률(%)로 만족하고 떠난다.
        private const int SatisfiedDepartureChancePercent = 15;

        [SerializeField] private DungeonFloorController floorController;

        private readonly EventBattleSession session =
            new EventBattleSession();

        private readonly IRandomSource rng =
            new CombatRng();

        private MonsterDefinition[] targetDefinitions;
        private string lastMonsterActionId;

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

            statusText =
                "당신의 차례입니다.";

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

            statusText =
                result.Message;

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

            statusText =
                "당신의 차례입니다.";

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

                statusText =
                    $"{name}이(가) 만족하여 떠났습니다.";

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

            statusText =
                $"{targetName}이(가) {(chosen != null ? chosen.DisplayName : "저항")}에 저항했다! 호감도 -{resistAmount}";

            Debug.Log(
                $"[Project Delta] 119일차 이벤트 전투 몬스터 차례 / {statusText}",
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
            session.TryFinish(
                outcome);

            Debug.Log(
                $"[Project Delta] 117일차 이벤트 전투 종료 / Outcome {outcome} / 최종 호감도 {session.Result?.FinalFavor}",
                this);

            SaveProfile();

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

            profile =
                null;

            callback?.Invoke();
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

            float width =
                ButtonsPerRow
                * (ButtonWidth + 8f)
                + 24f;

            float height =
                190f
                + rowCount
                * (ButtonHeight + 6f)
                + 44f;

            Rect panelRect =
                new Rect(
                    (Screen.width - width) * 0.5f,
                    Screen.height - height - 40f,
                    width,
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

            // 119일차: 대상이 여러 명이면 탭처럼 늘어놓고, 선택된 대상의 게이지를 아래에 보여준다.
            if (context.Targets.Count > 1)
            {
                GUILayout.BeginHorizontal();

                for (int index = 0; index < context.Targets.Count; index++)
                {
                    EventBattleParticipantState state =
                        context.Targets[index];

                    string label =
                        ResolveTargetDisplayName(
                            index);

                    if (state.HasLeftSatisfied)
                    {
                        label +=
                            " (이탈)";
                    }
                    else if (state.HasWon)
                    {
                        label +=
                            " (성공)";
                    }
                    else if (state.StageCount > 1)
                    {
                        label +=
                            $" ({state.CurrentStage}/{state.StageCount}단계)";
                    }

                    GUI.enabled =
                        state.IsActive;

                    if (GUILayout.Toggle(
                            index == context.SelectedTargetIndex,
                            label,
                            GUI.skin.button))
                    {
                        SelectTarget(
                            index);
                    }
                }

                GUI.enabled =
                    true;

                GUILayout.EndHorizontal();

                GUILayout.Space(
                    4f);
            }

            EventBattleParticipantState selected =
                context.SelectedTarget;

            Rect barBackground =
                GUILayoutUtility.GetRect(
                    width - 32f,
                    18f);

            GUI.Box(
                barBackground,
                string.Empty);

            if (selected != null)
            {
                Rect barFill =
                    new Rect(
                        barBackground.x,
                        barBackground.y,
                        barBackground.width
                        * (selected.Favor
                            / (float)EventBattleContext.FavorToWin),
                        barBackground.height);

                Color previousColor =
                    GUI.color;

                GUI.color =
                    new Color(0.85f, 0.35f, 0.55f, 1f);

                GUI.DrawTexture(
                    barFill,
                    Texture2D.whiteTexture);

                GUI.color =
                    previousColor;
            }

            GUILayout.Space(
                4f);

            GUILayout.Label(
                selected != null
                    ? $"{ResolveTargetDisplayName(context.SelectedTargetIndex)} 호감도 {selected.Favor} / {EventBattleContext.FavorToWin}   |   플레이어 MP {context.Player.CurrentMana}/{context.Player.MaxMana}   정력 {context.Player.CurrentStamina}/{context.Player.MaxStamina}"
                    : $"플레이어 MP {context.Player.CurrentMana}/{context.Player.MaxMana}   정력 {context.Player.CurrentStamina}/{context.Player.MaxStamina}");

            // 118일차: 플레이어 칸(정보 줄) 바로 아래에 지금 상태를 글자로 보여준다.
            GUIStyle statusStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic
                };

            statusStyle.normal.textColor =
                context.InitiativeHolder
                == EventBattleInitiativeHolder.Player
                    ? new Color(0.7f, 0.9f, 1f, 1f)
                    : new Color(1f, 0.7f, 0.7f, 1f);

            GUILayout.Label(
                $"상태: {statusText}",
                statusStyle);

            GUILayout.Space(
                6f);

            bool isPlayerTurn =
                context.InitiativeHolder
                == EventBattleInitiativeHolder.Player
                && selected != null;

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

                    if (GUILayout.Button(
                            $"{action.DisplayName} ({costLabel})",
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

            GUILayout.Space(
                6f);

            if (GUILayout.Button(
                    "포기",
                    GUILayout.Height(ButtonHeight)))
            {
                ConfirmAbort();
            }

            GUILayout.EndArea();
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
