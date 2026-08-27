using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public sealed class DungeonRunState
    {
        private readonly Dictionary<string, RoomInstance> rooms =
            new Dictionary<string, RoomInstance>();

        private readonly HashSet<string> revealedRoomIds =
            new HashSet<string>();

        private GeneratedDungeon currentGeneratedDungeon;

        public int CurrentFloor { get; private set; } = 1;

        public DungeonLayoutGraph Layout { get; private set; } =
            new DungeonLayoutGraph();

        public int CurrentDungeonSeed { get; private set; }

        public DungeonLayoutSnapshot CurrentLayoutSnapshot { get; private set; }

        public IReadOnlyCollection<string> RevealedRoomIds =>
            revealedRoomIds;

        public bool HasGeneratedFloor =>
            currentGeneratedDungeon != null
            && CurrentLayoutSnapshot != null;

        public void AdvanceFloor()
        {
            CurrentFloor++;
            Layout = new DungeonLayoutGraph();

            rooms.Clear();
            revealedRoomIds.Clear();

            currentGeneratedDungeon = null;
            CurrentLayoutSnapshot = null;
            CurrentDungeonSeed = 0;
        }

        public void SetFloor(int floor)
        {
            CurrentFloor =
                Math.Max(
                    1,
                    floor);
        }

        public void SetGeneratedFloor(
            GeneratedDungeon dungeon,
            int seed)
        {
            if (dungeon == null)
            {
                throw new ArgumentNullException(
                    nameof(dungeon));
            }

            currentGeneratedDungeon = dungeon;
            Layout = dungeon.Layout;
            CurrentDungeonSeed = seed;
            CurrentLayoutSnapshot =
                DungeonLayoutSnapshot.Capture(
                    dungeon,
                    seed);
        }

        public void RestoreGeneratedFloor(
            DungeonLayoutSnapshot snapshot,
            int savedSeed)
        {
            if (snapshot == null)
            {
                currentGeneratedDungeon = null;
                CurrentLayoutSnapshot = null;
                CurrentDungeonSeed = 0;
                Layout = new DungeonLayoutGraph();
                return;
            }

            currentGeneratedDungeon =
                snapshot.Restore();

            CurrentLayoutSnapshot = snapshot;
            CurrentDungeonSeed =
                savedSeed != 0
                    ? savedSeed
                    : snapshot.Seed;

            Layout =
                currentGeneratedDungeon.Layout;
        }

        public bool TryGetGeneratedFloor(
            out GeneratedDungeon dungeon,
            out int seed)
        {
            dungeon = currentGeneratedDungeon;
            seed = CurrentDungeonSeed;
            return dungeon != null;
        }

        public void RevealAround(string currentRoomId)
        {
            if (string.IsNullOrEmpty(currentRoomId)
                || Layout == null
                || !Layout.TryGetRoom(
                    currentRoomId,
                    out RoomNode currentRoom))
            {
                return;
            }

            foreach (RoomNode room in Layout.AllRooms)
            {
                int distanceX =
                    Math.Abs(
                        room.MacroCoordinate.X
                        - currentRoom.MacroCoordinate.X);

                int distanceZ =
                    Math.Abs(
                        room.MacroCoordinate.Z
                        - currentRoom.MacroCoordinate.Z);

                if (distanceX <= 1
                    && distanceZ <= 1)
                {
                    revealedRoomIds.Add(
                        room.RoomId);
                }
            }
        }

        public void MergeRevealedRooms(
            IEnumerable<string> roomIds)
        {
            if (roomIds == null)
            {
                return;
            }

            foreach (string roomId in roomIds)
            {
                if (!string.IsNullOrEmpty(roomId)
                    && Layout != null
                    && Layout.TryGetRoom(
                        roomId,
                        out _))
                {
                    revealedRoomIds.Add(
                        roomId);
                }
            }
        }

        public void RestoreRevealedRooms(
            IEnumerable<string> roomIds)
        {
            revealedRoomIds.Clear();
            MergeRevealedRooms(roomIds);
        }

        public bool IsRoomRevealed(string roomId)
        {
            return !string.IsNullOrEmpty(roomId)
                && revealedRoomIds.Contains(roomId);
        }

        public void Register(RoomInstance roomInstance)
        {
            if (roomInstance == null
                || string.IsNullOrEmpty(roomInstance.RoomId))
            {
                return;
            }

            rooms[roomInstance.RoomId] =
                roomInstance;
        }

        public bool TryGetRoom(
            string roomId,
            out RoomInstance roomInstance)
        {
            return rooms.TryGetValue(
                roomId,
                out roomInstance);
        }

        public IReadOnlyCollection<RoomInstance> AllRooms =>
            rooms.Values;
    }

    public sealed class InventoryItemStack
    {
        public string ItemId;
        public string DisplayName;
        public int Quantity;
        public int MaxStackSize;

        public InventoryItemStack(
            string itemId,
            string displayName)
            : this(
                itemId,
                displayName,
                1,
                1)
        {
        }

        public InventoryItemStack(
            string itemId,
            string displayName,
            int quantity)
            : this(
                itemId,
                displayName,
                quantity,
                1)
        {
        }

        public InventoryItemStack(
            string itemId,
            string displayName,
            int quantity,
            int maxStackSize)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Quantity =
                Math.Max(
                    1,
                    quantity);
            MaxStackSize =
                Math.Max(
                    1,
                    maxStackSize);
        }
    }

    public sealed class InventorySlotState
    {
        public string ItemId { get; private set; }

        public string DisplayName { get; private set; }

        public int Quantity { get; private set; }

        public int MaxStackSize { get; private set; } = 1;

        public bool IsEmpty =>
            string.IsNullOrEmpty(ItemId)
            || Quantity <= 0;

        public bool CanStackMore =>
            !IsEmpty
            && Quantity < MaxStackSize;

        public void Set(
            string itemId,
            string displayName,
            int quantity,
            int maxStackSize)
        {
            if (string.IsNullOrEmpty(itemId)
                || quantity <= 0)
            {
                Clear();
                return;
            }

            ItemId = itemId;
            DisplayName =
                string.IsNullOrEmpty(displayName)
                    ? itemId
                    : displayName;
            MaxStackSize =
                Math.Max(
                    1,
                    maxStackSize);
            Quantity =
                Math.Min(
                    Math.Max(
                        1,
                        quantity),
                    MaxStackSize);
        }

        public int AddQuantity(int amount)
        {
            if (IsEmpty
                || amount <= 0)
            {
                return amount;
            }

            int available =
                Math.Max(
                    0,
                    MaxStackSize - Quantity);

            int added =
                Math.Min(
                    available,
                    amount);

            Quantity += added;
            return amount - added;
        }

        public int RemoveQuantity(int amount)
        {
            if (IsEmpty
                || amount <= 0)
            {
                return 0;
            }

            int removed =
                Math.Min(
                    Quantity,
                    amount);

            Quantity -= removed;

            if (Quantity <= 0)
            {
                Clear();
            }

            return removed;
        }

        public void Clear()
        {
            ItemId = string.Empty;
            DisplayName = string.Empty;
            Quantity = 0;
            MaxStackSize = 1;
        }
    }

    public sealed class InventoryAddResult
    {
        public string ItemId { get; set; }

        public string DisplayName { get; set; }

        public int RequestedQuantity { get; set; }

        public int AddedQuantity { get; set; }

        public int RemainingQuantity { get; set; }

        public int FirstChangedSlotIndex { get; set; } = -1;

        public bool IsComplete =>
            RemainingQuantity <= 0;
    }

    public sealed class InventoryRunState
    {
        public const int BaseSlotCount = 10;

        private readonly List<InventorySlotState> slots =
            new List<InventorySlotState>();

        private readonly List<InventoryItemStack> items =
            new List<InventoryItemStack>();

        public static Func<string, int> MaxStackResolver { get; set; }

        public IReadOnlyList<InventorySlotState> Slots =>
            slots;

        public IReadOnlyList<InventoryItemStack> Items =>
            items;

        public int PermanentSlotBonus { get; private set; }

        public int BagSlotBonus { get; private set; }

        public int Capacity =>
            CalculateCapacity(
                PermanentSlotBonus,
                BagSlotBonus);

        public InventoryRunState()
        {
            EnsureCapacity();
        }

        public static int CalculateCapacity(
            int permanentSlotBonus,
            int bagSlotBonus)
        {
            return BaseSlotCount
                + Math.Max(
                    0,
                    permanentSlotBonus)
                + Math.Max(
                    0,
                    bagSlotBonus);
        }

        public static int ResolveMaxStackSize(string itemId)
        {
            int resolved =
                MaxStackResolver != null
                    ? MaxStackResolver(itemId)
                    : 1;

            return Math.Max(
                1,
                resolved);
        }

        public void SetCapacityBonuses(
            int permanentSlotBonus,
            int bagSlotBonus)
        {
            PermanentSlotBonus =
                Math.Max(
                    0,
                    permanentSlotBonus);

            BagSlotBonus =
                Math.Max(
                    0,
                    bagSlotBonus);

            EnsureCapacity();
        }

        public void Add(InventoryItemStack item)
        {
            if (item == null)
            {
                return;
            }

            TryAdd(
                item.ItemId,
                item.DisplayName,
                item.Quantity,
                Math.Max(
                    1,
                    item.MaxStackSize),
                out _);
        }

        public bool TryAdd(
            string itemId,
            string displayName,
            int quantity,
            out int slotIndex)
        {
            return TryAdd(
                itemId,
                displayName,
                quantity,
                ResolveMaxStackSize(
                    itemId),
                out slotIndex);
        }

        public bool TryAdd(
            string itemId,
            string displayName,
            int quantity,
            int maxStackSize,
            out int slotIndex)
        {
            InventoryAddResult result =
                TryAddDetailed(
                    itemId,
                    displayName,
                    quantity,
                    maxStackSize);

            slotIndex =
                result.FirstChangedSlotIndex;

            return result.AddedQuantity > 0;
        }

        public InventoryAddResult TryAddDetailed(
            string itemId,
            string displayName,
            int quantity)
        {
            return TryAddDetailed(
                itemId,
                displayName,
                quantity,
                ResolveMaxStackSize(
                    itemId));
        }

        public InventoryAddResult TryAddDetailed(
            string itemId,
            string displayName,
            int quantity,
            int maxStackSize)
        {
            InventoryAddResult result =
                new InventoryAddResult
                {
                    ItemId = itemId,
                    DisplayName = displayName,
                    RequestedQuantity = Math.Max(
                        0,
                        quantity),
                    RemainingQuantity = Math.Max(
                        0,
                        quantity)
                };

            if (string.IsNullOrEmpty(itemId)
                || quantity <= 0)
            {
                return result;
            }

            EnsureCapacity();

            int safeMaxStackSize =
                Math.Max(
                    1,
                    maxStackSize);

            result.RemainingQuantity =
                FillExistingStacks(
                    itemId,
                    result.RemainingQuantity,
                    safeMaxStackSize,
                    result);

            result.RemainingQuantity =
                FillEmptySlots(
                    itemId,
                    displayName,
                    result.RemainingQuantity,
                    safeMaxStackSize,
                    result);

            result.AddedQuantity =
                result.RequestedQuantity
                - result.RemainingQuantity;

            if (result.AddedQuantity > 0)
            {
                RebuildCompatibilityItems();
            }

            return result;
        }

        public bool TryRemoveAt(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex)
                || slots[slotIndex].IsEmpty)
            {
                return false;
            }

            slots[slotIndex].Clear();
            RebuildCompatibilityItems();
            return true;
        }

        public bool TryRemoveQuantityAt(
            int slotIndex,
            int amount,
            out int removedQuantity)
        {
            removedQuantity = 0;

            if (!IsValidSlotIndex(slotIndex)
                || slots[slotIndex].IsEmpty
                || amount <= 0)
            {
                return false;
            }

            removedQuantity =
                slots[slotIndex].RemoveQuantity(
                    amount);

            if (removedQuantity <= 0)
            {
                return false;
            }

            RebuildCompatibilityItems();
            return true;
        }

        public bool TryMoveOrSwap(
            int sourceIndex,
            int destinationIndex)
        {
            if (!IsValidSlotIndex(sourceIndex)
                || !IsValidSlotIndex(destinationIndex)
                || sourceIndex == destinationIndex
                || slots[sourceIndex].IsEmpty)
            {
                return false;
            }

            InventorySlotState source =
                slots[sourceIndex];

            InventorySlotState destination =
                slots[destinationIndex];

            if (!destination.IsEmpty
                && destination.ItemId == source.ItemId)
            {
                int sourceQuantity =
                    source.Quantity;

                int remaining =
                    destination.AddQuantity(
                        sourceQuantity);

                int moved =
                    sourceQuantity - remaining;

                if (moved <= 0)
                {
                    return false;
                }

                source.RemoveQuantity(
                    moved);

                RebuildCompatibilityItems();
                return true;
            }

            if (destination.IsEmpty)
            {
                destination.Set(
                    source.ItemId,
                    source.DisplayName,
                    source.Quantity,
                    source.MaxStackSize);

                source.Clear();
            }
            else
            {
                string sourceItemId =
                    source.ItemId;

                string sourceDisplayName =
                    source.DisplayName;

                int sourceQuantity =
                    source.Quantity;

                int sourceMaxStackSize =
                    source.MaxStackSize;

                string destinationItemId =
                    destination.ItemId;

                string destinationDisplayName =
                    destination.DisplayName;

                int destinationQuantity =
                    destination.Quantity;

                int destinationMaxStackSize =
                    destination.MaxStackSize;

                destination.Set(
                    sourceItemId,
                    sourceDisplayName,
                    sourceQuantity,
                    sourceMaxStackSize);

                source.Set(
                    destinationItemId,
                    destinationDisplayName,
                    destinationQuantity,
                    destinationMaxStackSize);
            }

            RebuildCompatibilityItems();
            return true;
        }

        public bool TryGetSlot(
            int slotIndex,
            out InventorySlotState slot)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                slot = null;
                return false;
            }

            slot = slots[slotIndex];
            return true;
        }

        public void ResetForRestore(
            int permanentSlotBonus,
            int bagSlotBonus)
        {
            SetCapacityBonuses(
                permanentSlotBonus,
                bagSlotBonus);

            for (int index = 0;
                 index < slots.Count;
                 index++)
            {
                slots[index].Clear();
            }

            RebuildCompatibilityItems();
        }

        public bool RestoreSlot(
            int slotIndex,
            string itemId,
            string displayName,
            int quantity)
        {
            return RestoreSlot(
                slotIndex,
                itemId,
                displayName,
                quantity,
                ResolveMaxStackSize(
                    itemId));
        }

        public bool RestoreSlot(
            int slotIndex,
            string itemId,
            string displayName,
            int quantity,
            int maxStackSize)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            slots[slotIndex].Set(
                itemId,
                displayName,
                quantity,
                maxStackSize);

            RebuildCompatibilityItems();
            return true;
        }

        private int FillExistingStacks(
            string itemId,
            int remainingQuantity,
            int maxStackSize,
            InventoryAddResult result)
        {
            for (int index = 0;
                 index < Capacity && remainingQuantity > 0;
                 index++)
            {
                InventorySlotState slot =
                    slots[index];

                if (slot.IsEmpty
                    || slot.ItemId != itemId
                    || !slot.CanStackMore)
                {
                    continue;
                }

                if (result.FirstChangedSlotIndex < 0)
                {
                    result.FirstChangedSlotIndex = index;
                }

                remainingQuantity =
                    slot.AddQuantity(
                        remainingQuantity);
            }

            return remainingQuantity;
        }

        private int FillEmptySlots(
            string itemId,
            string displayName,
            int remainingQuantity,
            int maxStackSize,
            InventoryAddResult result)
        {
            for (int index = 0;
                 index < Capacity && remainingQuantity > 0;
                 index++)
            {
                InventorySlotState slot =
                    slots[index];

                if (!slot.IsEmpty)
                {
                    continue;
                }

                int amountToPlace =
                    Math.Min(
                        remainingQuantity,
                        maxStackSize);

                slot.Set(
                    itemId,
                    displayName,
                    amountToPlace,
                    maxStackSize);

                if (result.FirstChangedSlotIndex < 0)
                {
                    result.FirstChangedSlotIndex = index;
                }

                remainingQuantity -= amountToPlace;
            }

            return remainingQuantity;
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0
                && slotIndex < Capacity
                && slotIndex < slots.Count;
        }

        private void EnsureCapacity()
        {
            int requiredCount =
                Capacity;

            while (slots.Count < requiredCount)
            {
                slots.Add(
                    new InventorySlotState());
            }
        }

        private void RebuildCompatibilityItems()
        {
            items.Clear();

            for (int index = 0;
                 index < slots.Count;
                 index++)
            {
                InventorySlotState slot =
                    slots[index];

                if (slot.IsEmpty)
                {
                    continue;
                }

                items.Add(
                    new InventoryItemStack(
                        slot.ItemId,
                        slot.DisplayName,
                        slot.Quantity,
                        slot.MaxStackSize));
            }
        }
    }

    public sealed class SkillRunState { }
    public sealed class CharacterRunState { }
    // 107일차: EventRunState는 Assets/ProjectDelta/Scripts/Domain/EventRunState.cs로 옮겼다.
    public sealed class BattleRunState { }
    public sealed class RewardRunState { }
    public sealed class RunStatistics { }
}
