using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EncounterResultResolverTests
    {
        [Test]
        public void TryCreateTestResult_Battle_ReturnsMonsterDefeated()
        {
            EncounterContext context =
                CreateContext();

            bool created =
                EncounterResultResolver.TryCreateTestResult(
                    context,
                    "Battle",
                    out EncounterResult result);

            Assert.IsTrue(
                created);

            Assert.IsNotNull(
                result);

            Assert.AreEqual(
                EncounterOutcome.MonsterDefeated,
                result.Outcome);

            Assert.IsTrue(
                result.CompletesRoom);

            Assert.IsTrue(
                result.RemovesMonster);
        }

        [Test]
        public void TryCreateTestResult_Escape_ReturnsEscapedAndCompletesRoom()
        {
            EncounterContext context =
                CreateContext();

            bool created =
                EncounterResultResolver.TryCreateTestResult(
                    context,
                    "Escape",
                    out EncounterResult result);

            Assert.IsTrue(
                created);

            Assert.IsNotNull(
                result);

            Assert.AreEqual(
                EncounterOutcome.Escaped,
                result.Outcome);

            Assert.IsTrue(
                result.CompletesRoom);

            Assert.IsTrue(
                result.RemovesMonster);
        }

        [Test]
        public void TryCreateTestResult_MissingContext_ReturnsFalse()
        {
            Assert.IsFalse(
                EncounterResultResolver.TryCreateTestResult(
                    null,
                    "Battle",
                    out EncounterResult result));

            Assert.IsNull(
                result);
        }

        [Test]
        public void TryCreateTestResult_UnknownCommand_ReturnsFalse()
        {
            Assert.IsFalse(
                EncounterResultResolver.TryCreateTestResult(
                    CreateContext(),
                    "Unknown",
                    out EncounterResult result));

            Assert.IsNull(
                result);
        }

        private static EncounterContext CreateContext()
        {
            return new EncounterContext(
                "ROOM_A",
                "MON_TEST",
                GridPosition.Zero);
        }
    }
}
