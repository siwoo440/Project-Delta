namespace ProjectDelta.Application
{
    // 49일차: 전투 내부 행동(Attack 등) 선택 결과를 UI에 전달하는 최소 결과 데이터.
    // 44일차 EncounterCommandResult와 동일한 구조를 Battle 내부 행동용으로 분리했다.
    public sealed class BattleCommandResult
    {
        public string CommandId { get; }
        public bool Accepted { get; }
        public string Message { get; }

        public BattleCommandResult(
            string commandId,
            bool accepted,
            string message)
        {
            CommandId =
                commandId;

            Accepted =
                accepted;

            Message =
                message;
        }

        public static BattleCommandResult Accept(
            string commandId,
            string message)
        {
            return new BattleCommandResult(
                commandId,
                true,
                message);
        }

        public static BattleCommandResult Reject(
            string commandId,
            string message)
        {
            return new BattleCommandResult(
                commandId,
                false,
                message);
        }
    }
}
