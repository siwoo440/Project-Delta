using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    [Serializable]
    public sealed class EquipmentItemState
    {
        public string ItemId { get; }

        public string DisplayName { get; }

        public EquipmentSlotType SlotType { get; }

        public int MaxStackSize { get; }

        public EquipmentItemState(
            string itemId,
            string displayName,
            EquipmentSlotType slotType,
            int maxStackSize)
        {
            ItemId =
                itemId
                ?? string.Empty;

            DisplayName =
                string.IsNullOrEmpty(
                    displayName)
                    ? ItemId
                    : displayName;

            SlotType =
                slotType;

            MaxStackSize =
                Math.Max(
                    1,
                    maxStackSize);
        }
    }

    // 장착 중인 아이템은 인벤토리와 분리해서 런타임 상태로 보관한다.
    public sealed class EquipmentRunState
    {
        private readonly Dictionary<EquipmentSlotType, EquipmentItemState> slots =
            new Dictionary<EquipmentSlotType, EquipmentItemState>();

        public EquipmentItemState GetEquippedItem(
            EquipmentSlotType slotType)
        {
            return slots.TryGetValue(
                    slotType,
                    out EquipmentItemState item)
                ? item
                : null;
        }

        public bool IsEquipped(
            string itemId)
        {
            if (string.IsNullOrEmpty(
                    itemId))
            {
                return false;
            }

            foreach (EquipmentItemState item
                     in slots.Values)
            {
                if (item != null
                    && item.ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        internal void SetEquippedItem(
            EquipmentSlotType slotType,
            EquipmentItemState item)
        {
            if (item == null)
            {
                slots.Remove(
                    slotType);

                return;
            }

            slots[slotType] =
                item;
        }

        internal EquipmentItemState ClearSlot(
            EquipmentSlotType slotType)
        {
            EquipmentItemState previous =
                GetEquippedItem(
                    slotType);

            slots.Remove(
                slotType);

            return previous;
        }
    }
}
