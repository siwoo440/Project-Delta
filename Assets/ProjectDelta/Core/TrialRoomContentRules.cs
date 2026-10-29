using System; // 독립 배열과 정수 비교
using System.Collections.Generic; // 방 후보 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class TrialRoomContentPlacement // 발생 확률 미확정의 시험 상자와 함정 배치
    { // 배치 클래스 시작
        private readonly DungeonRoom[] chests; // 외부 변경을 차단한 상자 방
        public DungeonRoom[] ChestRooms => (DungeonRoom[])chests.Clone(); // 독립 상자 방 목록
        public DungeonRoom TrapRoom { get; } // 시험 함정 방
        public string ChestGrade => "normal"; // 미확정 등급의 일반 상자 시험값
        public bool ChestLocked => false; // 미확정 잠금 여부의 미잠금 시험값
        private TrialRoomContentPlacement(DungeonRoom[] rooms, DungeonRoom trap) // 검증된 시험 배치 생성
        { // 생성 시작
            chests = (DungeonRoom[])rooms.Clone(); // 상자 방 목록의 독립 복사
            TrapRoom = trap; // 상자와 분리된 함정 방
        } // 생성 끝
        public static DungeonRoom[] Excluded(DungeonLayout layout, int floor, int generationVersion, bool groupEnabled) // 기존 첫 조우와 다수 조우 방 재현
        { // 제외 방 조회 시작
            if (floor != 1 || generationVersion != 3) // 해당 조우가 없는 층과 생성 규칙 검사
            { // 조우 없음 시작
                return new DungeonRoom[0]; // 제외할 조우 방 없음
            } // 조우 없음 끝
            if (layout == null) // 실제 층 구조 검사
            { // 구조 오류 시작
                return null; // 조우 방을 판정할 수 없는 구조 거절
            } // 구조 오류 끝
            DungeonRoom first = null; // 시작 방의 첫 일반 연결 목적지
            foreach (var door in layout.Doors) // 런타임 순서의 실제 출구 순회
            { // 출구 검사 시작
                if (door.RoomX == 0 && door.RoomY == 0 && !door.IsSecret) // 시작 방의 첫 비밀 아닌 출구 검사
                { // 첫 연결 시작
                    first = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 첫 슬라임 조우 방
                    break; // 이후 출구 검사 종료
                } // 첫 연결 끝
            } // 출구 검사 끝
            if (first == null || EncounterPlacement.Create(layout, 1, first.X, first.Y, new[] { "MON_SLIME" }, true) == null) // 런타임과 같은 첫 조우 유효성 검사
            { // 첫 조우 오류 시작
                return null; // 첫 조우가 없는 잘못된 구조 거절
            } // 첫 조우 오류 끝
            if (!groupEnabled) // 다수 조우 활성화 검사
            { // 첫 조우만 제외 시작
                return new[] { first }; // 첫 슬라임 방의 독립 배열
            } // 첫 조우만 제외 끝
            var group = TrialGroupRoomSelector.Select(layout, first.X, first.Y); // 기존 런타임의 동일한 다수 조우 방
            if (group == null) // 필요한 다수 조우 방 검사
            { // 다수 조우 오류 시작
                return null; // 기존 다수 조우가 없는 구조 거절
            } // 다수 조우 오류 끝
            return new[] { first, group }; // 두 조우 방의 독립 배열
        } // 제외 방 조회 끝
        public static bool TryCreate(DungeonLayout layout, int seed, DungeonRoom[] excluded, out TrialRoomContentPlacement placement) // 방과 씨드 기반 시험 콘텐츠 배치
        { // 배치 시작
            placement = null; // 기본 배치 실패
            if (layout == null) // 생성된 층 검사
            { // 층 오류 시작
                return false; // 없는 층의 배치 차단
            } // 층 오류 끝
            var candidates = new List<DungeonRoom>(); // 실제 배치 가능한 일반 방
            foreach (var room in layout.Rooms) // 층의 방 순회
            { // 후보 검사 시작
                if (room == null || room.IsSecret || room.Kind != RoomKind.Normal || (room.X == 0 && room.Y == 0) || IsExcluded(room, excluded)) // 필수 방과 호출자가 지정한 기존 콘텐츠 검사
                { // 제외 방 시작
                    continue; // 시험 콘텐츠 없는 다음 방 검사
                } // 제외 방 끝
                candidates.Add(room); // 안전한 시험 배치 후보
            } // 후보 검사 끝
            if (candidates.Count < 3) // 상자 두 개와 함정 한 개의 최소 공간 검사
            { // 공간 부족 시작
                return false; // 필수 콘텐츠가 겹치는 배치 차단
            } // 공간 부족 끝
            candidates.Sort((left, right) => CompareRooms(left, right, seed)); // 씨드와 좌표 기반 안정적인 후보 순서
            var count = (seed & 1) == 0 || candidates.Count < 4 ? 2 : 3; // 미확정 분포의 상자 두 개 또는 세 개 시험값
            var rooms = new DungeonRoom[count]; // 확정된 상자 방 목록
            candidates.CopyTo(0, rooms, 0, count); // 앞선 일반 방을 상자 방으로 선택
            placement = new TrialRoomContentPlacement(rooms, candidates[count]); // 남은 별도 일반 방 하나의 시험 함정
            return true; // 정상 시험 배치 성공
        } // 배치 끝
        private static bool IsExcluded(DungeonRoom room, DungeonRoom[] excluded) // 다른 콘텐츠의 좌표 제외
        { // 제외 검사 시작
            if (excluded == null) // 별도 제외 목록 검사
            { // 목록 없음 시작
                return false; // 지정된 제외 방 없음
            } // 목록 없음 끝
            foreach (var item in excluded) // 호출자가 지정한 방 순회
            { // 좌표 검사 시작
                if (item != null && item.X == room.X && item.Y == room.Y) // 실제 좌표 일치 검사
                { // 좌표 일치 시작
                    return true; // 다른 콘텐츠 방 제외
                } // 좌표 일치 끝
            } // 좌표 검사 끝
            return false; // 지정된 제외 좌표 없음
        } // 제외 검사 끝
        private static int CompareRooms(DungeonRoom left, DungeonRoom right, int seed) // 입력 순서와 무관한 결정적 방 순서
        { // 비교 시작
            var order = Rank(left, seed).CompareTo(Rank(right, seed)); // 씨드별 좌표 해시 비교
            if (order != 0) // 해시 차이 검사
            { // 해시 차이 시작
                return order; // 씨드 기반 순서 전달
            } // 해시 차이 끝
            order = left.X.CompareTo(right.X); // 같은 해시의 가로 좌표 비교
            return order != 0 ? order : left.Y.CompareTo(right.Y); // 같은 가로 좌표의 세로 비교
        } // 비교 끝
        private static uint Rank(DungeonRoom room, int seed) // 플랫폼에 독립적인 좌표 해시
        { // 해시 시작
            unchecked // 좌표 해시의 정수 넘침 허용
            { // 해시 계산 시작
                var value = 2166136261u; // 고정된 시작 해시
                value = (value ^ (uint)seed) * 16777619u; // 층 생성 씨드 반영
                value = (value ^ (uint)room.X) * 16777619u; // 방 가로 좌표 반영
                return (value ^ (uint)room.Y) * 16777619u; // 방 세로 좌표 반영
            } // 해시 계산 끝
        } // 해시 끝
    } // 배치 클래스 끝
    public sealed class TrialTrapResult // 첫 진입의 확정된 시험 함정 결과
    { // 결과 클래스 시작
        public bool Avoided { get; } // 시험 회피 성공 여부
        public int Damage { get; } // 회피 후 적용할 후보 피해
        internal TrialTrapResult(bool avoided, int damage) // 검증된 함정 결과 생성
        { // 생성 시작
            Avoided = avoided; // 실제 회피 여부 보관
            Damage = damage; // 실제 적용할 피해 보관
        } // 생성 끝
    } // 결과 클래스 끝
    public sealed class TrialTrapState // 저장 가능한 단일 방 함정 완료 상태
    { // 상태 클래스 시작
        public bool Completed { get; } // 첫 진입 처리 완료 여부
        private TrialTrapState(bool completed) // 검증된 완료 상태 생성
        { // 생성 시작
            Completed = completed; // 기존 완료 상태 보관
        } // 생성 끝
        public static TrialTrapState Untriggered => new TrialTrapState(false); // 아직 발동하지 않은 함정
        public static bool TryRestore(bool completed, out TrialTrapState state) // 저장된 완료 여부 복원
        { // 복원 시작
            state = new TrialTrapState(completed); // 저장한 완료 상태의 독립 복원
            return true; // 불리언 상태 복원 성공
        } // 복원 끝
        public bool TryTrigger(int evasion, int roll, int damageRoll, out TrialTrapState state, out TrialTrapResult result) // 첫 진입의 시험 회피와 피해 판정
        { // 판정 시작
            state = null; // 기본 상태 결과 없음
            result = null; // 기본 피해 결과 없음
            if (Completed || evasion < 0 || roll < 0 || roll >= 100 || damageRoll < 10 || damageRoll > 18) // 재발동과 시험 입력 범위 검사
            { // 판정 오류 시작
                return false; // 완료와 피해 변경 차단
            } // 판정 오류 끝
            var chance = (int)Math.Min(75L, 15L + evasion); // 회피 기본 15와 상한 75의 시험 1차안
            var avoided = roll < chance; // 영부터 시작하는 주사위의 회피 판정
            result = new TrialTrapResult(avoided, avoided ? 0 : damageRoll); // 회피 또는 후보 피해 결과
            state = new TrialTrapState(true); // 성공과 실패 모두 완료된 독립 상태
            return true; // 첫 진입의 판정 성공
        } // 판정 끝
    } // 상태 클래스 끝
} // 이름 공간 끝
