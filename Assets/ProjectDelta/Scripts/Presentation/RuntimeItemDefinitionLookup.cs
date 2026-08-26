using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 런타임에 이미 로드된 아이템 정의를 ID/에셋명/표시명으로 조회한다.
    public static class RuntimeItemDefinitionLookup
    {
        private static readonly Dictionary<string, ItemDefinition> itemLookup =
            new Dictionary<string, ItemDefinition>();

        private static bool cacheInitialized;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            itemLookup.Clear();

            cacheInitialized =
                false;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InvalidateAfterSceneLoad()
        {
            cacheInitialized =
                false;
        }

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

            EnsureCache();

            if (!itemLookup.TryGetValue(
                    itemKey,
                    out definition)
                || definition == null)
            {
                definition =
                    null;

                return false;
            }

            return true;
        }

        public static string ResolveCanonicalItemId(
            string itemKey)
        {
            return TryFind(
                    itemKey,
                    out ItemDefinition definition)
                && !string.IsNullOrEmpty(
                    definition.Id)
                    ? definition.Id
                    : itemKey;
        }

        public static string ResolveDisplayName(
            string itemKey)
        {
            return TryFind(
                    itemKey,
                    out ItemDefinition definition)
                && !string.IsNullOrEmpty(
                    definition.DisplayName)
                    ? definition.DisplayName
                    : itemKey;
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

        private static void EnsureCache()
        {
            if (cacheInitialized)
            {
                return;
            }

            RebuildCache();
        }

        private static void RebuildCache()
        {
            itemLookup.Clear();

            ItemDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<ItemDefinition>();

            for (int index = 0;
                 index < definitions.Length;
                 index++)
            {
                ItemDefinition definition =
                    definitions[index];

                if (definition == null)
                {
                    continue;
                }

                AddLookupKey(
                    definition.Id,
                    definition);

                AddLookupKey(
                    definition.name,
                    definition);

                AddLookupKey(
                    definition.DisplayName,
                    definition);
            }

            cacheInitialized =
                true;
        }

        private static void AddLookupKey(
            string key,
            ItemDefinition definition)
        {
            if (string.IsNullOrEmpty(
                    key)
                || definition == null)
            {
                return;
            }

            itemLookup[key] =
                definition;
        }
    }
}
