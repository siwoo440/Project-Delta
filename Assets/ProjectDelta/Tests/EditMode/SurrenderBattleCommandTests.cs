using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // 항복 Command 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class SurrenderBattleCommandTests // 항복 Command 테스트
    {
        [Test]
        public void Execute_PlayerActor_AcceptsSurrender() // 플레이어 항복 승인 확인
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player); // 플레이어 생성

            BattleContext context =
                new BattleContext(
                    player,
                    new BattleParticipant[0]); // 전투 정보 생성

            SurrenderBattleCommand command =
                new SurrenderBattleCommand(); // 항복 Command 생성

            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    null); // 항복 선언 실행

            Assert.IsTrue(
                result.Accepted); // 항복 승인 확인
        }

        [Test]
        public void Execute_EnemyActor_RejectsSurrender() // 적 항복 차단 확인
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player); // 플레이어 생성

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_A",
                    BattleTeam.Enemy); // 적 생성

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy }); // 전투 정보 생성

            SurrenderBattleCommand command =
                new SurrenderBattleCommand(); // 항복 Command 생성

            BattleCommandResult result =
                command.Execute(
                    context,
                    enemy,
                    null); // 적 항복 선언 실행

            Assert.IsFalse(
                result.Accepted); // 항복 거절 확인
        }

        [Test]
        public void Execute_NullContext_RejectsSurrender() // 전투 정보 없는 항복 차단 확인
        {
            SurrenderBattleCommand command =
                new SurrenderBattleCommand(); // 항복 Command 생성

            BattleCommandResult result =
                command.Execute(
                    null,
                    null,
                    null); // 잘못된 항복 선언 실행

            Assert.IsFalse(
                result.Accepted); // 항복 거절 확인
        }

        private static BattleParticipant CreateParticipant( // 테스트 참가자 생성
            string id, // 참가자 ID 입력
            BattleTeam team) // 참가자 팀 입력
        {
            return new BattleParticipant(
                id,
                id,
                team,
                20,
                10,
                10,
                5,
                90,
                5); // 최소 전투 참가자 생성
        }
    }
}
