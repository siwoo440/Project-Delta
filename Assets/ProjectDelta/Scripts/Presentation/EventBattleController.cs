using System;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 117일차: 일반 전투(ExplorationMonsterEncounterController)와 상호 배타적인 별도 이벤트
    // 전투 화면. EventBattleEntryService.TryEnter()로 시작해, 승리(호감도 100)·패배(자원 고갈)·
    // 중단(포기) 중 하나로 끝나면 호출자가 넘겨준 콜백으로 결과를 돌려준다 - 이 컨트롤러
    // 자신은 "이겼을 때 무엇을 할지"를 모른다(115~116일차 확립된 관심사 분리 원칙).
    public sealed class EventBattleController : MonoBehaviour
    {
        [SerializeField] private DungeonFloorController floorController;

        private readonly EventBattleSession session =
            new EventBattleSession();

        private readonly IEventBattleCommand courtCommand =
            new CourtEventBattleCommand();

        private readonly IEventBattleCommand sootheCommand =
            new SootheEventBattleCommand();

        private readonly IRandomSource rng =
            new CombatRng();

        private Action onWon;
        private Action onLostOrAborted;
        private string lastMessage;

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

            onWon =
                beginOnWon;

            onLostOrAborted =
                beginOnLostOrAborted;

            lastMessage =
                string.Empty;

            Debug.Log(
                $"[Project Delta] 117일차 이벤트 전투 시작 / Source {source} / Target {target.InstanceId}",
                this);

            return true;
        }

        public void ConfirmCourt()
        {
            ExecuteAndResolve(
                courtCommand);
        }

        public void ConfirmSoothe()
        {
            ExecuteAndResolve(
                sootheCommand);
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

        private void ExecuteAndResolve(
            IEventBattleCommand command)
        {
            if (!session.IsActive
                || session.Context == null)
            {
                return;
            }

            EventBattleCommandResult result =
                command.Execute(
                    session.Context,
                    rng);

            lastMessage =
                result.Message;

            if (!result.Accepted)
            {
                return;
            }

            if (session.Context.HasWon)
            {
                Finish(
                    EventBattleOutcome.Won);

                return;
            }

            if (!session.Context.PlayerCanAct(
                    courtCommand.ManaCost,
                    sootheCommand.StaminaCost))
            {
                Finish(
                    EventBattleOutcome.Lost);
            }
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

            const float width = 420f;
            const float height = 170f;

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

            if (!string.IsNullOrEmpty(
                    lastMessage))
            {
                GUILayout.Label(
                    lastMessage);
            }

            GUILayout.Space(
                6f);

            GUILayout.BeginHorizontal();

            GUI.enabled =
                context.Player.CurrentMana
                >= courtCommand.ManaCost;

            if (GUILayout.Button(
                    $"구애 (MP {courtCommand.ManaCost})",
                    GUILayout.Height(30f)))
            {
                ConfirmCourt();
            }

            GUI.enabled =
                context.Player.CurrentStamina
                >= sootheCommand.StaminaCost;

            if (GUILayout.Button(
                    $"달래기 (정력 {sootheCommand.StaminaCost})",
                    GUILayout.Height(30f)))
            {
                ConfirmSoothe();
            }

            GUI.enabled =
                true;

            if (GUILayout.Button(
                    "포기",
                    GUILayout.Height(30f)))
            {
                ConfirmAbort();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private string ResolveTargetDisplayName(
            EventBattleContext context)
        {
            if (floorController != null
                && floorController.TryFindMonsterDefinition(
                    context.Target.DefinitionId,
                    out MonsterDefinition definition)
                && definition != null
                && !string.IsNullOrEmpty(
                    definition.DisplayName))
            {
                return definition.DisplayName;
            }

            return context.Target.InstanceId;
        }
    }
}
