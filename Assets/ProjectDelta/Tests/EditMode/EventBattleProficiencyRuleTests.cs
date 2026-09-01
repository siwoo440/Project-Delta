using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleProficiencyRuleTests
    {
        [Test]
        public void GetMultiplier_Level1_ReturnsOne()
        {
            Assert.AreEqual(
                1f,
                EventBattleProficiencyRule.GetMultiplier(
                    1));
        }

        [Test]
        public void GetMultiplier_MaxLevel_ReturnsOnePointFour()
        {
            Assert.AreEqual(
                1.4f,
                EventBattleProficiencyRule.GetMultiplier(
                    EventBattleProficiencyRule.MaxLevel),
                0.001f);
        }

        [Test]
        public void GetMultiplier_AboveMaxLevel_ClampsToMax()
        {
            Assert.AreEqual(
                EventBattleProficiencyRule.GetMultiplier(
                    EventBattleProficiencyRule.MaxLevel),
                EventBattleProficiencyRule.GetMultiplier(
                    99));
        }

        [Test]
        public void AddExperience_EnoughToLevelUp_IncreasesLevelAndCarriesRemainder()
        {
            EventBattleActionProficiencyRecord record =
                new EventBattleActionProficiencyRecord
                {
                    Level = 1,
                    Experience = 0
                };

            bool leveledUp =
                EventBattleProficiencyRule.AddExperience(
                    record,
                    25);

            Assert.IsTrue(
                leveledUp);

            Assert.AreEqual(
                2,
                record.Level);

            Assert.AreEqual(
                5,
                record.Experience);
        }

        [Test]
        public void AddExperience_NotEnough_DoesNotLevelUp()
        {
            EventBattleActionProficiencyRecord record =
                new EventBattleActionProficiencyRecord
                {
                    Level = 1,
                    Experience = 0
                };

            bool leveledUp =
                EventBattleProficiencyRule.AddExperience(
                    record,
                    5);

            Assert.IsFalse(
                leveledUp);

            Assert.AreEqual(
                1,
                record.Level);
        }

        [Test]
        public void AddExperience_AtMaxLevel_ReturnsFalse()
        {
            EventBattleActionProficiencyRecord record =
                new EventBattleActionProficiencyRecord
                {
                    Level = EventBattleProficiencyRule.MaxLevel,
                    Experience = 0
                };

            bool leveledUp =
                EventBattleProficiencyRule.AddExperience(
                    record,
                    100);

            Assert.IsFalse(
                leveledUp);

            Assert.AreEqual(
                EventBattleProficiencyRule.MaxLevel,
                record.Level);
        }
    }
}
