using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Tests.EditMode
{
    // 110일차: 현재 방 상태 표시 UI가 Scene 기반 uGUI 구조로 만들어졌는지 검증한다.
    public sealed class RoomStatusHudControllerUguiTests
    {
        [Test]
        public void Controller_DoesNotUseOnGui()
        {
            MethodInfo onGui =
                typeof(RoomStatusHudController).GetMethod(
                    "OnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                onGui,
                Is.Null);
        }

        [Test]
        public void Controller_HasSerializedMovementControllerAndText()
        {
            AssertSerializedField(
                "movementController",
                typeof(PlayerGridMovementController));

            AssertSerializedField(
                "roomStatusText",
                typeof(Text));
        }

        private static void AssertSerializedField(
            string fieldName,
            System.Type expectedType)
        {
            FieldInfo field =
                typeof(RoomStatusHudController).GetField(
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
                field.GetCustomAttribute<SerializeField>(),
                Is.Not.Null,
                $"Field not marked [SerializeField]: {fieldName}");
        }
    }
}
