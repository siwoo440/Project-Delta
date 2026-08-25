namespace ProjectDelta.Application
{
    // 47일차: BattleContext 생명주기 상태.
    // 59일차: 기획서 4.2가 쓰는 "라운드" 용어에 맞춰 Turn → Round로 정정했다.
    public enum BattleState
    {
        Idle,
        Starting,
        RoundStart,
        AwaitingAction,
        ResolvingAction,
        RoundEnd,
        Finished
    }
}
