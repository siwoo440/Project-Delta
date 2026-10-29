using System.Collections.Generic; // 조우 대상 목록
using System.Collections.ObjectModel; // 읽기 전용 조우 대상
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterPlacement // 한 방에 고정된 조우 정의
    { // 클래스 시작
        public int Floor { get; } // 조우가 위치한 층
        public int RoomX { get; } // 조우 방 가로 좌표
        public int RoomY { get; } // 조우 방 세로 좌표
        public IReadOnlyList<MonsterDefinition> Monsters { get; } // 순서가 보존된 읽기 전용 대상
        private EncounterPlacement(int floor, int x, int y, List<MonsterDefinition> monsters) // 검증된 조우 구성
        { // 구성 시작
            Floor = floor; // 위치한 층 보관
            RoomX = x; // 방 가로 좌표 보관
            RoomY = y; // 방 세로 좌표 보관
            Monsters = new ReadOnlyCollection<MonsterDefinition>(monsters); // 외부 변경이 불가능한 목록
        } // 구성 끝
        public static EncounterPlacement Create(DungeonLayout layout, int floor, int x, int y, string[] ids, bool firstEncounter) // 조우 대상과 고정 방 검증
        { // 생성 시작
            if (layout == null || floor < 1 || ids == null || ids.Length < 1 || ids.Length > 3) // 던전과 조우 수 검사
            { // 입력 오류 시작
                return null; // 잘못된 조우 거절
            } // 입력 오류 끝
            var room = layout.GetRoom(x, y); // 실제 방 정의 조회
            if (room == null || room.IsSecret || (room.Kind != RoomKind.Normal && room.Kind != RoomKind.Elite)) // 조우 가능한 방 종류 검사
            { // 방 오류 시작
                return null; // 비밀방과 필수 방 거절
            } // 방 오류 끝
            var monsters = new List<MonsterDefinition>(); // 검증한 대상 목록
            var elites = 0; // 상위 개체 수량
            foreach (var id in ids) // 요청한 대상 순회
            { // 대상 검사 시작
                var definition = MonsterCatalog.Find(id); // 실제 대상 정의 조회
                if (definition == null || !definition.CanAppearOn(floor) || (firstEncounter && (floor != 1 || definition.Id != "MON_SLIME"))) // 등록과 층 및 첫 슬라임 검사
                { // 대상 오류 시작
                    return null; // 허용하지 않는 대상 거절
                } // 대상 오류 끝
                if (definition.IsElite) // 상위 개체 여부 검사
                { // 상위 개체 시작
                    elites++; // 상위 개체 수량 증가
                } // 상위 개체 끝
                monsters.Add(definition); // 검증된 대상 보관
            } // 대상 검사 끝
            if ((room.Kind == RoomKind.Normal && elites != 0) || (room.Kind == RoomKind.Elite && elites != 1)) // 방 종류별 상위 개체 검사
            { // 상위 개체 오류 시작
                return null; // 잘못된 상위 방 조합 거절
            } // 상위 개체 오류 끝
            return new EncounterPlacement(floor, x, y, monsters); // 검증된 고정 조우 전달
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
