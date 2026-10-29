using NUnit.Framework; // 기술 대기와 자원 검증 도구
using ProjectDelta.Core; // 독립 기술 사용 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class SkillCooldownStateTests // 기술별 대기와 공유 마나 검증
    { // 클래스 시작
        [Test] // 자기 차례 기준 대기 검증
        public void Cooldown_BlocksNextTurnsThenReleases() // 두 차례 대기 후 해제 확인
        { // 검증 시작
            var skill = new SkillCooldownState(); // 대기 없는 기술 상태
            var mana = 30; // 공유 마나 수치
            Assert.That(skill.TryUse(ref mana, 10, 2), Is.True); // 기술 사용 성공
            Assert.That(mana, Is.EqualTo(20)); // 사용한 마나 차감 확인
            skill.BeginTurn(); // 사용 후 첫 번째 자기 차례
            Assert.That(skill.TryUse(ref mana, 10, 2), Is.False); // 첫 번째 차례 사용 차단
            Assert.That(mana, Is.EqualTo(20)); // 대기 중 마나 보존
            skill.BeginTurn(); // 사용 후 두 번째 자기 차례
            Assert.That(skill.TryUse(ref mana, 10, 2), Is.False); // 두 번째 차례 사용 차단
            Assert.That(mana, Is.EqualTo(20)); // 반복 거절의 마나 보존
            skill.BeginTurn(); // 대기 이후 세 번째 자기 차례
            Assert.That(skill.TryUse(ref mana, 10, 2), Is.True); // 대기 해제 후 사용 확인
            Assert.That(mana, Is.EqualTo(10)); // 정상 재사용 비용 확인
        } // 검증 끝
        [Test] // 한 차례 대기 경계 검증
        public void OneTurnCooldown_BlocksNextTurnAndReleasesFollowingTurn() // 다음 한 번의 행동 차례 차단 확인
        { // 검증 시작
            var skill = new SkillCooldownState(); // 대기 없는 기술 상태
            var mana = 10; // 현재 공유 마나
            Assert.That(skill.TryUse(ref mana, 5, 1), Is.True); // 대기 한 차례 기술 사용
            skill.BeginTurn(); // 사용 후 다음 자기 차례
            Assert.That(skill.IsBlockedThisTurn, Is.True); // 이번 차례 대기 유지
            Assert.That(skill.TryUse(ref mana, 5, 1), Is.False); // 대기 차례 사용 거절
            skill.BeginTurn(); // 대기 이후 자기 차례
            Assert.That(skill.IsBlockedThisTurn, Is.False); // 다음 차례 대기 해제
            Assert.That(skill.TryUse(ref mana, 5, 1), Is.True); // 해제된 기술 재사용
        } // 검증 끝
        [Test] // 마나 부족과 잘못된 비용 검증
        public void RejectedUse_DoesNotConsumeManaOrStartCooldown() // 실패 시 상태 보존 확인
        { // 검증 시작
            var skill = new SkillCooldownState(); // 대기 없는 기술
            var mana = 3; // 현재 공유 마나
            Assert.That(skill.TryUse(ref mana, 4, 2), Is.False); // 마나 부족 거절
            Assert.That(skill.TryUse(ref mana, -1, 2), Is.False); // 음수 비용 거절
            Assert.That(skill.TryUse(ref mana, 1, -1), Is.False); // 음수 대기 거절
            Assert.That(mana, Is.EqualTo(3)); // 실패 후 마나 보존
            Assert.That(skill.RemainingBlockedTurns, Is.EqualTo(0)); // 실패 후 대기 미시작
            Assert.That(skill.TryUse(ref mana, 3, 0), Is.True); // 정확한 마나 사용 허용
            Assert.That(mana, Is.EqualTo(0)); // 남은 마나 차감 확인
        } // 검증 끝
        [Test] // 무료 기술과 동일 차례 중복 사용 검증
        public void ZeroCostAndCooldown_AllowNextTurnButNotSameTurn() // 무료 기술의 차례별 사용 확인
        { // 검증 시작
            var skill = new SkillCooldownState(); // 대기 없는 기술
            var mana = 0; // 마나가 없는 상태
            Assert.That(skill.TryUse(ref mana, 0, 0), Is.True); // 무료 기술 사용 허용
            Assert.That(skill.TryUse(ref mana, 0, 0), Is.False); // 같은 차례 중복 사용 차단
            skill.BeginTurn(); // 다음 자기 행동 차례
            Assert.That(skill.TryUse(ref mana, 0, 0), Is.True); // 다음 차례 사용 허용
            Assert.That(mana, Is.EqualTo(0)); // 무료 기술 마나 보존
        } // 검증 끝
        [Test] // 정수 경계와 독립 기술 검증
        public void ExtremeValues_StayInRangeAndShareMana() // 큰 정수와 기술별 대기 확인
        { // 검증 시작
            var first = new SkillCooldownState(); // 첫 번째 기술 상태
            var second = new SkillCooldownState(); // 독립된 두 번째 기술 상태
            var mana = int.MaxValue; // 최대 정수 마나
            Assert.That(first.TryUse(ref mana, int.MaxValue, int.MaxValue), Is.True); // 최대 비용과 대기 사용
            Assert.That(mana, Is.EqualTo(0)); // 최대 비용 차감 확인
            first.BeginTurn(); // 첫 번째 대기 차례
            Assert.That(first.RemainingBlockedTurns, Is.EqualTo(int.MaxValue - 1)); // 대기 감소 정수 범위 확인
            Assert.That(first.TryUse(ref mana, 0, 0), Is.False); // 대기 중 무료 사용 차단
            Assert.That(second.TryUse(ref mana, 0, 0), Is.True); // 다른 기술의 독립 대기 확인
            mana = -1; // 잘못된 공유 마나
            second.BeginTurn(); // 두 번째 기술의 새 차례
            Assert.That(second.TryUse(ref mana, 0, 0), Is.False); // 음수 마나 거절
            Assert.That(mana, Is.EqualTo(-1)); // 잘못된 입력의 원본 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
