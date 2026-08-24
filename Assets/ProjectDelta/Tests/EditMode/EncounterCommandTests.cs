using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EncounterCommandTests
    {
        [Test]
        public void BattleCommand_ExposesStableIdAndDisplayName()
        {
            IEncounterCommand command =
                new BattleEncounterCommand();

            Assert.AreEqual(
                "Battle",
                command.Id);

            Assert.AreEqual(
                "전투",
                command.DisplayName);
        }

        [Test]
        public void EscapeCommand_ExposesStableIdAndDisplayName()
        {
            IEncounterCommand command =
                new EscapeEncounterCommand();

            Assert.AreEqual(
                "Escape",
                command.Id);

            Assert.AreEqual(
                "회피",
                command.DisplayName);
        }

        [Test]
        public void BattleCommand_WithContext_ReturnsAcceptedResult()
        {
            EncounterContext context =
                CreateContext();

            EncounterCommandResult result =
                new BattleEncounterCommand()
                    .Execute(context);

            Assert.IsTrue(
                result.Accepted);

            Assert.AreEqual(
                "Battle",
                result.CommandId);

            StringAssert.Contains(
                "MON_TEST",
                result.Message);
        }

        [Test]
        public void EscapeCommand_WithContext_ReturnsAcceptedResult()
        {
            EncounterContext context =
                CreateContext();

            EncounterCommandResult result =
                new EscapeEncounterCommand()
                    .Execute(context);

            Assert.IsTrue(
                result.Accepted);

            Assert.AreEqual(
                "Escape",
                result.CommandId);

            StringAssert.Contains(
                "MON_TEST",
                result.Message);
        }

        [Test]
        public void BattleCommand_WithoutContext_ReturnsRejectedResult()
        {
            EncounterCommandResult result =
                new BattleEncounterCommand()
                    .Execute(null);

            Assert.IsFalse(
                result.Accepted);

            Assert.AreEqual(
                "Battle",
                result.CommandId);
        }

        [Test]
        public void EscapeCommand_WithoutContext_ReturnsRejectedResult()
        {
            EncounterCommandResult result =
                new EscapeEncounterCommand()
                    .Execute(null);

            Assert.IsFalse(
                result.Accepted);

            Assert.AreEqual(
                "Escape",
                result.CommandId);
        }

        private static EncounterContext CreateContext()
        {
            return new EncounterContext(
                "ROOM_A",
                "MON_TEST",
                new GridPosition(1, 0));
        }
    }
}
