using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 100일차: 등급별 표시명·배율·가중치가 등급이 높을수록 커지는 단조 증가 관계인지 확인한다.
    public sealed class EquipmentRarityRulesTests
    {
        [Test]
        public void GetDisplayName_ReturnsDistinctKoreanNameForEachRarity()
        {
            Assert.That(
                EquipmentRarityRules.GetDisplayName(
                    EquipmentRarity.Common),
                Is.EqualTo("일반"));

            Assert.That(
                EquipmentRarityRules.GetDisplayName(
                    EquipmentRarity.Uncommon),
                Is.EqualTo("고급"));

            Assert.That(
                EquipmentRarityRules.GetDisplayName(
                    EquipmentRarity.Rare),
                Is.EqualTo("희귀"));

            Assert.That(
                EquipmentRarityRules.GetDisplayName(
                    EquipmentRarity.Epic),
                Is.EqualTo("영웅"));

            Assert.That(
                EquipmentRarityRules.GetDisplayName(
                    EquipmentRarity.Legendary),
                Is.EqualTo("전설"));
        }

        [Test]
        public void GetStatMultiplier_IncreasesMonotonicallyWithRarity()
        {
            double common =
                EquipmentRarityRules.GetStatMultiplier(
                    EquipmentRarity.Common);

            double uncommon =
                EquipmentRarityRules.GetStatMultiplier(
                    EquipmentRarity.Uncommon);

            double rare =
                EquipmentRarityRules.GetStatMultiplier(
                    EquipmentRarity.Rare);

            double epic =
                EquipmentRarityRules.GetStatMultiplier(
                    EquipmentRarity.Epic);

            double legendary =
                EquipmentRarityRules.GetStatMultiplier(
                    EquipmentRarity.Legendary);

            Assert.That(
                common,
                Is.EqualTo(1.0));

            Assert.That(
                uncommon,
                Is.GreaterThan(common));

            Assert.That(
                rare,
                Is.GreaterThan(uncommon));

            Assert.That(
                epic,
                Is.GreaterThan(rare));

            Assert.That(
                legendary,
                Is.GreaterThan(epic));
        }

        [Test]
        public void GetDropWeight_DecreasesMonotonicallyWithRarity()
        {
            int common =
                EquipmentRarityRules.GetDropWeight(
                    EquipmentRarity.Common);

            int uncommon =
                EquipmentRarityRules.GetDropWeight(
                    EquipmentRarity.Uncommon);

            int rare =
                EquipmentRarityRules.GetDropWeight(
                    EquipmentRarity.Rare);

            int epic =
                EquipmentRarityRules.GetDropWeight(
                    EquipmentRarity.Epic);

            int legendary =
                EquipmentRarityRules.GetDropWeight(
                    EquipmentRarity.Legendary);

            Assert.That(
                common,
                Is.GreaterThan(uncommon));

            Assert.That(
                uncommon,
                Is.GreaterThan(rare));

            Assert.That(
                rare,
                Is.GreaterThan(epic));

            Assert.That(
                epic,
                Is.GreaterThan(legendary));
        }
    }
}
