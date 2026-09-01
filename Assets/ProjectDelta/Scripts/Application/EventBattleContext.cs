using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 117일차: 일반 BattleContext(47일차)와 상호 배타적인 전용 Context.
    // 118일차: 주도권(누가 다음에 행동하는가)과 종족 상성 배율을 추가했다.
    // 119일차: 대상을 하나(Target)에서 최대 3명(Targets)으로 늘렸다 - 각자 개별
    // EventBattleParticipantState(호감도·만족 이탈·보스 단계)를 갖는다. 플레이어는 지금
    // 어떤 대상에게 행동할지 SelectedTargetIndex로 고른다(일반 전투의 SelectedTarget과 같은 개념).
    public sealed class EventBattleContext
    {
        public const int FavorToWin = 100;

        public const int MaxTargets = 3;

        public EventBattleEntrySource Source { get; }

        public BattleParticipant Player { get; }

        public IReadOnlyList<EventBattleParticipantState> Targets { get; }

        public int SelectedTargetIndex { get; private set; }

        public int AttemptCount { get; private set; }

        // 118일차: 지금 행동을 취할 차례가 누구인지. Begin() 직후에는 항상 Player다.
        public EventBattleInitiativeHolder InitiativeHolder { get; private set; } =
            EventBattleInitiativeHolder.Player;

        // 118일차: 컨트롤러가 IEventBattleCommand.Execute() 호출 직전에 종족 상성
        // (EventBattleAffinityRule)으로 계산해 넣어두는 배율.
        public float PlayerActionFavorMultiplier { get; set; } =
            1f;

        // 119일차: 몬스터 전용 행동 AI가 "방금 플레이어가 뭘 했는가"에 반응할 수 있도록 기록한다.
        public string LastPlayerActionId { get; set; }

        public EventBattleContext(
            EventBattleEntrySource source,
            BattleParticipant player,
            IReadOnlyList<EventBattleParticipantState> targets)
        {
            Source =
                source;

            Player =
                player;

            Targets =
                targets;

            SelectedTargetIndex =
                FindFirstActiveIndex();
        }

        // 119일차: 지금 행동의 대상 - 없으면(전원 이탈/승리) null.
        public EventBattleParticipantState SelectedTarget =>
            SelectedTargetIndex >= 0
            && Targets != null
            && SelectedTargetIndex < Targets.Count
                ? Targets[SelectedTargetIndex]
                : null;

        public bool TrySelectTarget(
            int index)
        {
            if (Targets == null
                || index < 0
                || index >= Targets.Count
                || !Targets[index].IsActive)
            {
                return false;
            }

            SelectedTargetIndex =
                index;

            return true;
        }

        public void RegisterAttempt()
        {
            AttemptCount++;
        }

        // 119일차: 최소 한 명이라도 공략(HasWon)했으면 전체 결과를 승리로 본다.
        public bool HasWon
        {
            get
            {
                if (Targets == null)
                {
                    return false;
                }

                for (int index = 0; index < Targets.Count; index++)
                {
                    if (Targets[index].HasWon)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // 119일차: 활성 대상(행동 가능한 대상)이 하나도 안 남았는가 - 전원 승리/만족 이탈했다는 뜻.
        public bool AllTargetsResolved
        {
            get
            {
                if (Targets == null)
                {
                    return true;
                }

                for (int index = 0; index < Targets.Count; index++)
                {
                    if (Targets[index].IsActive)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void SetInitiativeHolder(
            EventBattleInitiativeHolder holder)
        {
            InitiativeHolder =
                holder;
        }

        // 117일차엔 구애/달래기 2개뿐이라 비용을 직접 받았지만, 118일차에 12종으로 늘어나면서
        // 카탈로그 전체를 훑어 "그중 하나라도 쓸 수 있는가"로 바뀌었다.
        public bool PlayerCanAct(
            IReadOnlyList<IEventBattleCommand> actions)
        {
            if (Player == null
                || actions == null)
            {
                return false;
            }

            for (int index = 0; index < actions.Count; index++)
            {
                IEventBattleCommand action =
                    actions[index];

                if (action == null)
                {
                    continue;
                }

                if (Player.CurrentMana >= action.ManaCost
                    && Player.CurrentStamina >= action.StaminaCost)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindFirstActiveIndex()
        {
            if (Targets == null)
            {
                return -1;
            }

            for (int index = 0; index < Targets.Count; index++)
            {
                if (Targets[index] != null
                    && Targets[index].IsActive)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
