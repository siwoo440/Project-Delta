using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // Placeholders for the remaining RunContext sub-states (기획서 10.2).
    // Each is filled in once its owning system exists:
    //   DungeonRunState   - 3.1~3.2절 던전 생성 (21일차 방 레지스트리, 22일차 층 번호, 28일차 연결 그래프)
    //   InventoryRunState - 6.4절 인벤토리·장비·유물
    //   SkillRunState     - 6.3절 스킬과 행동 숙련도
    //   CharacterRunState - 5장 몬스터·NPC (CharacterInstanceState)
    //   EventRunState     - 3.4~3.5절 이벤트
    //   BattleRunState    - 4장 전투
    //   RewardRunState    - 6.5절 아이템·보상
    //   RunStatistics     - 회차 단위 진행 통계

    // 지금 회차에 존재하는 방들의 런타임 상태(RoomInstance)를 방 ID로 조회한다.
    public sealed class DungeonRunState
    {
        private readonly Dictionary<string, RoomInstance> rooms = new Dictionary<string, RoomInstance>();

        // 현재 층 번호 (22일차).
        public int CurrentFloor { get; private set; } = 1;

        // 28일차: 현재 층의 방-방 연결 그래프. 절차적 생성 알고리즘(29일차 이후)이 이 그래프를 채운다.
        // 층이 바뀔 때(AdvanceFloor)마다 이전 층 그래프는 버리고 새로 시작한다 - 되돌아가는 방향이
        // 없는(기획서 3.1절) 이 게임에서 이전 층 연결 정보는 더 이상 쓸 일이 없다.
        public DungeonLayoutGraph Layout { get; private set; } = new DungeonLayoutGraph();

        // 계단으로 다음 층으로 내려갈 때 호출한다. 되돌아가는 방향은 없다(기획서 3.1절).
        public void AdvanceFloor()
        {
            CurrentFloor++;
            Layout = new DungeonLayoutGraph(); // 28일차: 새 층은 새 연결 그래프로 시작
        }

        // 26일차: 저장된 런을 이어할 때 층 번호를 그대로 복원한다.
        public void SetFloor(int floor)
        {
            CurrentFloor = floor;
        }

        public void Register(RoomInstance roomInstance)
        {
            if (roomInstance == null || string.IsNullOrEmpty(roomInstance.RoomId))
            {
                return;
            }

            rooms[roomInstance.RoomId] = roomInstance;
        }

        public bool TryGetRoom(string roomId, out RoomInstance roomInstance)
        {
            return rooms.TryGetValue(roomId, out roomInstance);
        }

        public IReadOnlyCollection<RoomInstance> AllRooms => rooms.Values;
    }

    // 25일차: 상자 상호작용 확인용 최소 인벤토리. 실제 아이템 정의(6.4절)가 생기기 전까지는
    // 아이템을 문자열 이름 하나로만 다룬다.
    public sealed class InventoryItemStack
    {
        public string ItemId; // 아이템 식별자 (지금은 표시 이름과 동일한 자리표시자)
        public string DisplayName; // 화면에 보일 이름

        public InventoryItemStack(string itemId, string displayName)
        {
            ItemId = itemId;
            DisplayName = displayName;
        }
    }

    public sealed class InventoryRunState
    {
        private readonly List<InventoryItemStack> items = new List<InventoryItemStack>();

        public IReadOnlyList<InventoryItemStack> Items => items;

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
