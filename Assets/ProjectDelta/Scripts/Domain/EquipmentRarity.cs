namespace ProjectDelta.Domain
{
    // 100일차: 장비 등급. 숫자가 높을수록 희귀하다.
    public enum EquipmentRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    // 등급별 표시명·가중치·스탯 배율의 단일 기준.
    public static class EquipmentRarityRules
    {
        // UI에 표시할 한글 등급명을 반환한다.
        public static string GetDisplayName(
            EquipmentRarity rarity)
        {
            switch (rarity)
            {
                case EquipmentRarity.Common:
                    return "일반";

                case EquipmentRarity.Uncommon:
                    return "고급";

                case EquipmentRarity.Rare:
                    return "희귀";

                case EquipmentRarity.Epic:
                    return "영웅";

                case EquipmentRarity.Legendary:
                    return "전설";

                default:
                    return "일반";
            }
        }

        // 등급이 높을수록 기본 스탯에 곱해지는 배율이 커진다.
        public static double GetStatMultiplier(
            EquipmentRarity rarity)
        {
            switch (rarity)
            {
                case EquipmentRarity.Common:
                    return 1.0;

                case EquipmentRarity.Uncommon:
                    return 1.15;

                case EquipmentRarity.Rare:
                    return 1.35;

                case EquipmentRarity.Epic:
                    return 1.6;

                case EquipmentRarity.Legendary:
                    return 2.0;

                default:
                    return 1.0;
            }
        }

        // 등급별 드랍/판정 가중치. 숫자가 클수록 잘 나온다.
        public static int GetDropWeight(
            EquipmentRarity rarity)
        {
            switch (rarity)
            {
                case EquipmentRarity.Common:
                    return 100;

                case EquipmentRarity.Uncommon:
                    return 55;

                case EquipmentRarity.Rare:
                    return 25;

                case EquipmentRarity.Epic:
                    return 10;

                case EquipmentRarity.Legendary:
                    return 3;

                default:
                    return 0;
            }
        }
    }
}
