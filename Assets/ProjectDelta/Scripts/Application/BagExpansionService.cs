using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public enum BagExpansionFailureReason
    {
        None = 0,
        InvalidInventory = 1,
        InvalidSlot = 2,
        NotABag = 3
    }

    public sealed class BagExpansionResult
    {
        public bool Success { get; private set; }

        public BagExpansionFailureReason FailureReason { get; private set; }

        public int AddedSlotBonus { get; private set; }

        public int NewBagSlotBonus { get; private set; }

        public static BagExpansionResult Succeeded(
            int addedSlotBonus,
            int newBagSlotBonus)
        {
            return new BagExpansionResult
            {
                Success = true,
                FailureReason = BagExpansionFailureReason.None,
                AddedSlotBonus = addedSlotBonus,
                NewBagSlotBonus = newBagSlotBonus
            };
        }

        public static BagExpansionResult Failed(
            BagExpansionFailureReason reason)
        {
            return new BagExpansionResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }

    // 102일차: 가방은 6부위 장비 슬롯 체계와 별개로, 사용하는 즉시 인벤토리 슬롯을
    // 영구적으로 넓혀주고 소모되는 아이템으로 취급한다. 여러 개를 얻으면
    // 등급별 확장치가 그대로 누적된다.
    public static class BagExpansionService
    {
        public static BagExpansionResult ApplyAndConsume(
            InventoryRunState inventory,
            int inventorySlotIndex,
            ItemDefinition definition)
        {
            if (inventory == null)
            {
                return BagExpansionResult.Failed(
                    BagExpansionFailureReason.InvalidInventory);
            }

            if (definition == null
                || definition.BagTier == BagTier.None)
            {
                return BagExpansionResult.Failed(
                    BagExpansionFailureReason.NotABag);
            }

            if (!inventory.TryGetSlot(
                    inventorySlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return BagExpansionResult.Failed(
                    BagExpansionFailureReason.InvalidSlot);
            }

            if (!inventory.TryRemoveQuantityAt(
                    inventorySlotIndex,
                    1,
                    out int removedQuantity)
                || removedQuantity != 1)
            {
                return BagExpansionResult.Failed(
                    BagExpansionFailureReason.InvalidSlot);
            }

            int addedSlotBonus =
                BagTierRules.GetSlotBonus(
                    definition.BagTier);

            int newBagSlotBonus =
                inventory.BagSlotBonus
                + addedSlotBonus;

            inventory.SetCapacityBonuses(
                inventory.PermanentSlotBonus,
                newBagSlotBonus);

            return BagExpansionResult.Succeeded(
                addedSlotBonus,
                newBagSlotBonus);
        }
    }
}
