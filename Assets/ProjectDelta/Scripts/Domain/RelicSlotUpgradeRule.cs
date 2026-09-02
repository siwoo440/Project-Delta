namespace ProjectDelta.Domain
{
    // 128일차: 기억의 조각으로 사는 유물 보유량 영구 확장.
    // 126~127일차와 같은 패턴 - ProfileData(Data)를 직접 참조하지 않고 순수 정수만 받아서
    // Domain의 "제로 의존성" 원칙을 지킨다.
    public static class RelicSlotUpgradeRule
    {
        public const int MaxLevel = 10;

        public const int CapacityPerLevel = 1;

        // 유물은 인벤토리 슬롯(127일차, 기본 8)보다도 희소성이 큰 영구 효과라 더 비싸게 잡는다.
        private const int BaseCost = 12;

        public static int GetNextLevelCost(
            int currentLevel)
        {
            return BaseCost
                * (currentLevel + 1);
        }

        public static bool TryGetUpgradeCost(
            int currentLevel,
            out int cost)
        {
            if (currentLevel >= MaxLevel)
            {
                cost = 0;
                return false;
            }

            cost =
                GetNextLevelCost(
                    currentLevel);

            return true;
        }

        // 런 시작 시 RelicRunState.SetMaxCapacity(...)에 그대로 넘길 최대 보유 수.
        public static int GetMaxCapacity(
            int level)
        {
            int clampedLevel =
                level < 0
                    ? 0
                    : (level > MaxLevel
                        ? MaxLevel
                        : level);

            return RelicRunState.DefaultMaxCapacity
                + (clampedLevel
                    * CapacityPerLevel);
        }
    }
}
