namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonRoom // 층 지도상의 방 정의
    { // 클래스 시작
        public readonly int X; // 방 가로 좌표
        public readonly int Y; // 방 세로 좌표
        public readonly bool IsSecret; // 비밀 방 여부
        public readonly RoomKind Kind; // 방의 게임 진행 역할
        public DungeonRoom(int x, int y, bool isSecret = false, RoomKind kind = RoomKind.Normal) // 방 정보 생성
        { // 생성 시작
            X = x; // 방 가로 좌표 보관
            Y = y; // 방 세로 좌표 보관
            IsSecret = isSecret; // 비밀 방 여부 보관
            Kind = kind; // 방 역할 보관
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
