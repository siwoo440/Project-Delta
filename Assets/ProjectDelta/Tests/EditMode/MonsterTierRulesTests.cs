using NUnit.Framework;
using ProjectDelta.Data;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class MonsterTierRulesTests
    {
        [Test]
        public void GetStatMultiplier_Normal_ReturnsOne()
        {
            Assert.AreEqual(
                1f,
                MonsterTierRules.GetStatMultiplier(
                    MonsterTier.Normal));
        }

        [Test]
        public void GetStatMultiplier_Elite_IsGreaterThanNormal()
        {
            Assert.Greater(
                MonsterTierRules.GetStatMultiplier(
                    MonsterTier.Elite),
                MonsterTierRules.GetStatMultiplier(
                    MonsterTier.Normal));
        }

        [Test]
        public void GetStatMultiplier_Boss_IsGreaterThanElite()
        {
            Assert.Greater(
                MonsterTierRules.GetStatMultiplier(
                    MonsterTier.Boss),
                MonsterTierRules.GetStatMultiplier(
                    MonsterTier.Elite));
        }

        [Test]
        public void GetRewardMultiplier_Normal_ReturnsOne()
        {
            Assert.AreEqual(
                1f,
                MonsterTierRules.GetRewardMultiplier(
                    MonsterTier.Normal));
        }

        [Test]
        public void GetRewardMultiplier_Boss_IsGreaterThanElite()
        {
            Assert.Greater(
                MonsterTierRules.GetRewardMultiplier(
                    MonsterTier.Boss),
                MonsterTierRules.GetRewardMultiplier(
                    MonsterTier.Elite));
        }

        [Test]
        public void GetDisplayName_ReturnsKoreanLabels()
        {
            Assert.AreEqual(
                "일반",
                MonsterTierRules.GetDisplayName(
                    MonsterTier.Normal));

            Assert.AreEqual(
                "정예",
                MonsterTierRules.GetDisplayName(
                    MonsterTier.Elite));

            Assert.AreEqual(
                "보스",
                MonsterTierRules.GetDisplayName(
                    MonsterTier.Boss));
        }
    }
}
