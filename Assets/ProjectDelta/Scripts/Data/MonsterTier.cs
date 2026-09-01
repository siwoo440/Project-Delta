namespace ProjectDelta.Data
{
    // 121일차: 일반 몬스터와 구분되는 상위 개체 등급. MonsterDefinition과 같은 어셈블리(Data)에
    // 둬서 능력치·보상 배율 규칙(MonsterTierRules)이 Application을 거치지 않고 바로 쓰인다 -
    // RoomTypeRules(Domain)가 RoomType과 한 파일에 있는 것과 같은 이유다.
    public enum MonsterTier
    {
        Normal = 0,
        Elite = 1,
        Boss = 2
    }

    public static class MonsterTierRules
    {
        // 121일차: "고정 능력치" - 층 보정/개체 편차가 아직 없어(54일차 주석 참고) 이 배율이
        // 곧 그 몬스터의 확정된 스탯이 된다. 무작위로 흔들리지 않는다는 뜻에서 "고정"이다.
        public static float GetStatMultiplier(
            MonsterTier tier)
        {
            switch (tier)
            {
                case MonsterTier.Elite:
                    return 1.3f;

                case MonsterTier.Boss:
                    return 1.7f;

                default:
                    return 1f;
            }
        }

        // 121일차: 경험치·골드·아이템 드롭 확률에 공통으로 곱하는 보상 배율.
        public static float GetRewardMultiplier(
            MonsterTier tier)
        {
            switch (tier)
            {
                case MonsterTier.Elite:
                    return 1.5f;

                case MonsterTier.Boss:
                    return 2f;

                default:
                    return 1f;
            }
        }

        public static string GetDisplayName(
            MonsterTier tier)
        {
            switch (tier)
            {
                case MonsterTier.Elite:
                    return "정예";

                case MonsterTier.Boss:
                    return "보스";

                default:
                    return "일반";
            }
        }
    }
}
