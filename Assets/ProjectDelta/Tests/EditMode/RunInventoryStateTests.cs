using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class RunInventoryStateTests
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
        public void NewInventory_HasTenEmptySlots()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            Assert.That(
                inventory.Capacity,
                Is.EqualTo(10));

            Assert.That(
                inventory.Slots.Count,
                Is.EqualTo(10));

            for (int index = 0;
                 index < 10;
                 index++)
            {
                Assert.That(
                    inventory.Slots[index].IsEmpty,
                    Is.True);
            }
        }

        [Test]
        public void TryAdd_PlacesItemInFirstEmptySlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            bool firstAdded =
                inventory.TryAdd(
                    "ITEM_A",
                    "아이템 A",
                    1,
                    1,
                    out int firstIndex);

            bool secondAdded =
                inventory.TryAdd(
                    "ITEM_B",
                    "아이템 B",
                    1,
                    1,
                    out int secondIndex);

            Assert.That(
                firstAdded,
                Is.True);

            Assert.That(
                firstIndex,
                Is.EqualTo(0));

            Assert.That(
                secondAdded,
                Is.True);

            Assert.That(
                secondIndex,
                Is.EqualTo(1));
        }

        [Test]
        public void DetailedAdd_StacksIntoExistingSlotFirst()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            InventoryAddResult result =
                inventory.TryAddDetailed(
                    "POTION",
                    "포션",
                    3,
                    5);

            Assert.That(
                result.AddedQuantity,
                Is.EqualTo(3));

            Assert.That(
                result.RemainingQuantity,
                Is.EqualTo(0));

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(5));

            Assert.That(
                inventory.Slots[1].Quantity,
                Is.EqualTo(2));
        }

        [Test]
        public void StackSizeOne_DoesNotMerge()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "RELIC",
                "유물",
                1,
                1,
                out _);

            inventory.TryAdd(
                "RELIC",
                "유물",
                1,
                1,
                out _);

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(1));

            Assert.That(
                inventory.Slots[1].Quantity,
                Is.EqualTo(1));
        }

        [Test]
        public void RemoveAndMove_PreserveSlotPositions()
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

            Assert.That(
                inventory.TryRemoveAt(0),
                Is.True);

            Assert.That(
                inventory.Slots[0].IsEmpty,
                Is.True);

            Assert.That(
                inventory.Slots[1].ItemId,
                Is.EqualTo("ITEM_B"));

            Assert.That(
                inventory.TryMoveOrSwap(
                    1,
                    4),
                Is.True);

            Assert.That(
                inventory.Slots[1].IsEmpty,
                Is.True);

            Assert.That(
                inventory.Slots[4].ItemId,
                Is.EqualTo("ITEM_B"));
        }

        [Test]
        public void MoveSameItem_MergesStacks()
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

            Assert.That(
                inventory.TryMoveOrSwap(
                    1,
                    0),
                Is.True);

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(5));

            Assert.That(
                inventory.Slots[1].Quantity,
                Is.EqualTo(2));
        }

        [Test]
        public void TryRemoveQuantityAt_DecreasesAndClearsSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                2,
                5,
                out _);

            bool firstRemoved =
                inventory.TryRemoveQuantityAt(
                    0,
                    1,
                    out int firstAmount);

            Assert.That(
                firstRemoved,
                Is.True);

            Assert.That(
                firstAmount,
                Is.EqualTo(1));

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(1));

            bool secondRemoved =
                inventory.TryRemoveQuantityAt(
                    0,
                    1,
                    out int secondAmount);

            Assert.That(
                secondRemoved,
                Is.True);

            Assert.That(
                secondAmount,
                Is.EqualTo(1));

            Assert.That(
                inventory.Slots[0].IsEmpty,
                Is.True);
        }

        [Test]
        public void DetailedAdd_ReturnsRemainingQuantityWhenFull()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            for (int index = 0;
                 index < inventory.Capacity;
                 index++)
            {
                inventory.TryAdd(
                    $"ITEM_{index}",
                    $"아이템 {index}",
                    1,
                    1,
                    out _);
            }

            inventory.TryRemoveAt(0);

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            InventoryAddResult result =
                inventory.TryAddDetailed(
                    "POTION",
                    "포션",
                    3,
                    5);

            Assert.That(
                result.AddedQuantity,
                Is.EqualTo(1));

            Assert.That(
                result.RemainingQuantity,
                Is.EqualTo(2));
        }

        [Test]
        public void Capacity_AddsPermanentAndBagBonuses()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.SetCapacityBonuses(
                2,
                4);

            Assert.That(
                inventory.Capacity,
                Is.EqualTo(16));

            Assert.That(
                inventory.Slots.Count,
                Is.EqualTo(16));
        }

        [Test]
        public void SaveMapper_RestoresExactSlotPosition()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY90_SOURCE");

            source.Inventory.TryAdd(
                "POTION",
                "포션",
                5,
                5,
                out _);

            source.Inventory.TryAdd(
                "POTION",
                "포션",
                2,
                5,
                out _);

            source.Inventory.TryAdd(
                "ITEM_B",
                "아이템 B",
                1,
                1,
                out _);

            source.Inventory.TryMoveOrSwap(
                2,
                7);

            var saved =
                ProjectDelta.Data.DungeonSaveMapper.BuildFromRunContext(
                    source);

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    "DAY90_RESTORED");

            ProjectDelta.Data.DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Inventory.Slots[0].Quantity,
                Is.EqualTo(5));

            Assert.That(
                restored.Inventory.Slots[1].Quantity,
                Is.EqualTo(2));

            Assert.That(
                restored.Inventory.Slots[7].ItemId,
                Is.EqualTo("ITEM_B"));
        }
    }
}
