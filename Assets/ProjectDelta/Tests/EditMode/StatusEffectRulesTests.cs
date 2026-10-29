using NUnit.Framework; // 상태 규칙 검증 도구
using ProjectDelta.Core; // 독립 상태 효과 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class StatusEffectRulesTests // 상태 성공률과 지속 피해 검증
    { // 클래스 시작
        [TestCase(10, 0, 100, 0, 5)] // 최소 성공률 검사
        [TestCase(90, 20, 0, 10, 95)] // 최대 성공률 검사
        [TestCase(60, 5, 20, 4, 49)] // 기본 보정과 저항 계산 검사
        [TestCase(int.MaxValue, int.MaxValue, int.MinValue, int.MaxValue, 95)] // 큰 정수 범위 검사
        public void SuccessChance_UsesModifiersAndClamps(int basic, int attacker, int resistance, int other, int expected) // 상태 성공률 계산 확인
        { // 검증 시작
            Assert.That(StatusEffectRules.SuccessChance(basic, attacker, resistance, other), Is.EqualTo(expected)); // 기획 성공률 확인
        } // 검증 끝
        [Test] // 독과 출혈의 라운드 종료 검증
        public void RoundEnd_AppliesDamageBeforeDurationDecreases() // 피해와 남은 지속 시간 확인
        { // 검증 시작
            var effects = new[] // 현재 적용된 상태 목록
            { // 목록 시작
                new RoundStatusEffect(RoundStatusKind.Poison, 2, 7), // 두 라운드 독과 시험 피해
                new RoundStatusEffect(RoundStatusKind.Bleeding, 1, 4) // 마지막 라운드 출혈과 시험 피해
            }; // 목록 끝
            var result = StatusEffectRules.ResolveRoundEnd(30, effects); // 실제 라운드 종료 처리
            Assert.That(result.Health, Is.EqualTo(19)); // 두 상태의 최종 피해 확인
            Assert.That(result.Effects.Count, Is.EqualTo(1)); // 만료된 출혈 제거 확인
            Assert.That(result.Effects[0].Kind, Is.EqualTo(RoundStatusKind.Poison)); // 남은 독 상태 확인
            Assert.That(result.Effects[0].RemainingRounds, Is.EqualTo(1)); // 독 지속 시간 감소 확인
            Assert.That(effects[0].RemainingRounds, Is.EqualTo(2)); // 입력 상태의 변경 방지 확인
        } // 검증 끝
        [Test] // 마지막 지속 시간과 생존 범위 검증
        public void RoundEnd_LastTickAppliesAndHealthNeverFallsBelowZero() // 마지막 피해와 체력 범위 확인
        { // 검증 시작
            var effects = new[] // 마지막 라운드 상태
            { // 목록 시작
                new RoundStatusEffect(RoundStatusKind.Poison, 1, 10) // 체력보다 큰 마지막 독 피해
            }; // 목록 끝
            var result = StatusEffectRules.ResolveRoundEnd(3, effects); // 상태 종료 처리
            Assert.That(result.Health, Is.EqualTo(0)); // 체력 하한 확인
            Assert.That(result.Effects.Count, Is.EqualTo(0)); // 마지막 피해 후 상태 만료 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
