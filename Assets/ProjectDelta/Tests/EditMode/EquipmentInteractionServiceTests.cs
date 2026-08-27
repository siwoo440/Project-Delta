using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 98일차: 인벤토리 ↔ 장비 UI가 사용할 EquipmentService 래퍼를 검증한다.
    public sealed class EquipmentInteractionServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void EquipFromInventory_UsesDefinitionOwnSlotAsBothDefinedAndTarget()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "IRON_SWORD",
                "철검",
                1,
                1,
                out int inventorySlot);

            ItemDefinition definition =
                CreateEquipmentDefinition(
                    EquipmentSlotType.Weapon);

            try
            {
                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    equipment.GetEquippedItem(
                        EquipmentSlotType.Weapon).ItemId,
                    Is.EqualTo(
                        "IRON_SWORD"));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void EquipFromInventory_NonEquipmentDefinition_FailsWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                1,
                5,
                out int inventorySlot);

            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.Consumable);

            try
            {
                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition);

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
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void EquipFromInventory_NullDefinition_FailsGracefully()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            EquipmentActionResult result =
                EquipmentInteractionService.EquipFromInventory(
                    inventory,
                    equipment,
                    0,
                    null);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.ItemNotEquipment));
        }

        [Test]
        public void Unequip_EmptySlot_FailsWithEquipmentSlotEmpty()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            EquipmentActionResult result =
                EquipmentInteractionService.Unequip(
                    inventory,
                    equipment,
                    EquipmentSlotType.Accessory);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.EquipmentSlotEmpty));
        }

        private static ItemDefinition CreateEquipmentDefinition(
            EquipmentSlotType slotType)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.Equipment);

            SetPrivateField(
                definition,
                "equipmentSlot",
                slotType);

            return definition;
        }

        private static void SetPrivateField(
            ItemDefinition definition,
            string fieldName,
            object value)
        {
            FieldInfo field =
                typeof(ItemDefinition).GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Private field not found: {fieldName}");

            field.SetValue(
                definition,
                value);
        }
    }
}
