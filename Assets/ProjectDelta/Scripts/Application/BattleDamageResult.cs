namespace ProjectDelta.Application
{
    // 50일차: 한 번의 공격 판정 결과 (명중 여부 + 피해량).
    public sealed class BattleDamageResult
    {
        public bool IsHit { get; }
        public int Damage { get; }
        public int HitChancePercent { get; }

        private BattleDamageResult(
            bool isHit,
            int damage,
            int hitChancePercent)
        {
            IsHit =
                isHit;

            Damage =
                damage;

            HitChancePercent =
                hitChancePercent;
        }

        public static BattleDamageResult Hit(
            int damage,
            int hitChancePercent)
        {
            return new BattleDamageResult(
                true,
                damage,
                hitChancePercent);
        }

        public static BattleDamageResult Miss(
            int hitChancePercent)
        {
            return new BattleDamageResult(
                false,
                0,
                hitChancePercent);
        }
    }
}
