namespace ProjectDelta.Application
{
    // 47일차: Battle 종료 시점의 최종 결과 데이터.
    public sealed class BattleResult
    {
        public BattleOutcome Outcome { get; }
        public int TurnCount { get; }

        public BattleResult(
            BattleOutcome outcome,
            int turnCount)
        {
            Outcome =
                outcome;

            TurnCount =
                turnCount;
        }
    }
}
