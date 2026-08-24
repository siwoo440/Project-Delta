namespace ProjectDelta.Application
{
    // 50일차: 명중/회피·피해·방어·관통 계산 공식을 한 곳에 모은다.
    // 실제 데미지 적용(51일차 사망 판정 포함)이나 Command 연결은 이 클래스의 책임이 아니다.
    public static class BattleDamageCalculator
    {
        public const int BaseHitChancePercent = 70; // 기본 명중률
        public const int MinHitChancePercent = 5; // 아무리 회피가 높아도 최소 명중률 보장
        public const int MaxHitChancePercent = 100; // 명중률 상한

        public const int MinDamage = 1; // 방어력이 아무리 높아도 최소 피해 보장

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

        // 피해량 = 공격력 + 관통 - 방어력, 최소 1은 보장.
        public static int CalculateDamage(
            BattleParticipant attacker,
            BattleParticipant defender)
        {
            int rawDamage =
                attacker.Attack
                + attacker.Penetration
                - defender.Defense;

            return rawDamage > MinDamage
                ? rawDamage
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
