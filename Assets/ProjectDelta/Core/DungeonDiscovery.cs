using System; // 구성 오류 알림
using System.Collections.Generic; // 방문 방 보관
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonDiscovery // 현재 탐험의 방 방문 기록
    { // 클래스 시작
        private readonly DungeonLayout layout; // 실제 방과 연결 정보
        private readonly HashSet<long> visited = new HashSet<long>(); // 직접 방문한 방 좌표
        private readonly HashSet<RoomDoor> discoveredSecretDoors = new HashSet<RoomDoor>(); // 실제로 통과한 비밀 연결
        private readonly HashSet<RoomDoor> openedSecretDoors = new HashSet<RoomDoor>(); // 실제 개방한 비밀 연결
        public bool IsSecretDiscovered(RoomDoor door) // 비밀 연결의 발견 여부
        { // 조회 시작
            return door != null && door.IsSecret && discoveredSecretDoors.Contains(door); // 실제 발견 기록 확인
        } // 조회 끝
        public bool IsSecretOpen(RoomDoor door) // 비밀 연결의 개방 여부
        { // 조회 시작
            return door != null && door.IsSecret && openedSecretDoors.Contains(door); // 실제 개방 기록 확인
        } // 조회 끝
        public bool TryDiscoverSecret(RoomDoor door) // 현재 방의 비밀벽 발견
        { // 발견 시작
            if (door == null || !door.IsSecret || door.RoomX != CurrentX || door.RoomY != CurrentY || layout.GetConnection(door.RoomX, door.RoomY, door.TargetRoomX, door.TargetRoomY) != door || IsSecretDiscovered(door)) // 현재 방과 실제 비밀 연결 검사
            { // 발견 거절 시작
                return false; // 잘못된 발견 요청 거절
            } // 발견 거절 끝
            discoveredSecretDoors.Add(door); // 현재 방향 발견
            discoveredSecretDoors.Add(layout.GetConnection(door.TargetRoomX, door.TargetRoomY, door.RoomX, door.RoomY)); // 반대 방향 발견
            return true; // 비밀벽 발견 성공
        } // 발견 끝
        public bool TryOpenSecret(RoomDoor door) // 발견한 비밀벽 개방
        { // 개방 시작
            if (door == null || door.RoomX != CurrentX || door.RoomY != CurrentY || !IsSecretDiscovered(door) || IsSecretOpen(door)) // 현재 방과 발견 상태 검사
            { // 개방 거절 시작
                return false; // 미발견 또는 중복 개방 차단
            } // 개방 거절 끝
            openedSecretDoors.Add(door); // 현재 방향 개방
            openedSecretDoors.Add(layout.GetConnection(door.TargetRoomX, door.TargetRoomY, door.RoomX, door.RoomY)); // 반대 방향 개방
            return true; // 양방향 개방 성공
        } // 개방 끝
        public SavedSecretPassage[] SaveSecrets() // 발견한 비밀 연결의 독립 복사
        { // 저장 시작
            var result = new List<SavedSecretPassage>(); // 발견한 비밀 통로 목록
            foreach (var door in layout.Doors) // 모든 연결 순회
            { // 연결 검사 시작
                if (!IsSecretDiscovered(door) || door.RoomX > door.TargetRoomX || (door.RoomX == door.TargetRoomX && door.RoomY > door.TargetRoomY)) // 미발견과 반대 방향 중복 검사
                { // 제외 조건 시작
                    continue; // 발견하지 않은 연결과 중복 제외
                } // 제외 조건 끝
                result.Add(new SavedSecretPassage { fromX = door.RoomX, fromY = door.RoomY, toX = door.TargetRoomX, toY = door.TargetRoomY, opened = IsSecretOpen(door) }); // 발견과 개방 상태 복사
            } // 연결 검사 끝
            return result.ToArray(); // 독립 비밀 연결 배열
        } // 저장 끝
        public SavedRoom[] SaveVisited() // 방문한 방 좌표의 독립 복사
        { // 저장 시작
            var result = new List<SavedRoom>(); // 방문한 방 목록
            foreach (var room in layout.Rooms) // 실제 방 순서 순회
            { // 방 순회 시작
                if (visited.Contains(DungeonLayout.Key(room.X, room.Y))) // 직접 방문 여부 검사
                { // 방문 조건 시작
                    result.Add(new SavedRoom { x = room.X, y = room.Y }); // 독립 좌표 저장
                } // 방문 조건 끝
            } // 방 순회 끝
            return result.ToArray(); // 방문 좌표 배열 전달
        } // 저장 끝
        public static DungeonDiscovery FromSaved(DungeonLayout layout, SavedRoom[] saved, int currentX, int currentY, SavedSecretPassage[] secrets = null) // 방문과 비밀 기록의 안전한 복원
        { // 복원 시작
            if (layout == null || saved == null || saved.Length == 0 || saved.Length > layout.Rooms.Count || layout.GetRoom(0, 0) == null) // 던전과 방문 목록 검사
            { // 오류 조건 시작
                return null; // 잘못된 기록 거절
            } // 오류 조건 끝
            var result = new DungeonDiscovery(layout, 0, 0); // 시작 방의 독립 기록
            var secretKeys = new HashSet<RoomDoor>(); // 중복 비밀 연결 검사
            if (secrets != null) // 저장된 비밀 연결 존재 검사
            { // 비밀 연결 복원 시작
                foreach (var secret in secrets) // 저장된 연결 순회
                { // 연결 복원 시작
                    if (secret == null) // 연결 기록 누락 검사
                    { // 누락 조건 시작
                        return null; // 잘못된 기록 거절
                    } // 누락 조건 끝
                    var door = layout.GetConnection(secret.fromX, secret.fromY, secret.toX, secret.toY); // 실제 저장 연결 조회
                    var reverse = layout.GetConnection(secret.toX, secret.toY, secret.fromX, secret.fromY); // 반대 방향 조회
                    if (door == null || reverse == null || !door.IsSecret || !reverse.IsSecret || !secretKeys.Add(door) || !secretKeys.Add(reverse)) // 비밀 연결과 중복 검사
                    { // 연결 오류 시작
                        return null; // 잘못된 연결 거절
                    } // 연결 오류 끝
                    result.discoveredSecretDoors.Add(door); // 출발 방향 발견 복원
                    result.discoveredSecretDoors.Add(reverse); // 반대 방향 발견 복원
                    if (secret.opened) // 저장된 개방 상태 검사
                    { // 개방 복원 시작
                        result.openedSecretDoors.Add(door); // 출발 방향 개방 복원
                        result.openedSecretDoors.Add(reverse); // 반대 방향 개방 복원
                    } // 개방 복원 끝
                } // 연결 복원 끝
            } // 비밀 연결 복원 끝
            var keys = new HashSet<long>(); // 저장 좌표의 중복 검사
            foreach (var room in saved) // 저장 방 순회
            { // 순회 시작
                if (room == null || layout.GetRoom(room.x, room.y) == null || !keys.Add(DungeonLayout.Key(room.x, room.y))) // 존재와 중복 방 검사
                { // 오류 조건 시작
                    return null; // 잘못된 방문 목록 거절
                } // 오류 조건 끝
            } // 순회 끝
            if (!keys.Contains(DungeonLayout.Key(0, 0)) || !keys.Contains(DungeonLayout.Key(currentX, currentY))) // 시작 방과 현재 방의 방문 여부
            { // 누락 조건 시작
                return null; // 불완전한 방문 기록 거절
            } // 누락 조건 끝
            if (secrets != null) // 저장된 비밀 기록 검사
            { // 발견 경로 검사 시작
                foreach (var secret in secrets) // 발견된 연결 순회
                { // 기록 검사 시작
                    if (!keys.Contains(DungeonLayout.Key(secret.fromX, secret.fromY)) && !keys.Contains(DungeonLayout.Key(secret.toX, secret.toY))) // 발견 가능한 방문 방 검사
                    { // 방문 누락 시작
                        return null; // 방문하지 않은 연결의 발견 기록 거절
                    } // 방문 누락 끝
                } // 기록 검사 끝
            } // 발견 경로 검사 끝
            var connected = new HashSet<long> { DungeonLayout.Key(0, 0) }; // 방문 방 연결 검사 집합
            var pending = new Queue<DungeonRoom>(); // 검사할 방문 방 대기열
            pending.Enqueue(layout.GetRoom(0, 0)); // 시작 방부터 검사
            while (pending.Count > 0) // 연결된 방문 방 순회
            { // 연결 검사 시작
                var room = pending.Dequeue(); // 다음 방문 방 조회
                foreach (var door in layout.Doors) // 현재 방의 연결 순회
                { // 연결 순회 시작
                    var target = DungeonLayout.Key(door.TargetRoomX, door.TargetRoomY); // 도착 방 식별자
                    if (door.RoomX == room.X && door.RoomY == room.Y && (!door.IsSecret || result.IsSecretOpen(door)) && keys.Contains(target) && connected.Add(target)) // 실제 방문한 개방 연결 검사
                    { // 연결 조건 시작
                        pending.Enqueue(layout.GetRoom(door.TargetRoomX, door.TargetRoomY)); // 다음 방문 방 예약
                    } // 연결 조건 끝
                } // 연결 순회 끝
            } // 연결 검사 끝
            if (connected.Count != keys.Count) // 방문 방의 연결 누락 검사
            { // 연결 오류 시작
                return null; // 도달할 수 없는 방문 기록 거절
            } // 연결 오류 끝
            result.visited.Clear(); // 초기 방문 기록 교체
            foreach (var key in keys) // 검증된 좌표 순회
            { // 복사 시작
                result.visited.Add(key); // 방문 좌표 복원
            } // 복사 끝
            result.CurrentX = currentX; // 현재 방 가로 좌표 복원
            result.CurrentY = currentY; // 현재 방 세로 좌표 복원
            return result; // 독립 방문 기록 전달
        } // 복원 끝
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
                foreach (var door in layout.Doors) // 비밀방으로 연결되는 통로 순회
                { // 발견 연결 검사 시작
                    if (door.TargetRoomX == x && door.TargetRoomY == y && IsSecretDiscovered(door)) // 발견된 비밀벽 검사
                    { // 발견 조건 시작
                        return RoomVisibility.Connected; // 발견한 미방문 비밀방 표시
                    } // 발견 조건 끝
                } // 발견 연결 검사 끝
                return RoomVisibility.Hidden; // 미발견 비밀 방 정보 가림
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
            if (door.IsSecret && !IsSecretOpen(door)) // 개방된 비밀 통로 검사
            { // 닫힌 통로 조건 시작
                return false; // 개방 전 비밀방 이동 차단
            } // 닫힌 통로 조건 끝
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
