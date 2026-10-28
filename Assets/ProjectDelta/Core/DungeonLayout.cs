using System; // 구성 오류와 읽기 전용 목록
using System.Collections.Generic; // 방과 문 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonLayout // 방과 양방향 출구 정보
    { // 클래스 시작
        private readonly Dictionary<long, DungeonRoom> roomsByPosition = new Dictionary<long, DungeonRoom>(); // 좌표별 방 조회
        public IReadOnlyList<DungeonRoom> Rooms // 등록된 방 목록
        { // 속성 시작
            get; // 방 목록 조회
        } // 속성 끝
        public IReadOnlyList<RoomDoor> Doors // 등록된 양방향 문 목록
        { // 속성 시작
            get; // 문 목록 조회
        } // 속성 끝
        public DungeonLayout(DungeonRoom[] rooms, RoomDoor[] doors) // 방과 출구 정의
        { // 생성 시작
            if (rooms == null || rooms.Length == 0 || doors == null) // 방과 문 목록 검사
            { // 누락 조건 시작
                throw new ArgumentException("A layout requires rooms and a door list."); // 잘못된 구성 알림
            } // 누락 조건 끝
            foreach (var room in rooms) // 모든 방 정의 순회
            { // 방 순회 시작
                if (room == null || roomsByPosition.ContainsKey(Key(room.X, room.Y))) // 누락 또는 좌표 중복 검사
                { // 중복 조건 시작
                    throw new ArgumentException("Room coordinates must be unique."); // 중복 방 거절
                } // 중복 조건 끝
                roomsByPosition.Add(Key(room.X, room.Y), room); // 좌표별 방 등록
            } // 방 순회 끝
            foreach (var door in doors) // 모든 출구 정의 순회
            { // 출구 순회 시작
                if (door == null || GetRoom(door.RoomX, door.RoomY) == null || GetRoom(door.TargetRoomX, door.TargetRoomY) == null || !IsAdjacent(door)) // 출발 방과 인접 방향 검사
                { // 잘못된 출구 조건 시작
                    throw new ArgumentException("Doors must connect existing adjacent rooms."); // 잘못된 출구 거절
                } // 잘못된 출구 조건 끝
                var reverseCount = 0; // 일치하는 반대 문 수
                var directionCount = 0; // 같은 출발 방향의 문 수
                foreach (var other in doors) // 출구 짝과 방향 중복 검사
                { // 내부 순회 시작
                    if (other == null) // 누락된 출구 검사
                    { // 누락 조건 시작
                        throw new ArgumentException("Door definitions cannot be null."); // 누락 출구 거절
                    } // 누락 조건 끝
                    if (other.RoomX == door.RoomX && other.RoomY == door.RoomY && other.Direction == door.Direction) // 같은 출발 방향 검사
                    { // 방향 중복 검사 시작
                        directionCount++; // 같은 방향의 문 수 증가
                    } // 방향 중복 검사 끝
                    if (other.RoomX == door.TargetRoomX && other.RoomY == door.TargetRoomY && other.TargetRoomX == door.RoomX && other.TargetRoomY == door.RoomY && other.Direction == Opposite(door.Direction) && other.ApproachX == door.TargetX && other.ApproachY == door.TargetY && other.TargetX == door.ApproachX && other.TargetY == door.ApproachY && other.IsSecret == door.IsSecret) // 방과 진입 칸과 비밀 여부가 맞는 반대 문 검사
                    { // 짝 문 조건 시작
                        reverseCount++; // 일치하는 반대 문 수 증가
                    } // 짝 문 조건 끝
                } // 내부 순회 끝
                if (directionCount != 1 || reverseCount != 1) // 유일한 방향과 역방향 문 검사
                { // 연결 오류 조건 시작
                    throw new ArgumentException("Doors must have one matching reverse connection."); // 잘못된 연결 거절
                } // 연결 오류 조건 끝
            } // 출구 순회 끝
            Rooms = Array.AsReadOnly((DungeonRoom[])rooms.Clone()); // 외부 변경이 불가능한 방 목록
            Doors = Array.AsReadOnly((RoomDoor[])doors.Clone()); // 외부 변경이 불가능한 문 목록
        } // 생성 끝
        public DungeonRoom GetRoom(int x, int y) // 좌표의 방 정의 조회
        { // 조회 시작
            return roomsByPosition.TryGetValue(Key(x, y), out var room) ? room : null; // 존재하는 방 반환
        } // 조회 끝
        public RoomDoor FindDoor(int roomX, int roomY, int x, int y, MoveDirection facing) // 현재 위치와 방향의 문 조회
        { // 조회 시작
            foreach (var door in Doors) // 등록된 문 순회
            { // 문 순회 시작
                if (door.RoomX == roomX && door.RoomY == roomY && door.ApproachX == x && door.ApproachY == y && door.Direction == facing) // 접근 위치와 방향 검사
                { // 일치 조건 시작
                    return door; // 일치하는 문 전달
                } // 일치 조건 끝
            } // 문 순회 끝
            return null; // 일치하는 문 없음
        } // 조회 끝
        public bool HasConnection(int fromX, int fromY, int toX, int toY) // 방 사이 연결 검사
        { // 검사 시작
            return GetConnection(fromX, fromY, toX, toY) != null; // 출구의 실제 연결 여부
        } // 검사 끝
        public RoomDoor GetConnection(int fromX, int fromY, int toX, int toY) // 두 방 사이의 출구 조회
        { // 검사 시작
            foreach (var door in Doors) // 등록된 문 순회
            { // 문 순회 시작
                if (door.RoomX == fromX && door.RoomY == fromY && door.TargetRoomX == toX && door.TargetRoomY == toY) // 출발 방과 도착 방 검사
                { // 연결 조건 시작
                    return door; // 실제 연결 전달
                } // 연결 조건 끝
            } // 문 순회 끝
            return null; // 연결 없음
        } // 검사 끝
        internal static long Key(int x, int y) // 두 정수 좌표의 고유 키
        { // 변환 시작
            return ((long)x << 32) | (uint)y; // 음수 좌표를 포함한 고유 조합
        } // 변환 끝
        private static bool IsAdjacent(RoomDoor door) // 출구 방향에 맞는 인접 좌표 검사
        { // 검사 시작
            var x = (long)door.TargetRoomX - door.RoomX; // 방 가로 차이
            var y = (long)door.TargetRoomY - door.RoomY; // 방 세로 차이
            return (door.Direction == MoveDirection.Right && x == 1 && y == 0) || (door.Direction == MoveDirection.Left && x == -1 && y == 0) || (door.Direction == MoveDirection.Up && x == 0 && y == 1) || (door.Direction == MoveDirection.Down && x == 0 && y == -1); // 네 방향 인접 조건
        } // 검사 끝
        private static MoveDirection Opposite(MoveDirection direction) // 출구의 반대 방향
        { // 변환 시작
            switch (direction) // 출구 방향 선택
            { // 선택 시작
                case MoveDirection.Up: // 위쪽 출구
                    return MoveDirection.Down; // 아래쪽 짝 문
                case MoveDirection.Down: // 아래쪽 출구
                    return MoveDirection.Up; // 위쪽 짝 문
                case MoveDirection.Left: // 왼쪽 출구
                    return MoveDirection.Right; // 오른쪽 짝 문
                case MoveDirection.Right: // 오른쪽 출구
                    return MoveDirection.Left; // 왼쪽 짝 문
                default: // 잘못된 방향
                    return MoveDirection.None; // 유효하지 않은 방향
            } // 선택 끝
        } // 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
