using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class RunInventoryStateTests
    {
        [TearDown]
        public void TearDown()
        {
            RunContext.End();
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
                    out int firstIndex);

            bool secondAdded =
                inventory.TryAdd(
                    "ITEM_B",
                    "아이템 B",
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
        public void RemoveAndMove_PreserveSlotPositions()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "ITEM_A",
                "아이템 A",
                1,
                out _);

            inventory.TryAdd(
                "ITEM_B",
                "아이템 B",
                1,
                out _);

            Assert.That(
                inventory.TryRemoveAt(
                    0),
                Is.True);

            Assert.That(
                inventory.Slots[0].IsEmpty,
                Is.True);

            Assert.That(
                inventory.Slots[1].ItemId,
                Is.EqualTo(
                    "ITEM_B"));

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
                Is.EqualTo(
                    "ITEM_B"));
        }

        [Test]
        public void MoveToOccupiedSlot_SwapsItems()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "ITEM_A",
                "아이템 A",
                1,
                out _);

            inventory.TryAdd(
                "ITEM_B",
                "아이템 B",
                1,
                out _);

            Assert.That(
                inventory.TryMoveOrSwap(
                    0,
                    1),
                Is.True);

            Assert.That(
                inventory.Slots[0].ItemId,
                Is.EqualTo(
                    "ITEM_B"));

            Assert.That(
                inventory.Slots[1].ItemId,
                Is.EqualTo(
                    "ITEM_A"));
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
                    "DAY89_SOURCE");

            source.Inventory.TryAdd(
                "ITEM_A",
                "아이템 A",
                1,
                out _);

            source.Inventory.TryAdd(
                "ITEM_B",
                "아이템 B",
                1,
                out _);

            source.Inventory.TryMoveOrSwap(
                1,
                7);

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    source);

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    "DAY89_RESTORED");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Inventory.Slots[0].ItemId,
                Is.EqualTo(
                    "ITEM_A"));

            Assert.That(
                restored.Inventory.Slots[1].IsEmpty,
                Is.True);

            Assert.That(
                restored.Inventory.Slots[7].ItemId,
                Is.EqualTo(
                    "ITEM_B"));
        }
    }
}
