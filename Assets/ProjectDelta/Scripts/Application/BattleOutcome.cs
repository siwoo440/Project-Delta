namespace ProjectDelta.Application
{
    // 47일차: 실제 승패 계산(51일차 이후) 전까지 테스트로 확정하는 전투 결과 종류.
    // 69일차: 도주 성공도 승패와 마찬가지로 BattleSession의 생명주기를 정식으로 끝내야
    // 하므로(64일차 상태 정리 포함) 별도 결과로 추가한다.
    public enum BattleOutcome
    {
        Victory,
        Defeat,
        Escaped
    }
}
