using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ItemUseServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            InventoryRunState.MaxStackResolver =
                null;
        }

        [TearDown]
        public void TearDown()
        {
            RunContext.End();

            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void ExplorationPreview_DoesNotMutatePlayerOrInventory()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "POTION",
                    "포션",
                    3);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            ItemDefinition definition =
                CreateItemDefinition(
                    "POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Both,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.PreviewExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    result.HpRecovered,
                    Is.EqualTo(25));

                Assert.That(
                    player.CurrentHp,
                    Is.EqualTo(50));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ExplorationCommit_RestoresHpAndConsumesOne()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "POTION",
                    "포션",
                    3);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            ItemDefinition definition =
                CreateItemDefinition(
                    "POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Both,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    player.CurrentHp,
                    Is.EqualTo(75));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(2));

                Assert.That(
                    result.ConsumedQuantity,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ExplorationCommit_ClampsHpRecoveryToMaximum()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "POTION",
                    "포션",
                    2);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                90;

            ItemDefinition definition =
                CreateItemDefinition(
                    "POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Exploration,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.HpRecovered,
                    Is.EqualTo(10));

                Assert.That(
                    player.CurrentHp,
                    Is.EqualTo(100));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ExplorationCommit_WhenResourceIsFull_DoesNotConsume()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "POTION",
                    "포션",
                    2);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            ItemDefinition definition =
                CreateItemDefinition(
                    "POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Both,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        ItemUseFailureReason.NoApplicableEffect));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ExplorationCommit_RestoresManaOnly()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "MANA",
                    "마나 물약",
                    2);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentMana =
                10;

            int originalHp =
                player.CurrentHp;

            ItemDefinition definition =
                CreateItemDefinition(
                    "MANA",
                    ItemCategory.Consumable,
                    ItemUseContext.Both,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreMana,
                        20));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.ManaRecovered,
                    Is.EqualTo(20));

                Assert.That(
                    player.CurrentMana,
                    Is.EqualTo(30));

                Assert.That(
                    player.CurrentHp,
                    Is.EqualTo(originalHp));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ExplorationCommit_RestoresStaminaOnly()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "STAMINA",
                    "활력제",
                    2);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentStamina =
                40;

            ItemDefinition definition =
                CreateItemDefinition(
                    "STAMINA",
                    ItemCategory.ExplorationTool,
                    ItemUseContext.Exploration,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreStamina,
                        30));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.StaminaRecovered,
                    Is.EqualTo(30));

                Assert.That(
                    player.CurrentStamina,
                    Is.EqualTo(70));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void KeyItem_IsNotUsableAndIsNotConsumed()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "KEY_ITEM",
                    "중요 아이템",
                    1);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            ItemDefinition definition =
                CreateItemDefinition(
                    "KEY_ITEM",
                    ItemCategory.KeyItem,
                    ItemUseContext.Both,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        50));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        ItemUseFailureReason.ItemNotUsable));

                Assert.That(
                    player.CurrentHp,
                    Is.EqualTo(50));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void BattleOnlyItem_CannotBeUsedInExploration()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "BATTLE_POTION",
                    "전투 물약",
                    1);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            ItemDefinition definition =
                CreateItemDefinition(
                    "BATTLE_POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Battle,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        ItemUseFailureReason.WrongContext));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void LastItemUse_ClearsInventorySlot()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "POTION",
                    "포션",
                    1);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                1;

            ItemDefinition definition =
                CreateItemDefinition(
                    "POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Both,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitExploration(
                        inventory,
                        0,
                        player,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    inventory.Slots[0].IsEmpty,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void BattleCommit_ChangesBattleParticipantAndConsumesInventoryOnly()
        {
            InventoryRunState inventory =
                CreateInventory(
                    "POTION",
                    "포션",
                    2);

            PlayerRunState explorationPlayer =
                PlayerRunState.CreateDefault();

            explorationPlayer.CurrentHp =
                60;

            BattleParticipant battlePlayer =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    100,
                    50,
                    50,
                    40,
                    90,
                    40,
                    50,
                    50,
                    50,
                    100,
                    50,
                    50,
                    40);

            ItemDefinition definition =
                CreateItemDefinition(
                    "POTION",
                    ItemCategory.Consumable,
                    ItemUseContext.Battle,
                    new ItemUseEffectDefinition(
                        ItemUseEffectKind.RestoreHp,
                        25));

            try
            {
                ItemUseResult result =
                    ItemUseService.CommitBattle(
                        inventory,
                        0,
                        battlePlayer,
                        definition);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    battlePlayer.CurrentHp,
                    Is.EqualTo(75));

                Assert.That(
                    explorationPlayer.CurrentHp,
                    Is.EqualTo(60));

                Assert.That(
                    inventory.Slots[0].Quantity,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        private static InventoryRunState CreateInventory(
            string itemId,
            string displayName,
            int quantity)
        {
            InventoryRunState inventory =
                new InventoryRunState();

            bool added =
                inventory.TryAdd(
                    itemId,
                    displayName,
                    quantity,
                    5,
                    out _);

            Assert.That(
                added,
                Is.True);

            return inventory;
        }

        private static ItemDefinition CreateItemDefinition(
            string id,
            ItemCategory category,
            ItemUseContext context,
            params ItemUseEffectDefinition[] effects)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetField(
                typeof(DefinitionBase),
                definition,
                "id",
                id);

            SetField(
                typeof(ItemDefinition),
                definition,
                "displayName",
                id);

            SetField(
                typeof(ItemDefinition),
                definition,
                "category",
                category);

            SetField(
                typeof(ItemDefinition),
                definition,
                "useContext",
                context);

            SetField(
                typeof(ItemDefinition),
                definition,
                "useEffects",
                effects);

            return definition;
        }

        private static void SetField(
            System.Type declaringType,
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                declaringType.GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"필드 '{fieldName}'를 찾지 못했습니다.");

            field.SetValue(
                target,
                value);
        }
    }
}
