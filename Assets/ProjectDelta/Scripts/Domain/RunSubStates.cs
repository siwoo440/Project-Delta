using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // Placeholders for the remaining RunContext sub-states (기획서 10.2).
    // Each is filled in once its owning system exists:
    //   DungeonRunState   - 3.1~3.2절 던전 생성
    //   InventoryRunState - 6.4절 인벤토리·장비·유물
    //   SkillRunState     - 6.3절 스킬과 행동 숙련도
    //   CharacterRunState - 5장 몬스터·NPC
    //   EventRunState     - 3.4~3.5절 이벤트
    //   BattleRunState    - 4장 전투
    //   RewardRunState    - 6.5절 아이템·보상
    //   RunStatistics     - 회차 단위 진행 통계

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

        // 39일차: 생성이 확정된 현재 층의 논리 던전과 Seed를 런 상태에 보관한다.
        // 이후 자동 저장은 Presentation을 직접 참조하지 않고 이 상태만 읽으면 된다.
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

        // 37일차 Fog of War 규칙을 RunContext에도 기록한다.
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

    // 25일차: 상자 상호작용 확인용 최소 인벤토리.
    public sealed class InventoryItemStack
    {
        public string ItemId;
        public string DisplayName;

        public InventoryItemStack(
            string itemId,
            string displayName)
        {
            ItemId = itemId;
            DisplayName = displayName;
        }
    }

    public sealed class InventoryRunState
    {
        private readonly List<InventoryItemStack> items =
            new List<InventoryItemStack>();

        public IReadOnlyList<InventoryItemStack> Items =>
            items;

        public void Add(InventoryItemStack item)
        {
            if (item == null)
            {
                return;
            }

            items.Add(item);
        }
    }

    public sealed class SkillRunState { }
    public sealed class CharacterRunState { }
    public sealed class EventRunState { }
    public sealed class BattleRunState { }
    public sealed class RewardRunState { }
    public sealed class RunStatistics { }
}
