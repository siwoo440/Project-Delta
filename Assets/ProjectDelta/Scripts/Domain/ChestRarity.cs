namespace ProjectDelta.Domain
{
    // 106일차: 보물상자 등급 3종.
    public enum ChestRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2
    }

    public static class ChestRarityRules
    {
        public static string GetDisplayName(
            ChestRarity rarity)
        {
            switch (rarity)
            {
                case ChestRarity.Common:
                    return "일반";

                case ChestRarity.Uncommon:
                    return "고급";

                case ChestRarity.Rare:
                    return "희귀";

                default:
                    return "일반";
            }
        }

        // 기획서 수치: 일반 8%, 고급 12%, 희귀 18%.
        public static int GetMimicChancePercent(
            ChestRarity rarity)
        {
            switch (rarity)
            {
                case ChestRarity.Common:
                    return 8;

                case ChestRarity.Uncommon:
                    return 12;

                case ChestRarity.Rare:
                    return 18;

                default:
                    return 8;
            }
        }
    }
}
