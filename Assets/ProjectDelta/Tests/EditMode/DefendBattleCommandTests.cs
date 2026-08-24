using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Defend Battle Command 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class DefendBattleCommandTests
    {
        [Test]
        public void Defend_ExposesStableIdAndDisplayName()
        {
            IBattleCommand command =
                new DefendBattleCommand();

            Assert.AreEqual(
                "Defend",
                command.Id);

            Assert.AreEqual(
                "방어",
                command.DisplayName);
        }

        [Test]
        public void Defend_WithContext_SetsActorDefendingAndReturnsAccepted()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            Assert.IsFalse(
                player.IsDefending); // 시작 전에는 방어 중 아님 확인

            BattleCommandResult result =
                new DefendBattleCommand()
                    .Execute(
                        context,
                        player,
                        null); // 대상 불필요

            Assert.IsTrue(
                result.Accepted);

            Assert.AreEqual(
                "Defend",
                result.CommandId);

            StringAssert.Contains(
                "PLAYER",
                result.Message);

            Assert.IsTrue(
                player.IsDefending); // 실행 후 방어 중으로 전환 확인
        }

        [Test]
        public void Defend_WithoutContext_ReturnsRejectedResultAndDoesNotChangeState()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleCommandResult result =
                new DefendBattleCommand()
                    .Execute(
                        null,
                        player,
                        null);

            Assert.IsFalse(
                result.Accepted);

            Assert.IsFalse(
                player.IsDefending); // 거부됐으므로 상태 변화 없음 확인
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                20,
                5,
                6,
                3,
                90,
                10,
                0);
        }

        private static BattleParticipant CreateEnemy()
        {
            return new BattleParticipant(
                "MON_TEST",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5,
                4,
                2,
                80,
                5,
                0);
        }
    }
}
