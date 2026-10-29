using NUnit.Framework; // 경험치와 레벨 검증 도구
using ProjectDelta.Core; // 순수 경험치 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ExperienceRulesTests // 일반 전투 경험치와 레벨 검증
    { // 클래스 시작
        [TestCase(1, 1, 0, 60)] // 1층 일반 적 경험치
        [TestCase(2, 1, 0, 80)] // 2층 일반 적 경험치
        [TestCase(1, 0, 1, 180)] // 1층 상위 개체 경험치
        [TestCase(2, 0, 1, 240)] // 2층 상위 개체 경험치
        [TestCase(1, 2, 1, 300)] // 일반 적과 상위 개체 합산
        [TestCase(2, 3, 0, 240)] // 세 일반 적의 경험치 합산
        public void VictoryExperience_UsesFloorAndEliteMultiplier(int floor, int normal, int elite, int expected) // 현재 시험 층의 다수 적 경험치 확인
        { // 검증 시작
            Assert.That(ExperienceRules.TryCalculateVictory(floor, normal, elite, out var experience), Is.True); // 유효한 시험 전투 검사
            Assert.That(experience, Is.EqualTo(expected)); // 기획된 개별 경험치 합산 확인
        } // 검증 끝
        [Test] // 미지원 층과 잘못된 적 수 검증
        public void VictoryExperience_RejectsInvalidFloorOrEnemyCount() // 시험 범위 외 전투 거절
        { // 검증 시작
            Assert.That(ExperienceRules.TryCalculateVictory(3, 1, 0, out _), Is.False); // 아직 미지원인 층 거절
            Assert.That(ExperienceRules.TryCalculateVictory(1, -1, 0, out _), Is.False); // 음수 적 수 거절
            Assert.That(ExperienceRules.TryCalculateVictory(1, 0, 0, out _), Is.False); // 제거한 적 없는 전투 거절
            Assert.That(ExperienceRules.TryCalculateVictory(1, 3, 1, out _), Is.False); // 네 적의 전투 거절
            Assert.That(ExperienceRules.TryCalculateVictory(1, int.MaxValue, int.MaxValue, out _), Is.False); // 최대 정수의 수량 오류 방지
        } // 검증 끝
        [Test] // 요구 경험치와 초과분 이월 검증
        public void AdvanceExperience_CarriesExcessAndAwardsLevelPoints() // 다중 레벨과 포인트 확인
        { // 검증 시작
            Assert.That(ExperienceRules.TryAdvance(1, 0, 460, out var result), Is.True); // 큰 경험치 획득
            Assert.That(result.Level, Is.EqualTo(4)); // 세 번의 레벨 상승
            Assert.That(result.CurrentExperience, Is.EqualTo(10)); // 초과 경험치 이월
            Assert.That(result.GainedLevels, Is.EqualTo(3)); // 상승한 레벨 수
            Assert.That(result.GainedStatPoints, Is.EqualTo(3)); // 레벨별 스탯 포인트 합산
            Assert.That(result.AppliedExperience, Is.EqualTo(460)); // 실제 반영한 경험치
        } // 검증 끝
        [TestCase(1, 100, 1)] // 2레벨의 포인트
        [TestCase(2, 150, 1)] // 3레벨의 포인트
        [TestCase(3, 200, 1)] // 4레벨의 포인트
        [TestCase(4, 250, 2)] // 5레벨의 포인트
        [TestCase(5, 300, 1)] // 6레벨의 포인트
        [TestCase(6, 350, 1)] // 7레벨의 포인트
        [TestCase(7, 400, 1)] // 8레벨의 포인트
        [TestCase(8, 450, 2)] // 9레벨의 포인트
        [TestCase(9, 500, 3)] // 10레벨의 포인트
        public void AdvanceExperience_UsesEachLevelThresholdAndPoint(int level, int required, int points) // 레벨별 요구량과 지급 포인트 확인
        { // 검증 시작
            Assert.That(ExperienceRules.TryAdvance(level, 0, required - 1, out var before), Is.True); // 요구량 직전 경험치 반영
            Assert.That(before.Level, Is.EqualTo(level)); // 요구량 직전 레벨 유지
            Assert.That(ExperienceRules.TryAdvance(level, 0, required, out var reached), Is.True); // 요구량에 도달한 경험치 반영
            Assert.That(reached.Level, Is.EqualTo(level + 1)); // 정확한 요구량의 레벨 상승
            Assert.That(reached.GainedStatPoints, Is.EqualTo(points)); // 도달 레벨별 포인트 확인
        } // 검증 끝
        [Test] // 최대 레벨과 정수 경계 검증
        public void AdvanceExperience_StopsAtLevelTenAndDiscardsExcess() // 레벨 상한과 잉여 경험치 확인
        { // 검증 시작
            Assert.That(ExperienceRules.TryAdvance(1, 0, int.MaxValue, out var result), Is.True); // 최대 정수 경험치 입력
            Assert.That(result.Level, Is.EqualTo(10)); // 최대 레벨 제한 확인
            Assert.That(result.CurrentExperience, Is.EqualTo(0)); // 최대 레벨 이후 경험치 저장 방지
            Assert.That(result.AppliedExperience, Is.EqualTo(2700)); // 레벨 상한까지 실제 반영 경험치
            Assert.That(result.GainedLevels, Is.EqualTo(9)); // 시작 레벨부터 전체 상승 수
            Assert.That(result.GainedStatPoints, Is.EqualTo(13)); // 레벨별 추가 포인트 합산
            Assert.That(ExperienceRules.TryAdvance(10, 0, 60, out var capped), Is.True); // 최대 레벨의 추가 획득
            Assert.That(capped.AppliedExperience, Is.EqualTo(0)); // 최대 레벨 이후 획득 차단
        } // 검증 끝
        [Test] // 잘못된 레벨과 경험치 검증
        public void AdvanceExperience_RejectsInvalidProgress() // 잘못된 저장 상태 차단
        { // 검증 시작
            Assert.That(ExperienceRules.TryAdvance(0, 0, 1, out _), Is.False); // 시작 레벨 미만 거절
            Assert.That(ExperienceRules.TryAdvance(11, 0, 1, out _), Is.False); // 최대 레벨 초과 거절
            Assert.That(ExperienceRules.TryAdvance(1, 100, 0, out _), Is.False); // 이미 레벨업해야 할 진행값 거절
            Assert.That(ExperienceRules.TryAdvance(10, 1, 0, out _), Is.False); // 최대 레벨의 잔여 경험치 거절
            Assert.That(ExperienceRules.TryAdvance(1, 0, -1, out _), Is.False); // 음수 획득량 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
