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
        [Min(1)]
        private int maxStackSize = 1;

        public string DisplayName =>
            displayName;

        public Sprite Icon =>
            icon;

        public string Description =>
            description;

        public int MaxStackSize =>
            Mathf.Max(
                1,
                maxStackSize);
    }
}
