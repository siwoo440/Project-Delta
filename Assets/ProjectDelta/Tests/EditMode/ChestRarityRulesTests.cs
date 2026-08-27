using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 106일차: 상자 등급별 표시명·미믹 확률(일반 8%·고급 12%·희귀 18%)을 검증한다.
    public sealed class ChestRarityRulesTests
    {
        [Test]
        public void GetDisplayName_ReturnsDistinctKoreanNames()
        {
            Assert.That(
                ChestRarityRules.GetDisplayName(
                    ChestRarity.Common),
                Is.EqualTo("일반"));

            Assert.That(
                ChestRarityRules.GetDisplayName(
                    ChestRarity.Uncommon),
                Is.EqualTo("고급"));

            Assert.That(
                ChestRarityRules.GetDisplayName(
                    ChestRarity.Rare),
                Is.EqualTo("희귀"));
        }

        [Test]
        public void GetMimicChancePercent_MatchesPlanningDocValues()
        {
            Assert.That(
                ChestRarityRules.GetMimicChancePercent(
                    ChestRarity.Common),
                Is.EqualTo(8));

            Assert.That(
                ChestRarityRules.GetMimicChancePercent(
                    ChestRarity.Uncommon),
                Is.EqualTo(12));

            Assert.That(
                ChestRarityRules.GetMimicChancePercent(
                    ChestRarity.Rare),
                Is.EqualTo(18));
        }
    }
}
