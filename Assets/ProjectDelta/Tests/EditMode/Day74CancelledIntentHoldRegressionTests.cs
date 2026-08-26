using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class Day74CancelledIntentHoldRegressionTests
    {
        [SetUp]
        public void SetUp()
        {
            BattleIntentService.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            BattleIntentService.Clear();
        }

        [Test]
        public void CancelledIntentBlocksReplacementUntilCancelledTurnIsConsumed()
        {
            BattleIntent skillIntent =
                new BattleIntent(
                    "ENEMY",
                    "PLAYER",
                    "Skill",
                    "강공격",
                    BattleIntentIconType.Attack,
                    "SKILL_MON_HEAVY_ATTACK",
                    true);

            Assert.That(
                BattleIntentService.TryRegister(
                    skillIntent),
                Is.True);

            Assert.That(
                BattleIntentService.Cancel(
                    "ENEMY",
                    BattleIntentCancelReason.Silenced),
                Is.True);

            Assert.That(
                BattleIntentService.HasPendingCancellation(
                    "ENEMY"),
                Is.True);

            BattleIntent replacementAttack =
                new BattleIntent(
                    "ENEMY",
                    "PLAYER",
                    "Attack",
                    "공격",
                    BattleIntentIconType.Attack);

            Assert.That(
                BattleIntentService.TryRegister(
                    replacementAttack),
                Is.False);

            Assert.That(
                BattleIntentService.TryConsumeCancellation(
                    "ENEMY",
                    out BattleIntentCancelReason consumedReason),
                Is.True);

            Assert.That(
                consumedReason,
                Is.EqualTo(BattleIntentCancelReason.Silenced));

            Assert.That(
                BattleIntentService.HasPendingCancellation(
                    "ENEMY"),
                Is.False);

            Assert.That(
                BattleIntentService.TryRegister(
                    replacementAttack),
                Is.True);
        }

        [Test]
        public void PendingCancellationReasonRemainsAvailableForHudUntilTurnConsumption()
        {
            BattleIntent skillIntent =
                new BattleIntent(
                    "ENEMY",
                    "PLAYER",
                    "Skill",
                    "강공격",
                    BattleIntentIconType.Attack,
                    "SKILL_MON_HEAVY_ATTACK",
                    true);

            BattleIntentService.TryRegister(
                skillIntent);

            BattleIntentService.Cancel(
                "ENEMY",
                BattleIntentCancelReason.Silenced);

            Assert.That(
                BattleIntentService.GetLastCancelReason(
                    "ENEMY"),
                Is.EqualTo(BattleIntentCancelReason.Silenced));

            Assert.That(
                BattleIntentService.GetLastCancelReason(
                    "ENEMY"),
                Is.EqualTo(BattleIntentCancelReason.Silenced));

            BattleIntentService.TryConsumeCancellation(
                "ENEMY",
                out _);

            Assert.That(
                BattleIntentService.GetLastCancelReason(
                    "ENEMY"),
                Is.EqualTo(BattleIntentCancelReason.None));
        }
    }
}
