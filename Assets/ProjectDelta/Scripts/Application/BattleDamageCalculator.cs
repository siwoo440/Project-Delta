namespace ProjectDelta.Application
{
    // 53일차: 명중/회피·피해·방어 계산에서 관통을 제거하고 정식 전투 능력치 구조에 맞춘다.
    // 55일차: 피해 공식을 비율형(방어력 감쇠)으로 바꾸고 95~105% 무작위 편차를 추가한다 (기획서 4.2).
    // 실제 데미지 적용(51일차 사망 판정 포함)이나 Command 연결은 이 클래스의 책임이 아니다.
    public static class BattleDamageCalculator
    {
        public const int BaseHitChancePercent = 70; // 기본 명중률
        public const int MinHitChancePercent = 5; // 아무리 회피가 높아도 최소 명중률 보장
        public const int MaxHitChancePercent = 100; // 명중률 상한

        public const int MinDamage = 1; // 방어력이 아무리 높아도 최소 피해 보장
        public const int DefendDamageReductionPercent = 50; // 52일차: 방어 중이면 최종 피해를 이 비율만큼 줄임

        // 55일차: 기획서 4.2 "최종 피해 = 기본 피해 × 95~105% 무작위 편차 × ...".
        // 치명타 배율·기타 보정은 58일차 이후 별도 항목에서 곱한다.
        public const int MinDamageVariancePercent = 95;
        public const int MaxDamageVariancePercent = 105;

        // varianceRoll이 가질 수 있는 값의 개수(0~10, 11칸) = 95~105% 11단계에 1:1 대응.
        public const int DamageVarianceRollCount =
            MaxDamageVariancePercent - MinDamageVariancePercent + 1;

        // 명중률(%) = 기본 명중률 + 공격자 명중 - 방어자 회피, 5~100% 사이로 고정.
        public static int CalculateHitChancePercent(
            BattleParticipant attacker,
            BattleParticipant defender)
        {
            int rawHitChance =
                BaseHitChancePercent
                + attacker.Accuracy
                - defender.Evasion;

            return Clamp(
                rawHitChance,
                MinHitChancePercent,
                MaxHitChancePercent);
        }

        // 55일차 기본 피해 = 공격력 × 100 ÷ (100 + 방어력).
        // "공격 배율"은 스킬 데이터(66일차 이후)에서 오므로, 기본 공격은 배율 100%로 취급한다.
        public static int CalculateBaseDamage(
            BattleParticipant attacker,
            BattleParticipant defender)
        {
            return attacker.Attack * 100 / (100 + defender.Defense);
        }

        // varianceRoll(0~10)을 95~105% 편차(%)로 바꾼다. 범위를 벗어나면 가장 가까운 경계로 고정한다.
        public static int CalculateVariancePercent(
            int varianceRoll)
        {
            return MinDamageVariancePercent
                + Clamp(
                    varianceRoll,
                    0,
                    DamageVarianceRollCount - 1);
        }

        // 기본 피해에 편차를 곱한 뒤, 방어 중이면 한 번 더 감소시키고 마지막에 최소 피해 1을 보장한다.
        public static int CalculateDamage(
            BattleParticipant attacker,
            BattleParticipant defender,
            int varianceRoll)
        {
            int baseDamage =
                CalculateBaseDamage(
                    attacker,
                    defender);

            int variancePercent =
                CalculateVariancePercent(
                    varianceRoll);

            int damage =
                baseDamage * variancePercent / 100;

            // 52일차: 대상이 방어 중이면 최종 피해를 한 번 더 비율만큼 줄인다.
            if (defender.IsDefending)
            {
                damage =
                    damage * (100 - DefendDamageReductionPercent) / 100;
            }

            return damage > MinDamage
                ? damage
                : MinDamage;
        }

        // roll0To99(0~99 난수)로 명중 여부를 판정하고, 명중 시에만 varianceRoll(0~10 난수)로
        // 피해량을 계산한다. 난수를 밖에서 주입받으므로 호출하는 쪽(실제 플레이 vs 테스트)이
        // 난수 발생 방식을 결정한다.
        public static BattleDamageResult Resolve(
            BattleParticipant attacker,
            BattleParticipant defender,
            int roll0To99,
            int varianceRoll)
        {
            int hitChancePercent =
                CalculateHitChancePercent(
                    attacker,
                    defender);

            bool isHit =
                roll0To99 < hitChancePercent;

            if (!isHit)
            {
                return BattleDamageResult.Miss(
                    hitChancePercent);
            }

            int damage =
                CalculateDamage(
                    attacker,
                    defender,
                    varianceRoll);

            // 55일차: 편차 적용 전 기본 피해·적용된 편차(%)를 디버그 표시용으로 함께 담는다.
            int baseDamage =
                CalculateBaseDamage(
                    attacker,
                    defender);

            int variancePercent =
                CalculateVariancePercent(
                    varianceRoll);

            return BattleDamageResult.Hit(
                damage,
                hitChancePercent,
                baseDamage,
                variancePercent);
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
