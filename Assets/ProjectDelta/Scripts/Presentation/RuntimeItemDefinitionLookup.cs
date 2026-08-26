using System.Reflection;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 기존 상자 문자열 데이터와 91일차 ItemDefinition을 연결하기 위한 런타임 조회 도우미다.
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

                if (candidate.name
                        == itemKey
                    || candidate.DisplayName
                        == itemKey
                    || ReadDefinitionId(
                        candidate)
                        == itemKey)
                {
                    definition =
                        candidate;

                    return true;
                }
            }

            return false;
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

        private static string ReadDefinitionId(
            ItemDefinition definition)
        {
            PropertyInfo property =
                definition.GetType().GetProperty(
                    "DefinitionId",
                    BindingFlags.Public
                    | BindingFlags.Instance);

            if (property != null
                && property.PropertyType
                    == typeof(string))
            {
                return property.GetValue(
                    definition) as string;
            }

            property =
                definition.GetType().GetProperty(
                    "Id",
                    BindingFlags.Public
                    | BindingFlags.Instance);

            if (property != null
                && property.PropertyType
                    == typeof(string))
            {
                return property.GetValue(
                    definition) as string;
            }

            FieldInfo field =
                definition.GetType().GetField(
                    "definitionId",
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance);

            if (field != null
                && field.FieldType
                    == typeof(string))
            {
                return field.GetValue(
                    definition) as string;
            }

            field =
                definition.GetType().GetField(
                    "id",
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance);

            if (field != null
                && field.FieldType
                    == typeof(string))
            {
                return field.GetValue(
                    definition) as string;
            }

            return string.Empty;
        }
    }
}
