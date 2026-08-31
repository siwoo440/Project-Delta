namespace ProjectDelta.Application
{
    // 117일차: 별도 이벤트 전투 종료 시점의 최종 결과 - BattleResult(47일차)와 같은 역할이다.
    public sealed class EventBattleResult
    {
        public EventBattleOutcome Outcome { get; }

        public int FinalFavor { get; }

        public int AttemptCount { get; }

        public EventBattleResult(
            EventBattleOutcome outcome,
            int finalFavor,
            int attemptCount)
        {
            Outcome =
                outcome;

            FinalFavor =
                finalFavor;

            AttemptCount =
                attemptCount;
        }
    }
}
