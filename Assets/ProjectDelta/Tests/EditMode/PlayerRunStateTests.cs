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

        // 99일차: EquipmentBonuses가 GetFinalStats()에 더해지는지 확인한다.
        [Test]
        public void GetFinalStats_IncludesEquipmentBonuses()
        {
            PlayerRunState state =
                PlayerRunState.CreateDefault();

            state.EquipmentBonuses =
                new StatBlock
                {
                    Attack = 15,
                    MaxHealth = 20
                };

            StatBlock finalStats =
                state.GetFinalStats();

            Assert.AreEqual(
                65,
                finalStats.Attack);

            Assert.AreEqual(
                120,
                finalStats.MaxHealth);
        }

        // 99일차: 장비 해제로 최대 체력이 줄어들면 현재 체력도 함께 줄어야 한다.
        [Test]
        public void ClampCurrentResourcesToFinalStats_ReducesCurrentHpAboveNewMax()
        {
            PlayerRunState state =
                PlayerRunState.CreateDefault();

            state.EquipmentBonuses =
                new StatBlock
                {
                    MaxHealth = 50
                };

            state.CurrentHp =
                state.GetFinalStats().MaxHealth;

            state.EquipmentBonuses =
                new StatBlock();

            state.ClampCurrentResourcesToFinalStats();

            Assert.AreEqual(
                100,
                state.CurrentHp);
        }

        // 99일차: 현재 자원이 최대치 이하이면 그대로 유지되어야 한다 (자동 회복 아님).
        [Test]
        public void ClampCurrentResourcesToFinalStats_DoesNotHealWhenBelowMax()
        {
            PlayerRunState state =
                PlayerRunState.CreateDefault();

            state.CurrentHp =
                40;

            state.EquipmentBonuses =
                new StatBlock
                {
                    MaxHealth = 50
                };

            state.ClampCurrentResourcesToFinalStats();

            Assert.AreEqual(
                40,
                state.CurrentHp);
        }
    }
}
