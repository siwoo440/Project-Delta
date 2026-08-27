using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 104일차: 유물 획득 규칙(중복 금지·최대 보유 수)을 검증한다.
    public sealed class RelicServiceTests
    {
        [Test]
        public void Acquire_NewRelic_Succeeds()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicAcquisitionResult result =
                RelicService.Acquire(
                    relics,
                    "RELIC_SUN",
                    "태양의 파편",
                    false);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.Relic.RelicId,
                Is.EqualTo(
                    "RELIC_SUN"));

            Assert.That(
                relics.HasRelic(
                    "RELIC_SUN"),
                Is.True);
        }

        [Test]
        public void Acquire_DuplicateId_FailsWithAlreadyOwned()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicService.Acquire(
                relics,
                "RELIC_SUN",
                "태양의 파편",
                false);

            RelicAcquisitionResult result =
                RelicService.Acquire(
                    relics,
                    "RELIC_SUN",
                    "태양의 파편",
                    false);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    RelicAcquisitionFailureReason.AlreadyOwned));

            Assert.That(
                relics.Relics.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void Acquire_AtCapacity_FailsWithCapacityFull()
        {
            RelicRunState relics =
                new RelicRunState();

            for (int index = 0;
                 index < RelicRunState.DefaultMaxCapacity;
                 index++)
            {
                RelicService.Acquire(
                    relics,
                    $"RELIC_{index}",
                    $"유물 {index}",
                    false);
            }

            RelicAcquisitionResult result =
                RelicService.Acquire(
                    relics,
                    "RELIC_OVERFLOW",
                    "넘치는 유물",
                    false);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    RelicAcquisitionFailureReason.CapacityFull));
        }

        [Test]
        public void Acquire_NullRelicRunState_FailsWithInvalidState()
        {
            RelicAcquisitionResult result =
                RelicService.Acquire(
                    null,
                    "RELIC_SUN",
                    "태양의 파편",
                    false);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    RelicAcquisitionFailureReason.InvalidState));
        }

        [Test]
        public void Acquire_EmptyRelicId_FailsWithInvalidState()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicAcquisitionResult result =
                RelicService.Acquire(
                    relics,
                    string.Empty,
                    "이름만 있는 유물",
                    false);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    RelicAcquisitionFailureReason.InvalidState));
        }

        // 104일차 저주 유물: 저주 여부가 그대로 저장되어 UI가 항상 공개할 수 있어야 한다.
        [Test]
        public void Acquire_CursedRelic_PreservesIsCursedFlag()
        {
            RelicRunState relics =
                new RelicRunState();

            RelicAcquisitionResult result =
                RelicService.Acquire(
                    relics,
                    "RELIC_DARK",
                    "어둠의 파편",
                    true);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.Relic.IsCursed,
                Is.True);
        }
    }
}
