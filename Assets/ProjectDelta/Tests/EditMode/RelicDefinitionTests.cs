using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 104일차: RelicDefinition이 이름·설명·저주 여부를 그대로 노출하는지 확인한다.
    public sealed class RelicDefinitionTests
    {
        private RelicDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition =
                ScriptableObject.CreateInstance<RelicDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (definition != null)
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ExposesConfiguredDisplayNameDescriptionAndCurse()
        {
            SetPrivateField(
                "displayName",
                "태양의 파편");

            SetPrivateField(
                "description",
                "획득 즉시 매 턴 체력을 소량 회복한다.");

            SetPrivateField(
                "isCursed",
                true);

            Assert.That(
                definition.DisplayName,
                Is.EqualTo("태양의 파편"));

            Assert.That(
                definition.Description,
                Is.EqualTo("획득 즉시 매 턴 체력을 소량 회복한다."));

            Assert.That(
                definition.IsCursed,
                Is.True);
        }

        [Test]
        public void IsCursed_DefaultsToFalse()
        {
            Assert.That(
                definition.IsCursed,
                Is.False);
        }

        private void SetPrivateField(
            string fieldName,
            object value)
        {
            FieldInfo field =
                typeof(RelicDefinition).GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Private field not found: {fieldName}");

            field.SetValue(
                definition,
                value);
        }
    }
}
