using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 117일차: 일반 BattleContext(47일차)와 상호 배타적인 전용 Context. 기존 BattleParticipant를
    // 그대로 재사용한다 - 정력·마나 자원과 매력·저항 능력치가 이미 있어 새 참가자 타입을 또
    // 만들 필요가 없었다. 대신 이 전투에서만 의미 있는 값(호감도 진행도·진입 경로)을 따로 담는다.
    // 118일차: 주도권(누가 다음에 행동하는가)과, 지금 실행 중인 행동에 곱해야 할 종족 상성
    // 배율을 추가로 담는다.
    public sealed class EventBattleContext
    {
        public const int FavorToWin = 100;

        public EventBattleEntrySource Source { get; }

        public BattleParticipant Player { get; }

        public BattleParticipant Target { get; }

        public int Favor { get; private set; }

        public int AttemptCount { get; private set; }

        // 118일차: 지금 행동을 취할 차례가 누구인지. Begin() 직후에는 항상 Player다.
        public EventBattleInitiativeHolder InitiativeHolder { get; private set; } =
            EventBattleInitiativeHolder.Player;

        // 118일차: 컨트롤러가 IEventBattleCommand.Execute() 호출 직전에 종족 상성
        // (EventBattleAffinityRule)으로 계산해 넣어두는 배율. 각 행동은 이 값을 곱해 최종
        // 호감도 증가량을 정한다 - 몬스터의 자체 저항 행동에는 적용하지 않으므로 매번 1로
        // 되돌려 둔다.
        public float PlayerActionFavorMultiplier { get; set; } =
            1f;

        public EventBattleContext(
            EventBattleEntrySource source,
            BattleParticipant player,
            BattleParticipant target)
        {
            Source =
                source;

            Player =
                player;

            Target =
                target;
        }

        public void AddFavor(
            int amount)
        {
            AttemptCount++;

            Favor =
                Favor + amount < 0
                    ? 0
                    : Favor + amount > FavorToWin
                        ? FavorToWin
                        : Favor + amount;
        }

        public bool HasWon =>
            Favor >= FavorToWin;

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
    }
}
