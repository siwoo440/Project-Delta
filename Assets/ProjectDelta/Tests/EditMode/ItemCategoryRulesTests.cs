using System;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ItemCategoryRulesTests
    {
        [Test]
        public void GameplayCategories_CountIsSeven()
        {
            int gameplayCategoryCount =
                0;

            Array values =
                Enum.GetValues(
                    typeof(ItemCategory));

            foreach (ItemCategory category in values)
            {
                if (ItemCategoryRules.IsGameplayCategory(
                        category))
                {
                    gameplayCategoryCount++;
                }
            }

            Assert.That(
                gameplayCategoryCount,
                Is.EqualTo(7));
        }

        [Test]
        public void Uncategorized_IsNotGameplayCategory()
        {
            Assert.That(
                ItemCategoryRules.IsGameplayCategory(
                    ItemCategory.Uncategorized),
                Is.False);
        }

        [Test]
        public void Consumable_AllowsUseSellDiscard_ButNotEquip()
        {
            Assert.That(
                ItemCategoryRules.CanUse(
                    ItemCategory.Consumable),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanSell(
                    ItemCategory.Consumable),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanDiscard(
                    ItemCategory.Consumable),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanEquip(
                    ItemCategory.Consumable),
                Is.False);
        }

        [Test]
        public void ExplorationTool_AllowsUseSellDiscard_ButNotEquip()
        {
            Assert.That(
                ItemCategoryRules.CanUse(
                    ItemCategory.ExplorationTool),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanSell(
                    ItemCategory.ExplorationTool),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanDiscard(
                    ItemCategory.ExplorationTool),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanEquip(
                    ItemCategory.ExplorationTool),
                Is.False);
        }

        [Test]
        public void KeyItem_DisallowsAllCommonActions()
        {
            Assert.That(
                ItemCategoryRules.GetUseAvailability(
                    ItemCategory.KeyItem),
                Is.EqualTo(
                    ItemActionAvailability.Unavailable));

            Assert.That(
                ItemCategoryRules.GetSellAvailability(
                    ItemCategory.KeyItem),
                Is.EqualTo(
                    ItemActionAvailability.Unavailable));

            Assert.That(
                ItemCategoryRules.GetDiscardAvailability(
                    ItemCategory.KeyItem),
                Is.EqualTo(
                    ItemActionAvailability.Unavailable));

            Assert.That(
                ItemCategoryRules.GetEquipAvailability(
                    ItemCategory.KeyItem),
                Is.EqualTo(
                    ItemActionAvailability.Unavailable));
        }

        [Test]
        public void Treasure_AllowsSellAndDiscardOnly()
        {
            Assert.That(
                ItemCategoryRules.CanSell(
                    ItemCategory.Treasure),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanDiscard(
                    ItemCategory.Treasure),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanUse(
                    ItemCategory.Treasure),
                Is.False);

            Assert.That(
                ItemCategoryRules.CanEquip(
                    ItemCategory.Treasure),
                Is.False);
        }

        [Test]
        public void Equipment_AllowsSellDiscardAndEquip()
        {
            Assert.That(
                ItemCategoryRules.CanSell(
                    ItemCategory.Equipment),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanDiscard(
                    ItemCategory.Equipment),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanEquip(
                    ItemCategory.Equipment),
                Is.True);

            Assert.That(
                ItemCategoryRules.CanUse(
                    ItemCategory.Equipment),
                Is.False);
        }

        [Test]
        public void Relic_SellAndDiscardAreConditional()
        {
            Assert.That(
                ItemCategoryRules.GetSellAvailability(
                    ItemCategory.Relic),
                Is.EqualTo(
                    ItemActionAvailability.Conditional));

            Assert.That(
                ItemCategoryRules.GetDiscardAvailability(
                    ItemCategory.Relic),
                Is.EqualTo(
                    ItemActionAvailability.Conditional));
        }

        [Test]
        public void Cursed_DiscardAndEquipAreConditional()
        {
            Assert.That(
                ItemCategoryRules.GetDiscardAvailability(
                    ItemCategory.Cursed),
                Is.EqualTo(
                    ItemActionAvailability.Conditional));

            Assert.That(
                ItemCategoryRules.GetEquipAvailability(
                    ItemCategory.Cursed),
                Is.EqualTo(
                    ItemActionAvailability.Conditional));
        }

        [TestCase(
            ItemCategory.Consumable,
            "소비 아이템")]
        [TestCase(
            ItemCategory.ExplorationTool,
            "탐험 도구")]
        [TestCase(
            ItemCategory.KeyItem,
            "중요 아이템")]
        [TestCase(
            ItemCategory.Treasure,
            "보물")]
        [TestCase(
            ItemCategory.Equipment,
            "장비")]
        [TestCase(
            ItemCategory.Relic,
            "유물")]
        [TestCase(
            ItemCategory.Cursed,
            "저주")]
        public void DisplayName_ReturnsKoreanCategoryName(
            ItemCategory category,
            string expected)
        {
            Assert.That(
                ItemCategoryRules.GetDisplayName(
                    category),
                Is.EqualTo(
                    expected));
        }

        [Test]
        public void ItemDefinition_DefaultsToUncategorized()
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            try
            {
                Assert.That(
                    definition.Category,
                    Is.EqualTo(
                        ItemCategory.Uncategorized));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    definition);
            }
        }
    }
}
