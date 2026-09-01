using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleDropServiceTests
    {
        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public FixedRandomSource(
                params int[] values)
            {
                this.values =
                    new Queue<int>(
                        values ?? Array.Empty<int>());
            }

            public int NextInt(
                int minInclusive,
                int maxExclusive)
            {
                if (values.Count == 0)
                {
                    return minInclusive;
                }

                int value =
                    values.Dequeue();

                if (value < minInclusive
                    || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"고정 난수 {value}가 범위 [{minInclusive}, {maxExclusive}) 밖입니다.");
                }

                return value;
            }
        }

        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
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
        public void MonsterWithoutDropTableProducesNoDrop()
        {
            MonsterDefinition monster =
                CreateMonster(
                    "MON_NONE",
                    null);

            BattleDropResult result =
                BattleDropService.RollBattleDrops(
                    new[] { monster },
                    new FixedRandomSource());

            Assert.That(
                result.Gold,
                Is.EqualTo(0));

            Assert.That(
                result.Items,
                Is.Empty);
        }

        [Test]
        public void BossTier_DoublesGoldCompareToNormal()
        {
            MonsterDropTable table =
                CreateTable(
                    10,
                    10);

            MonsterDefinition normalMonster =
                CreateMonster(
                    "MON_NORMAL",
                    table,
                    MonsterTier.Normal);

            MonsterDefinition bossMonster =
                CreateMonster(
                    "MON_BOSS",
                    table,
                    MonsterTier.Boss);

            BattleDropResult normalResult =
                BattleDropService.RollBattleDrops(
                    new[] { normalMonster },
                    new FixedRandomSource(10));

            BattleDropResult bossResult =
                BattleDropService.RollBattleDrops(
                    new[] { bossMonster },
                    new FixedRandomSource(10));

            Assert.That(
                bossResult.Gold,
                Is.EqualTo(
                    normalResult.Gold * 2));
        }

        [Test]
        public void ZeroPercentItemNeverDrops()
        {
            ItemDefinition item =
                CreateItem(
                    "ITEM_ZERO",
                    "Zero");

            MonsterDropTable table =
                CreateTable(
                    0,
                    0,
                    new MonsterDropEntry(
                        item,
                        0,
                        1,
                        1));

            MonsterDefinition monster =
                CreateMonster(
                    "MON_ZERO",
                    table);

            BattleDropResult result =
                BattleDropService.RollBattleDrops(
                    new[] { monster },
                    new FixedRandomSource());

            Assert.That(
                result.Items,
                Is.Empty);
        }

        [Test]
        public void GuaranteedItemAlwaysDrops()
        {
            ItemDefinition item =
                CreateItem(
                    "ITEM_ALWAYS",
                    "Always");

            MonsterDropTable table =
                CreateTable(
                    0,
                    0,
                    new MonsterDropEntry(
                        item,
                        MonsterDropEntry.MaximumChanceBasisPoints,
                        2,
                        2));

            MonsterDefinition monster =
                CreateMonster(
                    "MON_ALWAYS",
                    table);

            BattleDropResult result =
                BattleDropService.RollBattleDrops(
                    new[] { monster },
                    new FixedRandomSource());

            Assert.That(
                result.Items.Count,
                Is.EqualTo(1));

            Assert.That(
                result.Items[0].ItemId,
                Is.EqualTo("ITEM_ALWAYS"));

            Assert.That(
                result.Items[0].Quantity,
                Is.EqualTo(2));
        }

        [Test]
        public void GoldRollIncludesConfiguredMaximum()
        {
            MonsterDropTable table =
                CreateTable(
                    5,
                    12);

            MonsterDefinition monster =
                CreateMonster(
                    "MON_GOLD",
                    table);

            BattleDropResult result =
                BattleDropService.RollBattleDrops(
                    new[] { monster },
                    new FixedRandomSource(12));

            Assert.That(
                result.Gold,
                Is.EqualTo(12));
        }

        [Test]
        public void MultipleMonstersMergeSameItemQuantityAndGold()
        {
            ItemDefinition item =
                CreateItem(
                    "ITEM_SHARED",
                    "Shared");

            MonsterDefinition first =
                CreateMonster(
                    "MON_FIRST",
                    CreateTable(
                        3,
                        3,
                        new MonsterDropEntry(
                            item,
                            MonsterDropEntry.MaximumChanceBasisPoints,
                            1,
                            1)));

            MonsterDefinition second =
                CreateMonster(
                    "MON_SECOND",
                    CreateTable(
                        4,
                        4,
                        new MonsterDropEntry(
                            item,
                            MonsterDropEntry.MaximumChanceBasisPoints,
                            2,
                            2)));

            BattleDropResult result =
                BattleDropService.RollBattleDrops(
                    new[]
                    {
                        first,
                        second
                    },
                    new FixedRandomSource());

            Assert.That(
                result.Gold,
                Is.EqualTo(7));

            Assert.That(
                result.Items.Count,
                Is.EqualTo(1));

            Assert.That(
                result.Items[0].Quantity,
                Is.EqualTo(3));
        }

        [Test]
        public void ChanceUsesBasisPointBoundary()
        {
            ItemDefinition item =
                CreateItem(
                    "ITEM_HALF",
                    "Half");

            MonsterDropTable table =
                CreateTable(
                    0,
                    0,
                    new MonsterDropEntry(
                        item,
                        5000,
                        1,
                        1));

            MonsterDefinition monster =
                CreateMonster(
                    "MON_HALF",
                    table);

            BattleDropResult success =
                BattleDropService.RollBattleDrops(
                    new[] { monster },
                    new FixedRandomSource(4999));

            BattleDropResult failure =
                BattleDropService.RollBattleDrops(
                    new[] { monster },
                    new FixedRandomSource(5000));

            Assert.That(
                success.Items.Count,
                Is.EqualTo(1));

            Assert.That(
                failure.Items,
                Is.Empty);
        }

        [Test]
        public void NullRandomSourceIsRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () => BattleDropService.RollBattleDrops(
                    Array.Empty<MonsterDefinition>(),
                    null));
        }

        private MonsterDropTable CreateTable(
            int minimumGold,
            int maximumGold,
            params MonsterDropEntry[] drops)
        {
            MonsterDropTable table =
                MonsterDropTable.CreateRuntime(
                    minimumGold,
                    maximumGold,
                    drops);

            createdObjects.Add(
                table);

            return table;
        }

        private MonsterDefinition CreateMonster(
            string id,
            MonsterDropTable table,
            MonsterTier tier = MonsterTier.Normal)
        {
            MonsterDefinition monster =
                ScriptableObject.CreateInstance<MonsterDefinition>();

            SetDefinitionId(
                monster,
                id);

            SetPrivateField(
                typeof(MonsterDefinition),
                monster,
                "dropTable",
                table);

            SetPrivateField(
                typeof(MonsterDefinition),
                monster,
                "tier",
                tier);

            createdObjects.Add(
                monster);

            return monster;
        }

        private ItemDefinition CreateItem(
            string id,
            string displayName)
        {
            ItemDefinition item =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetDefinitionId(
                item,
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

        private static void SetDefinitionId(
            DefinitionBase definition,
            string id)
        {
            SetPrivateField(
                typeof(DefinitionBase),
                definition,
                "id",
                id);
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
                Is.Not.Null,
                $"필드를 찾지 못했습니다: {declaringType.Name}.{fieldName}");

            field.SetValue(
                target,
                value);
        }
    }
}
