using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    public sealed class BattleDropItemResult
    {
        public ItemDefinition Item { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public int Quantity { get; }

        public BattleDropItemResult(
            ItemDefinition item,
            int quantity)
        {
            Item =
                item;

            ItemId =
                item != null
                    ? item.Id
                    : string.Empty;

            DisplayName =
                item != null
                    ? item.DisplayName
                    : string.Empty;

            Quantity =
                Math.Max(
                    0,
                    quantity);
        }
    }

    public sealed class BattleDropResult
    {
        private readonly BattleDropItemResult[] items;

        public static BattleDropResult Empty { get; } =
            new BattleDropResult(
                0,
                Array.Empty<BattleDropItemResult>());

        public int Gold { get; }

        public IReadOnlyList<BattleDropItemResult> Items =>
            items;

        public bool IsEmpty =>
            Gold <= 0
            && items.Length == 0;

        public BattleDropResult(
            int gold,
            IEnumerable<BattleDropItemResult> items)
        {
            Gold =
                Math.Max(
                    0,
                    gold);

            if (items == null)
            {
                this.items =
                    Array.Empty<BattleDropItemResult>();

                return;
            }

            List<BattleDropItemResult> copied =
                new List<BattleDropItemResult>();

            foreach (BattleDropItemResult item
                     in items)
            {
                if (item == null
                    || item.Item == null
                    || item.Quantity <= 0)
                {
                    continue;
                }

                copied.Add(
                    item);
            }

            this.items =
                copied.ToArray();
        }
    }
}
