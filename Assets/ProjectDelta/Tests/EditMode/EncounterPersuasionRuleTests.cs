using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EncounterPersuasionRuleTests
    {
        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int fixedRoll;

            public FixedRandomSource(
                int fixedRoll)
            {
                this.fixedRoll =
                    fixedRoll;
            }

            public int NextInt(
                int minInclusive,
                int maxExclusive)
            {
                return fixedRoll;
            }
        }

        [Test]
        public void CalculateSuccessPercent_EqualStats_ReturnsBaseValue()
        {
            int percent =
                EncounterPersuasionRule.CalculateSuccessPercent(
                    50,
                    30,
                    30);

            Assert.AreEqual(
                50,
                percent);
        }

        [Test]
        public void CalculateSuccessPercent_HigherCharm_IncreasesSuccessPercent()
        {
            int percent =
                EncounterPersuasionRule.CalculateSuccessPercent(
                    50,
                    60,
                    30);

            Assert.AreEqual(
                80,
                percent);
        }

        [Test]
        public void CalculateSuccessPercent_ClampsToMinimum()
        {
            int percent =
                EncounterPersuasionRule.CalculateSuccessPercent(
                    50,
                    0,
                    1000);

            Assert.AreEqual(
                EncounterPersuasionRule.MinSuccessPercent,
                percent);
        }

        [Test]
        public void CalculateSuccessPercent_ClampsToMaximum()
        {
            int percent =
                EncounterPersuasionRule.CalculateSuccessPercent(
                    50,
                    1000,
                    0);

            Assert.AreEqual(
                EncounterPersuasionRule.MaxSuccessPercent,
                percent);
        }

        [Test]
        public void TryEvaluate_RollBelowSuccessPercent_ReturnsTrue()
        {
            bool success =
                EncounterPersuasionRule.TryEvaluate(
                    50,
                    50,
                    30,
                    new FixedRandomSource(0),
                    out int successPercent);

            Assert.IsTrue(
                success);

            Assert.AreEqual(
                70,
                successPercent);
        }

        [Test]
        public void TryEvaluate_RollAtOrAboveSuccessPercent_ReturnsFalse()
        {
            bool success =
                EncounterPersuasionRule.TryEvaluate(
                    50,
                    30,
                    30,
                    new FixedRandomSource(50),
                    out int successPercent);

            Assert.IsFalse(
                success);

            Assert.AreEqual(
                50,
                successPercent);
        }

        [Test]
        public void TryEvaluate_NullRandomSource_ReturnsFalse()
        {
            bool success =
                EncounterPersuasionRule.TryEvaluate(
                    50,
                    30,
                    30,
                    null,
                    out int successPercent);

            Assert.IsFalse(
                success);
        }
    }
}
