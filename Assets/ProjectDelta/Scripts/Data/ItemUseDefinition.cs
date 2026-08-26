using System;
using UnityEngine;

namespace ProjectDelta.Data
{
    public enum ItemUseContext
    {
        Both = 0,

        Exploration = 1,

        Battle = 2
    }

    public enum ItemUseEffectKind
    {
        None = 0,

        RestoreHp = 1,

        RestoreMana = 2,

        RestoreStamina = 3
    }

    [Serializable]
    public sealed class ItemUseEffectDefinition
    {
        [SerializeField]
        private ItemUseEffectKind kind =
            ItemUseEffectKind.None;

        [SerializeField]
        [Min(0)]
        private int value;

        public ItemUseEffectKind Kind =>
            kind;

        public int Value =>
            Mathf.Max(
                0,
                value);

        public ItemUseEffectDefinition()
        {
        }

        public ItemUseEffectDefinition(
            ItemUseEffectKind kind,
            int value)
        {
            this.kind =
                kind;

            this.value =
                Mathf.Max(
                    0,
                    value);
        }
    }
}
