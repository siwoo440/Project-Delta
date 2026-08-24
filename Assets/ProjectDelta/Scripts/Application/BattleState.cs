namespace ProjectDelta.Application
{
    // 47일차: BattleContext 생명주기 상태.
    public enum BattleState
    {
        Idle,
        Starting,
        TurnStart,
        AwaitingAction,
        ResolvingAction,
        TurnEnd,
        Finished
    }
}
