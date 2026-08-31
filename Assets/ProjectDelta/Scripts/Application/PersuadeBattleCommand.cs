namespace ProjectDelta.Application
{
    // 116일차: 전투 중 매력/저항 판정으로 대상을 물러나게 하는 회유 선언 의도만 반환한다 -
    // AttackBattleCommand(49일차)와 같은 원칙으로 대상 유효성만 확인한다. 실제 성공률 판정은
    // EncounterPersuasionRule로 Presentation(ConfirmPersuade)에서 굴린다.
    public sealed class PersuadeBattleCommand : IBattleCommand
    {
        public string Id =>
            "Persuade";

        public string DisplayName =>
            "회유";

        public BattleCommandResult Execute(
            BattleContext context,
            BattleParticipant actor,
            BattleParticipant target)
        {
            if (context == null
                || actor == null)
            {
                return BattleCommandResult.Reject(
                    Id,
                    "현재 Battle 정보가 없습니다.");
            }

            if (!BattleTargeting.IsValidTarget(
                    context,
                    actor,
                    target))
            {
                return BattleCommandResult.Reject(
                    Id,
                    "대상을 선택할 수 없습니다.");
            }

            return BattleCommandResult.Accept(
                Id,
                $"회유 시도 / {actor.InstanceId} → {target.InstanceId}");
        }
    }
}
