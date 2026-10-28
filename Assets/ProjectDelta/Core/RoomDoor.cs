namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class RoomDoor // 양방향 방 이동의 한쪽 문
    { // 클래스 시작
        public readonly int RoomX; // 출발 방 가로 좌표
        public readonly int RoomY; // 출발 방 세로 좌표
        public readonly int ApproachX; // 문 앞 가로 칸
        public readonly int ApproachY; // 문 앞 세로 칸
        public readonly MoveDirection Direction; // 문을 향한 방향
        public readonly int TargetRoomX; // 도착 방 가로 좌표
        public readonly int TargetRoomY; // 도착 방 세로 좌표
        public readonly int TargetX; // 도착 가로 칸
        public readonly int TargetY; // 도착 세로 칸
        public RoomDoor(int roomX, int roomY, int approachX, int approachY, MoveDirection direction, int targetRoomX, int targetRoomY, int targetX, int targetY) // 문 연결 정보 생성
        { // 생성 시작
            RoomX = roomX; // 출발 방 가로 좌표 보관
            RoomY = roomY; // 출발 방 세로 좌표 보관
            ApproachX = approachX; // 문 앞 가로 칸 보관
            ApproachY = approachY; // 문 앞 세로 칸 보관
            Direction = direction; // 문 방향 보관
            TargetRoomX = targetRoomX; // 도착 방 가로 좌표 보관
            TargetRoomY = targetRoomY; // 도착 방 세로 좌표 보관
            TargetX = targetX; // 도착 가로 칸 보관
            TargetY = targetY; // 도착 세로 칸 보관
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
