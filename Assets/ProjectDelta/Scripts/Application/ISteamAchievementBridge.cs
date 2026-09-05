namespace ProjectDelta.Application
{
    // 134일차: 로컬 도전과제(AchievementCatalog.Id)가 처음 True가 되는 순간 Steam API를
    // 호출하는 지점을 하나로 고정한다. 다른 서비스 인터페이스(ILogService 등)와 같은
    // 이유로 Application에 둔다 - Infrastructure가 Application을 참조하는 방향이라
    // 실제 구현체(NullSteamAchievementBridge, 나중의 Steamworks 구현)는 Infrastructure에
    // 두고 여기서는 계약만 정의한다.
    public interface ISteamAchievementBridge
    {
        // id는 AchievementDefinition.Id를 그대로 쓴다 - Steamworks 대시보드의
        // API Name도 이 값과 동일하게 등록해야 한다.
        void UnlockAchievement(string id);
    }
}
