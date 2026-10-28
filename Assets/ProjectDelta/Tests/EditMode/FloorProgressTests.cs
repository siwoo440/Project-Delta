using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 층 진행 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class FloorProgressTests // 층 전환 상태 검증
    { // 클래스 시작
        [Test] // 정상 전환과 중복 요청 검증
        public void Transition_AdvancesOnce_AndRejectsDuplicates() // 한 번의 전환만 반영
        { // 검증 시작
            var progress = new FloorProgress(); // 첫 층의 진행 상태
            Assert.That(progress.Floor, Is.EqualTo(1)); // 최초 층 확인
            Assert.That(progress.TryBegin(), Is.True); // 첫 전환 허용
            Assert.That(progress.TryBegin(), Is.False); // 중복 전환 거절
            Assert.That(progress.Floor, Is.EqualTo(1)); // 완료 전 층 유지
            Assert.That(progress.Complete(true), Is.True); // 성공 전환 확정
            Assert.That(progress.Floor, Is.EqualTo(2)); // 다음 층 확인
            Assert.That(progress.Complete(true), Is.False); // 중복 완료 거절
        } // 검증 끝
        [Test] // 실패와 재시도 검증
        public void FailedTransition_PreservesFloor_AndAllowsRetry() // 실패 후 기존 층 유지
        { // 검증 시작
            var progress = new FloorProgress(); // 첫 층의 진행 상태
            Assert.That(progress.TryBegin(), Is.True); // 첫 전환 허용
            Assert.That(progress.Complete(false), Is.False); // 실패 전환 거절
            Assert.That(progress.Floor, Is.EqualTo(1)); // 기존 층 유지
            Assert.That(progress.IsTransitioning, Is.False); // 전환 잠금 해제
            Assert.That(progress.TryBegin(), Is.True); // 새 전환 허용
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
