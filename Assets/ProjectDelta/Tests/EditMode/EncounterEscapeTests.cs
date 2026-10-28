using System.Collections.Generic; // 전투 참가자 목록
using System.Reflection; // 구현 전 규칙 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 전투 참가자
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterEscapeTests // 조우 단계 도망 판정 검증
    { // 클래스 시작
        private static MethodInfo Rule() // 조우 도망 규칙 조회
        { // 조회 시작
            var type = typeof(BattleActor).Assembly.GetType("ProjectDelta.Core.EncounterEscapeRules"); // 조우 도망 규칙 형식
            Assert.That(type, Is.Not.Null); // 도망 규칙 제공 확인
            var method = type.GetMethod("TryResolve"); // 도망 판정 조회
            Assert.That(method, Is.Not.Null); // 도망 판정 제공 확인
            return method; // 확인한 판정 전달
        } // 조회 끝
        [TestCase(1, 6, false)] // 속도 차이 5의 동점 실패
        [TestCase(2, 6, true)] // 동점보다 높은 플레이어 성공
        [TestCase(1, 20, false)] // 느린 판정 실패
        public void Resolve_RequiresStrictlyHigherScore(int playerRoll, int enemyRoll, bool expected) // 기획서의 동점 실패 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 플레이어 속도 10
            var actors = new List<BattleActor> // 조우 참가자
            { // 목록 시작
                player, // 플레이어 참가자
                new BattleActor("적", 5, false, 1) // 상대 속도 5
            }; // 목록 끝
            var arguments = new object[] // 도망 판정 인수
            { // 인수 시작
                player, actors, 0, playerRoll, enemyRoll, false // 플레이어와 주사위
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, arguments), Is.True); // 유효한 도망 판정
            Assert.That(arguments[5], Is.EqualTo(expected)); // 실제 성공 여부
        } // 검증 끝
        [Test] // 가장 빠른 생존 상대 검증
        public void Resolve_UsesFastestLivingOpponent() // 사망자와 같은 진영 제외
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 플레이어 속도 10
            var actors = new List<BattleActor> // 조우 참가자
            { // 목록 시작
                player, // 플레이어 참가자
                new BattleActor("동료", 100, true, 1), // 같은 진영 제외
                new BattleActor("적1", 5, false, 1), // 느린 상대
                new BattleActor("적2", 15, false, 2), // 가장 빠른 생존 상대
                new BattleActor("사망", 99, false, 3, false) // 사망 상대 제외
            }; // 목록 끝
            var arguments = new object[] // 도망 판정 인수
            { // 인수 시작
                player, actors, 0, 6, 1, false // 속도 차이 5의 동점
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, arguments), Is.True); // 유효한 도망 판정
            Assert.That(arguments[5], Is.False); // 가장 빠른 상대와 동점 실패
            arguments[2] = 1; // 플레이어 보정 한 점
            Assert.That(Rule().Invoke(null, arguments), Is.True); // 보정 포함 판정
            Assert.That(arguments[5], Is.True); // 보정으로 판정 성공
        } // 검증 끝
        [TestCase(0, 1)] // 플레이어 주사위 범위 미만
        [TestCase(1, 21)] // 상대 주사위 범위 초과
        public void Resolve_RejectsInvalidDice(int playerRoll, int enemyRoll) // 잘못된 주사위 차단
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 생존 플레이어
            var actors = new List<BattleActor> // 조우 참가자
            { // 목록 시작
                player, // 플레이어 참가자
                new BattleActor("적", 5, false, 1) // 생존 상대
            }; // 목록 끝
            var arguments = new object[] // 도망 판정 인수
            { // 인수 시작
                player, actors, 0, playerRoll, enemyRoll, false // 범위 밖 주사위
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, arguments), Is.False); // 잘못된 판정 거절
        } // 검증 끝
        [Test] // 상대 부재와 정수 범위 검증
        public void Resolve_RejectsNoLivingOpponentAndAvoidsOverflow() // 대상 부재와 속도 범위 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", int.MaxValue, true, 0); // 최대 속도 플레이어
            var dead = new BattleActor("사망", 5, false, 1, false); // 사망한 상대
            var actors = new List<BattleActor> // 조우 참가자
            { // 목록 시작
                player, dead // 플레이어와 사망 상대
            }; // 목록 끝
            var arguments = new object[] // 도망 판정 인수
            { // 인수 시작
                player, actors, int.MaxValue, 20, 1, false // 큰 보정과 주사위
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, arguments), Is.False); // 생존 상대 없는 판정 거절
            actors.Add(new BattleActor("적", int.MinValue, false, 2)); // 생존한 최소 속도 상대
            Assert.That(Rule().Invoke(null, arguments), Is.True); // 정수 범위의 유효한 판정
            Assert.That(arguments[5], Is.True); // 합산 범위의 실제 성공
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
