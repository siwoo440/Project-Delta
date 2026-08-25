namespace ProjectDelta.Application
{
    // 49일차: 전투 내부 행동(Attack 등)의 "선언이 유효한가" 판정 결과 (대상 지정 등).
    // 44일차 EncounterCommandResult와 동일한 구조를 Battle 내부 행동용으로 분리했다.
    // 59일차: 실제 판정 결과(피해·상태·전투 종료 등)는 BattleActionResult가 대신 담당한다.
    // 이 타입은 IBattleCommand.Execute()의 반환값(행동 선언 자체의 accept/reject)으로만 쓴다.
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
