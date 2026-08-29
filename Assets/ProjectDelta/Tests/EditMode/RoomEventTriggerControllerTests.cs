using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Presentation;

namespace ProjectDelta.Tests.EditMode
{
    // 111일차: RoomEventTriggerController가 Scene 기반 참조로 구성됐는지 검증한다.
    public sealed class RoomEventTriggerControllerTests
    {
        [Test]
        public void Controller_HasSerializedFields()
        {
            AssertSerializedField(
                "movementController",
                typeof(PlayerGridMovementController));

            AssertSerializedField(
                "eventHudController",
                typeof(EventHudController));

            AssertSerializedField(
                "eventPool",
                typeof(EventDefinition[]));
        }

        [Test]
        public void Controller_HasRoomEnteredHandlerMethod()
        {
            MethodInfo handler =
                typeof(RoomEventTriggerController).GetMethod(
                    "HandleRoomEntered",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                handler,
                Is.Not.Null);
        }

        private static void AssertSerializedField(
            string fieldName,
            System.Type expectedType)
        {
            FieldInfo field =
                typeof(RoomEventTriggerController).GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Field not found: {fieldName}");

            Assert.That(
                field.FieldType,
                Is.EqualTo(
                    expectedType));

            Assert.That(
                field.GetCustomAttribute<UnityEngine.SerializeField>(),
                Is.Not.Null,
                $"Field not marked [SerializeField]: {fieldName}");
        }
    }
}
