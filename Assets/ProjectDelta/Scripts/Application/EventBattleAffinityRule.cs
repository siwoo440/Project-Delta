using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 118일차: 종족별 강점4·보통4·약점4 배율 - 강점(그 종족이 잘 버팀) 50%, 보통 100%,
    // 약점(그 종족이 취약함) 150%. 몬스터 쪽 데이터(MonsterDefinition.EventBattleStrongActionIds/
    // EventBattleWeakActionIds)가 아직 채워지지 않은 종족은 전부 보통(100%)으로 취급된다 -
    // 실제 상성 값을 입력하는 건 133~135일차(몬스터 콘텐츠 완성) 몫이다.
    public static class EventBattleAffinityRule
    {
        public const float StrongMultiplier = 0.5f;
        public const float NormalMultiplier = 1f;
        public const float WeakMultiplier = 1.5f;

        public static float ResolveMultiplier(
            IReadOnlyList<string> strongActionIds,
            IReadOnlyList<string> weakActionIds,
            string actionId)
        {
            if (string.IsNullOrEmpty(
                    actionId))
            {
                return NormalMultiplier;
            }

            if (Contains(
                    strongActionIds,
                    actionId))
            {
                return StrongMultiplier;
            }

            if (Contains(
                    weakActionIds,
                    actionId))
            {
                return WeakMultiplier;
            }

            return NormalMultiplier;
        }

        private static bool Contains(
            IReadOnlyList<string> ids,
            string actionId)
        {
            if (ids == null)
            {
                return false;
            }

            for (int index = 0; index < ids.Count; index++)
            {
                if (ids[index] == actionId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
