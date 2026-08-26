using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class RuntimeItemDefinitionLookupCacheTests
    {
        private ItemDefinition definition;

        [SetUp]
        public void SetUp()
        {
            ResetCache();

            definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            definition.name =
                "DAY96_CACHE_ITEM";
        }

        [TearDown]
        public void TearDown()
        {
            if (definition != null)
            {
                Object.DestroyImmediate(
                    definition);
            }

            ResetCache();
        }

        [Test]
        public void TryFind_BuildsLookupCacheAndFindsLoadedDefinition()
        {
            bool found =
                RuntimeItemDefinitionLookup.TryFind(
                    definition.name,
                    out ItemDefinition resolved);

            Assert.That(
                found,
                Is.True);

            Assert.That(
                resolved,
                Is.SameAs(
                    definition));

            FieldInfo cacheField =
                typeof(RuntimeItemDefinitionLookup).GetField(
                    "itemLookup",
                    BindingFlags.Static
                    | BindingFlags.NonPublic);

            Assert.That(
                cacheField,
                Is.Not.Null);

            IDictionary cache =
                cacheField.GetValue(
                    null)
                as IDictionary;

            Assert.That(
                cache,
                Is.Not.Null);

            Assert.That(
                cache.Contains(
                    definition.name),
                Is.True);
        }

        [Test]
        public void ResolveMaxStackSize_UnknownItem_FallsBackToOne()
        {
            int maxStackSize =
                RuntimeItemDefinitionLookup.ResolveMaxStackSize(
                    "DAY96_UNKNOWN_ITEM");

            Assert.That(
                maxStackSize,
                Is.EqualTo(
                    1));
        }

        private static void ResetCache()
        {
            MethodInfo method =
                typeof(RuntimeItemDefinitionLookup).GetMethod(
                    "ResetCache",
                    BindingFlags.Static
                    | BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null);

            method.Invoke(
                null,
                null);
        }
    }
}
