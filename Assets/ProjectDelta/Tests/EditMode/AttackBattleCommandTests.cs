using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Attack Battle Command 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class AttackBattleCommandTests
    {
        [Test]
        public void Attack_ExposesStableIdAndDisplayName()
        {
            IBattleCommand command =
                new AttackBattleCommand();

            Assert.AreEqual(
                "Attack",
                command.Id);

            Assert.AreEqual(
                "공격",
                command.DisplayName);
        }

        [Test]
        public void Attack_WithValidTarget_ReturnsAcceptedResult()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant enemy =
                CreateEnemy(
                    "MON_TEST",
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            BattleCommandResult result =
                new AttackBattleCommand()
                    .Execute(
                        context,
                        player,
                        enemy);

            Assert.IsTrue(
                result.Accepted);

            Assert.AreEqual(
                "Attack",
                result.CommandId);

            StringAssert.Contains(
                "PLAYER",
                result.Message);

            StringAssert.Contains(
                "MON_TEST",
                result.Message);
        }

        [Test]
        public void Attack_WithoutContext_ReturnsRejectedResult()
        {
            BattleCommandResult result =
                new AttackBattleCommand()
                    .Execute(
                        null,
                        CreatePlayer(),
                        CreateEnemy("MON_TEST", 10));

            Assert.IsFalse(
                result.Accepted);

            Assert.AreEqual(
                "Attack",
                result.CommandId);
        }

        [Test]
        public void Attack_WithSameTeamTarget_ReturnsRejectedResult()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant enemy =
                CreateEnemy(
                    "MON_TEST",
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            BattleCommandResult result =
                new AttackBattleCommand()
                    .Execute(
                        context,
                        player,
                        player); // 아군(자기 자신) 대상

            Assert.IsFalse(
                result.Accepted); // 오폭 거부 확인
        }

        [Test]
        public void Attack_WithDeadTarget_ReturnsRejectedResult()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant deadEnemy =
                CreateEnemy(
                    "MON_DEAD",
                    0);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { deadEnemy });

            BattleCommandResult result =
                new AttackBattleCommand()
                    .Execute(
                        context,
                        player,
                        deadEnemy);

            Assert.IsFalse(
                result.Accepted); // 사망한 대상 거부 확인
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                20,
                5);
        }

        private static BattleParticipant CreateEnemy(
            string instanceId,
            int maxHp)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                BattleTeam.Enemy,
                maxHp,
                5);
        }
    }
}
