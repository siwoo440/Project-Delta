using System.Collections.Generic; // 신규 달성 ID 목록 사용

namespace ProjectDelta.Application
{
    // 134일차: 로비·향후 전용 화면·Steam 동기화가 공통으로 쓸 도전과제 진행도 요약이다.
    public sealed class AchievementProgressSnapshot
    {
        public int TotalCount { get; } // 전체 도전과제 수
        public int UnlockedCount { get; } // 영구 달성한 도전과제 수
        public int NewlyUnlockedCount => NewlyUnlockedIds.Count; // 이번 평가에서 새로 달성한 수
        public IReadOnlyList<string> NewlyUnlockedIds { get; } // 이번 평가에서 새로 True가 된 ID 목록 - Steam 동기화가 이 목록만 넘겨받는다
        public bool IsComplete => TotalCount > 0 && UnlockedCount >= TotalCount; // 100% 달성 여부

        public AchievementProgressSnapshot(
            int totalCount,
            int unlockedCount,
            IReadOnlyList<string> newlyUnlockedIds)
        {
            TotalCount = totalCount; // 전체 개수 저장
            UnlockedCount = unlockedCount; // 달성 개수 저장
            NewlyUnlockedIds = newlyUnlockedIds ?? new List<string>(); // null 안전 신규 달성 ID 목록 저장
        }

        public static AchievementProgressSnapshot Empty()
        {
            return new AchievementProgressSnapshot(0, 0, new List<string>()); // 빈 진행도 반환
        }
    }
}
