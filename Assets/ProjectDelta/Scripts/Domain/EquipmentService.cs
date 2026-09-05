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
        WrongEquipmentSlot = 6,

        // 101일차: 공격력·속도·매력·저항 요구치를 만족하지 못했다.
        RequirementNotMet = 7
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
            EquipmentRarity rarity = EquipmentRarity.Common,
            StatBlock requirements = null,
            bool isCursed = false)
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

            EquipmentItemState previousItem =
                equipment.GetEquippedItem(
                    targetSlotType);

            // 101일차: 교체 대상 슬롯에 이미 장비가 있다면 그 보너스를 제외한
            // 기준 수치로 요구 조건을 판정해, 지금 장비의 힘으로 상위 장비를
            // 계속 갈아타는 편법을 막는다. 인벤토리는 아직 건드리지 않은 상태다.
            if (!MeetsRequirements(
                    player,
                    previousItem,
                    requirements))
            {
                return EquipmentActionResult.Failed(
                    EquipmentActionFailureReason.RequirementNotMet,
                    targetSlotType);
            }

            EquipmentItemState incomingItem =
                new EquipmentItemState(
                    inventorySlot.ItemId,
                    inventorySlot.DisplayName,
                    definedSlotType,
                    inventorySlot.MaxStackSize,
                    equipmentBonuses,
                    rarity,
                    isCursed);

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

        // 101일차: 공격력·속도·매력·저항 요구치를 검사한다.
        // player가 없으면(테스트 등 UI 밖 호출) 판정할 기준이 없으므로 통과시킨다.
        // requirements가 없으면 요구 조건 자체가 없는 장비이므로 통과시킨다.
        private static bool MeetsRequirements(
            PlayerRunState player,
            EquipmentItemState currentSlotItem,
            StatBlock requirements)
        {
            if (player == null
                || requirements == null)
            {
                return true;
            }

            StatBlock finalStats =
                player.GetFinalStats();

            StatBlock baseline =
                currentSlotItem != null
                    ? StatBlock.Subtract(
                        finalStats,
                        currentSlotItem.EquipmentBonuses)
                    : finalStats;

            return baseline.Attack >= requirements.Attack
                && baseline.Speed >= requirements.Speed
                && baseline.Charm >= requirements.Charm
                && baseline.Resistance >= requirements.Resistance;
        }
    }
}
