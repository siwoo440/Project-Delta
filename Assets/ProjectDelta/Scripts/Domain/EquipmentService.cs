namespace ProjectDelta.Domain
{
    public enum EquipmentActionFailureReason
    {
        None = 0,
        InvalidState = 1,
        InvalidInventorySlot = 2,
        ItemNotEquipment = 3,
        EquipmentSlotEmpty = 4,
        InventoryFull = 5,

        // 아이템 자체에 정의된 장비 슬롯과 실제 장착 대상 슬롯이 다르다.
        WrongEquipmentSlot = 6
    }

    public sealed class EquipmentActionResult
    {
        public bool Success { get; private set; }

        public EquipmentActionFailureReason FailureReason { get; private set; }

        public EquipmentSlotType SlotType { get; private set; }

        public EquipmentItemState EquippedItem { get; private set; }

        public EquipmentItemState ReturnedItem { get; private set; }

        public static EquipmentActionResult Succeeded(
            EquipmentSlotType slotType,
            EquipmentItemState equippedItem,
            EquipmentItemState returnedItem)
        {
            return new EquipmentActionResult
            {
                Success = true,
                FailureReason = EquipmentActionFailureReason.None,
                SlotType = slotType,
                EquippedItem = equippedItem,
                ReturnedItem = returnedItem
            };
        }

        public static EquipmentActionResult Failed(
            EquipmentActionFailureReason reason,
            EquipmentSlotType slotType)
        {
            return new EquipmentActionResult
            {
                Success = false,
                FailureReason = reason,
                SlotType = slotType
            };
        }
    }

    // 인벤토리와 장비 슬롯 사이의 이동 규칙을 한 곳에서 처리한다.
    public static class EquipmentService
    {
        public static EquipmentActionResult Equip(
            InventoryRunState inventory,
            EquipmentRunState equipment,
            int inventorySlotIndex,
            ItemCategory itemCategory,
            EquipmentSlotType definedSlotType,
            EquipmentSlotType targetSlotType,
            StatBlock equipmentBonuses = null,
            PlayerRunState player = null,
            EquipmentRarity rarity = EquipmentRarity.Common)
        {
            if (inventory == null
                || equipment == null)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.InvalidState,
                    targetSlotType);
            }

            if (!ItemCategoryRules.CanEquip(
                    itemCategory))
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.ItemNotEquipment,
                    targetSlotType);
            }

            // ItemDefinition.EquipmentSlot과 실제 장착 대상이 다르면
            // 인벤토리를 수정하기 전에 즉시 거부한다.
            if (definedSlotType != targetSlotType)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.WrongEquipmentSlot,
                    targetSlotType);
            }

            if (!inventory.TryGetSlot(
                    inventorySlotIndex,
                    out InventorySlotState inventorySlot)
                || inventorySlot == null
                || inventorySlot.IsEmpty)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.InvalidInventorySlot,
                    targetSlotType);
            }

            EquipmentItemState incomingItem =
                new EquipmentItemState(
                    inventorySlot.ItemId,
                    inventorySlot.DisplayName,
                    definedSlotType,
                    inventorySlot.MaxStackSize,
                    equipmentBonuses,
                    rarity);

            EquipmentItemState previousItem =
                equipment.GetEquippedItem(
                    targetSlotType);

            if (!inventory.TryRemoveQuantityAt(
                    inventorySlotIndex,
                    1,
                    out int removedQuantity)
                || removedQuantity != 1)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.InvalidInventorySlot,
                    targetSlotType);
            }

            if (previousItem != null)
            {
                bool returned =
                    inventory.TryAdd(
                        previousItem.ItemId,
                        previousItem.DisplayName,
                        1,
                        previousItem.MaxStackSize,
                        out _);

                if (!returned)
                {
                    // 교체 실패 시 새 장비를 인벤토리에 되돌려
                    // 장비 또는 아이템 손실이 발생하지 않게 한다.
                    inventory.TryAdd(
                        incomingItem.ItemId,
                        incomingItem.DisplayName,
                        1,
                        incomingItem.MaxStackSize,
                        out _);

                    return EquipmentActionResult.Failed(
                        EquipmentActionFailureReason.InventoryFull,
                        targetSlotType);
                }
            }

            equipment.SetEquippedItem(
                targetSlotType,
                incomingItem);

            SyncPlayerStats(
                player,
                equipment);

            return EquipmentActionResult.Succeeded(
                targetSlotType,
                incomingItem,
                previousItem);
        }

        public static EquipmentActionResult Unequip(
            InventoryRunState inventory,
            EquipmentRunState equipment,
            EquipmentSlotType slotType,
            PlayerRunState player = null)
        {
            if (inventory == null
                || equipment == null)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.InvalidState,
                    slotType);
            }

            EquipmentItemState equippedItem =
                equipment.GetEquippedItem(
                    slotType);

            if (equippedItem == null)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.EquipmentSlotEmpty,
                    slotType);
            }

            bool returned =
                inventory.TryAdd(
                    equippedItem.ItemId,
                    equippedItem.DisplayName,
                    1,
                    equippedItem.MaxStackSize,
                    out _);

            if (!returned)
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.InventoryFull,
                    slotType);
            }

            equipment.ClearSlot(
                slotType);

            SyncPlayerStats(
                player,
                equipment);

            return EquipmentActionResult.Succeeded(
                slotType,
                null,
                equippedItem);
        }

        // 99일차: 장착/해제 직후 플레이어의 장비 보너스 합계를 갱신하고,
        // 최대치가 줄어들었다면 현재 자원도 함께 정리한다.
        private static void SyncPlayerStats(
            PlayerRunState player,
            EquipmentRunState equipment)
        {
            if (player == null
                || equipment == null)
            {
                return;
            }

            player.EquipmentBonuses =
                equipment.GetTotalBonuses();

            player.ClampCurrentResourcesToFinalStats();
        }
    }
}
