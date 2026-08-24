namespace ProjectDelta.Application
{
    // 52일차: 대상 선택이 필요 없는 자기 자신 대상 행동. target은 사용하지 않는다.
    public sealed class DefendBattleCommand : IBattleCommand
    {
        public string Id =>
            "Defend";

        public string DisplayName =>
            "방어";

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

            actor.SetDefending(
                true);

            return BattleCommandResult.Accept(
                Id,
                $"방어 / {actor.InstanceId} 방어 태세");
        }
    }
}
