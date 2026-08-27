using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 104일차: 유물 보유 목록의 최대 개수·중복 금지·복원 규칙을 검증한다.
    // AddRelic은 internal이라 RelicService.Acquire(공개 API)를 통해서만 채운다 -
    // 실제 게임 코드도 같은 경로로만 유물을 획득한다.
    public sealed class RelicRunStateTests
    {
        [Test]
        public void DefaultMaxCapacity_IsFive()
        {
            RelicRunState relics =
                new RelicRunState();

            Assert.That(
                relics.MaxCapacity,
                Is.EqualTo(5));
        }

        [Test]
        public void Acquire_UpToCapacity_FillsAllSlots()
        {
            RelicRunState relics =
                new RelicRunState();

            for (int index = 0;
                 index < RelicRunState.DefaultMaxCapacity;
                 index++)
            {
                RelicAcquisitionResult result =
                    RelicService.Acquire(
                        relics,
                        $"RELIC_{index}",
                        $"유물 {index}",
                        false);

                Assert.That(
                    result.Success,
                    Is.True);
            }

            Assert.That(
                relics.IsFull,
                Is.True);
        }

        [Test]
        public void HasRelic_ReturnsTrueOnlyForOwnedId()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_A",
                "유물 A",
                false);

            Assert.That(
                relics.HasRelic(
                    "RELIC_A"),
                Is.True);

            Assert.That(
                relics.HasRelic(
                    "RELIC_B"),
                Is.False);
        }

        [Test]
        public void SetMaxCapacity_ClampsToAtLeastOne()
        {
            RelicRunState relics =
                new RelicRunState();

            relics.SetMaxCapacity(
                0);

            Assert.That(
                relics.MaxCapacity,
                Is.EqualTo(1));

            relics.SetMaxCapacity(
                8);

            Assert.That(
                relics.MaxCapacity,
                Is.EqualTo(8));
        }

        [Test]
        public void RestoreFrom_SkipsDuplicatesAndOverCapacityEntries()
        {
            RelicRunState relics =
                new RelicRunState();

            List<RelicInstanceState> restored =
                new List<RelicInstanceState>
                {
                    new RelicInstanceState("RELIC_A", "A", false),
                    new RelicInstanceState("RELIC_A", "A 중복", false),
                    new RelicInstanceState("RELIC_B", "B", true),
                    new RelicInstanceState("RELIC_C", "C", false),
                    new RelicInstanceState("RELIC_D", "D", false),
                    new RelicInstanceState("RELIC_E", "E", false),
                    new RelicInstanceState("RELIC_F", "F", false)
                };

            relics.RestoreFrom(
                restored);

            Assert.That(
                relics.Relics.Count,
                Is.EqualTo(
                    RelicRunState.DefaultMaxCapacity));

            Assert.That(
                relics.HasRelic(
                    "RELIC_F"),
                Is.False);
        }

        [Test]
        public void RestoreFrom_NullClearsExistingRelics()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_A",
                "유물 A",
                false);

            relics.RestoreFrom(
                null);

            Assert.That(
                relics.Relics.Count,
                Is.EqualTo(0));
        }
    }
}
