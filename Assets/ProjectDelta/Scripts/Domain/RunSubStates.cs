using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // RunContext 아래의 회차 단위 하위 상태를 모아 둔다.
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
            CurrentFloor = Math.Max(1, floor);
        }

        // 확정된 현재 층의 논리 던전과 Seed를 런 상태에 보관한다.
        public void SetGeneratedFloor(
            GeneratedDungeon dungeon,
            int seed)
        {
            if (dungeon == null)
            {
                throw new ArgumentNullException(nameof(dungeon));
            }

            currentGeneratedDungeon = dungeon;
            Layout = dungeon.Layout;
            CurrentDungeonSeed = seed;
            CurrentLayoutSnapshot =
                DungeonLayoutSnapshot.Capture(
                    dungeon,
                    seed);
        }

        // 저장 데이터에서 동일한 방 좌표·연결·Entry/Stairs를 다시 만든다.
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

        // 현재 방을 포함한 3x3 범위에 실제로 존재하는 방만 발견 처리한다.
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
                    revealedRoomIds.Add(room.RoomId);
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
                    revealedRoomIds.Add(roomId);
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

    // 기존 상자·획득 코드가 사용하던 최소 아이템 표현을 유지한다.
    public sealed class InventoryItemStack
    {
        public string ItemId;
        public string DisplayName;
        public int Quantity;

        public InventoryItemStack(
            string itemId,
            string displayName)
            : this(
                itemId,
                displayName,
                1)
        {
        }

        public InventoryItemStack(
            string itemId,
            string displayName,
            int quantity)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Quantity = Math.Max(1, quantity);
        }
    }

    // 89일차: 실제 위치를 유지하는 하나의 인벤토리 슬롯이다.
    public sealed class InventorySlotState
    {
        public string ItemId { get; private set; }

        public string DisplayName { get; private set; }

        public int Quantity { get; private set; }

        public bool IsEmpty =>
            string.IsNullOrEmpty(ItemId)
            || Quantity <= 0;

        public void Set(
            string itemId,
            string displayName,
            int quantity)
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
            Quantity = quantity;
        }

        public void Clear()
        {
            ItemId = string.Empty;
            DisplayName = string.Empty;
            Quantity = 0;
        }
    }

    // 89일차: 10칸 슬롯을 런타임 인벤토리의 단일 기준으로 사용한다.
    public sealed class InventoryRunState
    {
        public const int BaseSlotCount = 10;

        private readonly List<InventorySlotState> slots =
            new List<InventorySlotState>();

        private readonly List<InventoryItemStack> items =
            new List<InventoryItemStack>();

        public IReadOnlyList<InventorySlotState> Slots =>
            slots;

        // 기존 코드의 Items 조회와 Add 호출을 깨지 않기 위해 호환 목록을 유지한다.
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

        // 기존 상자 획득 코드는 그대로 이 메서드를 사용해 첫 빈 슬롯에 들어간다.
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
                out _);
        }

        public bool TryAdd(
            string itemId,
            string displayName,
            int quantity,
            out int slotIndex)
        {
            slotIndex = -1;

            if (string.IsNullOrEmpty(itemId)
                || quantity <= 0)
            {
                return false;
            }

            EnsureCapacity();

            for (int index = 0;
                 index < Capacity;
                 index++)
            {
                InventorySlotState slot =
                    slots[index];

                if (!slot.IsEmpty)
                {
                    continue;
                }

                slot.Set(
                    itemId,
                    displayName,
                    quantity);

                slotIndex = index;

                RebuildCompatibilityItems();
                return true;
            }

            return false;
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

        // 빈 슬롯으로 이동하거나 두 슬롯에 아이템이 있으면 서로 교환한다.
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

            string sourceItemId =
                source.ItemId;

            string sourceDisplayName =
                source.DisplayName;

            int sourceQuantity =
                source.Quantity;

            if (destination.IsEmpty)
            {
                destination.Set(
                    sourceItemId,
                    sourceDisplayName,
                    sourceQuantity);

                source.Clear();
            }
            else
            {
                string destinationItemId =
                    destination.ItemId;

                string destinationDisplayName =
                    destination.DisplayName;

                int destinationQuantity =
                    destination.Quantity;

                destination.Set(
                    sourceItemId,
                    sourceDisplayName,
                    sourceQuantity);

                source.Set(
                    destinationItemId,
                    destinationDisplayName,
                    destinationQuantity);
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

            slot =
                slots[slotIndex];

            return true;
        }

        // 저장 데이터를 적용하기 전에 현재 슬롯을 초기화한다.
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

        // 저장된 슬롯 위치를 그대로 되살린다.
        public bool RestoreSlot(
            int slotIndex,
            string itemId,
            string displayName,
            int quantity)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            slots[slotIndex].Set(
                itemId,
                displayName,
                quantity);

            RebuildCompatibilityItems();
            return true;
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
                        slot.Quantity));
            }
        }
    }

    public sealed class SkillRunState { }
    public sealed class CharacterRunState { }
    public sealed class EventRunState { }
    public sealed class BattleRunState { }
    public sealed class RewardRunState { }
    public sealed class RunStatistics { }
}
