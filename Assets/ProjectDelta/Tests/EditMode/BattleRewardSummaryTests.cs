using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleRewardSummaryTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            for (int index = 0;
                 index < createdObjects.Count;
                 index++)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void SummaryIncludesGrowthAndDropGold()
        {
            BattleGrowthResult growth =
                new BattleGrowthResult(
                    90,
                    2,
                    3,
                    10,
                    0,
                    1,
                    1,
                    false);

            BattleDropResult drop =
                new BattleDropResult(
                    34,
                    Array.Empty<BattleDropItemResult>());

            string summary =
                BattleRewardSummaryFormatter.Build(
                    growth,
                    drop);

            StringAssert.Contains(
                "획득 경험치 +90 EXP",
                summary);

            StringAssert.Contains(
                "레벨 Lv.2 → Lv.3",
                summary);

            StringAssert.Contains(
                "스탯 포인트 +1",
                summary);

            StringAssert.Contains(
                "획득 골드 34 Gold",
                summary);
        }

        [Test]
        public void SummaryShowsNoneWhenNoItemsDropped()
        {
            string summary =
                BattleRewardSummaryFormatter.Build(
                    null,
                    BattleDropResult.Empty);

            StringAssert.Contains(
                "획득 아이템 없음",
                summary);
        }

        [Test]
        public void SummaryListsDroppedItems()
        {
            ItemDefinition potion =
                CreateItem(
                    "ITEM_POTION",
                    "작은 물약");

            ItemDefinition cloth =
                CreateItem(
                    "ITEM_CLOTH",
                    "낡은 천");

            BattleDropResult drop =
                new BattleDropResult(
                    0,
                    new[]
                    {
                        new BattleDropItemResult(
                            potion,
                            1),
                        new BattleDropItemResult(
                            cloth,
                            2)
                    });

            string summary =
                BattleRewardSummaryFormatter.Build(
                    null,
                    drop);

            StringAssert.Contains(
                "작은 물약 ×1",
                summary);

            StringAssert.Contains(
                "낡은 천 ×2",
                summary);
        }

        [Test]
        public void DropGoldPayoutAddsGoldToPlayer()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                10;

            BattleDropResult drop =
                new BattleDropResult(
                    34,
                    Array.Empty<BattleDropItemResult>());

            int applied =
                BattleRewardPayoutService.ApplyDropGold(
                    player,
                    drop);

            Assert.That(
                applied,
                Is.EqualTo(34));

            Assert.That(
                player.Gold,
                Is.EqualTo(44));
        }

        [Test]
        public void DropGoldPayoutSaturatesAtIntMax()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                int.MaxValue - 5;

            BattleDropResult drop =
                new BattleDropResult(
                    50,
                    Array.Empty<BattleDropItemResult>());

            BattleRewardPayoutService.ApplyDropGold(
                player,
                drop);

            Assert.That(
                player.Gold,
                Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void NullDropResultDoesNotChangeGold()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                77;

            int applied =
                BattleRewardPayoutService.ApplyDropGold(
                    player,
                    null);

            Assert.That(
                applied,
                Is.EqualTo(0));

            Assert.That(
                player.Gold,
                Is.EqualTo(77));
        }

        [Test]
        public void DroppedItemsAreAddedUsingCurrentMinimalInventory()
        {
            ItemDefinition potion =
                CreateItem(
                    "ITEM_POTION",
                    "작은 물약");

            BattleDropResult drop =
                new BattleDropResult(
                    0,
                    new[]
                    {
                        new BattleDropItemResult(
                            potion,
                            2)
                    });

            InventoryRunState inventory =
                new InventoryRunState();

            int added =
                BattleRewardPayoutService.ApplyDropItems(
                    inventory,
                    drop);

            Assert.That(
                added,
                Is.EqualTo(2));

            Assert.That(
                inventory.Items.Count,
                Is.EqualTo(2));

            Assert.That(
                inventory.Items[0].ItemId,
                Is.EqualTo("ITEM_POTION"));
        }

        [Test]
        public void DungeonSaveMapperPreservesGold()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY81_GOLD_SAVE");

            source.Player.Gold =
                321;

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    source);

            Assert.That(
                saved.PlayerStats.Gold,
                Is.EqualTo(321));

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    "DAY81_GOLD_RESTORE");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Player.Gold,
                Is.EqualTo(321));
        }

        private ItemDefinition CreateItem(
            string id,
            string displayName)
        {
            ItemDefinition item =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                typeof(DefinitionBase),
                item,
                "id",
                id);

            SetPrivateField(
                typeof(ItemDefinition),
                item,
                "displayName",
                displayName);

            createdObjects.Add(
                item);

            return item;
        }

        private static void SetPrivateField(
            Type declaringType,
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                declaringType.GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null);

            field.SetValue(
                target,
                value);
        }
    }
}
