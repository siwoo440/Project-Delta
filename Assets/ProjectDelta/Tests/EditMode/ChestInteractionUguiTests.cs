using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ChestInteractionUguiTests
    {
        [Test]
        public void ChestController_DoesNotUseOnGui()
        {
            MethodInfo onGui =
                typeof(ChestInteractionController).GetMethod(
                    "OnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(onGui, Is.Null);
        }

        [Test]
        public void ChestController_HasSerializedUguiView()
        {
            FieldInfo field =
                typeof(ChestInteractionController).GetField(
                    "interactionView",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(ChestInteractionView)));
            Assert.That(
                field.GetCustomAttribute<SerializeField>(),
                Is.Not.Null);
        }
    }
}
