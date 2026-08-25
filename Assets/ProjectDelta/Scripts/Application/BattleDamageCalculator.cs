namespace ProjectDelta.Application
{
    // 53일차: 명중/회피·피해·방어 계산에서 관통을 제거하고 정식 전투 능력치 구조에 맞춘다.
    // 실제 데미지 적용(51일차 사망 판정 포함)이나 Command 연결은 이 클래스의 책임이 아니다.
    public static class BattleDamageCalculator
    {
        public const int BaseHitChancePercent = 70; // 기본 명중률
        public const int MinHitChancePercent = 5; // 아무리 회피가 높아도 최소 명중률 보장
        public const int MaxHitChancePercent = 100; // 명중률 상한

        public const int MinDamage = 1; // 방어력이 아무리 높아도 최소 피해 보장
        public const int DefendDamageReductionPercent = 50; // 52일차: 방어 중이면 최종 피해를 이 비율만큼 줄임

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

        // 53일차 임시 피해량 = 공격력 - 방어력, 최소 1은 보장.
        // 비율형 피해 공식과 피해 편차는 이후 일차에서 별도로 적용한다.
        public static int CalculateDamage(
            BattleParticipant attacker,
            BattleParticipant defender)
        {
            int rawDamage =
                attacker.Attack
                - defender.Defense;

            int damage =
                rawDamage > MinDamage
                    ? rawDamage
                    : MinDamage;

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

        // roll0To99(0~99 난수)로 명중 여부를 판정하고, 명중 시에만 피해량을 계산한다.
        // 난수를 밖에서 주입받으므로 호출하는 쪽(실제 플레이 vs 테스트)이 난수 발생 방식을 결정한다.
        public static BattleDamageResult Resolve(
            BattleParticipant attacker,
            BattleParticipant defender,
            int roll0To99)
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
                    defender);

            return BattleDamageResult.Hit(
                damage,
                hitChancePercent);
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
