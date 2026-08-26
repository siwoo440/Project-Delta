using System;

namespace ProjectDelta.Domain
{
    // 아이템 획득을 실제로 적용하기 전에 계산한 결과다.
    public sealed class InventoryAcquisitionPlan
    {
        public string ItemId { get; set; }

        public string DisplayName { get; set; }

        public int RequestedQuantity { get; set; }

        public int MaxStackSize { get; set; }

        public int AddableQuantity { get; set; }

        public int RemainingQuantity { get; set; }

        public bool RequiresDecision =>
            RemainingQuantity > 0;
    }

    // 선택 결과를 실제 인벤토리에 적용한 뒤 반환하는 결과다.
    public sealed class InventoryAcquisitionCommitResult
    {
        public string ItemId { get; set; }

        public int RequestedQuantity { get; set; }

        public int AddedQuantity { get; set; }

        public int RemainingQuantity { get; set; }

        public int ReplacedSlotIndex { get; set; } = -1;

        public bool WasCancelled { get; set; }

        public bool ReplacementSucceeded =>
            ReplacedSlotIndex >= 0;

        public bool IsComplete =>
            RemainingQuantity <= 0;
    }

    // 92일차: 인벤토리가 가득 찼을 때 되돌리기 없이 처리하기 위한 획득 서비스다.
    public static class InventoryAcquisitionService
    {
        // 현재 인벤토리를 변경하지 않고 새 아이템이 얼마나 들어갈지 계산한다.
        public static InventoryAcquisitionPlan Preview(
            InventoryRunState inventory,
            string itemId,
            string displayName,
            int quantity,
            int maxStackSize)
        {
            int requestedQuantity =
                Math.Max(
                    0,
                    quantity);

            int safeMaxStackSize =
                Math.Max(
                    1,
                    maxStackSize);

            InventoryAcquisitionPlan plan =
                new InventoryAcquisitionPlan
                {
                    ItemId = itemId,
                    DisplayName =
                        string.IsNullOrEmpty(
                            displayName)
                            ? itemId
                            : displayName,
                    RequestedQuantity =
                        requestedQuantity,
                    MaxStackSize =
                        safeMaxStackSize,
                    RemainingQuantity =
                        requestedQuantity
                };

            if (inventory == null
                || string.IsNullOrEmpty(itemId)
                || requestedQuantity <= 0)
            {
                return plan;
            }

            int remainingQuantity =
                requestedQuantity;

            // 기존 동일 아이템 Stack의 남은 공간을 먼저 계산한다.
            for (int index = 0;
                 index < inventory.Slots.Count
                 && remainingQuantity > 0;
                 index++)
            {
                InventorySlotState slot =
                    inventory.Slots[index];

                if (slot == null
                    || slot.IsEmpty
                    || slot.ItemId != itemId)
                {
                    continue;
                }

                int availableQuantity =
                    Math.Max(
                        0,
                        slot.MaxStackSize
                        - slot.Quantity);

                int addableQuantity =
                    Math.Min(
                        availableQuantity,
                        remainingQuantity);

                remainingQuantity -=
                    addableQuantity;
            }

            // 기존 Stack에 다 들어가지 않으면 빈 슬롯에 들어갈 수량을 계산한다.
            for (int index = 0;
                 index < inventory.Slots.Count
                 && remainingQuantity > 0;
                 index++)
            {
                InventorySlotState slot =
                    inventory.Slots[index];

                if (slot == null
                    || !slot.IsEmpty)
                {
                    continue;
                }

                remainingQuantity -=
                    Math.Min(
                        safeMaxStackSize,
                        remainingQuantity);
            }

            plan.RemainingQuantity =
                remainingQuantity;

            plan.AddableQuantity =
                requestedQuantity
                - remainingQuantity;

            return plan;
        }

        // "두고 간다" 선택: 현재 들어갈 수 있는 수량만 실제로 적용한다.
        public static InventoryAcquisitionCommitResult CommitLeave(
            InventoryRunState inventory,
            InventoryAcquisitionPlan plan)
        {
            if (inventory == null
                || !IsValidPlan(plan))
            {
                return CreateNoChangeResult(
                    plan,
                    false);
            }

            InventoryAddResult addResult =
                inventory.TryAddDetailed(
                    plan.ItemId,
                    plan.DisplayName,
                    plan.RequestedQuantity,
                    plan.MaxStackSize);

            return new InventoryAcquisitionCommitResult
            {
                ItemId =
                    plan.ItemId,
                RequestedQuantity =
                    plan.RequestedQuantity,
                AddedQuantity =
                    addResult.AddedQuantity,
                RemainingQuantity =
                    addResult.RemainingQuantity
            };
        }

        // "취소" 선택: Preview 결과를 버리고 실제 인벤토리는 전혀 수정하지 않는다.
        public static InventoryAcquisitionCommitResult CommitCancel(
            InventoryAcquisitionPlan plan)
        {
            return CreateNoChangeResult(
                plan,
                true);
        }

        // 해당 종류의 기존 아이템을 공간 확보용 교체 대상으로 사용할 수 있는지 확인한다.
        public static bool CanReplaceTarget(
            ItemCategory targetCategory)
        {
            return ItemCategoryRules.GetDiscardAvailability(
                    targetCategory)
                == ItemActionAvailability.Available;
        }

        // "교체" 선택: 기존 Stack을 먼저 채운 뒤 지정한 슬롯 하나를 비워 남은 아이템을 넣는다.
        public static InventoryAcquisitionCommitResult CommitReplace(
            InventoryRunState inventory,
            InventoryAcquisitionPlan plan,
            int targetSlotIndex,
            ItemCategory targetCategory)
        {
            if (inventory == null
                || !IsValidPlan(plan)
                || !CanReplaceTarget(
                    targetCategory)
                || !inventory.TryGetSlot(
                    targetSlotIndex,
                    out InventorySlotState targetSlot)
                || targetSlot == null
                || targetSlot.IsEmpty)
            {
                return CreateNoChangeResult(
                    plan,
                    false);
            }

            // 교체 실패 시 원래 슬롯을 복원할 수 있도록 기존 값을 보관한다.
            string originalItemId =
                targetSlot.ItemId;

            string originalDisplayName =
                targetSlot.DisplayName;

            int originalQuantity =
                targetSlot.Quantity;

            int originalMaxStackSize =
                targetSlot.MaxStackSize;

            if (!inventory.TryRemoveAt(
                    targetSlotIndex))
            {
                return CreateNoChangeResult(
                    plan,
                    false);
            }

            InventoryAddResult addResult =
                inventory.TryAddDetailed(
                    plan.ItemId,
                    plan.DisplayName,
                    plan.RequestedQuantity,
                    plan.MaxStackSize);

            if (addResult.AddedQuantity <= 0)
            {
                inventory.RestoreSlot(
                    targetSlotIndex,
                    originalItemId,
                    originalDisplayName,
                    originalQuantity,
                    originalMaxStackSize);

                return CreateNoChangeResult(
                    plan,
                    false);
            }

            bool replacedSlotContainsIncomingItem =
                inventory.TryGetSlot(
                    targetSlotIndex,
                    out InventorySlotState replacedSlot)
                && replacedSlot != null
                && !replacedSlot.IsEmpty
                && replacedSlot.ItemId
                    == plan.ItemId;

            return new InventoryAcquisitionCommitResult
            {
                ItemId =
                    plan.ItemId,
                RequestedQuantity =
                    plan.RequestedQuantity,
                AddedQuantity =
                    addResult.AddedQuantity,
                RemainingQuantity =
                    addResult.RemainingQuantity,
                ReplacedSlotIndex =
                    replacedSlotContainsIncomingItem
                        ? targetSlotIndex
                        : -1
            };
        }

        private static bool IsValidPlan(
            InventoryAcquisitionPlan plan)
        {
            return plan != null
                && !string.IsNullOrEmpty(
                    plan.ItemId)
                && plan.RequestedQuantity > 0
                && plan.MaxStackSize > 0;
        }

        private static InventoryAcquisitionCommitResult CreateNoChangeResult(
            InventoryAcquisitionPlan plan,
            bool wasCancelled)
        {
            return new InventoryAcquisitionCommitResult
            {
                ItemId =
                    plan != null
                        ? plan.ItemId
                        : string.Empty,
                RequestedQuantity =
                    plan != null
                        ? plan.RequestedQuantity
                        : 0,
                AddedQuantity =
                    0,
                RemainingQuantity =
                    plan != null
                        ? plan.RequestedQuantity
                        : 0,
                WasCancelled =
                    wasCancelled
            };
        }
    }
}
