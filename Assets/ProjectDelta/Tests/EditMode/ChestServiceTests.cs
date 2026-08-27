using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 106일차: 잠긴 상자 열쇠 개방, 강제 개방(1회 제한), 미믹 판정 확정,
    // 보상 지급 확정(중복 방지) 규칙을 검증한다.
    public sealed class ChestServiceTests
    {
        [Test]
        public void UnlockWithKey_HasKeys_ConsumesOneKeyAndUnlocks()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    true);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.KeyCount =
                3;

            ChestActionResult result =
                ChestService.UnlockWithKey(
                    chest,
                    player);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                chest.IsLocked,
                Is.False);

            Assert.That(
                player.KeyCount,
                Is.EqualTo(2));
        }

        [Test]
        public void UnlockWithKey_NoKeys_FailsWithoutMutation()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    true);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.KeyCount =
                0;

            ChestActionResult result =
                ChestService.UnlockWithKey(
                    chest,
                    player);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.NoKeys));

            Assert.That(
                chest.IsLocked,
                Is.True);
        }

        [Test]
        public void UnlockWithKey_AlreadyUnlocked_FailsWithNotLocked()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    false);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.KeyCount =
                5;

            ChestActionResult result =
                ChestService.UnlockWithKey(
                    chest,
                    player);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.NotLocked));

            Assert.That(
                player.KeyCount,
                Is.EqualTo(5));
        }

        [Test]
        public void ForceOpen_LockedChest_SucceedsAndUnlocks()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Rare,
                    true);

            ChestActionResult result =
                ChestService.ForceOpen(
                    chest);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                chest.IsLocked,
                Is.False);
        }

        [Test]
        public void ForceOpen_SecondAttemptOnSameChest_FailsWithAlreadyForceOpened()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Rare,
                    true);

            ChestService.ForceOpen(
                chest);

            // 첫 시도로 이미 풀린 상태에서도, "다시 시도할 수 있는 것"처럼
            // NotLocked가 아니라 AlreadyForceOpened로 명확히 실패해야 한다.
            ChestActionResult secondAttempt =
                ChestService.ForceOpen(
                    chest);

            Assert.That(
                secondAttempt.Success,
                Is.False);

            Assert.That(
                secondAttempt.FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.AlreadyForceOpened));
        }

        [Test]
        public void ResolveMimic_FirstCall_StoresResult()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    false);

            ChestActionResult result =
                ChestService.ResolveMimic(
                    chest,
                    true);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                chest.MimicResolved,
                Is.True);

            Assert.That(
                chest.IsMimic,
                Is.True);
        }

        [Test]
        public void ResolveMimic_SecondCall_FailsAndKeepsFirstResult()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    false);

            ChestService.ResolveMimic(
                chest,
                false);

            ChestActionResult secondCall =
                ChestService.ResolveMimic(
                    chest,
                    true);

            Assert.That(
                secondCall.Success,
                Is.False);

            Assert.That(
                secondCall.FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.AlreadyMimicResolved));

            // 첫 판정 결과(false)가 그대로 유지되어야 한다.
            Assert.That(
                chest.IsMimic,
                Is.False);
        }

        [Test]
        public void GrantReward_FirstCall_Succeeds()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    false);

            ChestActionResult result =
                ChestService.GrantReward(
                    chest);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                chest.RewardGranted,
                Is.True);
        }

        [Test]
        public void GrantReward_SecondCall_FailsWithAlreadyRewarded()
        {
            ChestRunState chest =
                new ChestRunState(
                    ChestRarity.Common,
                    false);

            ChestService.GrantReward(
                chest);

            ChestActionResult secondCall =
                ChestService.GrantReward(
                    chest);

            Assert.That(
                secondCall.Success,
                Is.False);

            Assert.That(
                secondCall.FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.AlreadyRewarded));
        }

        [Test]
        public void AllMethods_NullChest_FailWithInvalidState()
        {
            Assert.That(
                ChestService.UnlockWithKey(
                    null,
                    PlayerRunState.CreateDefault()).FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.InvalidState));

            Assert.That(
                ChestService.ForceOpen(
                    null).FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.InvalidState));

            Assert.That(
                ChestService.ResolveMimic(
                    null,
                    true).FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.InvalidState));

            Assert.That(
                ChestService.GrantReward(
                    null).FailureReason,
                Is.EqualTo(
                    ChestActionFailureReason.InvalidState));
        }
    }
}
