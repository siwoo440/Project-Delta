using UnityEngine;

namespace ProjectDelta.Data
{
    // ID convention: <CATEGORY>_<NAME>, e.g. MON_SLIME, ITEM_HEAL_SMALL.
    // IDs are permanent once shipped; display names may change, IDs never do.
    public abstract class DefinitionBase : ScriptableObject
    {
        [SerializeField] private string id;

        public string Id => id;
    }
}
