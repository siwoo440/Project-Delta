namespace ProjectDelta.Application
{
    // 46일차: 행동 선택 결과와 분리된 최종 Encounter 결과 데이터.
    public sealed class EncounterResult
    {
        public string RoomId { get; }
        public string MonsterDefinitionId { get; }
        public EncounterOutcome Outcome { get; }

        public bool CompletesRoom =>
            Outcome == EncounterOutcome.MonsterDefeated;

        public bool RemovesMonster =>
            Outcome == EncounterOutcome.MonsterDefeated;

        public EncounterResult(
            string roomId,
            string monsterDefinitionId,
            EncounterOutcome outcome)
        {
            RoomId =
                roomId;

            MonsterDefinitionId =
                monsterDefinitionId;

            Outcome =
                outcome;
        }
    }
}
