namespace ProjectDelta.Application
{
    // 46일차: 실제 Battle 시스템 연결 전 선택된 Command를 테스트용 Encounter 결과로 변환한다.
    public static class EncounterResultResolver
    {
        private const string BattleCommandId =
            "Battle";

        private const string EscapeCommandId =
            "Escape";

        public static bool TryCreateTestResult(
            EncounterContext context,
            string selectedCommandId,
            out EncounterResult result)
        {
            result =
                null;

            if (context == null
                || string.IsNullOrEmpty(selectedCommandId))
            {
                return false;
            }

            if (selectedCommandId == BattleCommandId)
            {
                result =
                    new EncounterResult(
                        context.RoomId,
                        context.MonsterDefinitionId,
                        EncounterOutcome.MonsterDefeated);

                return true;
            }

            if (selectedCommandId == EscapeCommandId)
            {
                result =
                    new EncounterResult(
                        context.RoomId,
                        context.MonsterDefinitionId,
                        EncounterOutcome.Escaped);

                return true;
            }

            return false;
        }
    }
}
