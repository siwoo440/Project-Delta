namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class ObservationRules // 조우 단계의 관찰 판정
    { // 클래스 시작
        public static bool TryResolve(int speed, int bonus, int resistance, int concealment, int playerRoll, int enemyRoll, out bool success) // 속도와 저항의 두 주사위 비교
        { // 판정 시작
            success = false; // 기본 실패 상태
            if (playerRoll < 1 || playerRoll > 20 || enemyRoll < 1 || enemyRoll > 20) // 두 주사위의 1~20 범위 검사
            { // 잘못된 주사위 조건 시작
                return false; // 유효하지 않은 관찰 차단
            } // 잘못된 주사위 조건 끝
            success = (long)speed + bonus + playerRoll >= (long)resistance + concealment + enemyRoll; // 동점을 포함한 관찰 성공 판정
            return true; // 유효한 관찰 판정 완료
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
