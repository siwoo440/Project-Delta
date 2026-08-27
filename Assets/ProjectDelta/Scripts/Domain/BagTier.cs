namespace ProjectDelta.Domain
{
    // 102일차: 가방 등급. 6부위 장비 슬롯 체계에는 가방을 위한 별도 슬롯이 없으므로,
    // 가방은 장착 대상이 아니라 인벤토리 슬롯을 즉시·영구적으로 확장하는
    // 소모형 아이템으로 취급한다 (BagExpansionService 참고).
    public enum BagTier
    {
        None = 0,
        Small = 1,
        Medium = 2,
        Large = 3,
        Huge = 4
    }

    public static class BagTierRules
    {
        public static string GetDisplayName(
            BagTier tier)
        {
            switch (tier)
            {
                case BagTier.Small:
                    return "소형 가방";

                case BagTier.Medium:
                    return "중형 가방";

                case BagTier.Large:
                    return "대형 가방";

                case BagTier.Huge:
                    return "초대형 가방";

                default:
                    return "가방 아님";
            }
        }

        // 기획서 "+2~+8 확장" 범위를 4등급에 고르게 배분한다.
        public static int GetSlotBonus(
            BagTier tier)
        {
            switch (tier)
            {
                case BagTier.Small:
                    return 2;

                case BagTier.Medium:
                    return 4;

                case BagTier.Large:
                    return 6;

                case BagTier.Huge:
                    return 8;

                default:
                    return 0;
            }
        }
    }
}
