using System;
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
                UnityEngine.Object.DestroyImmediate(
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
                UnityEngine.Object.DestroyImmediate(
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
                // 100일차: 장착 시 등급/랜덤 옵션이 적용되므로, 실제로 무엇이 적용됐는지는
                // 동일한 시드로 직접 굴려서 기대값을 구한 뒤 비교한다.
                EquipmentRollResult expectedRoll =
                    EquipmentRollService.Roll(
                        definition,
                        new System.Random(
                            99));

                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition,
                        player,
                        new System.Random(
                            99));

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    player.GetFinalStats().Attack,
                    Is.EqualTo(
                        baseAttack + expectedRoll.Bonuses.Attack));

                // 등급 배율이 1.0 이상이므로 최소한 원래 정의값 이상은 적용되어야 한다.
                Assert.That(
                    expectedRoll.Bonuses.Attack,
                    Is.GreaterThanOrEqualTo(
                        12 * 0.9));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
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
                UnityEngine.Object.DestroyImmediate(
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
                UnityEngine.Object.DestroyImmediate(
                    definition);
            }
        }

        // 100일차: EquipFromInventory가 EquipmentRollService로 굴린 등급/보너스를
        // 그대로 EquipmentItemState에 저장하는지 확인한다.
        // 103일차: 비교 UI가 미리 굴려둔 EquipmentRollResult를 그대로 넘기면,
        // 다시 굴리지 않고 그 값을 그대로 저장해야 한다(미리보기와 실제 장착 결과 일치).
        [Test]
        public void EquipFromInventory_WithPrecomputedRoll_UsesThatRollExactly()
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
                    EquipmentSlotType.Weapon,
                    new StatBlock
                    {
                        Attack = 20
                    });

            EquipmentRollResult precomputedRoll =
                new EquipmentRollResult(
                    EquipmentRarity.Legendary,
                    new StatBlock
                    {
                        Attack = 999
                    });

            try
            {
                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition,
                        precomputedRoll);

                Assert.That(
                    result.Success,
                    Is.True);

                EquipmentItemState equipped =
                    equipment.GetEquippedItem(
                        EquipmentSlotType.Weapon);

                Assert.That(
                    equipped.Rarity,
                    Is.EqualTo(
                        EquipmentRarity.Legendary));

                Assert.That(
                    equipped.EquipmentBonuses.Attack,
                    Is.EqualTo(999));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void EquipFromInventory_WithSeededRandom_StoresRolledRarityAndBonuses()
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
                    EquipmentSlotType.Weapon,
                    new StatBlock
                    {
                        Attack = 20
                    });

            try
            {
                System.Random random =
                    new System.Random(
                        42);

                EquipmentRollResult expectedRoll =
                    EquipmentRollService.Roll(
                        definition,
                        new System.Random(
                            42));

                EquipmentActionResult result =
                    EquipmentInteractionService.EquipFromInventory(
                        inventory,
                        equipment,
                        inventorySlot,
                        definition,
                        null,
                        random);

                Assert.That(
                    result.Success,
                    Is.True);

                EquipmentItemState equipped =
                    equipment.GetEquippedItem(
                        EquipmentSlotType.Weapon);

                Assert.That(
                    equipped.Rarity,
                    Is.EqualTo(
                        expectedRoll.Rarity));

                Assert.That(
                    equipped.EquipmentBonuses.Attack,
                    Is.EqualTo(
                        expectedRoll.Bonuses.Attack));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    definition);
            }
        }

        // 101일차: definition.EquipmentRequirements가 EquipFromInventory를 통해
        // 실제로 강제되는지(요구 조건 미달 시 인벤토리 변경 없이 실패) 확인한다.
        [Test]
        public void EquipFromInventory_RequirementNotMet_FailsWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            inventory.TryAdd(
                "HEAVY_SWORD",
                "육중한 대검",
                1,
                1,
                out int inventorySlot);

            ItemDefinition definition =
                CreateEquipmentDefinition(
                    EquipmentSlotType.Weapon,
                    null,
                    new StatBlock
                    {
                        Attack = 999
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
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        EquipmentActionFailureReason.RequirementNotMet));

                Assert.That(
                    inventory.TryGetSlot(
                        inventorySlot,
                        out InventorySlotState slot),
                    Is.True);

                Assert.That(
                    slot.ItemId,
                    Is.EqualTo(
                        "HEAVY_SWORD"));

                Assert.That(
                    equipment.GetEquippedItem(
                        EquipmentSlotType.Weapon),
                    Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
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
            StatBlock equipmentBonuses = null,
            StatBlock equipmentRequirements = null)
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

            if (equipmentRequirements != null)
            {
                SetPrivateField(
                    definition,
                    "equipmentRequirements",
                    equipmentRequirements);
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
