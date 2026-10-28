using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 순수 게임 규칙 형식
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class PersuasionTests // 조우 회유 판정 검증
    { // 클래스 시작
        private static System.Reflection.MethodInfo Rule() // 구현된 회유 판정 조회
        { // 조회 시작
            var type = typeof(GamePhase).Assembly.GetType("ProjectDelta.Core.PersuasionRules"); // 순수 회유 규칙 형식
            Assert.That(type, Is.Not.Null); // 회유 규칙 제공 확인
            var method = type.GetMethod("TryResolve"); // 실제 판정 메서드
            Assert.That(method, Is.Not.Null); // 회유 판정 제공 확인
            return method; // 확인한 판정 전달
        } // 조회 끝
        [TestCase(10, 10, true)] // 동점 성공
        [TestCase(1, 20, false)] // 낮은 판정 실패
        [TestCase(20, 1, true)] // 높은 판정 성공
        public void Resolve_UsesBothDiceAndTieSucceeds(int playerRoll, int enemyRoll, bool expected) // 양쪽 주사위와 동점 검증
        { // 검증 시작
            var args = new object[] // 시험 판정 인수
            { // 인수 시작
                10, 0, 0, 10, 0, 0, playerRoll, enemyRoll, false // 매력과 저항 및 주사위
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, args), Is.True); // 유효한 회유 판정
            Assert.That(args[8], Is.EqualTo(expected)); // 실제 성공 여부
        } // 검증 끝
        [TestCase(0, 1)] // 플레이어 주사위 범위 미만
        [TestCase(1, 21)] // 상대 주사위 범위 초과
        public void Resolve_RejectsInvalidDice(int playerRoll, int enemyRoll) // 잘못된 주사위 차단
        { // 검증 시작
            var args = new object[] // 잘못된 주사위 인수
            { // 인수 시작
                10, 0, 0, 10, 0, 0, playerRoll, enemyRoll, false // 시험 능력치와 주사위
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, args), Is.False); // 범위 밖 판정 거절
        } // 검증 끝
        [Test] // 모든 보정과 오버플로 검증
        public void Resolve_AppliesEachModifierWithoutOverflow() // 높은 값의 보정 합산 확인
        { // 검증 시작
            var args = new object[] // 큰 능력치와 보정
            { // 인수 시작
                int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, int.MinValue, int.MinValue, 1, 20, false // 정수 양쪽 경계
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, args), Is.True); // 큰 수의 정상 판정
            Assert.That(args[8], Is.True); // 넘침 없는 성공
            args = new object[] // 대상 보정의 영향 확인
            { // 인수 시작
                10, 2, 3, 10, 4, 2, 10, 10, false // 대상 보정이 하나 높은 판정
            }; // 인수 끝
            Assert.That(Rule().Invoke(null, args), Is.True); // 보정 판정 실행
            Assert.That(args[8], Is.False); // 상대 보정에 따른 실패
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
