namespace ProjectDelta.Application
{
    // 117일차: 일반 BattleState(47일차)보다 단순하다 - 별도 이벤트 전투는 라운드·행동자 순서
    // 개념 없이 플레이어가 매번 행동을 고르는 1:1 진행이라 Idle/Active/Finished 셋이면 충분하다.
    public enum EventBattleState
    {
        Idle,
        Active,
        Finished
    }
}
