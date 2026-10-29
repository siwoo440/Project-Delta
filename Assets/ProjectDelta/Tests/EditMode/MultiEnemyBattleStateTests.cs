using System; // 잘못된 참가자 예외 검증
using NUnit.Framework; // 다수 적 전투 검증 도구
using ProjectDelta.Core; // 독립 전투 상태 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class MultiEnemyBattleStateTests // 다수 적 대상과 승패 검증
    { // 클래스 시작
        [Test] // 생존 대상 선택과 재선택 검증
        public void Selection_RejectsDeadOrExternalAndRequiresReselectAfterDeath() // 생존 대상 선택과 사망 처리 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 플레이어 참가자
            var first = new BattleActor("슬라임 1", 5, false, 1); // 첫 번째 적
            var second = new BattleActor("슬라임 2", 5, false, 2); // 두 번째 적
            var external = new BattleActor("다른 적", 5, false, 3); // 전투 밖 대상
            var battle = new MultiEnemyBattleState(player, new[] { first, second }); // 두 적의 전투 상태
            Assert.That(battle.SelectedEnemy, Is.Null); // 최초 대상의 명시적 선택 확인
            Assert.That(battle.TrySelectEnemy(external), Is.False); // 전투 밖 대상 차단
            Assert.That(battle.TrySelectEnemy(first), Is.True); // 첫 번째 생존 적 선택
            Assert.That(battle.SelectedEnemy, Is.SameAs(first)); // 실제 선택 대상 확인
            Assert.That(battle.TrySelectEnemy(player), Is.False); // 아군 선택 차단
            Assert.That(battle.TrySelectEnemy(null), Is.False); // 빈 대상 선택 차단
            Assert.That(battle.SelectedEnemy, Is.SameAs(first)); // 잘못된 선택의 기존 대상 보존
            first.IsAlive = false; // 선택한 적 사망 처리
            Assert.That(battle.SelectedEnemy, Is.Null); // 사망 대상 자동 해제 확인
            Assert.That(battle.TrySelectEnemy(first), Is.False); // 사망한 적 재선택 차단
            Assert.That(battle.SelectedEnemy, Is.Null); // 다른 적의 자동 선택 방지 확인
            Assert.That(battle.TrySelectEnemy(second), Is.True); // 다른 생존 적 명시적 선택
            Assert.That(battle.SelectedEnemy, Is.SameAs(second)); // 새로 선택한 대상 확인
        } // 검증 끝
        [Test] // 전체 생존 적과 플레이어 사망 검증
        public void Outcome_ContinuesWhileAnyEnemyLivesAndPrioritizesDefeat() // 전투 지속과 동시 사망 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 생존 플레이어
            var first = new BattleActor("적 1", 5, false, 1); // 첫 번째 생존 적
            var second = new BattleActor("적 2", 5, false, 2); // 두 번째 생존 적
            var battle = new MultiEnemyBattleState(player, new[] { first, second }); // 다수 적 전투
            first.IsAlive = false; // 첫 번째 적 처치
            Assert.That(battle.Outcome, Is.EqualTo(CombatOutcome.Ongoing)); // 생존 적이 남은 전투 지속
            second.IsAlive = false; // 마지막 적 처치
            Assert.That(battle.Outcome, Is.EqualTo(CombatOutcome.Victory)); // 전원 처치 승리 확인
            player.IsAlive = false; // 플레이어도 사망
            Assert.That(battle.Outcome, Is.EqualTo(CombatOutcome.Defeat)); // 동시 사망의 패배 우선
        } // 검증 끝
        [Test] // 잘못된 참가자와 적 최대 수 검증
        public void Construction_RejectsInvalidOrMoreThanThreeEnemies() // 참가자 구성 보호 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 정상 플레이어
            var enemy = new BattleActor("적", 5, false, 1); // 정상 적
            var enemies = new[] // 최대 수를 넘는 적 목록
            { // 목록 시작
                enemy, new BattleActor("적 2", 5, false, 2), new BattleActor("적 3", 5, false, 3), new BattleActor("적 4", 5, false, 4) // 네 적의 구성
            }; // 목록 끝
            Assert.Throws<ArgumentException>(() => new MultiEnemyBattleState(player, enemies)); // 네 적 거절 확인
            Assert.Throws<ArgumentException>(() => new MultiEnemyBattleState(player, new[] { enemy, enemy })); // 중복 개체 거절 확인
            Assert.Throws<ArgumentException>(() => new MultiEnemyBattleState(player, new[] { player })); // 아군 대상 거절 확인
            Assert.Throws<ArgumentException>(() => new MultiEnemyBattleState(player, new BattleActor[0])); // 빈 적 목록 거절 확인
            Assert.That(new MultiEnemyBattleState(player, new[] { enemy }).Outcome, Is.EqualTo(CombatOutcome.Ongoing)); // 한 적의 유효한 시작
            Assert.That(new MultiEnemyBattleState(player, new[] { enemies[0], enemies[1], enemies[2] }).Enemies.Count, Is.EqualTo(3)); // 세 적의 유효한 시작
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
