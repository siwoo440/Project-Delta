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

        // 모든 실제 아이템은 7개 분류 중 하나를 지정한다.
        // 기존 에셋의 안전한 마이그레이션을 위해 기본값은 미분류다.
        [SerializeField]
        private ItemCategory category =
            ItemCategory.Uncategorized;

        [SerializeField]
        [Min(1)]
        private int maxStackSize = 1;

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
    }
}
