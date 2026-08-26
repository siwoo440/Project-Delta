namespace ProjectDelta.Application
{
    // 76일차: 40일차 RoomEncounterPlacementService.CalculateStableRoll이 쓰던 FNV-1a 해시
    // 혼합 로직을 공용 유틸로 뽑아냈다. 몬스터 그룹 구성(76일차) 등 "같은 Seed면 항상 같은
    // 결과"가 필요한 다른 결정론적 굴림에서도 재사용한다.
    // string.GetHashCode()는 런타임/플랫폼마다 값이 달라질 수 있으므로 쓰지 않는다.
    public static class DeterministicRollHash
    {
        // seed와 문자열 조각들(부위·용도 구분용 salt 포함)을 섞어 안정적인 32비트 해시를 만든다.
        public static uint Compute(
            int seed,
            params string[] parts)
        {
            unchecked
            {
                uint hash = 2166136261u;

                MixInt(
                    ref hash,
                    seed);

                if (parts != null)
                {
                    for (int index = 0; index < parts.Length; index++)
                    {
                        MixString(
                            ref hash,
                            parts[index]);
                    }
                }

                return hash;
            }
        }

        // Compute()를 0~1 사이 실수로 정규화한다 (기존 CalculateStableRoll과 동일한 방식).
        public static float ComputeRoll01(
            int seed,
            params string[] parts)
        {
            uint hash =
                Compute(
                    seed,
                    parts);

            return (hash & 0x00FFFFFFu)
                / 16777216f;
        }

        private static void MixInt(
            ref uint hash,
            int value)
        {
            unchecked
            {
                uint data =
                    (uint)value;

                for (int i = 0; i < 4; i++)
                {
                    hash ^= data & 0xFFu;
                    hash *= 16777619u;
                    data >>= 8;
                }
            }
        }

        private static void MixString(
            ref uint hash,
            string value)
        {
            unchecked
            {
                if (string.IsNullOrEmpty(value))
                {
                    hash ^= 0u;
                    hash *= 16777619u;
                    return;
                }

                for (int i = 0; i < value.Length; i++)
                {
                    char character =
                        value[i];

                    hash ^= character;
                    hash *= 16777619u;
                }
            }
        }
    }
}
