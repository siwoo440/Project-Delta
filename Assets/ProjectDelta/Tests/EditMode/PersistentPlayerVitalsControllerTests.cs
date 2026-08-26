using NUnit.Framework;
using ProjectDelta.Presentation;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class PersistentPlayerVitalsControllerTests
    {
        [Test]
        public void ActionButtons_ShowOnlyDuringBattle()
        {
            Assert.That(
                PersistentPlayerVitalsController.ShouldShowActionButtons(
                    false),
                Is.False);

            Assert.That(
                PersistentPlayerVitalsController.ShouldShowActionButtons(
                    true),
                Is.True);
        }

        [Test]
        public void StaminaLabel_UsesKoreanText()
        {
            Assert.That(
                PersistentPlayerVitalsController.FormatVital(
                    "정력",
                    100,
                    100),
                Is.EqualTo(
                    "정력  100 / 100"));
        }

        [Test]
        public void VitalRatio_ClampsToZeroAndOne()
        {
            Assert.That(
                PersistentPlayerVitalsController.CalculateRatio(
                    -10,
                    100),
                Is.EqualTo(
                    0f));

            Assert.That(
                PersistentPlayerVitalsController.CalculateRatio(
                    200,
                    100),
                Is.EqualTo(
                    1f));
        }
    }
}
