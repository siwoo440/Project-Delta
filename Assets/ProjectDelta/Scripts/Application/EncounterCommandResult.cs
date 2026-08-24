namespace ProjectDelta.Application
{
    // 44일차: Encounter 행동 선택 결과를 UI와 이후 시스템에 전달하는 최소 결과 데이터.
    public sealed class EncounterCommandResult
    {
        public string CommandId { get; }
        public bool Accepted { get; }
        public string Message { get; }

        public EncounterCommandResult(
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

        public static EncounterCommandResult Accept(
            string commandId,
            string message)
        {
            return new EncounterCommandResult(
                commandId,
                true,
                message);
        }

        public static EncounterCommandResult Reject(
            string commandId,
            string message)
        {
            return new EncounterCommandResult(
                commandId,
                false,
                message);
        }
    }
}
