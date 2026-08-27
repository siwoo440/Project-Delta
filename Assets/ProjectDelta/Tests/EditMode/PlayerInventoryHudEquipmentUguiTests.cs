using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Tests.EditMode
{
    // 98일차: 인벤토리 ↔ 장비 장착 UI가 Scene 기반 uGUI 구조를 그대로 확장했는지 검증한다.
    public sealed class PlayerInventoryHudEquipmentUguiTests
    {
        [Test]
        public void Controller_HasSerializedEquipButton()
        {
            AssertSerializedField(
                "equipButton",
                typeof(Button));
        }

        [Test]
        public void Controller_HasSerializedEquipmentPanel()
        {
            AssertSerializedField(
                "equipmentPanel",
                typeof(GameObject));
        }

        [Test]
        public void Controller_HasSerializedEquipmentSlotArrays()
        {
            AssertSerializedField(
                "equipmentSlotButtons",
                typeof(Button[]));

            AssertSerializedField(
                "equipmentSlotIcons",
                typeof(Image[]));

            AssertSerializedField(
                "equipmentSlotNameTexts",
                typeof(Text[]));
        }

        [Test]
        public void Controller_HasEquipAndUnequipHandlers()
        {
            MethodInfo equipHandler =
                typeof(PlayerInventoryHudController).GetMethod(
                    "OnEquipButtonClicked",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                equipHandler,
                Is.Not.Null);

            MethodInfo unequipHandler =
                typeof(PlayerInventoryHudController).GetMethod(
                    "OnEquipmentSlotClicked",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                unequipHandler,
                Is.Not.Null);
        }

        // 103일차: 장비 비교 미리보기 굴림 캐시/빌더가 존재하는지 확인한다.
        [Test]
        public void Controller_HasEquipmentComparisonHelpers()
        {
            FieldInfo pendingRollField =
                typeof(PlayerInventoryHudController).GetField(
                    "pendingEquipRoll",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                pendingRollField,
                Is.Not.Null);

            MethodInfo updateRollMethod =
                typeof(PlayerInventoryHudController).GetMethod(
                    "UpdatePendingEquipRoll",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                updateRollMethod,
                Is.Not.Null);

            MethodInfo comparisonTextMethod =
                typeof(PlayerInventoryHudController).GetMethod(
                    "BuildEquipmentComparisonText",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                comparisonTextMethod,
                Is.Not.Null);
        }

        // 104일차: 유물 패널(읽기 전용) 필드가 노출되는지 확인한다.
        [Test]
        public void Controller_HasSerializedRelicPanel()
        {
            AssertSerializedField(
                "relicPanel",
                typeof(GameObject));

            AssertSerializedField(
                "relicSlotNameTexts",
                typeof(Text[]));
        }

        [Test]
        public void Controller_HasRefreshRelicPanelMethod()
        {
            MethodInfo refreshRelicPanel =
                typeof(PlayerInventoryHudController).GetMethod(
                    "RefreshRelicPanel",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                refreshRelicPanel,
                Is.Not.Null);
        }

        [Test]
        public void Controller_DoesNotUseOnGui()
        {
            MethodInfo onGui =
                typeof(PlayerInventoryHudController).GetMethod(
                    "OnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                onGui,
                Is.Null);
        }

        private static void AssertSerializedField(
            string fieldName,
            System.Type expectedType)
        {
            FieldInfo field =
                typeof(PlayerInventoryHudController).GetField(
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
