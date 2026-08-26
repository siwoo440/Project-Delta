namespace ProjectDelta.Application
{
    // 86일차: 전투 계산과 분리된 표시·대기 시간 배속 상태를 관리한다.
    public static class BattleSpeedState
    {
        public const float NormalMultiplier = 1f;
        public const float FastMultiplier = 2f;

        public static float CurrentMultiplier { get; private set; } =
            NormalMultiplier;

        public static bool IsFast =>
            CurrentMultiplier == FastMultiplier;

        public static string DisplayLabel =>
            IsFast
                ? "2×"
                : "1×";

        public static void Toggle()
        {
            CurrentMultiplier =
                IsFast
                    ? NormalMultiplier
                    : FastMultiplier;
        }

        public static void ResetToNormal()
        {
            CurrentMultiplier =
                NormalMultiplier;
        }

        public static float ScaleDuration(
            float baseDuration)
        {
            if (baseDuration <= 0f)
            {
                return 0f;
            }

            return baseDuration
                / CurrentMultiplier;
        }
    }
}
