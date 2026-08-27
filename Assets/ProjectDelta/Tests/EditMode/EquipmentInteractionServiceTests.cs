using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 98일차: 인벤토리 ↔ 장비 UI가 사용할 EquipmentService 래퍼를 검증한다.
    public sealed class EquipmentInteractionServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void EquipFromInventory_UsesDefinitionOwnSlotAsBothDefinedAndTarget()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "IRON_SWORD",
                "철검",
                1,
                1,
                out int inventorySlot);

            ItemDefinition definition =
                CreateEquipmentDefinition(
                    EquipmentSlotType.Weapon);

            try
            {
                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    equipment.GetEquippedItem(
                        EquipmentSlotType.Weapon).ItemId,
                    Is.EqualTo(
                        "IRON_SWORD"));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void EquipFromInventory_NonEquipmentDefinition_FailsWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                1,
                5,
                out int inventorySlot);

            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.Consumable);

            try
            {
                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        EquipmentActionFailureReason.ItemNotEquipment));

                Assert.That(
                    inventory.TryGetSlot(
                        inventorySlot,
                        out InventorySlotState slot),
                    Is.True);

                Assert.That(
                    slot.ItemId,
                    Is.EqualTo(
                        "POTION"));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void EquipFromInventory_NullDefinition_FailsGracefully()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            EquipmentActionResult result =
                EquipmentInteractionService.EquipFromInventory(
                    inventory,
                    equipment,
                    0,
                    null);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.ItemNotEquipment));
        }

        [Test]
        public void EquipFromInventory_WithPlayer_AddsBonusesToFinalStats()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            int baseAttack =
                player.GetFinalStats().Attack;

            inventory.TryAdd(
                "IRON_SWORD",
                "철검",
                1,
                1,
                out int inventorySlot);

            ItemDefinition definition =
                CreateEquipmentDefinition(
                    EquipmentSlotType.Weapon,
                    new StatBlock
                    {
                        Attack = 12
                    });

            try
            {
                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition,
                        player);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    player.GetFinalStats().Attack,
                    Is.EqualTo(
                        baseAttack + 12));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void Unequip_WithPlayer_RemovesBonusesFromFinalStats()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            int baseAttack =
                player.GetFinalStats().Attack;

            inventory.TryAdd(
                "IRON_SWORD",
                "철검",
                1,
                1,
                out int inventorySlot);

            ItemDefinition definition =
                CreateEquipmentDefinition(
                    EquipmentSlotType.Weapon,
                    new StatBlock
                    {
                        Attack = 12
                    });

            try
            {
                EquipmentInteractionService.EquipFromInventory(
                    inventory,
                    equipment,
                    inventorySlot,
                    definition,
                    player);

                EquipmentActionResult result =
                    EquipmentInteractionService.Unequip(
                        inventory,
                        equipment,
                        EquipmentSlotType.Weapon,
                        player);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    player.GetFinalStats().Attack,
                    Is.EqualTo(
                        baseAttack));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void Unequip_WithPlayer_ClampsCurrentHpToReducedMaxHealth()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            inventory.TryAdd(
                "VITALITY_RING",
                "생명의 반지",
                1,
                1,
                out int inventorySlot);

            ItemDefinition definition =
                CreateEquipmentDefinition(
                    EquipmentSlotType.Accessory,
                    new StatBlock
                    {
                        MaxHealth = 50
                    });

            try
            {
                EquipmentInteractionService.EquipFromInventory(
                    inventory,
                    equipment,
                    inventorySlot,
                    definition,
                    player);

                // 장착 중 최대 체력이 150까지 늘어난 상태에서 최대치로 채워둔다.
                player.CurrentHp =
                    player.GetFinalStats().MaxHealth;

                EquipmentActionResult result =
                    EquipmentInteractionService.Unequip(
                        inventory,
                        equipment,
                        EquipmentSlotType.Accessory,
                        player);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    player.GetFinalStats().MaxHealth,
                    Is.EqualTo(
                        100));

                Assert.That(
                    player.CurrentHp,
                    Is.EqualTo(
                        100));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void Unequip_EmptySlot_FailsWithEquipmentSlotEmpty()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            EquipmentActionResult result =
                EquipmentInteractionService.Unequip(
                    inventory,
                    equipment,
                    EquipmentSlotType.Accessory);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.EquipmentSlotEmpty));
        }

        private static ItemDefinition CreateEquipmentDefinition(
            EquipmentSlotType slotType,
            StatBlock equipmentBonuses = null)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.Equipment);

            SetPrivateField(
                definition,
                "equipmentSlot",
                slotType);

            if (equipmentBonuses != null)
            {
                SetPrivateField(
                    definition,
                    "equipmentStatBonuses",
                    equipmentBonuses);
            }

            return definition;
        }

        private static void SetPrivateField(
            ItemDefinition definition,
            string fieldName,
            object value)
        {
            FieldInfo field =
                typeof(ItemDefinition).GetField(
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
