using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 114일차: 보물사냥꾼 NPC의 저주 제거·희생 서비스를 검증한다.
    public sealed class NpcRelicServiceTests
    {
        private static PlayerRunState CreatePlayerWithGold(
            int gold)
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold = gold;

            return player;
        }

        [Test]
        public void RemoveCursedRelic_EnoughGold_RemovesRelicAndSpendsGold()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_CURSED",
                "저주받은 반지",
                true);

            PlayerRunState player =
                CreatePlayerWithGold(
                    50);

            NpcServiceActionResult result =
                NpcRelicService.RemoveCursedRelic(
                    relics,
                    player,
                    "RELIC_CURSED",
                    20);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                player.Gold,
                Is.EqualTo(30));

            Assert.That(
                relics.HasRelic(
                    "RELIC_CURSED"),
                Is.False);
        }

        [Test]
        public void RemoveCursedRelic_NotCursed_Fails()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_NORMAL",
                "평범한 반지",
                false);

            PlayerRunState player =
                CreatePlayerWithGold(
                    50);

            NpcServiceActionResult result =
                NpcRelicService.RemoveCursedRelic(
                    relics,
                    player,
                    "RELIC_NORMAL",
                    20);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.RelicNotCursed));

            Assert.That(
                relics.HasRelic(
                    "RELIC_NORMAL"),
                Is.True);
        }

        [Test]
        public void RemoveCursedRelic_NotEnoughGold_Fails()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_CURSED",
                "저주받은 반지",
                true);

            PlayerRunState player =
                CreatePlayerWithGold(
                    5);

            NpcServiceActionResult result =
                NpcRelicService.RemoveCursedRelic(
                    relics,
                    player,
                    "RELIC_CURSED",
                    20);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.NotEnoughGold));

            Assert.That(
                relics.HasRelic(
                    "RELIC_CURSED"),
                Is.True);
        }

        [Test]
        public void RemoveCursedRelic_UnknownRelic_FailsWithRelicNotFound()
        {
            RelicRunState relics =
                new RelicRunState();

            PlayerRunState player =
                CreatePlayerWithGold(
                    50);

            Assert.That(
                NpcRelicService.RemoveCursedRelic(
                    relics,
                    player,
                    "RELIC_MISSING",
                    20).FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.RelicNotFound));
        }

        [Test]
        public void SacrificeRelic_RemovesRelicAndGrantsGold()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_ANY",
                "평범한 유물",
                false);

            PlayerRunState player =
                CreatePlayerWithGold(
                    10);

            NpcServiceActionResult result =
                NpcRelicService.SacrificeRelic(
                    relics,
                    player,
                    "RELIC_ANY",
                    10);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.GoldChange,
                Is.EqualTo(10));

            Assert.That(
                player.Gold,
                Is.EqualTo(20));

            Assert.That(
                relics.HasRelic(
                    "RELIC_ANY"),
                Is.False);
        }

        [Test]
        public void SacrificeRelic_UnknownRelic_FailsWithRelicNotFound()
        {
            RelicRunState relics =
                new RelicRunState();

            PlayerRunState player =
                CreatePlayerWithGold(
                    10);

            Assert.That(
                NpcRelicService.SacrificeRelic(
                    relics,
                    player,
                    "RELIC_MISSING",
                    10).FailureReason,
                Is.EqualTo(
                    NpcServiceFailureReason.RelicNotFound));
        }
    }
}
