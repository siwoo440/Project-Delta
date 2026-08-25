namespace ProjectDelta.Application // 전투 응용 네임스페이스
{
    public sealed class SurrenderBattleCommand : IBattleCommand // 항복 전투 명령
    {
        public string Id => // 명령 식별자
            "Surrender";

        public string DisplayName => // 화면 표시 이름
            "항복";

        public BattleCommandResult Execute( // 항복 선언 검증
            BattleContext context, // 전투 정보 입력
            BattleParticipant actor, // 행동자 입력
            BattleParticipant target) // 미사용 대상 입력
        {
            if (context == null
                || actor == null) // 전투 정보 존재 확인
            {
                return BattleCommandResult.Reject(
                    Id,
                    "현재 Battle 정보가 없습니다."); // 항복 선언 거절
            }

            if (actor.Team != BattleTeam.Player) // 플레이어 행동 여부 확인
            {
                return BattleCommandResult.Reject(
                    Id,
                    "플레이어 차례에만 항복할 수 있습니다."); // 적 항복 차단
            }

            if (!actor.IsAlive) // 플레이어 생존 여부 확인
            {
                return BattleCommandResult.Reject(
                    Id,
                    "행동할 수 없는 상태입니다."); // 사망 상태 항복 차단
            }

            return BattleCommandResult.Accept(
                Id,
                $"항복 선택 / {actor.InstanceId}"); // 항복 선언 승인
        }
    }
}
