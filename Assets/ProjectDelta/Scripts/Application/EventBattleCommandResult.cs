namespace ProjectDelta.Application
{
    // 117일차: IBattleCommand.Execute()의 BattleCommandResult(49일차)와 같은 역할 -
    // "선언이 유효한가"만 담는다. 실제 호감도 증가량은 여기 FavorGained에 담아 반환한다.
    public sealed class EventBattleCommandResult
    {
        public string CommandId { get; }

        public bool Accepted { get; }

        public string Message { get; }

        public int FavorGained { get; }

        public EventBattleCommandResult(
            string commandId,
            bool accepted,
            string message,
            int favorGained)
        {
            CommandId =
                commandId;

            Accepted =
                accepted;

            Message =
                message;

            FavorGained =
                favorGained;
        }

        public static EventBattleCommandResult Accept(
            string commandId,
            string message,
            int favorGained)
        {
            return new EventBattleCommandResult(
                commandId,
                true,
                message,
                favorGained);
        }

        public static EventBattleCommandResult Reject(
            string commandId,
            string message)
        {
            return new EventBattleCommandResult(
                commandId,
                false,
                message,
                0);
        }
    }
}
