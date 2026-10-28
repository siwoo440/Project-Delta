using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 전투 순서 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class BattleRoundTests // 라운드와 행동 순서 검증
    { // 클래스 시작
        [Test] // 속도와 생존 상태 검증
        public void Round_OrdersAliveActorsBySpeed() // 살아 있는 참가자의 속도순 확인
        { // 검증 시작
            var actors = new[] // 시험 참가자 목록
            { // 목록 시작
                new BattleActor("플레이어", 10, true, 0), // 플레이어 속도
                new BattleActor("적 1", 20, false, 1), // 첫 적 속도
                new BattleActor("적 2", 15, false, 2), // 두 번째 적 속도
                new BattleActor("사망", 30, false, 3, false) // 행동 제외 참가자
            }; // 목록 끝
            var round = new BattleRound(actors); // 시험 라운드 생성
            Assert.That(round.TryStartRound(), Is.True); // 라운드 시작
            Assert.That(round.Number, Is.EqualTo(1)); // 첫 라운드 번호
            Assert.That(round.TryNext(out var first), Is.True); // 첫 행동자 선택
            Assert.That(first.Name, Is.EqualTo("적 1")); // 가장 빠른 생존 적 확인
            Assert.That(round.TryNext(out _), Is.False); // 진행 중 중복 행동 차단
            Assert.That(round.TryCompleteAction(), Is.True); // 첫 행동 완료
            Assert.That(round.TryNext(out var second), Is.True); // 두 번째 행동자 선택
            Assert.That(second.Name, Is.EqualTo("적 2")); // 두 번째 속도 확인
            Assert.That(round.TryCompleteAction(), Is.True); // 두 번째 행동 완료
            Assert.That(round.TryNext(out var third), Is.True); // 세 번째 행동자 선택
            Assert.That(third.Name, Is.EqualTo("플레이어")); // 가장 느린 참가자 확인
            Assert.That(round.TryCompleteAction(), Is.True); // 세 번째 행동 완료
            Assert.That(round.TryNext(out _), Is.False); // 모든 참가자 행동 확인
            Assert.That(round.TryEndRound(), Is.True); // 라운드 종료 확인
        } // 검증 끝
        [Test] // 동률 우선순위 검증
        public void Round_TiedSpeed_UsesPlayerThenEnemySlot() // 동률 행동 순서 확인
        { // 검증 시작
            var actors = new[] // 순서를 섞은 참가자 목록
            { // 목록 시작
                new BattleActor("적 2", 5, false, 2), // 두 번째 슬롯 적
                new BattleActor("적 1", 5, false, 1), // 첫 번째 슬롯 적
                new BattleActor("플레이어", 5, true, 0) // 동률 플레이어
            }; // 목록 끝
            var round = new BattleRound(actors); // 동률 시험 라운드
            Assert.That(round.TryStartRound(), Is.True); // 라운드 시작
            Assert.That(round.TryNext(out var first), Is.True); // 첫 행동자 선택
            Assert.That(first.Name, Is.EqualTo("플레이어")); // 플레이어 우선 확인
            round.TryCompleteAction(); // 첫 행동 종료
            Assert.That(round.TryNext(out var second), Is.True); // 두 번째 행동자 선택
            Assert.That(second.Name, Is.EqualTo("적 1")); // 적 슬롯 순서 확인
            round.TryCompleteAction(); // 두 번째 행동 종료
            Assert.That(round.TryNext(out var third), Is.True); // 세 번째 행동자 선택
            Assert.That(third.Name, Is.EqualTo("적 2")); // 두 번째 적 확인
        } // 검증 끝
        [Test] // 라운드 경계와 중복 요청 검증
        public void Round_RequiresAllActionsBeforeNextRound() // 모든 행동 후 다음 라운드
        { // 검증 시작
            var actors = new[] // 참가자 목록
            { // 목록 시작
                new BattleActor("플레이어", 5, true, 0), // 플레이어 참가자
                new BattleActor("적", 4, false, 1) // 적 참가자
            }; // 목록 끝
            var round = new BattleRound(actors); // 시험 라운드 생성
            Assert.That(round.TryStartRound(), Is.True); // 첫 라운드 시작
            Assert.That(round.TryStartRound(), Is.False); // 중복 시작 차단
            Assert.That(round.TryEndRound(), Is.False); // 행동 전 종료 차단
            Assert.That(round.TryNext(out _), Is.True); // 첫 행동자 선택
            Assert.That(round.TryEndRound(), Is.False); // 진행 중 종료 차단
            Assert.That(round.TryCompleteAction(), Is.True); // 첫 행동 완료
            Assert.That(round.TryEndRound(), Is.False); // 남은 행동자 확인
            Assert.That(round.TryNext(out _), Is.True); // 다음 행동자 선택
            Assert.That(round.TryCompleteAction(), Is.True); // 마지막 행동 완료
            Assert.That(round.TryEndRound(), Is.True); // 모든 행동 후 라운드 종료
            Assert.That(round.TryStartRound(), Is.True); // 다음 라운드 시작
            Assert.That(round.Number, Is.EqualTo(2)); // 다음 라운드 번호 확인
        } // 검증 끝
        [Test] // 진행 중 사망과 빈 목록 검증
        public void Round_SkipsActorWhoDiesBeforeActing() // 행동 대기 중 사망 제외
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 먼저 행동할 플레이어
            var enemy = new BattleActor("적", 5, false, 1); // 대기 중인 적
            var actors = new[] // 시험 참가자 목록
            { // 목록 시작
                player, enemy // 시험 참가자 순서
            }; // 목록 끝
            var round = new BattleRound(actors); // 시험 라운드 생성
            Assert.That(round.TryStartRound(), Is.True); // 라운드 시작
            Assert.That(round.TryNext(out _), Is.True); // 첫 행동자 선택
            enemy.IsAlive = false; // 적의 행동 전 사망
            Assert.That(round.TryCompleteAction(), Is.True); // 첫 행동 종료
            Assert.That(round.TryNext(out _), Is.False); // 사망한 행동자 제외
            Assert.That(round.TryEndRound(), Is.True); // 사망자 제외 후 라운드 종료
            Assert.That(new BattleRound(null).TryStartRound(), Is.False); // 참가자 없는 라운드 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
