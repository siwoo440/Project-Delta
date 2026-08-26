namespace ProjectDelta.Application
{
    // 46일차: 행동 선택 결과와 분리된 최종 Encounter 결과 데이터.
    public sealed class EncounterResult
    {
        public string RoomId { get; }
        public string MonsterDefinitionId { get; }
        public EncounterOutcome Outcome { get; }

        // 83일차: 도주 성공도 보상 없이 현재 인카운터를 끝내고 몬스터를 제거한다.
        public bool CompletesRoom =>
            Outcome == EncounterOutcome.MonsterDefeated
            || Outcome == EncounterOutcome.Escaped;

        public bool RemovesMonster =>
            Outcome == EncounterOutcome.MonsterDefeated
            || Outcome == EncounterOutcome.Escaped;

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
