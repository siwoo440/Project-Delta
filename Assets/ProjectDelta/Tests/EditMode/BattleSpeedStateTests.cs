using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleSpeedStateTests
    {
        [SetUp]
        public void SetUp()
        {
            BattleSpeedState.ResetToNormal();
        }

        [TearDown]
        public void TearDown()
        {
            BattleSpeedState.ResetToNormal();
        }

        [Test]
        public void ResetToNormal_SetsOneTimesSpeed()
        {
            BattleSpeedState.Toggle();

            BattleSpeedState.ResetToNormal();

            Assert.That(
                BattleSpeedState.CurrentMultiplier,
                Is.EqualTo(1f));

            Assert.That(
                BattleSpeedState.DisplayLabel,
                Is.EqualTo("1×"));
        }

        [Test]
        public void Toggle_FromNormal_SetsTwoTimesSpeed()
        {
            BattleSpeedState.Toggle();

            Assert.That(
                BattleSpeedState.CurrentMultiplier,
                Is.EqualTo(2f));

            Assert.That(
                BattleSpeedState.DisplayLabel,
                Is.EqualTo("2×"));
        }

        [Test]
        public void Toggle_Twice_ReturnsToNormalSpeed()
        {
            BattleSpeedState.Toggle();
            BattleSpeedState.Toggle();

            Assert.That(
                BattleSpeedState.CurrentMultiplier,
                Is.EqualTo(1f));
        }

        [Test]
        public void ScaleDuration_AtNormal_ReturnsBaseDuration()
        {
            float scaled =
                BattleSpeedState.ScaleDuration(
                    0.45f);

            Assert.That(
                scaled,
                Is.EqualTo(0.45f).Within(0.0001f));
        }

        [Test]
        public void ScaleDuration_AtFast_ReturnsHalfDuration()
        {
            BattleSpeedState.Toggle();

            float scaled =
                BattleSpeedState.ScaleDuration(
                    0.45f);

            Assert.That(
                scaled,
                Is.EqualTo(0.225f).Within(0.0001f));
        }

        [Test]
        public void ScaleDuration_NonPositive_ReturnsZero()
        {
            Assert.That(
                BattleSpeedState.ScaleDuration(
                    0f),
                Is.EqualTo(0f));

            Assert.That(
                BattleSpeedState.ScaleDuration(
                    -1f),
                Is.EqualTo(0f));
        }
    }
}
