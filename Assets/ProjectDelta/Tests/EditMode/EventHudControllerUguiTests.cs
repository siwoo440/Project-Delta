using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Tests.EditMode
{
    // 108일차: 이벤트 화면이 Scene 기반 uGUI 구조로 만들어졌는지 검증한다.
    public sealed class EventHudControllerUguiTests
    {
        [Test]
        public void Controller_DoesNotUseOnGui()
        {
            MethodInfo onGui =
                typeof(EventHudController).GetMethod(
                    "OnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                onGui,
                Is.Null);
        }

        [Test]
        public void Controller_HasSerializedPanelFields()
        {
            AssertSerializedField(
                "eventPanel",
                typeof(GameObject));

            AssertSerializedField(
                "titleText",
                typeof(Text));

            AssertSerializedField(
                "bodyText",
                typeof(Text));

            AssertSerializedField(
                "resultMessageText",
                typeof(Text));
        }

        [Test]
        public void Controller_HasSerializedChoiceArrays()
        {
            AssertSerializedField(
                "choiceButtons",
                typeof(Button[]));

            AssertSerializedField(
                "choiceButtonTexts",
                typeof(Text[]));
        }

        // 108일차 수정: 선택 즉시 닫히면 결과 메시지를 읽을 수 없어, 별도 닫기
        // 버튼으로 플레이어가 직접 닫게 했다.
        [Test]
        public void Controller_HasSerializedCloseButton()
        {
            AssertSerializedField(
                "closeButton",
                typeof(Button));
        }

        [Test]
        public void Controller_HasOpenAndCloseMethods()
        {
            Assert.That(
                typeof(EventHudController).GetMethod(
                    "Open",
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                typeof(EventHudController).GetMethod(
                    "Close",
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Not.Null);
        }

        private static void AssertSerializedField(
            string fieldName,
            System.Type expectedType)
        {
            FieldInfo field =
                typeof(EventHudController).GetField(
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
