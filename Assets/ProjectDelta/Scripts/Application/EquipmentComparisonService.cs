using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 103일차: 인벤토리 UI가 "현재 장비 vs 후보 장비"의 스탯 차이를 계산할 때 쓴다.
    // 저주 장비도 EquipmentStatBonuses에 이미 음수 값을 담을 수 있으므로,
    // 여기서는 그 값을 그대로 빼서 보여줄 뿐 별도 처리를 하지 않는다.
    public static class EquipmentComparisonService
    {
        public static StatBlock ComputeBonusDelta(
            EquipmentRunState equipment,
            EquipmentSlotType slotType,
            StatBlock candidateBonuses)
        {
            EquipmentItemState currentItem =
                equipment != null
                    ? equipment.GetEquippedItem(
                        slotType)
                    : null;

            StatBlock currentBonuses =
                currentItem != null
                    ? currentItem.EquipmentBonuses
                    : new StatBlock();

            return StatBlock.Subtract(
                candidateBonuses
                ?? new StatBlock(),
                currentBonuses);
        }
    }
}
