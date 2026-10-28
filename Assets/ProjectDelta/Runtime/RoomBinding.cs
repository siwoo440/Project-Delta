using System; // 직렬화 표시
using UnityEngine; // 씬 방 객체
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    [Serializable] // Unity 저장 대상 표시
    public sealed class RoomBinding // 방 좌표와 씬 객체 연결
    { // 클래스 시작
        public int X; // 층 지도 가로 좌표
        public int Y; // 층 지도 세로 좌표
        public bool IsSecret; // 비밀 방 여부
        public GameObject Root; // 실제 방 객체
        public RoomBinding(int x, int y, bool isSecret, GameObject root) // 방 연결 생성
        { // 생성 시작
            X = x; // 가로 좌표 보관
            Y = y; // 세로 좌표 보관
            IsSecret = isSecret; // 비밀 방 여부 보관
            Root = root; // 씬 객체 보관
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
