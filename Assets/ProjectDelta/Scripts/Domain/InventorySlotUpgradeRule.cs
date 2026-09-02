namespace ProjectDelta.Domain
{
    // 127일차: 기억의 조각으로 사는 인벤토리 슬롯 영구 확장.
    // 126일차 PermanentStatUpgradeRule과 같은 패턴 - ProfileData(Data)를 직접 참조하지 않고
    // 순수 정수만 받아서 Domain의 "제로 의존성" 원칙을 지킨다.
    public static class InventorySlotUpgradeRule
    {
        public const int MaxLevel = 10;

        public const int SlotsPerLevel = 1;

        // 가방 아이템(102일차)보다 훨씬 강한 영구 효과라 스탯 강화(126일차, 기본 5)보다
        // 조금 더 비싸게 잡는다.
        private const int BaseCost = 8;

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

        // 런 시작 시 InventoryRunState.SetCapacityBonuses(...)에 그대로 넘길 보너스 슬롯 수.
        public static int GetBonusSlots(
            int level)
        {
            int clampedLevel =
                level < 0
                    ? 0
                    : (level > MaxLevel
                        ? MaxLevel
                        : level);

            return clampedLevel
                * SlotsPerLevel;
        }
    }
}
