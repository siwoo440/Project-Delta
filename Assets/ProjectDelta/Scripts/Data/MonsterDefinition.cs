using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(fileName = "MonsterDefinition", menuName = "ProjectDelta/Data/Monster Definition")]
    public sealed class MonsterDefinition : DefinitionBase
    {
        [SerializeField] private string displayName;

        public string DisplayName => displayName;
    }
}
