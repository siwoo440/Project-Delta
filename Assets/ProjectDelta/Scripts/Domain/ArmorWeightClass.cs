namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 102일차: 방어구(ChestArmor·Leggings·Boots)의 무게 분류. 순수 분류 태그이며
    // 아직 스탯 계산에 직접 관여하지 않는다.
    public enum ArmorWeightClass // 방어구 무게 분류
    {
        None = 0, // 분류 없음
        Light = 1, // 경갑
        Heavy = 2, // 중갑
        Robe = 3 // 로브
    }

    public static class ArmorWeightClassRules // 무게 분류 표시명 규칙
    {
        public static string GetDisplayName( // 분류에 대응하는 한글 표시명 반환
            ArmorWeightClass weightClass) // 조회할 무게 분류
        {
            switch (weightClass) // 분류별 분기
            {
                case ArmorWeightClass.Light: // 경갑인 경우
                    return "경갑"; // 경갑 표시명 반환

                case ArmorWeightClass.Heavy: // 중갑인 경우
                    return "중갑"; // 중갑 표시명 반환

                case ArmorWeightClass.Robe: // 로브인 경우
                    return "로브"; // 로브 표시명 반환

                default: // 그 외(None 등)
                    return "미분류"; // 미분류 표시명 반환
            }
        }
    }
}
