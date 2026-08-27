using System;
using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EquipmentServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }
        }

        [Test]
        public void EquipmentSlotType_HasSixExpectedSlots()
        {
            EquipmentSlotType[] slots =
                (EquipmentSlotType[])Enum.GetValues(
                    typeof(EquipmentSlotType));

            Assert.That(
                slots,
                Is.EqualTo(
                    new[]
                    {
                        EquipmentSlotType.Weapon,
                        EquipmentSlotType.Helmet,
                        EquipmentSlotType.ChestArmor,
                        EquipmentSlotType.Leggings,
                        EquipmentSlotType.Boots,
                        EquipmentSlotType.Accessory
                    }));
        }

        [Test]
        public void RunContext_Begin_CreatesEmptyEquipmentState()
        {
            RunContext context =
                RunContext.Begin(
                    "DAY97_EQUIPMENT");

            Assert.That(
                context.Equipment,
                Is.Not.Null);

            foreach (EquipmentSlotType slotType
                     in Enum.GetValues(
                         typeof(EquipmentSlotType)))
            {
                Assert.That(
                    context.Equipment.GetEquippedItem(
                        slotType),
                    Is.Null);
            }
        }

        [Test]
        public void Equip_EquipmentItem_MovesOneItemFromInventoryToDefinedSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "IRON_SWORD",
                    "철검",
                    1,
                    1,
                    out int inventorySlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Weapon).ItemId,
                Is.EqualTo(
                    "IRON_SWORD"));

            Assert.That(
                inventory.TryGetSlot(
                    inventorySlot,
                    out InventorySlotState slot),
                Is.True);

            Assert.That(
                slot.IsEmpty,
                Is.True);
        }

        [Test]
        public void Equip_DefinedSlotAndTargetSlotDiffer_IsRejectedWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "IRON_HELMET",
                    "강철 투구",
                    1,
                    1,
                    out int inventorySlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Boots);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.WrongEquipmentSlot));

            Assert.That(
                inventory.TryGetSlot(
                    inventorySlot,
                    out InventorySlotState inventoryItem),
                Is.True);

            Assert.That(
                inventoryItem.ItemId,
                Is.EqualTo(
                    "IRON_HELMET"));

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Helmet),
                Is.Null);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Boots),
                Is.Null);
        }

        [Test]
        public void Equip_NonEquipmentItem_IsRejectedWithoutInventoryMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "POTION",
                    "포션",
                    1,
                    5,
                    out int inventorySlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Consumable,
                    EquipmentSlotType.Accessory,
                    EquipmentSlotType.Accessory);

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

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Accessory),
                Is.Null);
        }

        [Test]
        public void Equip_OccupiedSlot_ReturnsOldEquipmentToInventory()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "OLD_HELMET",
                    "낡은 투구",
                    1,
                    1,
                    out int oldSlot),
                Is.True);

            Assert.That(
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    oldSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Helmet).Success,
                Is.True);

            Assert.That(
                inventory.TryAdd(
                    "NEW_HELMET",
                    "강철 투구",
                    1,
                    1,
                    out int newSlot),
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    newSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Helmet);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Helmet).ItemId,
                Is.EqualTo(
                    "NEW_HELMET"));

            Assert.That(
                ContainsItem(
                    inventory,
                    "OLD_HELMET"),
                Is.True);
        }

        [Test]
        public void Unequip_EquippedItem_ReturnsItemToInventory()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "LEATHER_BOOTS",
                    "가죽 신발",
                    1,
                    1,
                    out int inventorySlot),
                Is.True);

            Assert.That(
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Boots,
                    EquipmentSlotType.Boots).Success,
                Is.True);

            EquipmentActionResult result =
                EquipmentService.Unequip(
                    inventory,
                    equipment,
                    EquipmentSlotType.Boots);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Boots),
                Is.Null);

            Assert.That(
                ContainsItem(
                    inventory,
                    "LEATHER_BOOTS"),
                Is.True);
        }

        [Test]
        public void Unequip_FullInventory_FailsAndKeepsEquipment()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            Assert.That(
                inventory.TryAdd(
                    "LEGGINGS",
                    "가죽 레깅스",
                    1,
                    1,
                    out int leggingsSlot),
                Is.True);

            Assert.That(
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    leggingsSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Leggings,
                    EquipmentSlotType.Leggings).Success,
                Is.True);

            FillInventory(
                inventory);

            EquipmentActionResult result =
                EquipmentService.Unequip(
                    inventory,
                    equipment,
                    EquipmentSlotType.Leggings);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.InventoryFull));

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Leggings).ItemId,
                Is.EqualTo(
                    "LEGGINGS"));
        }

        // 99일차: 장착 시 전달한 스탯 보너스가 최종 스탯과 EquipmentRunState 합산에 반영되는지 확인한다.
        [Test]
        public void Equip_WithBonusesAndPlayer_UpdatesPlayerFinalStatsAndTotalBonuses()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            inventory.TryAdd(
                "STEEL_HELMET",
                "강철 투구",
                1,
                1,
                out int inventorySlot);

            StatBlock bonuses =
                new StatBlock
                {
                    Defense = 8
                };

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Helmet,
                    EquipmentSlotType.Helmet,
                    bonuses,
                    player);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                equipment.GetTotalBonuses().Defense,
                Is.EqualTo(
                    8));

            Assert.That(
                player.GetFinalStats().Defense,
                Is.EqualTo(
                    48));
        }

        // 99일차: player 인자를 넘기지 않으면 97일차 기존 동작과 동일해야 한다.
        [Test]
        public void Equip_WithoutPlayer_StillSucceedsAsBefore()
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

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon);

            Assert.That(
                result.Success,
                Is.True);
        }

        // 100일차: rarity를 넘기지 않으면 기존과 동일하게 Common으로 저장되어야 한다.
        [Test]
        public void Equip_WithoutRarity_DefaultsToCommon()
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

            EquipmentService.Equip(
                inventory,
                equipment,
                inventorySlot,
                ItemCategory.Equipment,
                EquipmentSlotType.Weapon,
                EquipmentSlotType.Weapon);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Weapon).Rarity,
                Is.EqualTo(
                    EquipmentRarity.Common));
        }

        // 100일차: 전달한 rarity가 EquipmentItemState에 그대로 저장되는지 확인한다.
        [Test]
        public void Equip_WithRarity_StoresRarityOnEquippedItem()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "LEGEND_SWORD",
                "전설의 검",
                1,
                1,
                out int inventorySlot);

            EquipmentService.Equip(
                inventory,
                equipment,
                inventorySlot,
                ItemCategory.Equipment,
                EquipmentSlotType.Weapon,
                EquipmentSlotType.Weapon,
                null,
                null,
                EquipmentRarity.Legendary);

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Weapon).Rarity,
                Is.EqualTo(
                    EquipmentRarity.Legendary));
        }

        // 101일차: 요구 조건을 만족하지 못하면 인벤토리·장비 상태 변경 없이 실패해야 한다.
        [Test]
        public void Equip_RequirementsNotMet_FailsWithoutMutation()
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

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon,
                    null,
                    player,
                    EquipmentRarity.Common,
                    new StatBlock
                    {
                        Attack = 999
                    });

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

        // 101일차: 요구 조건을 만족하면 정상적으로 장착된다.
        [Test]
        public void Equip_RequirementsMet_Succeeds()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            inventory.TryAdd(
                "BASIC_SWORD",
                "기본 검",
                1,
                1,
                out int inventorySlot);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon,
                    null,
                    player,
                    EquipmentRarity.Common,
                    new StatBlock
                    {
                        Attack = 10
                    });

            Assert.That(
                result.Success,
                Is.True);
        }

        // 101일차: player를 넘기지 않으면(검증 불가 상황) 요구 조건 검사를 건너뛴다.
        [Test]
        public void Equip_RequirementsWithoutPlayer_SkipsCheck()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            EquipmentRunState equipment =
                new EquipmentRunState();

            inventory.TryAdd(
                "HEAVY_SWORD",
                "육중한 대검",
                1,
                1,
                out int inventorySlot);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    inventorySlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon,
                    null,
                    null,
                    EquipmentRarity.Common,
                    new StatBlock
                    {
                        Attack = 999
                    });

            Assert.That(
                result.Success,
                Is.True);
        }

        // 101일차 핵심 규칙: 교체 대상 슬롯에 이미 장비가 있으면 그 보너스를 제외한
        // 기준 수치로 판정해야 한다 — 지금 장비 힘으로 상위 장비를 계속 갈아타지 못하게 한다.
        [Test]
        public void Equip_ReplacingItem_ExcludesCurrentSlotBonusFromRequirementCheck()
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
                "WEAK_SWORD",
                "낡은 검",
                1,
                1,
                out int weakSlot);

            // 낡은 검 자체가 공격력을 크게 올려줘서, 착용한 채로는 요구치를 넘어 보인다.
            EquipmentService.Equip(
                inventory,
                equipment,
                weakSlot,
                ItemCategory.Equipment,
                EquipmentSlotType.Weapon,
                EquipmentSlotType.Weapon,
                new StatBlock
                {
                    Attack = 40
                },
                player);

            Assert.That(
                player.GetFinalStats().Attack,
                Is.EqualTo(
                    baseAttack + 40));

            inventory.TryAdd(
                "STRONG_SWORD",
                "강력한 검",
                1,
                1,
                out int strongSlot);

            EquipmentActionResult result =
                EquipmentService.Equip(
                    inventory,
                    equipment,
                    strongSlot,
                    ItemCategory.Equipment,
                    EquipmentSlotType.Weapon,
                    EquipmentSlotType.Weapon,
                    null,
                    player,
                    EquipmentRarity.Common,
                    new StatBlock
                    {
                        // 낡은 검 보너스(40)를 빼면 기준 공격력은 baseAttack이므로,
                        // baseAttack보다 높은 요구치는 실패해야 한다.
                        Attack = baseAttack + 1
                    });

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    EquipmentActionFailureReason.RequirementNotMet));

            Assert.That(
                equipment.GetEquippedItem(
                    EquipmentSlotType.Weapon).ItemId,
                Is.EqualTo(
                    "WEAK_SWORD"));
        }

        private static bool ContainsItem(
            InventoryRunState inventory,
            string itemId)
        {
            for (int index = 0;
                 index < inventory.Slots.Count;
                 index++)
            {
                InventorySlotState slot =
                    inventory.Slots[index];

                if (slot != null
                    && !slot.IsEmpty
                    && slot.ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void FillInventory(
            InventoryRunState inventory)
        {
            int fillIndex =
                0;

            while (HasEmptySlot(
                inventory))
            {
                bool added =
                    inventory.TryAdd(
                        $"FILLER_{fillIndex}",
                        $"채움 아이템 {fillIndex}",
                        1,
                        1,
                        out _);

                Assert.That(
                    added,
                    Is.True);

                fillIndex++;
            }
        }

        private static bool HasEmptySlot(
            InventoryRunState inventory)
        {
            for (int index = 0;
                 index < inventory.Slots.Count;
                 index++)
            {
                if (inventory.Slots[index].IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
