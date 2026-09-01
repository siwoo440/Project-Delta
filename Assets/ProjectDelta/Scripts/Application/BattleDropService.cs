using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    public static class BattleDropService
    {
        private sealed class ItemAccumulator
        {
            public ItemDefinition Item { get; }
            public int Quantity { get; private set; }

            public ItemAccumulator(
                ItemDefinition item,
                int quantity)
            {
                Item =
                    item;

                Quantity =
                    Math.Max(
                        0,
                        quantity);
            }

            public void Add(
                int quantity)
            {
                Quantity =
                    SaturatingAdd(
                        Quantity,
                        quantity);
            }
        }

        public static BattleDropResult RollBattleDrops(
            IEnumerable<MonsterDefinition> defeatedMonsters,
            IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(
                    nameof(randomSource));
            }

            if (defeatedMonsters == null)
            {
                return BattleDropResult.Empty;
            }

            int totalGold =
                0;

            Dictionary<string, ItemAccumulator> itemsById =
                new Dictionary<string, ItemAccumulator>(
                    StringComparer.Ordinal);

            List<string> itemOrder =
                new List<string>();

            foreach (MonsterDefinition monster
                     in defeatedMonsters)
            {
                if (monster == null
                    || monster.DropTable == null)
                {
                    continue;
                }

                MonsterDropTable table =
                    monster.DropTable;

                // 121일차: 정예/보스는 골드·아이템 드롭 확률에 같은 등급 배율을 곱해 더 후하게 준다.
                float rewardMultiplier =
                    MonsterTierRules.GetRewardMultiplier(
                        monster.Tier);

                totalGold =
                    SaturatingAdd(
                        totalGold,
                        (int)(
                            RollInclusive(
                                randomSource,
                                table.MinimumGold,
                                table.MaximumGold)
                            * rewardMultiplier));

                IReadOnlyList<MonsterDropEntry> entries =
                    table.ItemDrops;

                for (int index = 0;
                     index < entries.Count;
                     index++)
                {
                    MonsterDropEntry entry =
                        entries[index];

                    if (entry == null
                        || entry.Item == null
                        || string.IsNullOrEmpty(entry.Item.Id))
                    {
                        continue;
                    }

                    int scaledChanceBasisPoints =
                        Math.Min(
                            MonsterDropEntry.MaximumChanceBasisPoints,
                            (int)(
                                entry.ChanceBasisPoints
                                * rewardMultiplier));

                    if (!PassesChance(
                            scaledChanceBasisPoints,
                            randomSource))
                    {
                        continue;
                    }

                    int quantity =
                        RollInclusive(
                            randomSource,
                            entry.MinimumQuantity,
                            entry.MaximumQuantity);

                    if (quantity <= 0)
                    {
                        continue;
                    }

                    string itemId =
                        entry.Item.Id;

                    if (!itemsById.TryGetValue(
                            itemId,
                            out ItemAccumulator accumulator))
                    {
                        accumulator =
                            new ItemAccumulator(
                                entry.Item,
                                quantity);

                        itemsById.Add(
                            itemId,
                            accumulator);

                        itemOrder.Add(
                            itemId);

                        continue;
                    }

                    accumulator.Add(
                        quantity);
                }
            }

            List<BattleDropItemResult> itemResults =
                new List<BattleDropItemResult>(
                    itemOrder.Count);

            for (int index = 0;
                 index < itemOrder.Count;
                 index++)
            {
                ItemAccumulator accumulator =
                    itemsById[itemOrder[index]];

                itemResults.Add(
                    new BattleDropItemResult(
                        accumulator.Item,
                        accumulator.Quantity));
            }

            return new BattleDropResult(
                totalGold,
                itemResults);
        }

        private static bool PassesChance(
            int chanceBasisPoints,
            IRandomSource randomSource)
        {
            if (chanceBasisPoints <= 0)
            {
                return false;
            }

            if (chanceBasisPoints
                >= MonsterDropEntry.MaximumChanceBasisPoints)
            {
                return true;
            }

            return randomSource.NextInt(
                       0,
                       MonsterDropEntry.MaximumChanceBasisPoints)
                   < chanceBasisPoints;
        }

        private static int RollInclusive(
            IRandomSource randomSource,
            int minimum,
            int maximum)
        {
            int safeMinimum =
                Math.Max(
                    0,
                    minimum);

            int safeMaximum =
                Math.Max(
                    safeMinimum,
                    maximum);

            if (safeMinimum == safeMaximum)
            {
                return safeMinimum;
            }

            if (safeMaximum == int.MaxValue)
            {
                return randomSource.NextInt(
                    safeMinimum,
                    int.MaxValue);
            }

            return randomSource.NextInt(
                safeMinimum,
                safeMaximum + 1);
        }

        private static int SaturatingAdd(
            int current,
            int value)
        {
            long result =
                (long)Math.Max(
                    0,
                    current)
                + Math.Max(
                    0,
                    value);

            return result >= int.MaxValue
                ? int.MaxValue
                : (int)result;
        }
    }
}
