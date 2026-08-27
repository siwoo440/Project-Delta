namespace ProjectDelta.Domain
{
    // 102일차: 방어구(ChestArmor·Leggings·Boots)의 무게 분류. 순수 분류 태그이며
    // 아직 스탯 계산에 직접 관여하지 않는다.
    public enum ArmorWeightClass
    {
        None = 0,
        Light = 1,
        Heavy = 2,
        Robe = 3
    }

    public static class ArmorWeightClassRules
    {
        public static string GetDisplayName(
            ArmorWeightClass weightClass)
        {
            switch (weightClass)
            {
                case ArmorWeightClass.Light:
                    return "경갑";

                case ArmorWeightClass.Heavy:
                    return "중갑";

                case ArmorWeightClass.Robe:
                    return "로브";

                default:
                    return "미분류";
            }
        }
    }
}
