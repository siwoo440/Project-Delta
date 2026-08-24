namespace ProjectDelta.Application
{
    // 44일차: Encounter UI가 구체적인 행동 구현에 직접 의존하지 않도록 하는 공통 Command 계약.
    public interface IEncounterCommand
    {
        string Id { get; }
        string DisplayName { get; }

        EncounterCommandResult Execute(
            EncounterContext context);
    }
}
