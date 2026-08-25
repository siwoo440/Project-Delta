namespace ProjectDelta.Application
{
    // 50일차: 한 번의 공격 판정 결과 (명중 여부 + 피해량).
    // 55일차: 디버그 표시용으로 편차 적용 전 기본 피해와 적용된 편차(%)도 함께 담는다.
    public sealed class BattleDamageResult
    {
        public bool IsHit { get; }
        public int Damage { get; }
        public int HitChancePercent { get; }
        public int BaseDamage { get; }
        public int VariancePercent { get; }

        private BattleDamageResult(
            bool isHit,
            int damage,
            int hitChancePercent,
            int baseDamage,
            int variancePercent)
        {
            IsHit =
                isHit;

            Damage =
                damage;

            HitChancePercent =
                hitChancePercent;

            BaseDamage =
                baseDamage;

            VariancePercent =
                variancePercent;
        }

        public static BattleDamageResult Hit(
            int damage,
            int hitChancePercent,
            int baseDamage,
            int variancePercent)
        {
            return new BattleDamageResult(
                true,
                damage,
                hitChancePercent,
                baseDamage,
                variancePercent);
        }

        public static BattleDamageResult Miss(
            int hitChancePercent)
        {
            return new BattleDamageResult(
                false,
                0,
                hitChancePercent,
                0,
                0);
        }
    }
}
