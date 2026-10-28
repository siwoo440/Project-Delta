using System; // 잘못된 호출 예외
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class GameFlowState // 진행 상태 관리
    { // 클래스 시작
        private GamePhase previousPhase; // 전환 전 상태
        private GamePhase destinationPhase; // 전환 목적 상태
        public GamePhase Phase // 현재 상태
        { // 속성 시작
            get; // 상태 읽기
            private set; // 내부 상태 변경
        } = GamePhase.Initializing; // 초기 준비 상태
        public bool TryOpenInventory() // 탐험 중 인벤토리 진입
        { // 진입 시작
            if (Phase != GamePhase.Exploration) // 탐험 상태 검사
            { // 진입 거절 조건 시작
                return false; // 탐험 외 인벤토리 차단
            } // 진입 거절 조건 끝
            Phase = GamePhase.Inventory; // 인벤토리 화면 상태 적용
            return true; // 인벤토리 진입 허용
        } // 진입 끝
        public bool TryCloseInventory() // 인벤토리에서 탐험 복귀
        { // 복귀 시작
            if (Phase != GamePhase.Inventory) // 인벤토리 상태 검사
            { // 복귀 거절 조건 시작
                return false; // 잘못된 닫기 요청 차단
            } // 복귀 거절 조건 끝
            Phase = GamePhase.Exploration; // 기존 탐험 상태 복구
            return true; // 인벤토리 닫기 허용
        } // 복귀 끝
        public bool TryStartCombat() // 탐험에서 시험 전투 진입
        { // 진입 시작
            if (Phase != GamePhase.Exploration) // 탐험 상태 검사
            { // 진입 거절 조건 시작
                return false; // 탐험 외 조우 차단
            } // 진입 거절 조건 끝
            Phase = GamePhase.Combat; // 시험 전투 상태 적용
            return true; // 전투 진입 허용
        } // 진입 끝
        public bool TryEndCombat() // 시험 전투에서 탐험 복귀
        { // 복귀 시작
            if (Phase != GamePhase.Combat) // 전투 상태 검사
            { // 복귀 거절 조건 시작
                return false; // 전투 외 종료 차단
            } // 복귀 거절 조건 끝
            Phase = GamePhase.Exploration; // 기존 탐험 상태 복구
            return true; // 전투 종료 허용
        } // 복귀 끝
        public bool TryBeginTransition(GamePhase destination) // 전환 요청 검사
        { // 메서드 시작
            var canOpenTitle = destination == GamePhase.Title && Phase != GamePhase.Loading; // 시작과 복귀 가능 여부
            var canExplore = destination == GamePhase.Exploration && Phase == GamePhase.Title; // 새 게임 가능 여부
            if (!canOpenTitle && !canExplore) // 허용되지 않은 전환 검사
            { // 거절 조건 시작
                return false; // 중복 또는 잘못된 요청 거절
            } // 거절 조건 끝
            previousPhase = Phase; // 실패 복구 상태 보관
            destinationPhase = destination; // 전환 목적 보관
            Phase = GamePhase.Loading; // 전환 중 상태 적용
            return true; // 단일 전환 승인
        } // 메서드 끝
        public void CompleteTransition() // 화면 전환 완료
        { // 메서드 시작
            if (Phase != GamePhase.Loading) // 전환 시작 여부 검사
            { // 오류 조건 시작
                throw new InvalidOperationException("No scene transition is active."); // 잘못된 완료 호출 알림
            } // 오류 조건 끝
            Phase = destinationPhase; // 도착 상태 적용
        } // 메서드 끝
        public void CancelTransition() // 화면 전환 취소
        { // 메서드 시작
            if (Phase == GamePhase.Loading) // 복구 가능한 상태 검사
            { // 복구 조건 시작
                Phase = previousPhase; // 이전 화면 상태 복구
            } // 복구 조건 끝
        } // 메서드 끝
    } // 클래스 끝
} // 이름 공간 끝
