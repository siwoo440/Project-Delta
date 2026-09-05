namespace ProjectDelta.Domain
{
    // 134일차: 주요 엔딩을 하나라도 영구 기록한 프로필은 자유 탐험 모드를 해금한다.
    public static class FreeExplorationUnlockRule
    {
        public static bool IsUnlocked(
            int unlockedMainEndingCount)
        {
            return unlockedMainEndingCount > 0; // 기존 세이브까지 자동 호환되는 파생 해금 조건
        }
    }
}
