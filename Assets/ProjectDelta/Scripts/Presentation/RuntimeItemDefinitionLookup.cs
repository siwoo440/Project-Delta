using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    public static class RuntimeItemDefinitionLookup
    {
        public static bool TryFind(
            string itemKey,
            out ItemDefinition definition)
        {
            definition =
                null;

            if (string.IsNullOrEmpty(
                    itemKey))
            {
                return false;
            }

            ItemDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<ItemDefinition>();

            for (int index = 0;
                 index < definitions.Length;
                 index++)
            {
                ItemDefinition candidate =
                    definitions[index];

                if (candidate == null)
                {
                    continue;
                }

                if ((!string.IsNullOrEmpty(
                            candidate.Id)
                        && candidate.Id
                            == itemKey)
                    || candidate.name
                        == itemKey
                    || candidate.DisplayName
                        == itemKey)
                {
                    definition =
                        candidate;

                    return true;
                }
            }

            return false;
        }

        public static string ResolveCanonicalItemId(
            string itemKey)
        {
            if (TryFind(
                    itemKey,
                    out ItemDefinition definition)
                && !string.IsNullOrEmpty(
                    definition.Id))
            {
                return definition.Id;
            }

            return itemKey;
        }

        public static string ResolveDisplayName(
            string itemKey)
        {
            if (TryFind(
                    itemKey,
                    out ItemDefinition definition)
                && !string.IsNullOrEmpty(
                    definition.DisplayName))
            {
                return definition.DisplayName;
            }

            return itemKey;
        }

        public static int ResolveMaxStackSize(
            string itemKey)
        {
            return TryFind(
                    itemKey,
                    out ItemDefinition definition)
                ? definition.MaxStackSize
                : 1;
        }

        public static ItemCategory ResolveCategory(
            string itemKey)
        {
            return TryFind(
                    itemKey,
                    out ItemDefinition definition)
                ? definition.Category
                : ItemCategory.Uncategorized;
        }
    }
}
