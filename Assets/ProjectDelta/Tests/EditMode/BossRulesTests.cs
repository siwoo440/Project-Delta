using NUnit.Framework; // 마왕 규칙 검증 도구
using ProjectDelta.Core; // 순수 마왕 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class BossRulesTests // 시험 마왕 능력치와 전투 규칙 검증
    { // 클래스 시작
        [Test] // 기존 자산의 시험 능력치 검증
        public void TrialStats_ExposeExplicitPrototypeValues() // 마왕 시험값과 승리 경험치
        { // 검증 시작
            Assert.That(BossRules.TrialMaxHealth, Is.EqualTo(200)); // 시험 체력
            Assert.That(BossRules.TrialMaxMana, Is.EqualTo(40)); // 시험 마나
            Assert.That(BossRules.TrialSpeed, Is.EqualTo(9)); // 시험 속도
            Assert.That(BossRules.TrialAttack, Is.EqualTo(26)); // 시험 공격력
            Assert.That(BossRules.TrialDefense, Is.EqualTo(18)); // 시험 방어력
            Assert.That(BossRules.TrialAccuracy, Is.EqualTo(90)); // 시험 명중
            Assert.That(BossRules.TrialEvasion, Is.EqualTo(10)); // 시험 회피
            Assert.That(BossRules.TrialCharm, Is.EqualTo(15)); // 시험 매력
            Assert.That(BossRules.TrialResistance, Is.EqualTo(30)); // 시험 저항
            Assert.That(BossRules.VictoryExperience, Is.Zero); // 일반 승리 경험치 없음
            Assert.That(BossRules.CanEscape, Is.False); // 마왕전 도주 불가
            Assert.That(BossRules.RequiresSurrenderConfirmation, Is.True); // 항복 전 확인 필요
        } // 검증 끝
        [Test] // 세 체력 단계의 정확한 경계 검증
        public void Phase_UsesThreeEqualHealthRanges() // 시험 최대 체력의 삼등분
        { // 검증 시작
            Assert.That(BossRules.Phase(200), Is.EqualTo(1)); // 최대 체력의 첫 단계
            Assert.That(BossRules.Phase(134), Is.EqualTo(1)); // 첫 단계 하한
            Assert.That(BossRules.Phase(133), Is.EqualTo(2)); // 둘째 단계 상한
            Assert.That(BossRules.Phase(67), Is.EqualTo(2)); // 둘째 단계 하한
            Assert.That(BossRules.Phase(66), Is.EqualTo(3)); // 마지막 단계 상한
            Assert.That(BossRules.Phase(1), Is.EqualTo(3)); // 생존 최저 체력
            Assert.That(BossRules.Phase(0), Is.EqualTo(3)); // 처치 체력의 기존 마지막 단계
            Assert.That(BossRules.Phase(-1), Is.EqualTo(-1)); // 음수 체력 거절
            Assert.That(BossRules.Phase(201), Is.EqualTo(-1)); // 최대 초과 체력 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
