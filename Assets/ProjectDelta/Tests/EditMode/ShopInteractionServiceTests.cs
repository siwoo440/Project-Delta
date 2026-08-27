using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 105일차: 상점 UI가 사용할 ItemDefinition 어댑터를 검증한다.
    public sealed class ShopInteractionServiceTests
    {
        [Test]
        public void Sell_UsesDefinitionCategoryAndBasePrice()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "OLD_SWORD",
                "낡은 검",
                1,
                1,
                out int slotIndex);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                0;

            ItemDefinition definition =
                CreateDefinition(
                    ItemCategory.Equipment,
                    100);

            try
            {
                ShopActionResult result =
                    ShopInteractionService.Sell(
                        inventory,
                        player,
                        slotIndex,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    result.GoldChange,
                    Is.EqualTo(50));

                Assert.That(
                    player.Gold,
                    Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void Sell_NullDefinition_FailsWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "OLD_SWORD",
                "낡은 검",
                1,
                1,
                out int slotIndex);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            ShopActionResult result =
                ShopInteractionService.Sell(
                    inventory,
                    player,
                    slotIndex,
                    null);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ShopActionFailureReason.ItemNotSellable));

            Assert.That(
                inventory.Slots[slotIndex].ItemId,
                Is.EqualTo("OLD_SWORD"));
        }

        [Test]
        public void CreateProduct_WithoutOverride_UsesDefinitionBasePrice()
        {
            ItemDefinition definition =
                CreateDefinition(
                    ItemCategory.Consumable,
                    40);

            try
            {
                ShopProductState product =
                    ShopInteractionService.CreateProduct(
                        definition);

                Assert.That(
                    product.Price,
                    Is.EqualTo(40));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void CreateProduct_WithOverride_UsesOverridePrice()
        {
            ItemDefinition definition =
                CreateDefinition(
                    ItemCategory.Consumable,
                    40);

            try
            {
                ShopProductState product =
                    ShopInteractionService.CreateProduct(
                        definition,
                        999);

                Assert.That(
                    product.Price,
                    Is.EqualTo(999));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void CreateProduct_NullDefinition_ReturnsNull()
        {
            Assert.That(
                ShopInteractionService.CreateProduct(
                    null),
                Is.Null);
        }

        private static ItemDefinition CreateDefinition(
            ItemCategory category,
            int basePrice)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                category);

            SetPrivateField(
                definition,
                "basePrice",
                basePrice);

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
