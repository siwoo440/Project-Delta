using System; // 직렬화 표시
using ProjectDelta.Core; // 순수 방 출구 형식
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    [Serializable] // Unity 저장 대상 표시
    public sealed class DoorBinding // 직렬화 가능한 방 출구
    { // 클래스 시작
        public int RoomX; // 출발 방 가로 좌표
        public int RoomY; // 출발 방 세로 좌표
        public int ApproachX; // 문 앞 가로 칸
        public int ApproachY; // 문 앞 세로 칸
        public MoveDirection Direction; // 문을 향한 방향
        public int TargetRoomX; // 도착 방 가로 좌표
        public int TargetRoomY; // 도착 방 세로 좌표
        public int TargetX; // 도착 가로 칸
        public int TargetY; // 도착 세로 칸
        public bool IsSecret; // 비밀 출구 여부
        public DoorBinding(int roomX, int roomY, int approachX, int approachY, MoveDirection direction, int targetRoomX, int targetRoomY, int targetX, int targetY, bool isSecret = false) // 방 출구 정보 생성
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
            IsSecret = isSecret; // 비밀 출구 여부 보관
        } // 생성 끝
        public RoomDoor ToDoor() // 순수 출구 정보 생성
        { // 변환 시작
            return new RoomDoor(RoomX, RoomY, ApproachX, ApproachY, Direction, TargetRoomX, TargetRoomY, TargetX, TargetY, IsSecret); // 화면과 분리된 문 정보
        } // 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
