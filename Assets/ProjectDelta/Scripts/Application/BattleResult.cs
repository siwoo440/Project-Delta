namespace ProjectDelta.Application
{
    // 47일차: Battle 종료 시점의 최종 결과 데이터.
    // 59일차: 기획서 4.2 용어에 맞춰 TurnCount → RoundCount로 정정했다.
    public sealed class BattleResult
    {
        public BattleOutcome Outcome { get; }
        public int RoundCount { get; }

        public BattleResult(
            BattleOutcome outcome,
            int roundCount)
        {
            Outcome =
                outcome;

            RoundCount =
                roundCount;
        }
    }
}
