using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 98일차: 인벤토리 ↔ 장비 UI가 ItemDefinition을 직접 EquipmentService 인자로
    // 풀어 쓰지 않도록 한곳에 모은다. 실제 규칙 판단은 여전히 EquipmentService가 담당한다.
    public static class EquipmentInteractionService
    {
        public static EquipmentActionResult EquipFromInventory(
            InventoryRunState inventory,
            EquipmentRunState equipment,
            int inventorySlotIndex,
            ItemDefinition definition)
        {
            if (definition == null)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.ItemNotEquipment,
                    default(EquipmentSlotType));
            }

            // 98일차 인벤토리 UI는 슬롯을 직접 고르지 않고 아이템 자체의
            // EquipmentSlot에만 장착하므로 defined/target이 항상 같다.
            return EquipmentService.Equip(
                inventory,
                equipment,
                inventorySlotIndex,
                definition.Category,
                definition.EquipmentSlot,
                definition.EquipmentSlot);
        }

        public static EquipmentActionResult Unequip(
            InventoryRunState inventory,
            EquipmentRunState equipment,
            EquipmentSlotType slotType)
        {
            return EquipmentService.Unequip(
                inventory,
                equipment,
                slotType);
        }
    }
}
