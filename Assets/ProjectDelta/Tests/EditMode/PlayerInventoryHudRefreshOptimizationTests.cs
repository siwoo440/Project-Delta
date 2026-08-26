using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Domain;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class PlayerInventoryHudRefreshOptimizationTests
    {
        private GameObject gameObject;
        private PlayerInventoryHudController controller;

        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            RunContext.Begin(
                "DAY96_INVENTORY_HUD_REFRESH");

            gameObject =
                new GameObject(
                    "PlayerInventoryHudRefreshOptimizationTests");

            controller =
                gameObject.AddComponent<PlayerInventoryHudController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(
                    gameObject);
            }

            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void RefreshGate_AfterCapture_SameStateDoesNotRequestRefresh()
        {
            InvokePrivate(
                "CaptureRefreshSignature");

            bool shouldRefresh =
                (bool)InvokePrivate(
                    "ShouldRefreshInventory");

            Assert.That(
                shouldRefresh,
                Is.False);
        }

        [Test]
        public void RefreshGate_InventoryQuantityChange_RequestsRefresh()
        {
            InventoryRunState inventory =
                RunContext.Current.Inventory;

            Assert.That(
                inventory.TryAdd(
                    "POTION",
                    "포션",
                    1,
                    5,
                    out _),
                Is.True);

            InvokePrivate(
                "CaptureRefreshSignature");

            Assert.That(
                (bool)InvokePrivate(
                    "ShouldRefreshInventory"),
                Is.False);

            Assert.That(
                inventory.TryAdd(
                    "POTION",
                    "포션",
                    1,
                    5,
                    out _),
                Is.True);

            Assert.That(
                (bool)InvokePrivate(
                    "ShouldRefreshInventory"),
                Is.True);
        }

        [Test]
        public void RefreshGate_SelectedItemPlayerResourceChange_RequestsRefresh()
        {
            InventoryRunState inventory =
                RunContext.Current.Inventory;

            Assert.That(
                inventory.TryAdd(
                    "POTION",
                    "포션",
                    1,
                    5,
                    out int slotIndex),
                Is.True);

            SetPrivateField(
                "selectedSlotIndex",
                slotIndex);

            RunContext.Current.Player.CurrentHp =
                10;

            InvokePrivate(
                "CaptureRefreshSignature");

            Assert.That(
                (bool)InvokePrivate(
                    "ShouldRefreshInventory"),
                Is.False);

            RunContext.Current.Player.CurrentHp =
                9;

            Assert.That(
                (bool)InvokePrivate(
                    "ShouldRefreshInventory"),
                Is.True);
        }

        private object InvokePrivate(
            string methodName)
        {
            MethodInfo method =
                typeof(PlayerInventoryHudController).GetMethod(
                    methodName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"Private method not found: {methodName}");

            return method.Invoke(
                controller,
                null);
        }

        private void SetPrivateField(
            string fieldName,
            object value)
        {
            FieldInfo field =
                typeof(PlayerInventoryHudController).GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Private field not found: {fieldName}");

            field.SetValue(
                controller,
                value);
        }
    }
}
