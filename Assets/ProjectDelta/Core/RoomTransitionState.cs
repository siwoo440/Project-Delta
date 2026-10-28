using System; // 도착 칸 판정 함수
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class RoomTransitionState // 층 지도상의 현재 방
    { // 클래스 시작
        public int RoomX // 현재 방 가로 좌표
        { // 속성 시작
            get; // 좌표 조회
            private set; // 내부 좌표 변경
        } // 속성 끝
        public int RoomY // 현재 방 세로 좌표
        { // 속성 시작
            get; // 좌표 조회
            private set; // 내부 좌표 변경
        } // 속성 끝
        public RoomTransitionState(int roomX, int roomY) // 시작 방 설정
        { // 생성 시작
            RoomX = roomX; // 시작 방 가로 좌표
            RoomY = roomY; // 시작 방 세로 좌표
        } // 생성 끝
        public bool TryTransition(RoomDoor door, int x, int y, MoveDirection facing, Func<int, int, int, int, bool> canArrive) // 문 접근과 도착 칸 판정
        { // 판정 시작
            if (door == null || canArrive == null || door.RoomX != RoomX || door.RoomY != RoomY || door.ApproachX != x || door.ApproachY != y || door.Direction != facing) // 현재 방과 접근 위치 검사
            { // 접근 불일치 조건 시작
                return false; // 방 전환 거절
            } // 접근 불일치 조건 끝
            var offsetX = door.TargetRoomX - RoomX; // 도착 방 가로 차이
            var offsetY = door.TargetRoomY - RoomY; // 도착 방 세로 차이
            var adjacent = (facing == MoveDirection.Right && offsetX == 1 && offsetY == 0) || (facing == MoveDirection.Left && offsetX == -1 && offsetY == 0) || (facing == MoveDirection.Up && offsetX == 0 && offsetY == 1) || (facing == MoveDirection.Down && offsetX == 0 && offsetY == -1); // 방향에 맞는 인접 방 검사
            if (!adjacent || !canArrive(door.TargetRoomX, door.TargetRoomY, door.TargetX, door.TargetY)) // 연결 방향과 도착 가능 여부 검사
            { // 도착 불가 조건 시작
                return false; // 기존 방 유지
            } // 도착 불가 조건 끝
            RoomX = door.TargetRoomX; // 새 방 가로 좌표 확정
            RoomY = door.TargetRoomY; // 새 방 세로 좌표 확정
            return true; // 방 전환 승인
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
