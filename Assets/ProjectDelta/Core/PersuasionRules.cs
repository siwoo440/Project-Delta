namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class PersuasionRules // 조우 단계의 회유 판정
    { // 클래스 시작
        public static bool TryResolve(int charm, int choice, int items, int resistance, int hostility, int state, int playerRoll, int enemyRoll, out bool success) // 양쪽 능력치와 주사위 비교
        { // 판정 시작
            success = false; // 기본 실패 상태
            if (playerRoll < 1 || playerRoll > 20 || enemyRoll < 1 || enemyRoll > 20) // 두 주사위 범위 검사
            { // 잘못된 주사위 조건 시작
                return false; // 유효하지 않은 회유 차단
            } // 잘못된 주사위 조건 끝
            success = (long)charm + choice + items + playerRoll >= (long)resistance + hostility + state + enemyRoll; // 동점을 포함한 회유 성공 판정
            return true; // 유효한 회유 판정 완료
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
