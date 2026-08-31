namespace ProjectDelta.Application
{
    // 116일차: 회유보다 기준 성공률이 낮은(EncounterPersuasionRule 기준값 차이) 유혹 선언 의도만
    // 반환한다. 성공 시 전용 이벤트 전투로 분기하는 것은 아직 그 이벤트 전투 시스템이 없어
    // 117일차 이후 과제로 남기고, 지금은 회유와 같은 방식(전투 종료)으로 처리한다.
    public sealed class SeduceBattleCommand : IBattleCommand
    {
        public string Id =>
            "Seduce";

        public string DisplayName =>
            "유혹";

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
                $"유혹 시도 / {actor.InstanceId} → {target.InstanceId}");
        }
    }
}
