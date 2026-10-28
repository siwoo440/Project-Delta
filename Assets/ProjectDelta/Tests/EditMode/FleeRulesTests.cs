using System; // 형식 조회
using System.Collections.Generic; // 참가자 목록 계약
using System.Reflection; // 구현 전 규칙 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 기존 전투 참가자
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class FleeRulesTests // 기존 도주 확률과 난수 검증
    { // 클래스 시작
        private static MethodInfo Rule(string name) // 도주 규칙 메서드 조회
        { // 조회 시작
            var type = typeof(BattleActor).Assembly.GetType("ProjectDelta.Core.FleeRules"); // 순수 도주 규칙 형식
            Assert.That(type, Is.Not.Null); // 도주 규칙 제공 확인
            var method = type.GetMethod(name); // 요청한 규칙 조회
            Assert.That(method, Is.Not.Null); // 요청한 규칙 존재 확인
            return method; // 확인한 규칙 반환
        } // 조회 끝
        [Test] // 평균 속도와 생존 상대 검증
        public void Chance_UsesOnlyLivingOpponentsAndIntegerAverage() // 살아 있는 상대 평균 속도 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 20, true, 0); // 행동자 속도 20
            var opponents = new List<BattleActor> // 참가자 목록
            { // 목록 시작
                player, // 행동자 제외
                new BattleActor("동료", 99, true, 1), // 같은 진영 제외
                new BattleActor("적1", 10, false, 1), // 첫 번째 상대
                new BattleActor("적2", 15, false, 2), // 두 번째 상대
                new BattleActor("사망", 100, false, 3, false) // 사망 상대 제외
            }; // 목록 끝
            var result = Rule("Chance").Invoke(null, new object[] // 확률 조회 인수
            { // 인수 시작
                player, opponents // 행동자와 참가자 목록
            }); // 인수 끝
            Assert.That(result, Is.EqualTo(58)); // 기본 50과 속도 차이 8
        } // 검증 끝
        [Test] // 확률 상하한 검증
        public void Chance_ClampsExtremeSpeedDifferences() // 속도 차이 상하한 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", int.MaxValue, true, 0); // 높은 속도 행동자
            var enemy = new BattleActor("적", int.MinValue, false, 1); // 낮은 속도 상대
            var actors = new List<BattleActor> // 양측 참가자
            { // 목록 시작
                player, enemy // 속도 극단값 참가자
            }; // 목록 끝
            var arguments = new object[] // 확률 조회 인수
            { // 인수 시작
                player, actors // 행동자와 참가자 목록
            }; // 인수 끝
            Assert.That(Rule("Chance").Invoke(null, arguments), Is.EqualTo(95)); // 최대 확률 제한
            arguments[0] = enemy; // 상대 진영 행동자로 변경
            Assert.That(Rule("Chance").Invoke(null, arguments), Is.EqualTo(5)); // 최소 확률 제한
        } // 검증 끝
        [TestCase(54, true)] // 확률 미만 난수 성공
        [TestCase(55, false)] // 확률과 같은 난수 실패
        public void TryFlee_UsesExclusiveChanceThreshold(int roll, bool expected) // 난수 경계 확인
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 행동자 속도 10
            var enemy = new BattleActor("적", 5, false, 1); // 상대 속도 5
            var actors = new List<BattleActor> // 전투 참가자
            { // 목록 시작
                player, enemy // 플레이어와 상대
            }; // 목록 끝
            var arguments = new object[] // 도주 판정 인수
            { // 인수 시작
                player, actors, roll, false // 행동자와 참가자 및 난수
            }; // 인수 끝
            Assert.That(Rule("TryFlee").Invoke(null, arguments), Is.True); // 유효한 행동 처리
            Assert.That(arguments[3], Is.EqualTo(expected)); // 실제 도주 성공 여부
        } // 검증 끝
        [TestCase(-1)] // 하한 미만 난수
        [TestCase(100)] // 상한 초과 난수
        public void TryFlee_RejectsInvalidRoll(int roll) // 잘못된 난수 차단
        { // 검증 시작
            var player = new BattleActor("플레이어", 10, true, 0); // 생존 행동자
            var actors = new List<BattleActor> // 전투 참가자
            { // 목록 시작
                player // 플레이어 참가자
            }; // 목록 끝
            var arguments = new object[] // 도주 판정 인수
            { // 인수 시작
                player, actors, roll, false // 잘못된 난수와 출력
            }; // 인수 끝
            Assert.That(Rule("TryFlee").Invoke(null, arguments), Is.False); // 범위 밖 난수 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
