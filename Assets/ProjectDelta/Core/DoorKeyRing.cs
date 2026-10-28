namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class DoorKeyRing // 현재 게임의 문 열쇠 수
    { // 클래스 시작
        private int reserved; // 이동 성공 전 예약된 열쇠 수
        public int Count // 보유 열쇠 수
        { // 속성 시작
            get; // 보유 수 조회
            private set; // 내부 수량 변경
        } // 속성 끝
        public bool Add(int amount) // 열쇠 수 증가
        { // 증가 시작
            if (amount <= 0 || amount > int.MaxValue - Count - reserved) // 예약 수량을 포함한 정수 범위 검사
            { // 증가 거절 조건 시작
                return false; // 잘못된 열쇠 증가 차단
            } // 증가 거절 조건 끝
            Count += amount; // 보유 열쇠 증가
            return true; // 열쇠 증가 성공
        } // 증가 끝
        internal bool TryReserve() // 문 이동 중 열쇠 한 개 예약
        { // 예약 시작
            if (reserved != 0 || Count <= 0) // 중복 예약과 보유 열쇠 검사
            { // 예약 거절 조건 시작
                return false; // 잘못된 열쇠 예약 차단
            } // 예약 거절 조건 끝
            Count--; // 사용 가능한 열쇠 한 개 예약
            reserved = 1; // 복구 가능한 예약 수량 보관
            return true; // 열쇠 예약 성공
        } // 예약 끝
        internal bool CommitReservation() // 성공한 문 이동의 예약 확정
        { // 확정 시작
            if (reserved != 1) // 예약된 열쇠 검사
            { // 예약 없음 조건 시작
                return false; // 중복 확정 차단
            } // 예약 없음 조건 끝
            reserved = 0; // 사용한 열쇠 예약 해제
            return true; // 열쇠 소모 확정
        } // 확정 끝
        internal bool RefundReservation() // 실패한 문 이동의 예약 복구
        { // 복구 시작
            if (reserved != 1) // 예약된 열쇠 검사
            { // 예약 없음 조건 시작
                return false; // 중복 복구 차단
            } // 예약 없음 조건 끝
            reserved = 0; // 예약 수량 해제
            Count++; // 예약한 열쇠 한 개 복구
            return true; // 열쇠 복구 성공
        } // 복구 끝
        public bool TryConsume() // 문 열쇠 한 개 소모
        { // 소모 시작
            if (Count <= 0) // 보유 열쇠 검사
            { // 열쇠 없음 조건 시작
                return false; // 없는 열쇠 소모 차단
            } // 열쇠 없음 조건 끝
            Count--; // 보유 열쇠 한 개 소모
            return true; // 열쇠 소모 성공
        } // 소모 끝
    } // 클래스 끝
} // 이름 공간 끝
