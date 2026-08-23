using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // Placeholders for the remaining RunContext sub-states (기획서 10.2).
    // Each is filled in once its owning system exists:
    //   DungeonRunState   - 3.1~3.2절 던전 생성 (21일차: 방 레지스트리만 우선 채움)
    //   InventoryRunState - 6.4절 인벤토리·장비·유물
    //   SkillRunState     - 6.3절 스킬과 행동 숙련도
    //   CharacterRunState - 5장 몬스터·NPC (CharacterInstanceState)
    //   EventRunState     - 3.4~3.5절 이벤트
    //   BattleRunState    - 4장 전투
    //   RewardRunState    - 6.5절 아이템·보상
    //   RunStatistics     - 회차 단위 진행 통계

    // 지금 회차에 존재하는 방들의 런타임 상태(RoomInstance)를 방 ID로 조회한다.
    // 전체 층 생성·연결 데이터(DungeonLayoutData 등)는 26~35일차 던전 생성 구간에서 채운다.
    public sealed class DungeonRunState
    {
        private readonly Dictionary<string, RoomInstance> rooms = new Dictionary<string, RoomInstance>();

        // 현재 층 번호 (22일차). 실제 절차적 생성·연결 데이터는 26~35일차에 채운다.
        public int CurrentFloor { get; private set; } = 1;

        // 계단으로 다음 층으로 내려갈 때 호출한다. 되돌아가는 방향은 없다(기획서 3.1절).
        public void AdvanceFloor()
        {
            CurrentFloor++;
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

    public sealed class InventoryRunState { }
    public sealed class SkillRunState { }
    public sealed class CharacterRunState { }
    public sealed class EventRunState { }
    public sealed class BattleRunState { }
    public sealed class RewardRunState { }
    public sealed class RunStatistics { }
}
