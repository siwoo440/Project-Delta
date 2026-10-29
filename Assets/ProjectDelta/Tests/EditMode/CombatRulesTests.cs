using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 전투 판정 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class CombatRulesTests // 기본 공격과 승패 검증
    { // 클래스 시작
        [Test] // 명중률과 상하한 검증
        public void HitChance_UsesAccuracyHalfEvasionAndBounds() // 최신 기획 명중률 공식 확인
        { // 검증 시작
            var attacker = Fighter(true, 100, 100, 0, 10, 0); // 공격자 명중 10
            var defender = Fighter(false, 100, 0, 0, 0, 10); // 방어자 회피 10
            Assert.That(CombatRules.HitChance(attacker, defender), Is.EqualTo(95)); // 기본 90과 명중 및 절반 회피
            Assert.That(CombatRules.HitChance(Fighter(true, 100, 0, 0, 1000, 0), defender), Is.EqualTo(95)); // 최대 명중률
            Assert.That(CombatRules.HitChance(attacker, Fighter(false, 100, 0, 0, 0, 1000)), Is.EqualTo(5)); // 최소 명중률
        } // 검증 끝
        [Test] // 방어와 피해 편차 검증
        public void Damage_UsesDefenseWithoutVarianceAndMinimum() // 최신 기획 피해 공식 확인
        { // 검증 시작
            var attacker = Fighter(true, 100, 100, 0, 0, 0); // 공격력 100
            var defender = Fighter(false, 100, 0, 25, 0, 0); // 방어력 25
            Assert.That(CombatRules.Damage(attacker, defender), Is.EqualTo(75)); // 공격력에서 방어력 차감
            Assert.That(CombatRules.Damage(Fighter(true, 100, 0, 0, 0, 0), defender), Is.EqualTo(1)); // 최소 피해 보장
        } // 검증 끝
        [Test] // 명중과 빗나감 검증
        public void Attack_HitThreshold_AndHealthClamp() // 명중 경계와 체력 감소 확인
        { // 검증 시작
            var attacker = Fighter(true, 100, 100, 0, 0, 0); // 명중률 시험 공격자
            var defender = Fighter(false, 10, 0, 25, 0, 10); // 체력 10과 회피 10의 적
            Assert.That(CombatRules.TryAttack(attacker, defender, 85, out var missed), Is.True); // 명중 경계 공격 처리
            Assert.That(missed, Is.EqualTo(0)); // 경계 난수의 빗나감 확인
            Assert.That(defender.Health, Is.EqualTo(10)); // 빗나간 체력 유지
            Assert.That(CombatRules.TryAttack(attacker, defender, 84, out var applied), Is.True); // 명중 공격 처리
            Assert.That(applied, Is.EqualTo(10)); // 남은 체력까지만 피해 적용
            Assert.That(defender.Health, Is.Zero); // 체력 하한 확인
            Assert.That(defender.Actor.IsAlive, Is.False); // 사망 시 행동 제외
        } // 검증 끝
        [Test] // 잘못된 공격과 승패 검증
        public void Outcome_RequiresOpponents_AndDefeatTakesPriority() // 대상 제한과 승패 우선 확인
        { // 검증 시작
            var player = Fighter(true, 10, 1, 0, 0, 0); // 생존 플레이어
            var enemy = Fighter(false, 10, 1, 0, 0, 0); // 생존 적
            Assert.That(CombatRules.Outcome(player, enemy), Is.EqualTo(CombatOutcome.Ongoing)); // 양측 생존 전투 유지
            Assert.That(CombatRules.Outcome(null, enemy), Is.EqualTo(CombatOutcome.Defeat)); // 플레이어 누락 시 패배 우선
            Assert.That(CombatRules.Outcome(player, null), Is.EqualTo(CombatOutcome.Victory)); // 적이 없으면 승리
            Assert.That(CombatRules.TryAttack(player, player, 0, out _), Is.False); // 같은 편 공격 거절
            Assert.That(CombatRules.TryAttack(player, enemy, -1, out _), Is.False); // 잘못된 명중 난수 거절
            enemy.ApplyDamage(10); // 적 체력 소진
            Assert.That(CombatRules.Outcome(player, enemy), Is.EqualTo(CombatOutcome.Victory)); // 적 전멸 승리
            player.ApplyDamage(10); // 플레이어 체력 소진
            Assert.That(CombatRules.Outcome(player, enemy), Is.EqualTo(CombatOutcome.Defeat)); // 동시 전멸 시 패배 우선
            Assert.That(CombatRules.TryAttack(player, enemy, 0, out _), Is.False); // 사망 참가자 공격 차단
        } // 검증 끝
        private static CombatFighter Fighter(bool isPlayer, int health, int attack, int defense, int accuracy, int evasion) // 시험 참가자 생성
        { // 생성 시작
            var actor = new BattleActor(isPlayer ? "플레이어" : "적", 5, isPlayer, isPlayer ? 0 : 1); // 진영과 행동 참가자
            return new CombatFighter(actor, health, attack, defense, accuracy, evasion); // 시험 능력치 연결
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
