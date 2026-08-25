using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // FleeBattleCommand 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class FleeBattleCommandTests
    {
        [Test]
        public void Execute_ValidContextAndActor_Accepts()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateParticipant("MON_TEST", BattleTeam.Enemy) });

            FleeBattleCommand command =
                new FleeBattleCommand();

            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    null); // 도주는 대상 선택이 필요 없음

            Assert.IsTrue(
                result.Accepted);

            Assert.AreEqual(
                "Flee",
                command.Id);
        }

        [Test]
        public void Execute_NullContext_Rejects()
        {
            FleeBattleCommand command =
                new FleeBattleCommand();

            BattleCommandResult result =
                command.Execute(
                    null,
                    CreateParticipant("PLAYER", BattleTeam.Player),
                    null);

            Assert.IsFalse(
                result.Accepted);
        }

        [Test]
        public void Execute_NullActor_Rejects()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateParticipant("MON_TEST", BattleTeam.Enemy) });

            FleeBattleCommand command =
                new FleeBattleCommand();

            BattleCommandResult result =
                command.Execute(
                    context,
                    null,
                    null);

            Assert.IsFalse(
                result.Accepted);
        }

        private static BattleParticipant CreateParticipant(
            string instanceId,
            BattleTeam team)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                team,
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
