namespace ProjectDelta.Application
{
    // 44일차: 실제 회피 확률 판정 전까지 회피 선택 의도만 반환한다.
    public sealed class EscapeEncounterCommand : IEncounterCommand
    {
        public string Id =>
            "Escape";

        public string DisplayName =>
            "회피";

        public EncounterCommandResult Execute(
            EncounterContext context)
        {
            if (context == null)
            {
                return EncounterCommandResult.Reject(
                    Id,
                    "현재 Encounter 정보가 없습니다.");
            }

            return EncounterCommandResult.Accept(
                Id,
                $"회피 선택 / Target {context.MonsterDefinitionId}");
        }
    }
}
