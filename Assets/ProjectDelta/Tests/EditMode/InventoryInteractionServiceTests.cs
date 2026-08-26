using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class InventoryInteractionServiceTests
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
        public void Move_DifferentItems_PreservesBothStacks()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "ITEM_A",
                "아이템 A",
                2,
                3,
                out _);

            inventory.TryAdd(
                "ITEM_B",
                "아이템 B",
                1,
                7,
                out _);

            InventoryInteractionResult result =
                InventoryInteractionService.Move(
                    inventory,
                    0,
                    1);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                inventory.Slots[0].ItemId,
                Is.EqualTo("ITEM_B"));

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(1));

            Assert.That(
                inventory.Slots[0].MaxStackSize,
                Is.EqualTo(7));

            Assert.That(
                inventory.Slots[1].ItemId,
                Is.EqualTo("ITEM_A"));

            Assert.That(
                inventory.Slots[1].Quantity,
                Is.EqualTo(2));

            Assert.That(
                inventory.Slots[1].MaxStackSize,
                Is.EqualTo(3));
        }

        [Test]
        public void Move_ToEmptySlot_MovesWholeStack()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            InventoryInteractionResult result =
                InventoryInteractionService.Move(
                    inventory,
                    0,
                    5);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                inventory.Slots[0].IsEmpty,
                Is.True);

            Assert.That(
                inventory.Slots[5].ItemId,
                Is.EqualTo("POTION"));

            Assert.That(
                inventory.Slots[5].Quantity,
                Is.EqualTo(4));
        }

        [Test]
        public void Move_SameItem_MergesOnlyAvailableAmount()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                5,
                5,
                out _);

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            inventory.TryRemoveQuantityAt(
                0,
                2,
                out _);

            InventoryInteractionResult result =
                InventoryInteractionService.Move(
                    inventory,
                    1,
                    0);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(5));

            Assert.That(
                inventory.Slots[1].Quantity,
                Is.EqualTo(2));
        }

        [Test]
        public void DiscardOne_RemovesOnlyOneQuantity()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                3,
                5,
                out _);

            InventoryInteractionResult result =
                InventoryInteractionService.DiscardOne(
                    inventory,
                    0,
                    ItemCategory.Consumable);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.RemovedQuantity,
                Is.EqualTo(1));

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(2));
        }

        [Test]
        public void DiscardAll_ClearsSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "TREASURE",
                "보물",
                4,
                5,
                out _);

            InventoryInteractionResult result =
                InventoryInteractionService.DiscardAll(
                    inventory,
                    0,
                    ItemCategory.Treasure);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.RemovedQuantity,
                Is.EqualTo(4));

            Assert.That(
                inventory.Slots[0].IsEmpty,
                Is.True);
        }

        [TestCase(ItemCategory.KeyItem)]
        [TestCase(ItemCategory.Relic)]
        [TestCase(ItemCategory.Cursed)]
        [TestCase(ItemCategory.Uncategorized)]
        public void Discard_ProtectedOrConditionalCategory_IsBlocked(
            ItemCategory category)
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "PROTECTED",
                "보호 아이템",
                2,
                5,
                out _);

            InventoryInteractionResult oneResult =
                InventoryInteractionService.DiscardOne(
                    inventory,
                    0,
                    category);

            InventoryInteractionResult allResult =
                InventoryInteractionService.DiscardAll(
                    inventory,
                    0,
                    category);

            Assert.That(
                oneResult.Success,
                Is.False);

            Assert.That(
                allResult.Success,
                Is.False);

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(2));
        }

        [Test]
        public void Move_KeyItem_IsAllowed()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "KEY",
                "중요 아이템",
                1,
                1,
                out _);

            InventoryInteractionResult result =
                InventoryInteractionService.Move(
                    inventory,
                    0,
                    8);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                inventory.Slots[8].ItemId,
                Is.EqualTo("KEY"));
        }

        [Test]
        public void DirectSwap_DifferentItems_PreservesOriginalDestination()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "ITEM_A",
                "아이템 A",
                1,
                1,
                out _);

            inventory.TryAdd(
                "ITEM_B",
                "아이템 B",
                1,
                1,
                out _);

            bool moved =
                inventory.TryMoveOrSwap(
                    0,
                    1);

            Assert.That(
                moved,
                Is.True);

            Assert.That(
                inventory.Slots[0].ItemId,
                Is.EqualTo("ITEM_B"));

            Assert.That(
                inventory.Slots[1].ItemId,
                Is.EqualTo("ITEM_A"));
        }
    }
}
