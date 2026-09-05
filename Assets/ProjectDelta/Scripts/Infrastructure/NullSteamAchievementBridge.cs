using ProjectDelta.Application;

namespace ProjectDelta.Infrastructure
{
    // 134일차: Steamworks.NET이 아직 프로젝트에 없는 동안 쓰는 기본 구현 - 실제 Steam API
    // 호출 대신 로그만 남겨서, 나중에 진짜 구현으로 교체하기 전까지도 호출 흐름 자체는
    // 지금 바로 검증할 수 있게 한다.
    public sealed class NullSteamAchievementBridge : ISteamAchievementBridge
    {
        private readonly ILogService _log;

        public NullSteamAchievementBridge(
            ILogService log)
        {
            _log =
                log;
        }

        public void UnlockAchievement(
            string id)
        {
            _log?.Info(
                $"[Steam] UnlockAchievement skipped (Steamworks not integrated yet): {id}");
        }
    }
}
