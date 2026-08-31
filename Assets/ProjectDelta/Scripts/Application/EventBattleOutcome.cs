namespace ProjectDelta.Application
{
    // 117일차: 일반 BattleOutcome(Victory/Defeat/Escaped)과 분리된, 별도 이벤트 전투 전용 결과.
    public enum EventBattleOutcome
    {
        Won,
        Lost,
        Aborted
    }
}
