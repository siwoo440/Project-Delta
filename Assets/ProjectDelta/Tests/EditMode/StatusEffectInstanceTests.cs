using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // StatusEffectInstance 사용
using ProjectDelta.Data; // StatusEffectKind 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class StatusEffectInstanceTests
    {
        [Test]
        public void Constructor_StoresAllFields()
        {
            StatusEffectInstance instance =
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    3,
                    2,
                    -5,
                    StatusEffectKind.DamageOverTime);

            Assert.AreEqual(
                "STATUS_POISON",
                instance.DefinitionId);

            Assert.AreEqual(
                "MON_TEST",
                instance.SourceInstanceId);

            Assert.AreEqual(
                3,
                instance.RemainingRounds);

            Assert.AreEqual(
                2,
                instance.StackCount);

            Assert.AreEqual(
                -5,
                instance.AppliedValue);
        }

        [Test]
        public void IsExpired_FalseWhileRoundsRemain()
        {
            StatusEffectInstance instance =
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    1,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            Assert.IsFalse(
                instance.IsExpired);
        }

        [Test]
        public void IsExpired_TrueWhenZeroRoundsRemain()
        {
            StatusEffectInstance instance =
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    0,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            Assert.IsTrue(
                instance.IsExpired);
        }

        [Test]
        public void DecrementRemainingRounds_ReducesByOne()
        {
            StatusEffectInstance instance =
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            instance.DecrementRemainingRounds();

            Assert.AreEqual(
                1,
                instance.RemainingRounds);

            Assert.IsFalse(
                instance.IsExpired);

            instance.DecrementRemainingRounds();

            Assert.AreEqual(
                0,
                instance.RemainingRounds);

            Assert.IsTrue(
                instance.IsExpired);
        }

        [Test]
        public void DecrementRemainingRounds_NeverGoesBelowZero()
        {
            StatusEffectInstance instance =
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    0,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            instance.DecrementRemainingRounds();

            Assert.AreEqual(
                0,
                instance.RemainingRounds);
        }
    }
}
