namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class EventCheck // 일반 이벤트의 두 주사위 판정
    { // 클래스 시작
        public static bool TryResolve(int stat, int modifier, int difficulty, int playerRoll, int eventRoll, out bool success) // 이벤트 판정과 주사위 범위 검사
        { // 판정 시작
            success = false; // 기본 실패 상태
            if (playerRoll < 1 || playerRoll > 20 || eventRoll < 1 || eventRoll > 20) // 두 주사위의 1~20 범위 검사
            { // 잘못된 주사위 조건 시작
                return false; // 유효하지 않은 판정 거절
            } // 잘못된 주사위 조건 끝
            success = (long)stat + modifier + playerRoll >= (long)difficulty + eventRoll; // 동점을 포함한 기존 성공 판정
            return true; // 유효한 이벤트 판정 완료
        } // 판정 끝
        public static int SuccessCount(int stat, int modifier, int difficulty) // 가능한 400개 주사위 중 성공 조합
        { // 계산 시작
            var successes = 0; // 성공한 주사위 조합 수
            for (var playerRoll = 1; playerRoll <= 20; playerRoll++) // 플레이어 주사위 순회
            { // 플레이어 주사위 시작
                for (var eventRoll = 1; eventRoll <= 20; eventRoll++) // 이벤트 주사위 순회
                { // 이벤트 주사위 시작
                    if ((long)stat + modifier + playerRoll >= (long)difficulty + eventRoll) // 동점 포함 성공 검사
                    { // 성공 조건 시작
                        successes++; // 성공 조합 수 증가
                    } // 성공 조건 끝
                } // 이벤트 주사위 끝
            } // 플레이어 주사위 끝
            return successes; // 400개 조합의 성공 수 반환
        } // 계산 끝
    } // 클래스 끝
} // 이름 공간 끝
