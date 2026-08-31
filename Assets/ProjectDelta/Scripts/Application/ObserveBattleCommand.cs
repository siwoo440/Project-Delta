namespace ProjectDelta.Application
{
    // 116일차: 전투 중 대상 능력치를 확인하는 관찰 선언 의도만 반환한다 - DefendBattleCommand와
    // 같은 원칙으로 실제 텍스트 구성은 Presentation(ConfirmObserve)이 담당한다.
    public sealed class ObserveBattleCommand : IBattleCommand
    {
        public string Id =>
            "Observe";

        public string DisplayName =>
            "관찰";

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
                $"관찰 / {actor.InstanceId} → {target.InstanceId}");
        }
    }
}
