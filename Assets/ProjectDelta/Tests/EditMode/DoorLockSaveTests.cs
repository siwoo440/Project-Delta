using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 문 잠금 상태
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DoorLockSaveTests // 잠긴 문 복원 검증
    { // 클래스 시작
        [Test] // 실패한 강제 개방 기록 보존
        public void Restore_PreservesFailedForceAttempt() // 강제 개방 중복 방지
        { // 검증 시작
            var layout = DungeonGenerator.Generate(123); // 시험 던전
            var locks = new DoorLockState(layout); // 최초 문 잠금
            var door = locks.TrialDoor; // 시험 잠긴 문
            Assert.That(door, Is.Not.Null); // 시험 문 존재 확인
            Assert.That(locks.TryForce(door, 1, 0, 100, 1, 20, out var success), Is.True); // 강제 개방 판정 실행
            Assert.That(success, Is.False); // 실패 판정 확인
            var saved = locks.Save(); // 문 잠금 저장본
            var restored = DoorLockState.FromSaved(layout, saved); // 문 상태 복원
            Assert.That(restored.IsLocked(restored.TrialDoor), Is.True); // 잠금 상태 보존
            Assert.That(restored.CanForce(restored.TrialDoor), Is.False); // 재시도 차단 보존
        } // 검증 끝
        [Test] // 잘못된 문 저장 상태 거절
        public void Restore_RejectsMissingState() // 누락 데이터 거절
        { // 검증 시작
            Assert.That(DoorLockState.FromSaved(DungeonGenerator.Generate(123), null), Is.Null); // 누락된 문 상태 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
