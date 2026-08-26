using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public enum InventoryInteractionFailureReason
    {
        None = 0,

        InvalidInventory = 1,

        InvalidSourceSlot = 2,

        InvalidDestinationSlot = 3,

        SameSlot = 4,

        MoveFailed = 5,

        DiscardNotAllowed = 6,

        RemoveFailed = 7
    }

    public sealed class InventoryInteractionResult
    {
        public bool Success { get; set; }

        public InventoryInteractionFailureReason FailureReason { get; set; }

        public int SourceSlotIndex { get; set; } = -1;

        public int DestinationSlotIndex { get; set; } = -1;

        public int RemovedQuantity { get; set; }

        public static InventoryInteractionResult Failed(
            InventoryInteractionFailureReason reason,
            int sourceSlotIndex = -1,
            int destinationSlotIndex = -1)
        {
            return new InventoryInteractionResult
            {
                Success =
                    false,
                FailureReason =
                    reason,
                SourceSlotIndex =
                    sourceSlotIndex,
                DestinationSlotIndex =
                    destinationSlotIndex
            };
        }
    }

    // 94일차: 인벤토리 UI가 슬롯 규칙을 직접 구현하지 않도록 이동·교체·버리기를 한곳에 모은다.
    public static class InventoryInteractionService
    {
        public static InventoryInteractionResult Move(
            InventoryRunState inventory,
            int sourceSlotIndex,
            int destinationSlotIndex)
        {
            if (inventory == null)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidInventory,
                    sourceSlotIndex,
                    destinationSlotIndex);
            }

            if (sourceSlotIndex
                == destinationSlotIndex)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.SameSlot,
                    sourceSlotIndex,
                    destinationSlotIndex);
            }

            if (!inventory.TryGetSlot(
                    sourceSlotIndex,
                    out InventorySlotState source)
                || source == null
                || source.IsEmpty)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidSourceSlot,
                    sourceSlotIndex,
                    destinationSlotIndex);
            }

            if (!inventory.TryGetSlot(
                    destinationSlotIndex,
                    out InventorySlotState destination)
                || destination == null)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidDestinationSlot,
                    sourceSlotIndex,
                    destinationSlotIndex);
            }

            // 빈 슬롯과 동일 아이템 Stack은 기존 인벤토리 기본 동작을 그대로 사용한다.
            if (destination.IsEmpty
                || destination.ItemId
                    == source.ItemId)
            {
                bool moved =
                    inventory.TryMoveOrSwap(
                        sourceSlotIndex,
                        destinationSlotIndex);

                return moved
                    ? new InventoryInteractionResult
                    {
                        Success =
                            true,
                        FailureReason =
                            InventoryInteractionFailureReason.None,
                        SourceSlotIndex =
                            sourceSlotIndex,
                        DestinationSlotIndex =
                            destinationSlotIndex
                    }
                    : InventoryInteractionResult.Failed(
                        InventoryInteractionFailureReason.MoveFailed,
                        sourceSlotIndex,
                        destinationSlotIndex);
            }

            // 서로 다른 아이템 Swap은 양쪽 데이터를 먼저 보관한다.
            // 90일차 TryMoveOrSwap의 기존 덮어쓰기 버그가 남아 있어도 UI 이동은 안전하게 동작한다.
            string sourceItemId =
                source.ItemId;

            string sourceDisplayName =
                source.DisplayName;

            int sourceQuantity =
                source.Quantity;

            int sourceMaxStackSize =
                source.MaxStackSize;

            string destinationItemId =
                destination.ItemId;

            string destinationDisplayName =
                destination.DisplayName;

            int destinationQuantity =
                destination.Quantity;

            int destinationMaxStackSize =
                destination.MaxStackSize;

            bool sourceRestored =
                inventory.RestoreSlot(
                    sourceSlotIndex,
                    destinationItemId,
                    destinationDisplayName,
                    destinationQuantity,
                    destinationMaxStackSize);

            bool destinationRestored =
                inventory.RestoreSlot(
                    destinationSlotIndex,
                    sourceItemId,
                    sourceDisplayName,
                    sourceQuantity,
                    sourceMaxStackSize);

            if (!sourceRestored
                || !destinationRestored)
            {
                inventory.RestoreSlot(
                    sourceSlotIndex,
                    sourceItemId,
                    sourceDisplayName,
                    sourceQuantity,
                    sourceMaxStackSize);

                inventory.RestoreSlot(
                    destinationSlotIndex,
                    destinationItemId,
                    destinationDisplayName,
                    destinationQuantity,
                    destinationMaxStackSize);

                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.MoveFailed,
                    sourceSlotIndex,
                    destinationSlotIndex);
            }

            return new InventoryInteractionResult
            {
                Success =
                    true,
                FailureReason =
                    InventoryInteractionFailureReason.None,
                SourceSlotIndex =
                    sourceSlotIndex,
                DestinationSlotIndex =
                    destinationSlotIndex
            };
        }

        public static InventoryInteractionResult DiscardOne(
            InventoryRunState inventory,
            int slotIndex,
            ItemCategory category)
        {
            if (inventory == null)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidInventory,
                    slotIndex);
            }

            if (!ItemCategoryRules.CanDiscard(
                    category))
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.DiscardNotAllowed,
                    slotIndex);
            }

            if (!inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidSourceSlot,
                    slotIndex);
            }

            bool removed =
                inventory.TryRemoveQuantityAt(
                    slotIndex,
                    1,
                    out int removedQuantity);

            if (!removed
                || removedQuantity <= 0)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.RemoveFailed,
                    slotIndex);
            }

            return new InventoryInteractionResult
            {
                Success =
                    true,
                FailureReason =
                    InventoryInteractionFailureReason.None,
                SourceSlotIndex =
                    slotIndex,
                RemovedQuantity =
                    removedQuantity
            };
        }

        public static InventoryInteractionResult DiscardAll(
            InventoryRunState inventory,
            int slotIndex,
            ItemCategory category)
        {
            if (inventory == null)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidInventory,
                    slotIndex);
            }

            if (!ItemCategoryRules.CanDiscard(
                    category))
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.DiscardNotAllowed,
                    slotIndex);
            }

            if (!inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.InvalidSourceSlot,
                    slotIndex);
            }

            int removedQuantity =
                slot.Quantity;

            if (!inventory.TryRemoveAt(
                    slotIndex))
            {
                return InventoryInteractionResult.Failed(
                    InventoryInteractionFailureReason.RemoveFailed,
                    slotIndex);
            }

            return new InventoryInteractionResult
            {
                Success =
                    true,
                FailureReason =
                    InventoryInteractionFailureReason.None,
                SourceSlotIndex =
                    slotIndex,
                RemovedQuantity =
                    removedQuantity
            };
        }
    }
}
