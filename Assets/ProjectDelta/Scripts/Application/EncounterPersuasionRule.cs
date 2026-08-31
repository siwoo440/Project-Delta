namespace ProjectDelta.Application
{
    // 116일차: 회유·유혹 성공률을 플레이어 매력과 몬스터 저항 차이로 계산한다.
    // baseSuccessPercent는 두 행동을 구분하는 값이다 - 회유는 50, 유혹은 그보다 낮은 값을 써서
    // "유혹이 더 위험한 시도"라는 차이를 능력치 하나 늘리지 않고 기준값만으로 표현한다.
    public static class EncounterPersuasionRule
    {
        public const int MinSuccessPercent = 5;
        public const int MaxSuccessPercent = 95;

        public static int CalculateSuccessPercent(
            int baseSuccessPercent,
            int playerCharm,
            int monsterResistance)
        {
            int percent =
                baseSuccessPercent
                + (playerCharm - monsterResistance);

            if (percent < MinSuccessPercent)
            {
                return MinSuccessPercent;
            }

            if (percent > MaxSuccessPercent)
            {
                return MaxSuccessPercent;
            }

            return percent;
        }

        public static bool TryEvaluate(
            int baseSuccessPercent,
            int playerCharm,
            int monsterResistance,
            IRandomSource rng,
            out int successPercent)
        {
            successPercent =
                CalculateSuccessPercent(
                    baseSuccessPercent,
                    playerCharm,
                    monsterResistance);

            if (rng == null)
            {
                return false;
            }

            int roll =
                rng.NextInt(
                    0,
                    100);

            return roll < successPercent;
        }
    }
}
