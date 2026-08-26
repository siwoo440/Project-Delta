using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ItemSystemPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            InventoryRunState.MaxStackResolver =
                null;

            DungeonSaveMapper.ClearPendingRestore();
        }

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            InventoryRunState.MaxStackResolver =
                null;

            DungeonSaveMapper.ClearPendingRestore();
        }

        [Test]
        public void InventorySaveRestore_PreservesQuantityAndMaxStackWithoutResolver()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY95_SOURCE");

            Assert.That(
                source.Inventory.TryAdd(
                    "POTION",
                    "포션",
                    4,
                    5,
                    out int slotIndex),
                Is.True);

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    source);

            Assert.That(
                saved.Inventory.Slots[slotIndex].Quantity,
                Is.EqualTo(4));

            Assert.That(
                saved.Inventory.Slots[slotIndex].MaxStackSize,
                Is.EqualTo(5));

            RunContext.End();

            InventoryRunState.MaxStackResolver =
                null;

            RunContext restored =
                RunContext.Begin(
                    "DAY95_RESTORED");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Inventory.Slots[slotIndex].Quantity,
                Is.EqualTo(4));

            Assert.That(
                restored.Inventory.Slots[slotIndex].MaxStackSize,
                Is.EqualTo(5));
        }

        [Test]
        public void InventoryRestore_OldSaveWithoutMaxStack_UsesLegacyResolver()
        {
            RunData saved =
                new RunData();

            saved.Inventory.Slots.Add(
                new RunInventorySlotData
                {
                    ItemId =
                        "POTION",
                    DisplayName =
                        "포션",
                    Quantity =
                        3,
                    MaxStackSize =
                        0
                });

            InventoryRunState.MaxStackResolver =
                itemId =>
                    itemId == "POTION"
                        ? 5
                        : 1;

            RunContext restored =
                RunContext.Begin(
                    "DAY95_LEGACY");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Inventory.Slots[0].Quantity,
                Is.EqualTo(3));

            Assert.That(
                restored.Inventory.Slots[0].MaxStackSize,
                Is.EqualTo(5));
        }

        [Test]
        public void ChestSave_PreservesOnlyRemainingItems()
        {
            RunContext context =
                RunContext.Begin(
                    "DAY95_CHEST");

            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_CHEST",
                    "ROOM_CHEST",
                    null);

            room.InitializeChestContents(
                new[]
                {
                    "POTION",
                    "SWORD",
                    "TREASURE"
                });

            room.MarkChestOpened();

            Assert.That(
                room.TryTakeChestItem(
                    0,
                    out string taken),
                Is.True);

            Assert.That(
                taken,
                Is.EqualTo("POTION"));

            context.Dungeon.Register(
                room);

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    context);

            RoomRunState savedRoom =
                FindRoom(
                    saved,
                    "ROOM_CHEST");

            Assert.That(
                savedRoom,
                Is.Not.Null);

            Assert.That(
                savedRoom.HasChestContentsSnapshot,
                Is.True);

            Assert.That(
                savedRoom.ChestRemainingItems.Count,
                Is.EqualTo(2));

            Assert.That(
                savedRoom.ChestRemainingItems[0],
                Is.EqualTo("SWORD"));

            Assert.That(
                savedRoom.ChestRemainingItems[1],
                Is.EqualTo("TREASURE"));
        }

        [Test]
        public void ChestSave_EmptySnapshot_RemainsExplicitlyEmpty()
        {
            RunContext context =
                RunContext.Begin(
                    "DAY95_EMPTY_CHEST");

            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_EMPTY_CHEST",
                    "ROOM_EMPTY_CHEST",
                    null);

            room.InitializeChestContents(
                new[]
                {
                    "POTION"
                });

            room.MarkChestOpened();

            Assert.That(
                room.TryTakeChestItem(
                    0,
                    out _),
                Is.True);

            context.Dungeon.Register(
                room);

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    context);

            RoomRunState savedRoom =
                FindRoom(
                    saved,
                    "ROOM_EMPTY_CHEST");

            Assert.That(
                savedRoom.HasChestContentsSnapshot,
                Is.True);

            Assert.That(
                savedRoom.ChestRemainingItems,
                Is.Empty);
        }

        [Test]
        public void ChestRestoreSnapshot_PreservesRemainingOrder()
        {
            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_RESTORE",
                    "ROOM_RESTORE",
                    null);

            room.RestoreChestContents(
                new[]
                {
                    "SWORD",
                    "TREASURE"
                });

            Assert.That(
                room.HasChestContentsSnapshot,
                Is.True);

            Assert.That(
                room.ChestRemainingItems.Count,
                Is.EqualTo(2));

            Assert.That(
                room.ChestRemainingItems[0],
                Is.EqualTo("SWORD"));

            Assert.That(
                room.ChestRemainingItems[1],
                Is.EqualTo("TREASURE"));
        }

        [Test]
        public void BeginRestore_ExposesChestSnapshotForSceneReconstruction()
        {
            RunData saved =
                new RunData();

            RoomRunState room =
                new RoomRunState
                {
                    RoomId =
                        "ROOM_PENDING",
                    ChestOpened =
                        true,
                    HasChestContentsSnapshot =
                        true
                };

            room.ChestRemainingItems.Add(
                "SWORD");

            saved.DungeonState.Rooms.Add(
                room);

            DungeonSaveMapper.BeginRestore(
                saved);

            Assert.That(
                DungeonSaveMapper.TryGetRoomState(
                    "ROOM_PENDING",
                    out RoomRunState pending),
                Is.True);

            Assert.That(
                pending.HasChestContentsSnapshot,
                Is.True);

            Assert.That(
                pending.ChestRemainingItems.Count,
                Is.EqualTo(1));

            Assert.That(
                pending.ChestRemainingItems[0],
                Is.EqualTo("SWORD"));
        }

        private static RoomRunState FindRoom(
            RunData data,
            string roomId)
        {
            if (data?.DungeonState?.Rooms == null)
            {
                return null;
            }

            for (int index = 0;
                 index < data.DungeonState.Rooms.Count;
                 index++)
            {
                RoomRunState room =
                    data.DungeonState.Rooms[index];

                if (room != null
                    && room.RoomId
                        == roomId)
                {
                    return room;
                }
            }

            return null;
        }
    }
}
