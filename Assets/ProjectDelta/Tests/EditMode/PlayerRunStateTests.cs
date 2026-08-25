using NUnit.Framework; // NUnit 테스트 기능
using ProjectDelta.Domain; // PlayerRunState 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class PlayerRunStateTests
    {
        // 54일차: 기획서 6.1 기본 능력치 표와 일치하는지 확인한다.
        [Test]
        public void CreateDefault_MatchesPlanningDoc61BaseStats()
        {
            PlayerRunState state =
                PlayerRunState.CreateDefault();

            StatBlock finalStats =
                state.GetFinalStats();

            Assert.AreEqual(
                100,
                finalStats.MaxHealth);

            Assert.AreEqual(
                50,
                finalStats.MaxMana);

            Assert.AreEqual(
                100,
                finalStats.MaxStamina);

            Assert.AreEqual(
                50,
                finalStats.Attack);

            Assert.AreEqual(
                40,
                finalStats.Defense);

            Assert.AreEqual(
                50,
                finalStats.Speed);

            Assert.AreEqual(
                50,
                finalStats.Charm);

            Assert.AreEqual(
                40,
                finalStats.Evasion);

            Assert.AreEqual(
                50,
                finalStats.Resistance);
        }

        [Test]
        public void CreateDefault_CurrentResourcesStartAtMax()
        {
            PlayerRunState state =
                PlayerRunState.CreateDefault();

            Assert.AreEqual(
                100,
                state.CurrentHp);

            Assert.AreEqual(
                50,
                state.CurrentMana);

            Assert.AreEqual(
                100,
                state.CurrentStamina);
        }

        [Test]
        public void CreateDefault_StartsAtLevelOne()
        {
            PlayerRunState state =
                PlayerRunState.CreateDefault();

            Assert.AreEqual(
                1,
                state.Level);
        }
    }
}
