using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 69일차: 전투 중 도주 성공률 계산. 기획서에 구체적인 공식이 없어 56일차 명중 공식과
    // 같은 형태의 임시 공식을 쓴다 - 정확한 수치가 확정되면 상수만 조정하면 된다.
    //
    //   도주 성공률(%) = 기본 50% + (내 유효 Speed - 상대 진영 평균 유효 Speed), 5~95% 클램프
    //
    // BattleTargeting.GetValidTargets()를 그대로 재사용해 "상대 진영"을 구한다 - Player면
    // 살아있는 Enemy 전원, Enemy면 살아있는 Player. 65일차 강화·약화 상태(StatModifier)가
    // 반영된 유효 Speed를 쓴다.
    public static class BattleEscapeCalculator
    {
        public const int BaseEscapeChancePercent = 50;
        public const int MinEscapeChancePercent = 5;
        public const int MaxEscapeChancePercent = 95;

        public static int CalculateEscapeChancePercent(
            BattleContext context,
            BattleParticipant actor)
        {
            int actorSpeed =
                BattleStatModifierService.GetEffectiveSpeed(
                    actor);

            int opponentAverageSpeed =
                CalculateOpponentAverageSpeed(
                    context,
                    actor);

            int rawChance =
                BaseEscapeChancePercent
                + (actorSpeed - opponentAverageSpeed);

            return Clamp(
                rawChance,
                MinEscapeChancePercent,
                MaxEscapeChancePercent);
        }

        private static int CalculateOpponentAverageSpeed(
            BattleContext context,
            BattleParticipant actor)
        {
            IReadOnlyList<BattleParticipant> opponents =
                BattleTargeting.GetValidTargets(
                    context,
                    actor);

            if (opponents.Count == 0)
            {
                return 0; // 상대가 없으면(전멸 직전 등) 속도 차이 없이 기본 확률만 적용
            }

            int totalSpeed = 0;

            for (int index = 0; index < opponents.Count; index++)
            {
                totalSpeed +=
                    BattleStatModifierService.GetEffectiveSpeed(
                        opponents[index]);
            }

            return totalSpeed / opponents.Count;
        }

        private static int Clamp(
            int value,
            int min,
            int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
