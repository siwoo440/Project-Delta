using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class InventoryActionUiSerializationTests
    {
        [TestCase("useButton")]
        [TestCase("moveButton")]
        [TestCase("discardButton")]
        [TestCase("moveButtonText")]
        [TestCase("discardConfirmPanel")]
        [TestCase("discardConfirmText")]
        [TestCase("discardOneButton")]
        [TestCase("discardAllButton")]
        [TestCase("discardCancelButton")]
        public void ActionUiFields_AreSerializedSceneReferences(string fieldName)
        {
            FieldInfo field =
                typeof(PlayerInventoryHudController).GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(
                field.GetCustomAttribute<SerializeField>(),
                Is.Not.Null);
        }

        [Test]
        public void RuntimeActionUiFactory_IsRemoved()
        {
            MethodInfo method =
                typeof(PlayerInventoryHudController).GetMethod(
                    "EnsureActionUi",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Null);
        }
    }
}
