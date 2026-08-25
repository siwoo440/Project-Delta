namespace ProjectDelta.Application
{
    // 59일차: BattleActionResult.DamageChanges 한 항목 — 이번 행동으로 발생한 피해 변화 하나.
    // 기획서 10.3 "BattleActionResult ├── DamageChanges"에 대응한다.
    public sealed class BattleDamageChange
    {
        public BattleParticipant Attacker { get; }
        public BattleParticipant Target { get; }
        public BattleDamageResult DamageResult { get; }
        public int AppliedDamage { get; } // 남은 HP보다 큰 피해는 잘려 있으므로, 실제로 깎인 양

        public BattleDamageChange(
            BattleParticipant attacker,
            BattleParticipant target,
            BattleDamageResult damageResult,
            int appliedDamage)
        {
            Attacker =
                attacker;

            Target =
                target;

            DamageResult =
                damageResult;

            AppliedDamage =
                appliedDamage;
        }
    }
}
