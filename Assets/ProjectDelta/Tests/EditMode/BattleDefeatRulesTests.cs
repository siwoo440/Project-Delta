using NUnit.Framework; // 패배 기록 규칙 검증 도구
using ProjectDelta.Core; // 순수 전투 참가자와 패배 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class BattleDefeatRulesTests // 패배 원인과 기록 대상 검증
    { // 클래스 시작
        [Test] // 선택 대상과 마지막 영향자의 우선순위 검증
        public void Surrender_PrefersSelectedEnemyThenLastInfluencer() // 항복의 실제 기록 대상 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 생존 플레이어
            var first = new BattleActor("적 1", 5, false, 1); // 첫 번째 생존 적
            var second = new BattleActor("적 2", 5, false, 2); // 마지막 영향 적
            var enemies = new[] { first, second }; // 현재 전투의 적 목록
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, first, second, null, out var selected), Is.True); // 선택한 적이 있는 항복
            Assert.That(selected.Reason, Is.EqualTo(BattleDefeatReason.Surrender)); // 실제 항복 원인 확인
            Assert.That(selected.Opponent, Is.SameAs(first)); // 선택 대상 우선 확인
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, null, second, null, out var fallback), Is.True); // 선택 대상 없는 항복
            Assert.That(fallback.Opponent, Is.SameAs(second)); // 마지막 직접 영향 대상 사용 확인
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, null, null, null, out _), Is.False); // 근거 없는 임의 대상 차단
        } // 검증 끝
        [Test] // 플레이어 사망의 항복보다 높은 우선순위 검증
        public void HealthZero_UsesLastDamageSourceEvenWhenSurrenderRequested() // 체력 소진의 실제 피해자 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0, false); // 체력이 소진된 플레이어
            var first = new BattleActor("적 1", 5, false, 1); // 현재 선택된 적
            var source = new BattleActor("적 2", 5, false, 2, false); // 지속 피해를 부여한 사망 적
            var enemies = new[] { first, source }; // 현재 전투 참가자
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, first, first, source, out var record), Is.True); // 동시 항복 요청과 체력 소진
            Assert.That(record.Reason, Is.EqualTo(BattleDefeatReason.HealthZero)); // 플레이어 사망 우선 확인
            Assert.That(record.Opponent, Is.SameAs(source)); // 실제 마지막 피해 원인 확인
            first.IsAlive = false; // 사망한 이전 선택 대상
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, first, null, source, out var deadSelection), Is.True); // 체력 소진의 이전 선택 무시
            Assert.That(deadSelection.Opponent, Is.SameAs(source)); // 실제 피해 원인의 우선 기록 확인
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, false, null, first, null, out _), Is.False); // 피해 원인이 없는 패배 기록 차단
        } // 검증 끝
        [Test] // 잘못된 플레이어와 대상 데이터 차단 검증
        public void InvalidInputs_AreRejectedWithoutFabricatedRecord() // 잘못된 대상과 미확인 항복 거절
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 생존 플레이어
            var enemy = new BattleActor("적", 5, false, 1); // 정상 적
            var outsider = new BattleActor("외부 적", 5, false, 2); // 전투에 없는 적
            var enemies = new[] { enemy }; // 실제 참가자 목록
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, false, enemy, null, null, out _), Is.False); // 미확인 항복 차단
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, outsider, enemy, null, out _), Is.False); // 외부 선택 대상 차단
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, player, enemy, null, out _), Is.False); // 아군 대상 차단
            Assert.That(BattleDefeatRules.TryCreate(null, enemies, true, enemy, null, null, out _), Is.False); // 플레이어 누락 차단
            Assert.That(BattleDefeatRules.TryCreate(player, null, true, enemy, null, null, out _), Is.False); // 적 목록 누락 차단
            Assert.That(BattleDefeatRules.TryCreate(player, new[] { enemy, enemy }, true, enemy, null, null, out _), Is.False); // 중복 적 목록 차단
            enemy.IsAlive = false; // 현재 선택 대상의 사망
            Assert.That(BattleDefeatRules.TryCreate(player, enemies, true, enemy, null, null, out _), Is.False); // 사망한 선택 대상 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
