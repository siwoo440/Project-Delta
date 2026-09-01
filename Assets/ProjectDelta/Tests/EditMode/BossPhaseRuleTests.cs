using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BossPhaseRuleTests
    {
        [Test]
        public void GetCurrentPhase_SinglePhase_AlwaysReturnsOne()
        {
            Assert.AreEqual(
                1,
                BossPhaseRule.GetCurrentPhase(
                    100,
                    100,
                    1));

            Assert.AreEqual(
                1,
                BossPhaseRule.GetCurrentPhase(
                    1,
                    100,
                    1));
        }

        [Test]
        public void GetCurrentPhase_TwoPhase_FullHealth_ReturnsPhaseOne()
        {
            Assert.AreEqual(
                1,
                BossPhaseRule.GetCurrentPhase(
                    100,
                    100,
                    2));
        }

        [Test]
        public void GetCurrentPhase_TwoPhase_AtHalfHealth_ReturnsPhaseTwo()
        {
            Assert.AreEqual(
                2,
                BossPhaseRule.GetCurrentPhase(
                    50,
                    100,
                    2));
        }

        [Test]
        public void GetCurrentPhase_TwoPhase_JustAboveHalf_ReturnsPhaseOne()
        {
            Assert.AreEqual(
                1,
                BossPhaseRule.GetCurrentPhase(
                    51,
                    100,
                    2));
        }

        [Test]
        public void GetCurrentPhase_Dead_ReturnsFinalPhase()
        {
            Assert.AreEqual(
                3,
                BossPhaseRule.GetCurrentPhase(
                    0,
                    100,
                    3));
        }

        [Test]
        public void GetCurrentPhase_ThreePhase_LowHealth_ReturnsFinalPhase()
        {
            Assert.AreEqual(
                3,
                BossPhaseRule.GetCurrentPhase(
                    5,
                    100,
                    3));
        }

        [Test]
        public void GetCurrentPhase_ZeroMaxHp_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => BossPhaseRule.GetCurrentPhase(
                    0,
                    0,
                    2));
        }
    }
}
