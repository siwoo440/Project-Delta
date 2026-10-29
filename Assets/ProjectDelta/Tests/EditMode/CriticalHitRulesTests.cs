using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 치명타 판정 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class CriticalHitRulesTests // 치명타 확률과 배율 검증
    { // 클래스 시작
        [TestCase(10, 0, 0, 150, 10, false)] // 확률 0의 치명타 없음
        [TestCase(11, 100, 99, 150, 16, true)] // 치명타 피해의 소수점 버림
        [TestCase(11, 40, 39, 200, 22, true)] // 치명타 확률 경계 안쪽
        [TestCase(11, 40, 40, 200, 11, false)] // 치명타 확률 경계 바깥
        [TestCase(11, -10, 0, 150, 11, false)] // 음수 확률의 0 제한
        [TestCase(11, 150, 99, 150, 16, true)] // 과도한 확률의 100 제한
        public void Critical_AppliesProbabilityAndConfiguredMultiplier(int damage, int chance, int roll, int multiplier, int expected, bool critical) // 설정된 확률과 배율 판정
        { // 검증 시작
            Assert.That(CriticalHitRules.TryApply(damage, chance, roll, multiplier, out var result, out var occurred), Is.True); // 유효한 판정 확인
            Assert.That(result, Is.EqualTo(expected)); // 실제 최종 피해 확인
            Assert.That(occurred, Is.EqualTo(critical)); // 치명타 발생 여부 확인
        } // 검증 끝
        [Test] // 잘못된 치명타 자료와 정수 범위 검증
        public void Critical_RejectsInvalidRollAndSaturatesOverflow() // 입력 범위와 최대 피해 확인
        { // 검증 시작
            Assert.That(CriticalHitRules.TryApply(1, 50, -1, 150, out _, out _), Is.False); // 잘못된 주사위 차단
            Assert.That(CriticalHitRules.TryApply(1, 50, 0, 99, out _, out _), Is.False); // 잘못된 배율 차단
            Assert.That(CriticalHitRules.TryApply(int.MaxValue, 100, 0, 200, out var damage, out _), Is.True); // 큰 피해 판정
            Assert.That(damage, Is.EqualTo(int.MaxValue)); // 정수 범위 제한
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
