using System; // 예외 형식
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 게임 상태 형식
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class GameFlowStateTests // 진행 상태 검증
    { // 클래스 시작
        [Test] // 개별 검증 표시
        public void Startup_ReachesTitleOnlyAfterLoadCompletes() // 초기 화면 전환 검증
        { // 검증 시작
            var flow = new GameFlowState(); // 초기 상태 준비
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Initializing)); // 초기 준비 상태 확인
            Assert.That(flow.TryBeginTransition(GamePhase.Title), Is.True); // 타이틀 요청 승인 확인
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Loading)); // 입력 잠금 상태 확인
            flow.CompleteTransition(); // 전환 완료 처리
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Title)); // 타이틀 도착 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Loading_RejectsRepeatedNewGameRequests() // 새 게임 연타 검증
        { // 검증 시작
            var flow = CreateTitleState(); // 타이틀 상태 준비
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 첫 요청 승인 확인
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.False); // 중복 요청 거절 확인
            Assert.That(flow.TryBeginTransition(GamePhase.Title), Is.False); // 반대 요청 거절 확인
            flow.CompleteTransition(); // 단일 전환 완료
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 탐험 도착 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void InvalidDestination_DoesNotChangeCurrentScreen() // 잘못된 전환 검증
        { // 검증 시작
            var flow = new GameFlowState(); // 초기 상태 준비
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.False); // 타이틀 생략 방지
            Assert.That(flow.TryBeginTransition((GamePhase)99), Is.False); // 알 수 없는 상태 방지
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Initializing)); // 기존 상태 보존 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void FailedLoad_RestoresPreviousScreenAndAllowsRetry() // 실패 복구 검증
        { // 검증 시작
            var flow = CreateTitleState(); // 타이틀 상태 준비
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 탐험 요청 승인 확인
            flow.CancelTransition(); // 실패 상황 복구
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Title)); // 타이틀 복구 확인
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 재시도 가능 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Exploration_CanReturnToTitleAndStartAgain() // 왕복 실행 검증
        { // 검증 시작
            var flow = CreateTitleState(); // 타이틀 상태 준비
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 탐험 시작 확인
            flow.CompleteTransition(); // 탐험 전환 완료
            Assert.That(flow.TryBeginTransition(GamePhase.Title), Is.True); // 복귀 요청 확인
            flow.CompleteTransition(); // 복귀 완료 처리
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 다시 시작 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void CompletionWithoutLoading_RejectsInvalidLifecycle() // 잘못된 완료 호출 검증
        { // 검증 시작
            var flow = new GameFlowState(); // 초기 상태 준비
            Assert.Throws<InvalidOperationException>(() => flow.CompleteTransition()); // 누락된 시작 단계 확인
        } // 검증 끝
        [Test] // 전투 진입과 중복 요청 검증
        public void Combat_RequiresExploration_AndReturnsToExploration() // 탐험과 전투 왕복 확인
        { // 검증 시작
            var flow = CreateTitleState(); // 타이틀 상태 준비
            Assert.That(flow.TryStartCombat(), Is.False); // 타이틀 조우 차단
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 탐험 전환 요청
            flow.CompleteTransition(); // 탐험 전환 완료
            Assert.That(flow.TryEndCombat(), Is.False); // 전투 전 종료 차단
            Assert.That(flow.TryStartCombat(), Is.True); // 탐험에서 조우 허용
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Combat)); // 전투 상태 확인
            Assert.That(flow.TryStartCombat(), Is.False); // 중복 조우 차단
            Assert.That(flow.TryEndCombat(), Is.True); // 전투 종료 허용
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 기존 탐험 복귀
            Assert.That(flow.TryEndCombat(), Is.False); // 중복 종료 차단
        } // 검증 끝
        [Test] // 인벤토리 전환과 중복 요청 검증
        public void Inventory_RequiresExploration_AndRestoresExploration() // 탐험과 인벤토리 왕복 확인
        { // 검증 시작
            var flow = CreateTitleState(); // 타이틀 상태 준비
            Assert.That(flow.TryOpenInventory(), Is.False); // 타이틀에서 열기 차단
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 탐험 전환 요청
            flow.CompleteTransition(); // 탐험 전환 완료
            Assert.That(flow.TryOpenInventory(), Is.True); // 탐험에서 열기 허용
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 인벤토리 상태 확인
            Assert.That(flow.TryOpenInventory(), Is.False); // 중복 열기 차단
            Assert.That(flow.TryStartCombat(), Is.False); // 열린 인벤토리의 조우 차단
            Assert.That(flow.TryCloseInventory(), Is.True); // 인벤토리 닫기 허용
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 기존 탐험 복귀
            Assert.That(flow.TryCloseInventory(), Is.False); // 중복 닫기 차단
        } // 검증 끝
        private static GameFlowState CreateTitleState() // 타이틀 검증 준비
        { // 준비 시작
            var flow = new GameFlowState(); // 초기 상태 생성
            Assert.That(flow.TryBeginTransition(GamePhase.Title), Is.True); // 초기 요청 확인
            flow.CompleteTransition(); // 타이틀 전환 완료
            return flow; // 준비된 상태 전달
        } // 준비 끝
    } // 클래스 끝
} // 이름 공간 끝
