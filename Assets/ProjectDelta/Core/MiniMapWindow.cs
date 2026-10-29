using System; // 좌표 거리 계산
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class MiniMapWindow // 현재 방 주변 미니맵 범위
    { // 클래스 시작
        public static bool Contains(int x, int y, int centerX, int centerY) // 다섯 칸 범위 검사
        { // 검사 시작
            return Math.Abs((long)x - centerX) <= 2 && Math.Abs((long)y - centerY) <= 2; // 현재 방 기준 가로 세로 두 칸
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
