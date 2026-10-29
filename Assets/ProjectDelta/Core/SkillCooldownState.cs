namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class SkillCooldownState // 기술별 재사용 대기와 행동 상태
    { // 클래스 시작
        private bool blockedThisTurn; // 현재 자기 차례의 대기 여부
        private bool usedThisTurn; // 현재 자기 차례의 사용 여부
        public int RemainingBlockedTurns // 현재 차례 이후 남은 대기 차례
        { // 속성 시작
            get; // 남은 대기 차례 조회
            private set; // 내부 대기 차례 변경
        } // 속성 끝
        public bool IsBlockedThisTurn => blockedThisTurn; // 현재 차례의 대기 상태
        public void BeginTurn() // 자신의 새 행동 차례 시작
        { // 차례 시작
            usedThisTurn = false; // 새 차례의 사용 상태 초기화
            blockedThisTurn = RemainingBlockedTurns > 0; // 이번 차례의 기술 대기 확정
            if (blockedThisTurn) // 대기 중인 자기 차례 검사
            { // 대기 감소 시작
                RemainingBlockedTurns--; // 이번 차례를 제외한 남은 대기 감소
            } // 대기 감소 끝
        } // 차례 끝
        public bool TryUse(ref int mana, int manaCost, int cooldownTurns) // 공유 마나로 기술 사용 시도
        { // 사용 시도 시작
            if (mana < 0 || manaCost < 0 || cooldownTurns < 0 || mana < manaCost || blockedThisTurn || usedThisTurn) // 수치와 현재 기술 사용 가능 여부 검사
            { // 사용 거절 시작
                return false; // 실패 시 마나와 기술 상태 보존
            } // 사용 거절 끝
            mana -= manaCost; // 유효한 기술의 마나 비용 차감
            RemainingBlockedTurns = cooldownTurns; // 다음 자기 차례부터 대기 적용
            usedThisTurn = true; // 동일 기술의 같은 차례 중복 차단
            return true; // 기술 사용 성공
        } // 사용 시도 끝
    } // 클래스 끝
} // 이름 공간 끝
