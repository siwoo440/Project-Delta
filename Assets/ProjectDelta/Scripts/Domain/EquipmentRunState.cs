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

        // 99일차: 장착 시점의 ItemDefinition.EquipmentStatBonuses 스냅샷.
        // Domain이 Data 계층(ItemDefinition)에 의존하지 않도록, 값만 복사해서 들고 있는다.
        public StatBlock EquipmentBonuses { get; }

        public EquipmentItemState(
            string itemId,
            string displayName,
            EquipmentSlotType slotType,
            int maxStackSize,
            StatBlock equipmentBonuses = null)
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

            EquipmentBonuses =
                equipmentBonuses
                ?? new StatBlock();
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

        // 99일차: 6부위에 장착된 아이템의 스탯 보너스를 모두 합산한다.
        public StatBlock GetTotalBonuses()
        {
            List<StatBlock> bonuses =
                new List<StatBlock>();

            foreach (EquipmentItemState item
                     in slots.Values)
            {
                if (item != null)
                {
                    bonuses.Add(
                        item.EquipmentBonuses);
                }
            }

            return StatBlock.Sum(
                bonuses.ToArray());
        }
    }
}
