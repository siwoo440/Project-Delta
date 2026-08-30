using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 114일차: 치료사 NPC 회복 서비스(골드 소비, 전량 회복, 이미 가득 참, 골드 부족)를 검증한다.
    public sealed class NpcHealingServiceTests
    {
        [Test]
        public void Heal_EnoughGold_RestoresToFullAndSpendsGold()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold = 100;
            player.CurrentHp = 1;
            player.CurrentMana = 0;
            player.CurrentStamina = 0;

            NpcServiceActionResult result =
                NpcHealingService.Heal(
                    player,
                    15);

            StatBlock finalStats =
                player.GetFinalStats();

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.GoldChange,
                Is.EqualTo(-15));

            Assert.That(
                player.Gold,
                Is.EqualTo(85));

            Assert.That(
                player.CurrentHp,
                Is.EqualTo(finalStats.MaxHealth));

            Assert.That(
                player.CurrentMana,
                Is.EqualTo(finalStats.MaxMana));

            Assert.That(
                player.CurrentStamina,
                Is.EqualTo(finalStats.MaxStamina));
        }

        [Test]
        public void Heal_NotEnoughGold_Fails()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold = 5;
            player.CurrentHp = 1;

            NpcServiceActionResult result =
                NpcHealingService.Heal(
                    player,
                    15);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.NotEnoughGold));

            Assert.That(
                player.Gold,
                Is.EqualTo(5));
        }

        [Test]
        public void Heal_AlreadyFull_FailsWithoutSpendingGold()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold = 100;

            NpcServiceActionResult result =
                NpcHealingService.Heal(
                    player,
                    15);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.AlreadyFull));

            Assert.That(
                player.Gold,
                Is.EqualTo(100));
        }

        [Test]
        public void Heal_NullPlayer_FailsWithInvalidState()
        {
            Assert.That(
                NpcHealingService.Heal(
                    null,
                    15).FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.InvalidState));
        }
    }
}
