using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 102일차: 방어구 무게 분류 / 장신구 역할 / 가방 등급 표시 규칙을 검증한다.
    public sealed class EquipmentClassificationRulesTests
    {
        [Test]
        public void ArmorWeightClassRules_GetDisplayName_ReturnsDistinctKoreanNames()
        {
            Assert.That(
                ArmorWeightClassRules.GetDisplayName(
                    ArmorWeightClass.Light),
                Is.EqualTo("경갑"));

            Assert.That(
                ArmorWeightClassRules.GetDisplayName(
                    ArmorWeightClass.Heavy),
                Is.EqualTo("중갑"));

            Assert.That(
                ArmorWeightClassRules.GetDisplayName(
                    ArmorWeightClass.Robe),
                Is.EqualTo("로브"));

            Assert.That(
                ArmorWeightClassRules.GetDisplayName(
                    ArmorWeightClass.None),
                Is.EqualTo("미분류"));
        }

        [Test]
        public void AccessoryRoleRules_GetDisplayName_ReturnsDistinctKoreanNames()
        {
            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.Combat),
                Is.EqualTo("전투형"));

            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.Evasion),
                Is.EqualTo("회피형"));

            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.Exploration),
                Is.EqualTo("탐험형"));

            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.Resource),
                Is.EqualTo("자원형"));

            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.Charm),
                Is.EqualTo("매력형"));

            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.Resistance),
                Is.EqualTo("저항형"));

            Assert.That(
                AccessoryRoleRules.GetDisplayName(
                    AccessoryRole.None),
                Is.EqualTo("미분류"));
        }

        [Test]
        public void BagTierRules_GetSlotBonus_IncreasesMonotonicallyFrom2To8()
        {
            Assert.That(
                BagTierRules.GetSlotBonus(
                    BagTier.None),
                Is.EqualTo(0));

            int small =
                BagTierRules.GetSlotBonus(
                    BagTier.Small);

            int medium =
                BagTierRules.GetSlotBonus(
                    BagTier.Medium);

            int large =
                BagTierRules.GetSlotBonus(
                    BagTier.Large);

            int huge =
                BagTierRules.GetSlotBonus(
                    BagTier.Huge);

            Assert.That(small, Is.EqualTo(2));
            Assert.That(huge, Is.EqualTo(8));

            Assert.That(medium, Is.GreaterThan(small));
            Assert.That(large, Is.GreaterThan(medium));
            Assert.That(huge, Is.GreaterThan(large));
        }

        [Test]
        public void BagTierRules_GetDisplayName_ReturnsDistinctKoreanNames()
        {
            Assert.That(
                BagTierRules.GetDisplayName(
                    BagTier.Small),
                Is.EqualTo("소형 가방"));

            Assert.That(
                BagTierRules.GetDisplayName(
                    BagTier.Huge),
                Is.EqualTo("초대형 가방"));

            Assert.That(
                BagTierRules.GetDisplayName(
                    BagTier.None),
                Is.EqualTo("가방 아님"));
        }
    }
}
