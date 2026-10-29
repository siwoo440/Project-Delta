using System; // 고유 거래 식별자
using NUnit.Framework; // 순수 규칙 검증
using ProjectDelta.Core; // 시험 이벤트 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventRulesTests // 시험 이벤트 저장과 판정 검증
    { // 클래스 시작
        [Test] // 선택 순서와 복사 검증
        public void Choices_KeepOrderAndRejectTampering() // 지정 순서의 독립 저장
        { // 검증 시작
            Assert.That(TrialEventState.TryCreate("event-1", 1, out var initial), Is.True); // 떠나기 우선 생성
            var data = initial.Snapshot(); // 독립 저장본
            Assert.That(data.choices[0], Is.EqualTo(1)); // 첫 선택 순서 확인
            Assert.That(data.choices[1], Is.EqualTo(0)); // 둘째 선택 순서 확인
            Assert.That(TrialEventState.TryRestore(data, out var restored), Is.True); // 지정 순서 복원
            data.choices[0] = 0; // 외부 저장본 변경
            Assert.That(restored.Snapshot().choices[0], Is.EqualTo(1)); // 내부 선택 순서 보존
            Assert.That(TrialEventState.TryRestore(data, out _), Is.False); // 중복 선택 순서 차단
        } // 검증 끝
        [Test] // 조건 실패와 기존 성공률 검증
        public void Conditions_ShowReasonAndUseExistingFourHundredRolls() // 비용 부족과 성공 조합
        { // 검증 시작
            Assert.That(TrialEventRules.CanChoose(0, 9, out var reason), Is.False); // 비용 부족 행동 차단
            Assert.That(string.IsNullOrEmpty(reason), Is.False); // 비활성 이유 표시
            Assert.That(TrialEventRules.CanChoose(1, 0, out _), Is.True); // 비용 없는 떠나기
            Assert.That(TrialEventRules.SuccessCount(50), Is.EqualTo(EventCheck.SuccessCount(50, 0, 50))); // 기존 사백 조합 사용
            Assert.That(TrialEventRules.SuccessCount(50), Is.EqualTo(210)); // 동점 포함 성공 조합
        } // 검증 끝
        [Test] // 행동 결과 저장과 재실행 차단 검증
        public void Action_SavesTieSuccessAndCompletesOnlyOnce() // 비용과 주사위의 확정 기록
        { // 검증 시작
            Assert.That(TrialEventState.TryCreate("event-2", 1, out var initial), Is.True); // 떠나기가 첫 번째인 이벤트
            Assert.That(initial.TryChoose(1, 9, 50, 10, 10, Guid.NewGuid().ToString("N"), out _, out _), Is.False); // 골드 부족 차단
            Assert.That(initial.TryChoose(1, 10, 50, 10, 10, Guid.NewGuid().ToString("N"), out var decided, out _), Is.True); // 동점 행동 선택
            var saved = decided.Snapshot(); // 확정된 저장본
            Assert.That(saved.phase, Is.EqualTo(1)); // 결과 표시 단계
            Assert.That(saved.result, Is.EqualTo(TrialEventRules.Success)); // 동점 성공 확인
            Assert.That(saved.cost, Is.EqualTo(10)); // 시험 비용 확인
            Assert.That(saved.rewardGold, Is.EqualTo(20)); // 시험 보상 확인
            Assert.That(saved.playerRoll, Is.EqualTo(10)); // 고정 플레이어 주사위
            Assert.That(saved.eventRoll, Is.EqualTo(10)); // 고정 이벤트 주사위
            Assert.That(TrialEventState.TryRestore(saved, out var restored), Is.True); // 결과 복원
            Assert.That(restored.TryChoose(1, 10, 50, 20, 1, Guid.NewGuid().ToString("N"), out _, out _), Is.False); // 결정 후 재추첨 차단
            Assert.That(restored.TryComplete(out var complete), Is.True); // 비용과 보상 지급 준비
            Assert.That(complete.Snapshot().phase, Is.EqualTo(2)); // 완료 단계 확인
            Assert.That(complete.TryComplete(out _), Is.False); // 중복 지급 차단
            Assert.That(TrialEventState.TryRestore(complete.Snapshot(), out _), Is.True); // 완료 기록 복원
        } // 검증 끝
        [Test] // 실패와 떠나기 결과 검증
        public void FailureAndLeave_AreFixedWithoutInventedRolls() // 행동 실패와 비용 없는 떠나기
        { // 검증 시작
            Assert.That(TrialEventState.TryCreate("event-3", 0, out var action), Is.True); // 행동 우선 이벤트
            Assert.That(action.TryChoose(0, 10, 50, 1, 20, Guid.NewGuid().ToString("N"), out var failed, out _), Is.True); // 실제 실패 판정
            Assert.That(failed.Snapshot().result, Is.EqualTo(TrialEventRules.Failure)); // 실패 결과 확인
            Assert.That(failed.Snapshot().rewardGold, Is.Zero); // 실패 보상 없음
            Assert.That(action.TryChoose(1, 0, 0, 0, 0, Guid.NewGuid().ToString("N"), out var left, out _), Is.True); // 주사위 없는 떠나기
            Assert.That(left.Snapshot().result, Is.EqualTo(TrialEventRules.Left)); // 떠나기 결과 확인
            Assert.That(left.Snapshot().cost, Is.Zero); // 떠나기 비용 없음
            Assert.That(TrialEventState.TryRestore(left.Snapshot(), out _), Is.True); // 떠나기 저장 복원
        } // 검증 끝
        [Test] // 손상된 이벤트 기록 검증
        public void Restore_RejectsInvalidOutcomeAndPhase() // 위조 결과와 단계 차단
        { // 검증 시작
            Assert.That(TrialEventState.TryCreate("event-4", out var initial), Is.True); // 기본 이벤트 생성
            Assert.That(initial.TryChoose(0, 10, 50, 10, 10, Guid.NewGuid().ToString("N"), out var decided, out _), Is.True); // 정상 결과 생성
            var bad = decided.Snapshot(); // 독립 결과 저장본
            bad.playerRoll = 0; // 잘못된 주사위 변경
            Assert.That(TrialEventState.TryRestore(bad, out _), Is.False); // 손상된 주사위 차단
            bad = decided.Snapshot(); // 독립 결과 재조회
            bad.rewardGold++; // 위조된 보상
            Assert.That(TrialEventState.TryRestore(bad, out _), Is.False); // 잘못된 보상 차단
            bad = decided.Snapshot(); // 독립 결과 재조회
            bad.phase = 2; // 완료되지 않은 종료 단계
            Assert.That(TrialEventState.TryRestore(bad, out _), Is.False); // 단계 불일치 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
