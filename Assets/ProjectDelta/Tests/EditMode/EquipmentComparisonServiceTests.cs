using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 103일차: 장비 비교 UI가 사용하는 스탯 델타 계산을 검증한다.
    public sealed class EquipmentComparisonServiceTests
    {
        [Test]
        public void ComputeBonusDelta_EmptySlot_ReturnsCandidateBonusesAsIs()
        {
            EquipmentRunState equipment =
                new EquipmentRunState();

            StatBlock candidate =
                new StatBlock
                {
                    Attack = 15,
                    Defense = 5
                };

            StatBlock delta =
                EquipmentComparisonService.ComputeBonusDelta(
                    equipment,
                    EquipmentSlotType.Weapon,
                    candidate);

            Assert.That(
                delta.Attack,
                Is.EqualTo(15));

            Assert.That(
                delta.Defense,
                Is.EqualTo(5));
        }

        [Test]
        public void ComputeBonusDelta_ExistingItem_ReturnsDifferenceFromCurrent()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "OLD_SWORD",
                "낡은 검",
                1,
                1,
                out int slot);

            EquipmentService.Equip(
                inventory,
                equipment,
                slot,
                ItemCategory.Equipment,
                EquipmentSlotType.Weapon,
                EquipmentSlotType.Weapon,
                new StatBlock
                {
                    Attack = 10
                });

            StatBlock candidate =
                new StatBlock
                {
                    Attack = 18
                };

            StatBlock delta =
                EquipmentComparisonService.ComputeBonusDelta(
                    equipment,
                    EquipmentSlotType.Weapon,
                    candidate);

            Assert.That(
                delta.Attack,
                Is.EqualTo(8));
        }

        // 103일차 저주 장비: 후보 장비의 음수 옵션도 그대로(가감 없이) delta에 드러나야 한다.
        [Test]
        public void ComputeBonusDelta_CursedCandidateWithNegativeStat_ExposesNegativeDelta()
        {
            EquipmentRunState equipment =
                new EquipmentRunState();

            StatBlock candidate =
                new StatBlock
                {
                    Attack = 30,
                    Speed = -10
                };

            StatBlock delta =
                EquipmentComparisonService.ComputeBonusDelta(
                    equipment,
                    EquipmentSlotType.Weapon,
                    candidate);

            Assert.That(
                delta.Attack,
                Is.EqualTo(30));

            Assert.That(
                delta.Speed,
                Is.EqualTo(-10));
        }

        [Test]
        public void ComputeBonusDelta_NullEquipment_TreatsAsNoCurrentItem()
        {
            StatBlock candidate =
                new StatBlock
                {
                    Attack = 7
                };

            StatBlock delta =
                EquipmentComparisonService.ComputeBonusDelta(
                    null,
                    EquipmentSlotType.Weapon,
                    candidate);

            Assert.That(
                delta.Attack,
                Is.EqualTo(7));
        }
    }
}
