namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class EncounterRange // 몬스터 주변 조우 범위
    { // 클래스 시작
        public static bool Contains(int playerX, int playerY, int monsterX, int monsterY) // 상하좌우와 대각선 거리 판정
        { // 판정 시작
            var horizontal = System.Math.Abs((long)playerX - monsterX); // 가로 칸 차이
            var vertical = System.Math.Abs((long)playerY - monsterY); // 세로 칸 차이
            return horizontal <= 1 && vertical <= 1; // 대각선 포함 한 칸 판정
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
