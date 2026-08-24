namespace ProjectDelta.Application
{
    // 49일차: 실제 명중률·데미지 계산(50일차) 전까지 공격 대상 지정 의도만 반환한다.
    public sealed class AttackBattleCommand : IBattleCommand
    {
        public string Id =>
            "Attack";

        public string DisplayName =>
            "공격";

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
                $"공격 선언 / {actor.InstanceId} → {target.InstanceId}");
        }
    }
}
