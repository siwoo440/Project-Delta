using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 102일차: 가방 사용이 인벤토리 슬롯을 즉시·영구적으로 확장하고 소모되는지 검증한다.
    public sealed class BagExpansionServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void ApplyAndConsume_SmallBag_AddsTwoSlotsAndConsumesItem()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "SMALL_BAG",
                "소형 가방",
                1,
                1,
                out int slotIndex);

            ItemDefinition definition =
                CreateBagDefinition(
                    BagTier.Small);

            try
            {
                BagExpansionResult result =
                    BagExpansionService.ApplyAndConsume(
                        inventory,
                        slotIndex,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    result.AddedSlotBonus,
                    Is.EqualTo(2));

                Assert.That(
                    inventory.BagSlotBonus,
                    Is.EqualTo(2));

                Assert.That(
                    inventory.TryGetSlot(
                        slotIndex,
                        out InventorySlotState slot),
                    Is.True);

                Assert.That(
                    slot.IsEmpty,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ApplyAndConsume_MultipleBags_StacksSlotBonus()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            ItemDefinition smallBag =
                CreateBagDefinition(
                    BagTier.Small);

            ItemDefinition largeBag =
                CreateBagDefinition(
                    BagTier.Large);

            try
            {
                inventory.TryAdd(
                    "SMALL_BAG",
                    "소형 가방",
                    1,
                    1,
                    out int smallSlot);

                BagExpansionService.ApplyAndConsume(
                    inventory,
                    smallSlot,
                    smallBag);

                inventory.TryAdd(
                    "LARGE_BAG",
                    "대형 가방",
                    1,
                    1,
                    out int largeSlot);

                BagExpansionResult result =
                    BagExpansionService.ApplyAndConsume(
                        inventory,
                        largeSlot,
                        largeBag);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    result.NewBagSlotBonus,
                    Is.EqualTo(8));

                Assert.That(
                    inventory.BagSlotBonus,
                    Is.EqualTo(8));
            }
            finally
            {
                Object.DestroyImmediate(
                    smallBag);

                Object.DestroyImmediate(
                    largeBag);
            }
        }

        [Test]
        public void ApplyAndConsume_NonBagDefinition_FailsWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                1,
                5,
                out int slotIndex);

            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.Consumable);

            try
            {
                BagExpansionResult result =
                    BagExpansionService.ApplyAndConsume(
                        inventory,
                        slotIndex,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        BagExpansionFailureReason.NotABag));

                Assert.That(
                    inventory.TryGetSlot(
                        slotIndex,
                        out InventorySlotState slot),
                    Is.True);

                Assert.That(
                    slot.ItemId,
                    Is.EqualTo(
                        "POTION"));

                Assert.That(
                    inventory.BagSlotBonus,
                    Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ApplyAndConsume_EmptySlot_FailsWithInvalidSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            ItemDefinition definition =
                CreateBagDefinition(
                    BagTier.Medium);

            try
            {
                BagExpansionResult result =
                    BagExpansionService.ApplyAndConsume(
                        inventory,
                        0,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        BagExpansionFailureReason.InvalidSlot));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ApplyAndConsume_NullInventory_FailsWithInvalidInventory()
        {
            ItemDefinition definition =
                CreateBagDefinition(
                    BagTier.Medium);

            try
            {
                BagExpansionResult result =
                    BagExpansionService.ApplyAndConsume(
                        null,
                        0,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        BagExpansionFailureReason.InvalidInventory));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        private static ItemDefinition CreateBagDefinition(
            BagTier tier)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.ExplorationTool);

            SetPrivateField(
                definition,
                "bagTier",
                tier);

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
