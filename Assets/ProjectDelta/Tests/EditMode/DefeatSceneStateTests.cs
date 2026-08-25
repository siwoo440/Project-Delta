using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class DefeatSceneStateTests
    {
        [SetUp]
        public void SetUp()
        {
            DefeatSceneState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            DefeatSceneState.Clear();
        }

        [Test]
        public void CaptureCopiesDefeatRecordAndFloor()
        {
            BattleDefeatRecord record =
                new BattleDefeatRecord(
                    BattleDefeatReason.EnemyAttack,
                    "enemy-instance-1",
                    "MON_TEST",
                    4);

            RunDefeatSummary summary =
                DefeatSceneState.Capture(
                    record,
                    3);

            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.Reason, Is.EqualTo(BattleDefeatReason.EnemyAttack));
            Assert.That(summary.AttackerInstanceId, Is.EqualTo("enemy-instance-1"));
            Assert.That(summary.AttackerDefinitionId, Is.EqualTo("MON_TEST"));
            Assert.That(summary.RoundNumber, Is.EqualTo(4));
            Assert.That(summary.FloorNumber, Is.EqualTo(3));
        }

        [Test]
        public void CaptureSurrenderKeepsAttackerEmpty()
        {
            BattleDefeatRecord record =
                new BattleDefeatRecord(
                    BattleDefeatReason.Surrender,
                    null,
                    null,
                    2);

            RunDefeatSummary summary =
                DefeatSceneState.Capture(
                    record,
                    5);

            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.Reason, Is.EqualTo(BattleDefeatReason.Surrender));
            Assert.That(summary.HasAttacker, Is.False);
            Assert.That(summary.FloorNumber, Is.EqualTo(5));
        }

        [Test]
        public void ClearRemovesCurrentSummary()
        {
            BattleDefeatRecord record =
                new BattleDefeatRecord(
                    BattleDefeatReason.EnemyAttack,
                    "enemy-instance-1",
                    "MON_TEST",
                    1);

            DefeatSceneState.Capture(
                record,
                1);

            DefeatSceneState.Clear();

            Assert.That(DefeatSceneState.Current, Is.Null);
        }
    }
}
