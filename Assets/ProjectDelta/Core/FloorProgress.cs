namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class FloorProgress // 현재 층과 전환 상태
    { // 클래스 시작
        public int Floor // 현재 층 번호
        { // 속성 시작
            get; // 현재 층 조회
            private set; // 내부 층 변경
        } = 1; // 첫 번째 층 초기화
        public bool TryRestore(int floor) // 검증된 현재 층 복원
        { // 복원 시작
            if (IsTransitioning || floor < 1 || floor > 2) // 전환 중과 구현된 층 범위 검사
            { // 복원 거절 시작
                return false; // 잘못된 층 거절
            } // 복원 거절 끝
            Floor = floor; // 검증된 층 번호 복원
            return true; // 층 번호 복원 성공
        } // 복원 끝
        public bool IsTransitioning // 전환 진행 여부
        { // 속성 시작
            get; // 전환 상태 조회
            private set; // 내부 상태 변경
        } // 속성 끝
        public bool TryBegin() // 다음 층 전환 요청
        { // 요청 시작
            if (IsTransitioning || Floor == int.MaxValue) // 중복 요청과 층 범위 검사
            { // 거절 조건 시작
                return false; // 전환 요청 거절
            } // 거절 조건 끝
            IsTransitioning = true; // 전환 중 잠금
            return true; // 새 전환 허용
        } // 요청 끝
        public bool Complete(bool succeeded) // 전환 성공 또는 실패 확정
        { // 확정 시작
            if (!IsTransitioning) // 진행 중인 전환 검사
            { // 전환 없음 조건 시작
                return false; // 중복 완료 거절
            } // 전환 없음 조건 끝
            IsTransitioning = false; // 전환 잠금 해제
            if (!succeeded) // 전환 실패 검사
            { // 실패 조건 시작
                return false; // 기존 층 보존
            } // 실패 조건 끝
            Floor++; // 다음 층 확정
            return true; // 성공 처리
        } // 확정 끝
    } // 클래스 끝
} // 이름 공간 끝
