using System;
using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EquipmentServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }
        }

        [Test]
        public void EquipmentSlotType_HasSixExpectedSlots()
        {
            EquipmentSlotType[] slots =
                (EquipmentSlotType[])Enum.GetValues(
                    typeof(EquipmentSlotType));

            Assert.That(
                slots,
                Is.EqualTo(
                    new[]
                    {
                        EquipmentSlotType.Weapon,
                        EquipmentSlotType.Helmet,
                        EquipmentSlotType.ChestArmor,
                        EquipmentSlotType.Leggings,
                        EquipmentSlotType.Boots,
                        EquipmentSlotType.Accessory
                    }));
        }

        [Test]
        public void RunContext_Begin_CreatesEmptyEquipmentState()
        {
            RunContext context =
                RunContext.Begin(
                    "DAY97_EQUIPMENT");

            Assert.That(
                context.Equipment,
                Is.Not.Null);

            foreach (EquipmentSlotType slotType
                     in Enum.GetValues(
                         typeof(EquipmentSlotType)))
            {
                Assert.That(
                    context.Equipment.GetEquippedItem(
                        slotType),
                    Is.Null);
            }
        }

        [Test]
        public void Equip_EquipmentItem_MovesOneItemFromInventoryToDefinedSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "IRON_SWORD",
                    "철검",
                    1,
                    1,
                    out int inventorySlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Weapon).ItemId,
                Is.EqualTo(
                    "IRON_SWORD"));

            Assert.That(
                inventory.TryGetSlot(
                    inventorySlot,
                    out InventorySlotState slot),
                Is.True);

            Assert.That(
                slot.IsEmpty,
                Is.True);
        }

        [Test]
        public void Equip_DefinedSlotAndTargetSlotDiffer_IsRejectedWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "IRON_HELMET",
                    "강철 투구",
                    1,
                    1,
                    out int inventorySlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Boots);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.WrongEquipmentSlot));

            Assert.That(
                inventory.TryGetSlot(
                    inventorySlot,
                    out InventorySlotState inventoryItem),
                Is.True);

            Assert.That(
                inventoryItem.ItemId,
                Is.EqualTo(
                    "IRON_HELMET"));

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Helmet),
                Is.Null);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Boots),
                Is.Null);
        }

        [Test]
        public void Equip_NonEquipmentItem_IsRejectedWithoutInventoryMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "POTION",
                    "포션",
                    1,
                    5,
                    out int inventorySlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Consumable,
                    EquipmentSlotType.Accessory,
                    EquipmentSlotType.Accessory);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.ItemNotEquipment));

            Assert.That(
                inventory.TryGetSlot(
                    inventorySlot,
                    out InventorySlotState slot),
                Is.True);

            Assert.That(
                slot.ItemId,
                Is.EqualTo(
                    "POTION"));

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Accessory),
                Is.Null);
        }

        [Test]
        public void Equip_OccupiedSlot_ReturnsOldEquipmentToInventory()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "OLD_HELMET",
                    "낡은 투구",
                    1,
                    1,
                    out int oldSlot),
                Is.True);

            Assert.That(
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    oldSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Helmet).Success,
                Is.True);

            Assert.That(
                inventory.TryAdd(
                    "NEW_HELMET",
                    "강철 투구",
                    1,
                    1,
                    out int newSlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    newSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Helmet);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Helmet).ItemId,
                Is.EqualTo(
                    "NEW_HELMET"));

            Assert.That(
                ContainsItem(
                    inventory,
                    "OLD_HELMET"),
                Is.True);
        }

        [Test]
        public void Unequip_EquippedItem_ReturnsItemToInventory()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "LEATHER_BOOTS",
                    "가죽 신발",
                    1,
                    1,
                    out int inventorySlot),
                Is.True);

            Assert.That(
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Boots,
                    EquipmentSlotType.Boots).Success,
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Unequip(
                    inventory,
                    equipment,
                    EquipmentSlotType.Boots);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Boots),
                Is.Null);

            Assert.That(
                ContainsItem(
                    inventory,
                    "LEATHER_BOOTS"),
                Is.True);
        }

        [Test]
        public void Unequip_FullInventory_FailsAndKeepsEquipment()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "LEGGINGS",
                    "가죽 레깅스",
                    1,
                    1,
                    out int leggingsSlot),
                Is.True);

            Assert.That(
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    leggingsSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Leggings,
                    EquipmentSlotType.Leggings).Success,
                Is.True);

            FillInventory(
                inventory);

            EquipmentActionResult result =
                EquipmentService.Unequip(
                    inventory,
                    equipment,
                    EquipmentSlotType.Leggings);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.InventoryFull));

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Leggings).ItemId,
                Is.EqualTo(
                    "LEGGINGS"));
        }

        private static bool ContainsItem(
            InventoryRunState inventory,
            string itemId)
        {
            for (int index = 0;
                 index < inventory.Slots.Count;
                 index++)
            {
                InventorySlotState slot =
                    inventory.Slots[index];

                if (slot != null
                    && !slot.IsEmpty
                    && slot.ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void FillInventory(
            InventoryRunState inventory)
        {
            int fillIndex =
                0;

            while (HasEmptySlot(
                inventory))
            {
                bool added =
                    inventory.TryAdd(
                        $"FILLER_{fillIndex}",
                        $"채움 아이템 {fillIndex}",
                        1,
                        1,
                        out _);

                Assert.That(
                    added,
                    Is.True);

                fillIndex++;
            }
        }

        private static bool HasEmptySlot(
            InventoryRunState inventory)
        {
            for (int index = 0;
                 index < inventory.Slots.Count;
                 index++)
            {
                if (inventory.Slots[index].IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
