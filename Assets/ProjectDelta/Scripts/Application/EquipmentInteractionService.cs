using System;
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
            ItemDefinition definition,
            PlayerRunState player = null,
            Random random = null)
        {
            if (definition == null)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.ItemNotEquipment,
                    default(EquipmentSlotType));
            }

            // 100일차: 등급과 랜덤 옵션은 장착 시점에 EquipmentRollService가 판정한다.
            return EquipFromInventory(
                inventory,
                equipment,
                inventorySlotIndex,
                definition,
                EquipmentRollService.Roll(
                    definition,
                    random),
                player);
        }

        // 103일차: 장비 비교 UI가 미리 굴려서 보여준 결과와 실제 장착 결과가
        // 어긋나지 않도록, 미리 굴린 EquipmentRollResult를 그대로 받아 재사용한다.
        public static EquipmentActionResult EquipFromInventory(
            InventoryRunState inventory,
            EquipmentRunState equipment,
            int inventorySlotIndex,
            ItemDefinition definition,
            EquipmentRollResult precomputedRoll,
            PlayerRunState player = null)
        {
            if (definition == null)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.ItemNotEquipment,
                    default(EquipmentSlotType));
            }

            // 98일차 인벤토리 UI는 슬롯을 직접 고르지 않고 아이템 자체의
            // EquipmentSlot에만 장착하므로 defined/target이 항상 같다.
            // 99일차: 장착 시점의 EquipmentStatBonuses를 EquipmentItemState에 그대로 전달하고,
            // player가 있으면 최종 스탯에 즉시 반영한다.
            // 101일차: 요구 조건(EquipmentRequirements) 검사는 EquipmentService.Equip 내부에서 처리한다.
            EquipmentRollResult roll =
                precomputedRoll
                ?? EquipmentRollService.Roll(
                    definition);

            return EquipmentService.Equip(
                inventory,
                equipment,
                inventorySlotIndex,
                definition.Category,
                definition.EquipmentSlot,
                definition.EquipmentSlot,
                roll.Bonuses,
                player,
                roll.Rarity,
                definition.EquipmentRequirements,
                definition.IsCursed);
        }

        public static EquipmentActionResult Unequip(
            InventoryRunState inventory,
            EquipmentRunState equipment,
            EquipmentSlotType slotType,
            PlayerRunState player = null)
        {
            return EquipmentService.Unequip(
                inventory,
                equipment,
                slotType,
                player);
        }
    }
}
