using System; // 구성 오류 알림
using System.Collections.Generic; // 방문 방 보관
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonDiscovery // 현재 탐험의 방 방문 기록
    { // 클래스 시작
        private readonly DungeonLayout layout; // 실제 방과 연결 정보
        private readonly HashSet<long> visited = new HashSet<long>(); // 직접 방문한 방 좌표
        private readonly HashSet<RoomDoor> discoveredSecretDoors = new HashSet<RoomDoor>(); // 실제로 통과한 비밀 연결
        public int CurrentX // 현재 방 가로 좌표
        { // 속성 시작
            get; // 좌표 조회
            private set; // 내부 좌표 변경
        } // 속성 끝
        public int CurrentY // 현재 방 세로 좌표
        { // 속성 시작
            get; // 좌표 조회
            private set; // 내부 좌표 변경
        } // 속성 끝
        public DungeonDiscovery(DungeonLayout layout, int startX, int startY) // 시작 방의 방문 기록 생성
        { // 생성 시작
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout)); // 유효한 방 구조 보관
            if (layout.GetRoom(startX, startY) == null) // 시작 방 존재 검사
            { // 시작 방 누락 조건 시작
                throw new ArgumentException("Start room must exist in the layout."); // 잘못된 시작 방 거절
            } // 시작 방 누락 조건 끝
            CurrentX = startX; // 시작 방 가로 좌표
            CurrentY = startY; // 시작 방 세로 좌표
            visited.Add(DungeonLayout.Key(startX, startY)); // 시작 방 방문 처리
        } // 생성 끝
        public RoomVisibility GetVisibility(int x, int y) // 플레이어에게 보이는 방 상태
        { // 조회 시작
            var room = layout.GetRoom(x, y); // 표시 대상 방 정의 조회
            if (room == null) // 실제 방 존재 검사
            { // 방 없음 조건 시작
                return RoomVisibility.Hidden; // 없는 방 표시 제외
            } // 방 없음 조건 끝
            if (visited.Contains(DungeonLayout.Key(x, y))) // 직접 방문 검사
            { // 방문 조건 시작
                return RoomVisibility.Visited; // 방문 방 표시
            } // 방문 조건 끝
            if (room.IsSecret) // 방문하지 않은 비밀 방 검사
            { // 비밀 방 조건 시작
                return RoomVisibility.Hidden; // 비밀 방 정보 가림
            } // 비밀 방 조건 끝
            foreach (var previous in layout.Rooms) // 전체 방의 방문 기록 순회
            { // 기록 순회 시작
                if (visited.Contains(DungeonLayout.Key(previous.X, previous.Y)) && IsKnownConnection(previous.X, previous.Y, x, y)) // 방문 방에서 확인한 공개 연결 검사
                { // 연결 확인 조건 시작
                    return RoomVisibility.Connected; // 연결만 확인한 방 표시
                } // 연결 확인 조건 끝
            } // 기록 순회 끝
            return RoomVisibility.Hidden; // 아직 발견하지 못한 방 가림
        } // 조회 끝
        public bool TryVisit(int x, int y) // 연결된 방의 방문 처리
        { // 방문 시작
            var door = layout.GetConnection(CurrentX, CurrentY, x, y); // 현재 방에서 도착 방의 출구 조회
            if (door == null) // 현재 방에서 연결 검사
            { // 연결 없음 조건 시작
                return false; // 잘못된 방문 거절
            } // 연결 없음 조건 끝
            if (door.IsSecret) // 실제 통과한 비밀 연결 검사
            { // 비밀 연결 조건 시작
                discoveredSecretDoors.Add(door); // 출발 방향의 비밀 통로 발견
                discoveredSecretDoors.Add(layout.GetConnection(x, y, CurrentX, CurrentY)); // 복귀 방향의 비밀 통로 발견
            } // 비밀 연결 조건 끝
            CurrentX = x; // 도착 방 가로 좌표 확정
            CurrentY = y; // 도착 방 세로 좌표 확정
            visited.Add(DungeonLayout.Key(x, y)); // 직접 방문 기록 추가
            return true; // 방문 처리 성공
        } // 방문 끝
        public bool IsConnectionVisible(int fromX, int fromY, int toX, int toY) // 지도 연결선 공개 검사
        { // 검사 시작
            var from = GetVisibility(fromX, fromY); // 출발 방의 공개 상태
            var to = GetVisibility(toX, toY); // 도착 방의 공개 상태
            return from != RoomVisibility.Hidden && to != RoomVisibility.Hidden && (from == RoomVisibility.Visited || to == RoomVisibility.Visited) && IsKnownConnection(fromX, fromY, toX, toY); // 방문 방에서 확인한 연결만 표시
        } // 검사 끝
        private bool IsKnownConnection(int fromX, int fromY, int toX, int toY) // 일반 또는 발견한 비밀 연결 검사
        { // 검사 시작
            var door = layout.GetConnection(fromX, fromY, toX, toY); // 두 방 사이 출구 조회
            return door != null && (!door.IsSecret || discoveredSecretDoors.Contains(door)); // 비밀 연결의 공개 시점 보호
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
