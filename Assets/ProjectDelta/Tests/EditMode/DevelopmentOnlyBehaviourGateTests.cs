using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class DevelopmentOnlyBehaviourGateTests
    {
        [Test]
        public void Gate_HasSerializedBehaviourTargets()
        {
            FieldInfo field =
                typeof(DevelopmentOnlyBehaviourGate).GetField(
                    "targets",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(Behaviour[])));
            Assert.That(
                field.GetCustomAttribute<SerializeField>(),
                Is.Not.Null);
        }
    }
}
