namespace ProjectDelta.Application
{
    // 44일차: 실제 전투 시스템 연결 전까지 전투 선택 의도만 반환한다.
    public sealed class BattleEncounterCommand : IEncounterCommand
    {
        public string Id =>
            "Battle";

        public string DisplayName =>
            "전투";

        public EncounterCommandResult Execute(
            EncounterContext context)
        {
            if (context == null)
            {
                return EncounterCommandResult.Reject(
                    Id,
                    "현재 Encounter 정보가 없습니다.");
            }

            ApplicationFlow.Current?.SaveBattleEncounterCheckpoint(
                context); // 전투 시작 직전 자동 저장

            return EncounterCommandResult.Accept(
                Id,
                $"전투 선택 / Target {context.MonsterDefinitionId}");
        }
    }
}
