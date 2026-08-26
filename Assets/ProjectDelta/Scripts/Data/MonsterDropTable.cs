using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    [Serializable]
    public sealed class MonsterDropEntry
    {
        public const int MaximumChanceBasisPoints = 10000;

        [SerializeField] private ItemDefinition item;

        [Range(0, MaximumChanceBasisPoints)]
        [SerializeField] private int chanceBasisPoints;

        [Min(1)]
        [SerializeField] private int minimumQuantity = 1;

        [Min(1)]
        [SerializeField] private int maximumQuantity = 1;

        public ItemDefinition Item => item;

        public int ChanceBasisPoints =>
            Mathf.Clamp(
                chanceBasisPoints,
                0,
                MaximumChanceBasisPoints);

        public int MinimumQuantity =>
            Mathf.Max(
                1,
                minimumQuantity);

        public int MaximumQuantity =>
            Mathf.Max(
                MinimumQuantity,
                maximumQuantity);

        public MonsterDropEntry()
        {
        }

        public MonsterDropEntry(
            ItemDefinition item,
            int chanceBasisPoints,
            int minimumQuantity,
            int maximumQuantity)
        {
            this.item =
                item;

            this.chanceBasisPoints =
                Mathf.Clamp(
                    chanceBasisPoints,
                    0,
                    MaximumChanceBasisPoints);

            this.minimumQuantity =
                Mathf.Max(
                    1,
                    minimumQuantity);

            this.maximumQuantity =
                Mathf.Max(
                    this.minimumQuantity,
                    maximumQuantity);
        }
    }

    [CreateAssetMenu(
        fileName = "MonsterDropTable",
        menuName = "Project Delta/Data/Monster Drop Table")]
    public sealed class MonsterDropTable : ScriptableObject
    {
        private static readonly MonsterDropEntry[] EmptyEntries =
            Array.Empty<MonsterDropEntry>();

        [Header("골드")]
        [Min(0)]
        [SerializeField] private int minimumGold;

        [Min(0)]
        [SerializeField] private int maximumGold;

        [Header("아이템 드롭")]
        [SerializeField] private List<MonsterDropEntry> itemDrops =
            new List<MonsterDropEntry>();

        public int MinimumGold =>
            Math.Max(
                0,
                minimumGold);

        public int MaximumGold =>
            Math.Max(
                MinimumGold,
                maximumGold);

        public IReadOnlyList<MonsterDropEntry> ItemDrops =>
            itemDrops != null
                ? itemDrops
                : EmptyEntries;

        public static MonsterDropTable CreateRuntime(
            int minimumGold,
            int maximumGold,
            IEnumerable<MonsterDropEntry> itemDrops = null)
        {
            MonsterDropTable table =
                CreateInstance<MonsterDropTable>();

            table.minimumGold =
                Math.Max(
                    0,
                    minimumGold);

            table.maximumGold =
                Math.Max(
                    table.minimumGold,
                    maximumGold);

            table.itemDrops =
                itemDrops != null
                    ? new List<MonsterDropEntry>(itemDrops)
                    : new List<MonsterDropEntry>();

            return table;
        }
    }
}
