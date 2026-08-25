using System;
using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleIntentServiceTests
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
        public void IntentIconTypeHasSevenValues()
        {
            Array values =
                Enum.GetValues(
                    typeof(BattleIntentIconType));

            Assert.That(
                values.Length,
                Is.EqualTo(7));
        }

        [Test]
        public void RegisteredIntentIsLockedUntilConsumedOrCancelled()
        {
            BattleIntent first =
                CreateIntent(
                    "ENEMY_A",
                    false);

            BattleIntent second =
                new BattleIntent(
                    "ENEMY_A",
                    "PLAYER",
                    "Defend",
                    "방어",
                    BattleIntentIconType.Defend);

            Assert.That(
                BattleIntentService.TryRegister(
                    first),
                Is.True);

            Assert.That(
                BattleIntentService.TryRegister(
                    second),
                Is.False);

            Assert.That(
                BattleIntentService.TryGet(
                    "ENEMY_A",
                    out BattleIntent stored),
                Is.True);

            Assert.That(
                stored.CommandId,
                Is.EqualTo("Attack"));
        }

        [Test]
        public void ConsumedIntentIsRemoved()
        {
            BattleIntentService.TryRegister(
                CreateIntent(
                    "ENEMY_A",
                    false));

            Assert.That(
                BattleIntentService.TryConsume(
                    "ENEMY_A",
                    out BattleIntent consumed),
                Is.True);

            Assert.That(
                consumed.DisplayName,
                Is.EqualTo("공격"));

            Assert.That(
                BattleIntentService.TryGet(
                    "ENEMY_A",
                    out _),
                Is.False);
        }

        [TestCase(false, false, false, false, true, BattleIntentCancelReason.ActorDefeated)]
        [TestCase(true, true, false, false, true, BattleIntentCancelReason.Stunned)]
        [TestCase(true, false, true, false, true, BattleIntentCancelReason.Silenced)]
        [TestCase(true, false, false, true, true, BattleIntentCancelReason.Satisfied)]
        [TestCase(true, false, false, false, false, BattleIntentCancelReason.TargetUnavailable)]
        public void ExactFiveCancelConditionsAreEvaluated(
            bool actorAlive,
            bool isStunned,
            bool isSilenced,
            bool isSatisfied,
            bool targetAvailable,
            BattleIntentCancelReason expected)
        {
            BattleIntent intent =
                CreateIntent(
                    "ENEMY_A",
                    true);

            BattleIntentCancelReason result =
                BattleIntentService.EvaluateCancelReason(
                    intent,
                    actorAlive,
                    isStunned,
                    isSilenced,
                    isSatisfied,
                    targetAvailable);

            Assert.That(
                result,
                Is.EqualTo(expected));
        }

        [Test]
        public void SilenceDoesNotCancelNonSilenceSensitiveAttack()
        {
            BattleIntent intent =
                CreateIntent(
                    "ENEMY_A",
                    false);

            BattleIntentCancelReason result =
                BattleIntentService.EvaluateCancelReason(
                    intent,
                    true,
                    false,
                    true,
                    false,
                    true);

            Assert.That(
                result,
                Is.EqualTo(BattleIntentCancelReason.None));
        }

        [Test]
        public void CancelStoresReasonAndRemovesIntent()
        {
            BattleIntentService.TryRegister(
                CreateIntent(
                    "ENEMY_A",
                    false));

            Assert.That(
                BattleIntentService.Cancel(
                    "ENEMY_A",
                    BattleIntentCancelReason.Stunned),
                Is.True);

            Assert.That(
                BattleIntentService.GetLastCancelReason(
                    "ENEMY_A"),
                Is.EqualTo(BattleIntentCancelReason.Stunned));

            Assert.That(
                BattleIntentService.TryGet(
                    "ENEMY_A",
                    out _),
                Is.False);
        }

        private static BattleIntent CreateIntent(
            string actorId,
            bool silenceSensitive)
        {
            return new BattleIntent(
                actorId,
                "PLAYER",
                "Attack",
                "공격",
                BattleIntentIconType.Attack,
                null,
                silenceSensitive);
        }
    }
}
