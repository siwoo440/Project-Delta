using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Tests.EditMode
{
    // 109일차: 저장 슬롯 UI가 Scene 기반 uGUI 구조로 만들어졌는지 검증한다.
    public sealed class SaveSlotHudControllerUguiTests
    {
        [Test]
        public void Controller_DoesNotUseOnGui()
        {
            MethodInfo onGui =
                typeof(SaveSlotHudController).GetMethod(
                    "OnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                onGui,
                Is.Null);
        }

        [Test]
        public void Controller_HasSerializedPanelAndCloseButton()
        {
            AssertSerializedField(
                "panel",
                typeof(GameObject));

            AssertSerializedField(
                "closeButton",
                typeof(Button));
        }

        [Test]
        public void Controller_HasSerializedRowsArray()
        {
            AssertSerializedField(
                "rows",
                typeof(SaveSlotRowRefs[]));
        }

        [Test]
        public void Controller_HasOpenAndCloseMethods()
        {
            Assert.That(
                typeof(SaveSlotHudController).GetMethod(
                    "Open",
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                typeof(SaveSlotHudController).GetMethod(
                    "Close",
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void SaveSlotRowRefs_HasExpectedUiFields()
        {
            Assert.That(
                typeof(SaveSlotRowRefs).GetField(
                    "slotLabelText"),
                Is.Not.Null);

            Assert.That(
                typeof(SaveSlotRowRefs).GetField(
                    "savedTimeText"),
                Is.Not.Null);

            Assert.That(
                typeof(SaveSlotRowRefs).GetField(
                    "playtimeText"),
                Is.Not.Null);

            Assert.That(
                typeof(SaveSlotRowRefs).GetField(
                    "emptyStatusText"),
                Is.Not.Null);

            Assert.That(
                typeof(SaveSlotRowRefs).GetField(
                    "saveButton"),
                Is.Not.Null);

            Assert.That(
                typeof(SaveSlotRowRefs).GetField(
                    "loadButton"),
                Is.Not.Null);
        }

        private static void AssertSerializedField(
            string fieldName,
            System.Type expectedType)
        {
            FieldInfo field =
                typeof(SaveSlotHudController).GetField(
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
