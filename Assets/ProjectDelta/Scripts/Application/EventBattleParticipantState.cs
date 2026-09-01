namespace ProjectDelta.Application
{
    // 119일차: "다수 참가자(최대 3명), 개별 게이지" 요구사항 - 예전엔 EventBattleContext가
    // 대상 하나(Target)와 호감도 하나를 직접 들고 있었지만, 이제 대상마다 이 상태 객체를
    // 하나씩 갖는다. 상위 개체(보스) 2단계 게이지도 같은 구조로 표현한다 - StageCount가
    // 2 이상이면 한 단계를 다 채운 뒤 게이지가 비워지고 다음 단계로 넘어간다.
    public sealed class EventBattleParticipantState
    {
        public BattleParticipant Participant { get; }

        // 119일차: 만족 이탈 판정 기준 - 이 값 이상이면 그 대상의 턴마다 만족하고 떠날 수 있다.
        public const int SatisfiedFavorThreshold = 60;

        public int StageCount { get; }

        public int CurrentStage { get; private set; } =
            1;

        public int Favor { get; private set; }

        public bool HasLeftSatisfied { get; private set; }

        public bool HasWon { get; private set; }

        public EventBattleParticipantState(
            BattleParticipant participant,
            int stageCount = 1)
        {
            Participant =
                participant;

            StageCount =
                stageCount < 1
                    ? 1
                    : stageCount;
        }

        // 119일차: 만족해서 떠났거나 이미 승리(공략 완료)한 대상은 더 이상 행동의 대상이 될 수 없다.
        public bool IsActive =>
            !HasLeftSatisfied
            && !HasWon;

        public void AddFavor(
            int amount)
        {
            if (!IsActive)
            {
                return;
            }

            int newFavor =
                Favor
                + amount;

            if (newFavor < 0)
            {
                newFavor =
                    0;
            }

            if (newFavor >= EventBattleContext.FavorToWin)
            {
                if (CurrentStage < StageCount)
                {
                    // 118일차 EventBattleContext.FavorToWin 100 도달 - 다음 단계로 넘어가고
                    // 게이지를 다시 채운다(보스 2단계 게이지).
                    CurrentStage++;

                    Favor =
                        0;

                    return;
                }

                Favor =
                    EventBattleContext.FavorToWin;

                HasWon =
                    true;

                return;
            }

            Favor =
                newFavor;
        }

        public void MarkSatisfiedDeparture()
        {
            HasLeftSatisfied =
                true;
        }
    }
}
