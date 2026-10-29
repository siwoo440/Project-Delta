using System.Reflection; // 구현 전 상태 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 전투 참가자와 판정
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class CombatDefenseTests // 방어 행동과 피해 감소 검증
    { // 클래스 시작
        [Test] // 방어 상태와 최소 피해 검증
        public void Defense_ReducesDamageUntilNextTurnAndKeepsMinimum() // 방어 상태의 피해와 종료 확인
        { // 검증 시작
            var attacker = Fighter(false, 100, 100, 0); // 공격력 100의 적
            var defender = Fighter(true, 100, 0, 25); // 방어력 25의 플레이어
            var begin = typeof(CombatFighter).GetMethod("BeginDefense"); // 방어 시작 조회
            var end = typeof(CombatFighter).GetMethod("EndDefense"); // 방어 해제 조회
            var property = typeof(CombatFighter).GetProperty("IsDefending"); // 방어 상태 조회
            Assert.That(begin, Is.Not.Null); // 방어 시작 제공 확인
            Assert.That(end, Is.Not.Null); // 방어 해제 제공 확인
            Assert.That(property, Is.Not.Null); // 방어 상태 제공 확인
            Assert.That(begin.Invoke(defender, null), Is.True); // 살아 있는 참가자 방어 시작
            Assert.That(property.GetValue(defender), Is.True); // 현재 방어 상태 확인
            Assert.That(CombatRules.Damage(attacker, defender), Is.EqualTo(65)); // 방어력 25와 방어 보정 10 적용
            Assert.That(begin.Invoke(defender, null), Is.True); // 중복 방어 행동 적용
            Assert.That(CombatRules.Damage(attacker, defender), Is.EqualTo(65)); // 방어 보정 중첩 방지
            var weak = Fighter(false, 100, 0, 0); // 공격력이 없는 시험 적
            Assert.That(CombatRules.Damage(weak, defender), Is.EqualTo(1)); // 방어 중에도 최소 피해 보장
            end.Invoke(defender, null); // 다음 행동 차례의 방어 종료
            Assert.That(property.GetValue(defender), Is.False); // 방어 상태 해제 확인
            Assert.That(CombatRules.Damage(attacker, defender), Is.EqualTo(75)); // 해제 후 원래 피해
            defender.ApplyDamage(100); // 플레이어 체력 소진
            Assert.That(begin.Invoke(defender, null), Is.False); // 사망 참가자의 방어 차단
        } // 검증 끝
        private static CombatFighter Fighter(bool player, int health, int attack, int defense) // 시험 참가자 생성
        { // 생성 시작
            var actor = new BattleActor(player ? "플레이어" : "적", 5, player, player ? 0 : 1); // 진영별 행동 참가자
            return new CombatFighter(actor, health, attack, defense, 90, 0); // 시험 전투 능력치
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
