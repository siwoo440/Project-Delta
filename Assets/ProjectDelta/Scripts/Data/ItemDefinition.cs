using System;
using System.Collections.Generic;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(
        fileName = "ItemDefinition",
        menuName = "ProjectDelta/Data/Item Definition")]
    public sealed class ItemDefinition : DefinitionBase
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        [SerializeField]
        private ItemCategory category =
            ItemCategory.Uncategorized;

        [SerializeField]
        [Min(1)]
        private int maxStackSize = 1;

        // 93일차: 아이템을 사용할 수 있는 상황.
        [SerializeField]
        private ItemUseContext useContext =
            ItemUseContext.Both;

        // 93일차: 하나의 아이템이 적용하는 실제 사용 효과 목록.
        [SerializeField]
        private ItemUseEffectDefinition[] useEffects =
            Array.Empty<ItemUseEffectDefinition>();

        public string DisplayName =>
            displayName;

        public Sprite Icon =>
            icon;

        public string Description =>
            description;

        public ItemCategory Category =>
            category;

        public int MaxStackSize =>
            Mathf.Max(
                1,
                maxStackSize);

        public ItemUseContext UseContext =>
            useContext;

        public IReadOnlyList<ItemUseEffectDefinition> UseEffects =>
            useEffects
            ?? Array.Empty<ItemUseEffectDefinition>();
    }
}
