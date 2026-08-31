namespace ProjectDelta.Application
{
    // 117일차: IBattleCommand(49일차)와 같은 원칙 - 별도 이벤트 전투 화면이 구체적인 행동
    // 구현에 직접 의존하지 않게 하는 공통 계약. 명중 판정 없이 자원(마나/정력)을 쓰고 대신
    // 호감도를 올리는 행동이라 대상(target) 인자가 없다 - 대상은 항상 Context.Target이다.
    public interface IEventBattleCommand
    {
        string Id { get; }

        string DisplayName { get; }

        int ManaCost { get; }

        int StaminaCost { get; }

        // 118일차: 이 행동을 쓴 쪽이 다음 주도권 굴림에서 받는 보정치 - EventBattleInitiativeRule 참고.
        int InitiativeModifier { get; }

        EventBattleCommandResult Execute(
            EventBattleContext context,
            IRandomSource rng);
    }
}
