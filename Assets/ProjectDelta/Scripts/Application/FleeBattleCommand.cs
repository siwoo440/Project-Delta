namespace ProjectDelta.Application
{
    // 69일차: 전투 중 도주 선언. AttackBattleCommand(49일차)·DefendBattleCommand(52일차)와
    // 같은 원칙으로, Execute()는 Battle 정보가 있는지만 확인한다. 대상 선택이 필요 없어
    // DefendBattleCommand처럼 target은 쓰지 않는다. 실제 도주 성공률 판정은
    // BattleEscapeCalculator가 담당하고, Presentation(ConfirmFlee)에서 굴림을 넣는다.
    public sealed class FleeBattleCommand : IBattleCommand
    {
        public string Id =>
            "Flee";

        public string DisplayName =>
            "도주";

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

            return BattleCommandResult.Accept(
                Id,
                $"도주 시도 / {actor.InstanceId}");
        }
    }
}
