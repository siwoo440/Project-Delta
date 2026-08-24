namespace ProjectDelta.Application
{
    // 49일차: 전투 화면(공격·방어·아이템·도주)이 구체적인 행동 구현에 직접 의존하지 않도록 하는 공통 계약.
    // 44일차 IEncounterCommand는 Encounter 진입/탈출 선택을 다루고, 이 계약은 전투 내부 행동을 다룬다.
    public interface IBattleCommand
    {
        string Id { get; }
        string DisplayName { get; }

        BattleCommandResult Execute(
            BattleContext context,
            BattleParticipant actor,
            BattleParticipant target);
    }
}
