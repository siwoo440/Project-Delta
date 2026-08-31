using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 117일차: 일반 전투(ExplorationMonsterEncounterController)와 상호 배타적인 별도 이벤트
    // 전투 화면. EventBattleEntryService.TryEnter()로 시작해, 승리(호감도 100)·패배(자원 고갈)·
    // 중단(포기) 중 하나로 끝나면 호출자가 넘겨준 콜백으로 결과를 돌려준다 - 이 컨트롤러
    // 자신은 "이겼을 때 무엇을 할지"를 모른다(115~116일차 확립된 관심사 분리 원칙).
    // 118일차: 공통 행동 12종(EventBattleActionCatalog)·주도권(누구 차례인가)·종족 상성
    // 배율을 추가했다 - 이제 플레이어만 계속 누르는 게 아니라 몬스터도 차례를 가져가
    // 저항한다.
    public sealed class EventBattleController : MonoBehaviour
    {
        private const float ButtonWidth = 128f;
        private const float ButtonHeight = 34f;
        private const int ButtonsPerRow = 4;

        [SerializeField] private DungeonFloorController floorController;

        private readonly EventBattleSession session =
            new EventBattleSession();

        private readonly IRandomSource rng =
            new CombatRng();

        private MonsterDefinition targetDefinition;

        private Action onWon;
        private Action onLostOrAborted;

        // 118일차: 사용자 요청 - 플레이어 정보 아래에 지금 무슨 상태인지 글자로 항상 보여준다.
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

        // 117일차: 4갈래 진입 경로(유혇·스킬/몬스터 행동·일반 이벤트·보스) 중 하나가 이 메서드를
        // 부른다. onWon/onLostOrAborted는 호출자가 원래 진행 중이던 흐름(예: 일반 전투)을
        // 어떻게 이어갈지 결정한다.
        public bool Begin(
            BattleParticipant player,
            BattleParticipant target,
            EventBattleEntrySource source,
            Action beginOnWon,
            Action beginOnLostOrAborted)
        {
            if (session.IsActive
                || !EventBattleEntryService.TryEnter(
                    source,
                    player,
                    target,
                    out EventBattleContext context))
            {
                return false;
            }

            if (!session.TryBegin(
                    context))
            {
                return false;
            }

            targetDefinition =
                null;

            if (floorController != null)
            {
                floorController.TryFindMonsterDefinition(
                    target.DefinitionId,
                    out targetDefinition);
            }

            onWon =
                beginOnWon;

            onLostOrAborted =
                beginOnLostOrAborted;

            statusText =
                "당신의 차례입니다.";

            Debug.Log(
                $"[Project Delta] 117일차 이벤트 전투 시작 / Source {source} / Target {target.InstanceId}",
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
                || session.Context.InitiativeHolder != EventBattleInitiativeHolder.Player)
            {
                return;
            }

            EventBattleContext context =
                session.Context;

            context.PlayerActionFavorMultiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    targetDefinition != null
                        ? targetDefinition.EventBattleStrongActionIds
                        : null,
                    targetDefinition != null
                        ? targetDefinition.EventBattleWeakActionIds
                        : null,
                    command.Id);

            EventBattleCommandResult result =
                command.Execute(
                    context,
                    rng);

            context.PlayerActionFavorMultiplier =
                1f;

            statusText =
                result.Message;

            if (!result.Accepted)
            {
                return;
            }

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

            EventBattleInitiativeHolder next =
                EventBattleInitiativeRule.RollNext(
                    context.Player.Charm,
                    playerActionInitiativeModifier,
                    context.Target.Charm,
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

            if (!context.PlayerCanAct(
                    EventBattleActionCatalog.All))
            {
                Finish(
                    EventBattleOutcome.Lost);

                return;
            }

            statusText =
                "당신의 차례입니다.";
        }

        // 118일차: 몬스터도 같은 공통 행동 12종을 공유한다 - 무작위로 하나를 골라 "저항"
        // 명목으로 호감도를 깎는다. 몬스터는 마나·정력을 쓰지 않으므로 항상 행동할 수 있다.
        private void ResolveTargetTurn()
        {
            if (!session.IsActive
                || session.Context == null)
            {
                return;
            }

            EventBattleContext context =
                session.Context;

            IReadOnlyList<IEventBattleCommand> catalog =
                EventBattleActionCatalog.All;

            IEventBattleCommand flavor =
                catalog[
                    rng.NextInt(
                        0,
                        catalog.Count)];

            int resistBase =
                5
                + Mathf.Max(
                    0,
                    context.Target.Resistance
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

            context.AddFavor(
                -resistAmount);

            string targetName =
                ResolveTargetDisplayName(
                    context);

            statusText =
                $"{targetName}이(가) {flavor.DisplayName}에 저항했다! 호감도 -{resistAmount}";

            Debug.Log(
                $"[Project Delta] 118일차 이벤트 전투 몬스터 차례 / {statusText}",
                this);

            if (!context.PlayerCanAct(
                    catalog))
            {
                Finish(
                    EventBattleOutcome.Lost);

                return;
            }

            AdvanceInitiative(
                0,
                flavor.InitiativeModifier);
        }

        private void Finish(
            EventBattleOutcome outcome)
        {
            session.TryFinish(
                outcome);

            Debug.Log(
                $"[Project Delta] 117일차 이벤트 전투 종료 / Outcome {outcome} / Favor {session.Result?.FinalFavor}",
                this);

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

            callback?.Invoke();
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
                150f
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
                $"{ResolveTargetDisplayName(context)} - 이벤트 전투",
                headerStyle);

            GUILayout.Space(
                6f);

            Rect barBackground =
                GUILayoutUtility.GetRect(
                    width - 32f,
                    18f);

            GUI.Box(
                barBackground,
                string.Empty);

            Rect barFill =
                new Rect(
                    barBackground.x,
                    barBackground.y,
                    barBackground.width
                    * (context.Favor
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

            GUILayout.Space(
                4f);

            GUILayout.Label(
                $"호감도 {context.Favor} / {EventBattleContext.FavorToWin}   |   플레이어 MP {context.Player.CurrentMana}/{context.Player.MaxMana}   정력 {context.Player.CurrentStamina}/{context.Player.MaxStamina}");

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
                == EventBattleInitiativeHolder.Player;

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
            EventBattleContext context)
        {
            if (targetDefinition != null
                && !string.IsNullOrEmpty(
                    targetDefinition.DisplayName))
            {
                return targetDefinition.DisplayName;
            }

            return context.Target.InstanceId;
        }
    }
}
