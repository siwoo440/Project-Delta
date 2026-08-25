using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleRewardStateTests
    {
        [SetUp]
        public void SetUp()
        {
            BattleRewardState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            BattleRewardState.Clear();
        }

        [Test]
        public void BeginDefaultRewardsCreatesThreeChoices()
        {
            BattleRewardState.BeginDefaultRewards();

            Assert.That(BattleRewardState.IsPending, Is.True);
            Assert.That(BattleRewardState.CurrentOptions.Count, Is.EqualTo(3));
            Assert.That(BattleRewardState.CurrentOptions[0].Id, Is.EqualTo("REWARD_GOLD_100"));
            Assert.That(BattleRewardState.CurrentOptions[1].Id, Is.EqualTo("REWARD_HEAL_10"));
            Assert.That(BattleRewardState.CurrentOptions[2].Id, Is.EqualTo("REWARD_MANA_5"));
        }

        [Test]
        public void ClaimGoldAddsGoldAndEndsSelection()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            BattleRewardState.BeginDefaultRewards();

            bool claimed =
                BattleRewardState.TryClaim(
                    "REWARD_GOLD_100",
                    player);

            Assert.That(claimed, Is.True);
            Assert.That(player.Gold, Is.EqualTo(100));
            Assert.That(BattleRewardState.IsPending, Is.False);
            Assert.That(BattleRewardState.LastClaimedRewardId, Is.EqualTo("REWARD_GOLD_100"));
        }

        [Test]
        public void ClaimHealthRestoresWithoutExceedingMaximum()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                95;

            BattleRewardState.BeginDefaultRewards();

            bool claimed =
                BattleRewardState.TryClaim(
                    "REWARD_HEAL_10",
                    player);

            Assert.That(claimed, Is.True);
            Assert.That(player.CurrentHp, Is.EqualTo(100));
        }

        [Test]
        public void ClaimManaRestoresRequestedAmount()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentMana =
                20;

            BattleRewardState.BeginDefaultRewards();

            bool claimed =
                BattleRewardState.TryClaim(
                    "REWARD_MANA_5",
                    player);

            Assert.That(claimed, Is.True);
            Assert.That(player.CurrentMana, Is.EqualTo(25));
        }

        [Test]
        public void SecondClaimIsRejected()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            BattleRewardState.BeginDefaultRewards();

            bool firstClaim =
                BattleRewardState.TryClaim(
                    "REWARD_GOLD_100",
                    player);

            bool secondClaim =
                BattleRewardState.TryClaim(
                    "REWARD_HEAL_10",
                    player);

            Assert.That(firstClaim, Is.True);
            Assert.That(secondClaim, Is.False);
            Assert.That(player.Gold, Is.EqualTo(100));
            Assert.That(player.CurrentHp, Is.EqualTo(100));
        }
    }
}
