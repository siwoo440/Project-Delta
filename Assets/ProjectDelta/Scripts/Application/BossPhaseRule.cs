namespace ProjectDelta.Application
{
    // 122일차: 보스 체력 구간에 따른 현재 페이즈 계산. 페이즈 수(N)만큼 체력을 균등하게
    // 나눠, 마지막 구간(체력 0 초과)까지 살아있으면 항상 N번째 페이즈다 - 예를 들어 2페이즈
    // 보스는 체력 50% 이하로 내려가는 순간 2페이즈로 전환된다.
    public static class BossPhaseRule
    {
        public static int GetCurrentPhase(
            int currentHp,
            int maxHp,
            int phaseCount)
        {
            int safePhaseCount =
                phaseCount < 1
                    ? 1
                    : phaseCount;

            if (safePhaseCount <= 1
                || maxHp <= 0)
            {
                return safePhaseCount <= 1
                    ? 1
                    : safePhaseCount;
            }

            if (currentHp <= 0)
            {
                return safePhaseCount;
            }

            float hpRatio =
                currentHp
                / (float)maxHp;

            // 체력을 페이즈 수만큼 균등 구간으로 나눈다 - 예) 2페이즈면 50% 경계.
            float segment =
                1f
                / safePhaseCount;

            int phaseFromTop =
                (int)(
                    (1f - hpRatio)
                    / segment);

            int phase =
                phaseFromTop
                + 1;

            return phase > safePhaseCount
                ? safePhaseCount
                : phase;
        }
    }
}
