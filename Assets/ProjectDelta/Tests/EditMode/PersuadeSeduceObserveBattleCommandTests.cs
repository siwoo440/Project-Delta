using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    // 116일차: 전투 중 회유·유혹·관찰 - 공격(AttackBattleCommand)과 같은 대상 유효성 규칙을 쓴다.
    public sealed class PersuadeSeduceObserveBattleCommandTests
    {
        [Test]
        public void Persuade_ExposesStableIdAndDisplayName()
        {
            IBattleCommand command =
                new PersuadeBattleCommand();

            Assert.AreEqual(
                "Persuade",
                command.Id);

            Assert.AreEqual(
                "회유",
                command.DisplayName);
        }

        [Test]
        public void Seduce_ExposesStableIdAndDisplayName()
        {
            IBattleCommand command =
                new SeduceBattleCommand();

            Assert.AreEqual(
                "Seduce",
                command.Id);

            Assert.AreEqual(
                "유혹",
                command.DisplayName);
        }

        [Test]
        public void Observe_ExposesStableIdAndDisplayName()
        {
            IBattleCommand command =
                new ObserveBattleCommand();

            Assert.AreEqual(
                "Observe",
                command.Id);

            Assert.AreEqual(
                "관찰",
                command.DisplayName);
        }

        [Test]
        public void Persuade_WithValidTarget_ReturnsAcceptedResult()
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
                new PersuadeBattleCommand()
                    .Execute(
                        context,
                        player,
                        enemy);

            Assert.IsTrue(
                result.Accepted);
        }

        [Test]
        public void Seduce_WithValidTarget_ReturnsAcceptedResult()
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
                new SeduceBattleCommand()
                    .Execute(
                        context,
                        player,
                        enemy);

            Assert.IsTrue(
                result.Accepted);
        }

        [Test]
        public void Observe_WithValidTarget_ReturnsAcceptedResult()
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
                new ObserveBattleCommand()
                    .Execute(
                        context,
                        player,
                        enemy);

            Assert.IsTrue(
                result.Accepted);
        }

        [Test]
        public void Persuade_WithoutTarget_ReturnsRejectedResult()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy("MON_TEST", 10) });

            BattleCommandResult result =
                new PersuadeBattleCommand()
                    .Execute(
                        context,
                        player,
                        null);

            Assert.IsFalse(
                result.Accepted);
        }

        [Test]
        public void Seduce_WithNullContext_ReturnsRejectedResult()
        {
            BattleCommandResult result =
                new SeduceBattleCommand()
                    .Execute(
                        null,
                        CreatePlayer(),
                        null);

            Assert.IsFalse(
                result.Accepted);
        }

        [Test]
        public void Observe_WithNullContext_ReturnsRejectedResult()
        {
            BattleCommandResult result =
                new ObserveBattleCommand()
                    .Execute(
                        null,
                        CreatePlayer(),
                        null);

            Assert.IsFalse(
                result.Accepted);
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                10,
                5,
                4,
                2,
                80,
                5,
                0);
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
                5,
                3,
                1,
                80,
                5,
                0);
        }
    }
}
