using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 105일차: 골드 획득·소비 공통 API를 검증한다.
    public sealed class GoldServiceTests
    {
        [Test]
        public void Earn_AddsAmountAndReturnsActualIncrease()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                10;

            int earned =
                GoldService.Earn(
                    player,
                    34);

            Assert.That(
                earned,
                Is.EqualTo(34));

            Assert.That(
                player.Gold,
                Is.EqualTo(44));
        }

        [Test]
        public void Earn_SaturatesAtIntMaxValue()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                int.MaxValue - 5;

            GoldService.Earn(
                player,
                100);

            Assert.That(
                player.Gold,
                Is.EqualTo(
                    int.MaxValue));
        }

        [Test]
        public void Earn_NullPlayer_ReturnsZero()
        {
            Assert.That(
                GoldService.Earn(
                    null,
                    10),
                Is.EqualTo(0));
        }

        [Test]
        public void Earn_NonPositiveAmount_DoesNothing()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                50;

            Assert.That(
                GoldService.Earn(
                    player,
                    0),
                Is.EqualTo(0));

            Assert.That(
                GoldService.Earn(
                    player,
                    -10),
                Is.EqualTo(0));

            Assert.That(
                player.Gold,
                Is.EqualTo(50));
        }

        [Test]
        public void TrySpend_SufficientGold_DeductsAndSucceeds()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                100;

            bool spent =
                GoldService.TrySpend(
                    player,
                    30);

            Assert.That(
                spent,
                Is.True);

            Assert.That(
                player.Gold,
                Is.EqualTo(70));
        }

        [Test]
        public void TrySpend_InsufficientGold_FailsWithoutMutation()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                10;

            bool spent =
                GoldService.TrySpend(
                    player,
                    30);

            Assert.That(
                spent,
                Is.False);

            Assert.That(
                player.Gold,
                Is.EqualTo(10));
        }

        [Test]
        public void TrySpend_ZeroAmount_SucceedsAsNoOp()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                10;

            Assert.That(
                GoldService.TrySpend(
                    player,
                    0),
                Is.True);

            Assert.That(
                player.Gold,
                Is.EqualTo(10));
        }

        [Test]
        public void TrySpend_NegativeAmountOrNullPlayer_Fails()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            Assert.That(
                GoldService.TrySpend(
                    player,
                    -1),
                Is.False);

            Assert.That(
                GoldService.TrySpend(
                    null,
                    10),
                Is.False);
        }
    }
}
