using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "ProjectDelta/Data/Item Definition")]
    public sealed class ItemDefinition : DefinitionBase
    {
        [SerializeField] private string displayName;

        public string DisplayName => displayName;
    }
}
