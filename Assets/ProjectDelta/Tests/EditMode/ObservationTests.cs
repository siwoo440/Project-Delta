using System.Reflection; // 구현 전 규칙 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 게임 규칙 형식
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ObservationTests // 조우 관찰 판정과 회차 기록 검증
    { // 클래스 시작
        private static System.Type RuleType(string name) // 순수 관찰 형식 조회
        { // 조회 시작
            var type = typeof(GamePhase).Assembly.GetType("ProjectDelta.Core." + name); // 게임 규칙 형식
            Assert.That(type, Is.Not.Null); // 규칙 형식 제공 확인
            return type; // 확인한 형식 전달
        } // 조회 끝
        [TestCase(10, 10, true)] // 동점 관찰 성공
        [TestCase(1, 20, false)] // 낮은 관찰 판정 실패
        [TestCase(20, 1, true)] // 높은 관찰 판정 성공
        public void Resolve_UsesSpeedResistanceAndTieSucceeds(int playerRoll, int enemyRoll, bool expected) // 관찰 주사위와 동점 확인
        { // 검증 시작
            var method = RuleType("ObservationRules").GetMethod("TryResolve"); // 관찰 판정 조회
            Assert.That(method, Is.Not.Null); // 관찰 판정 제공 확인
            var arguments = new object[] // 관찰 판정 인수
            { // 인수 목록 시작
                10, 0, 10, 0, playerRoll, enemyRoll, false // 시험 속도와 저항 및 주사위
            }; // 인수 목록 끝
            Assert.That(method.Invoke(null, arguments), Is.True); // 유효한 관찰 판정
            Assert.That(arguments[6], Is.EqualTo(expected)); // 실제 관찰 성공 여부
        } // 검증 끝
        [TestCase(0, 1)] // 플레이어 주사위 하한 미만
        [TestCase(1, 21)] // 상대 주사위 상한 초과
        public void Resolve_RejectsInvalidDice(int playerRoll, int enemyRoll) // 잘못된 주사위 차단
        { // 검증 시작
            var method = RuleType("ObservationRules").GetMethod("TryResolve"); // 관찰 판정 조회
            var arguments = new object[] // 관찰 판정 인수
            { // 인수 목록 시작
                10, 0, 10, 0, playerRoll, enemyRoll, false // 범위 밖 주사위
            }; // 인수 목록 끝
            Assert.That(method.Invoke(null, arguments), Is.False); // 유효하지 않은 판정 거절
        } // 검증 끝
        [Test] // 보정과 정수 범위 검증
        public void Resolve_AppliesModifiersWithoutOverflow() // 관찰 보정과 큰 수치 확인
        { // 검증 시작
            var method = RuleType("ObservationRules").GetMethod("TryResolve"); // 관찰 판정 조회
            var arguments = new object[] // 관찰 판정 인수
            { // 인수 목록 시작
                int.MaxValue, int.MaxValue, int.MinValue, int.MinValue, 1, 20, false // 큰 능력치와 보정
            }; // 인수 목록 끝
            Assert.That(method.Invoke(null, arguments), Is.True); // 정수 범위의 유효한 판정
            Assert.That(arguments[6], Is.True); // 넘침 없는 성공 확인
        } // 검증 끝
        [Test] // 회차별 관찰 기록 검증
        public void Journal_RecordsOnceAndRejectsEmptyIds() // 대상별 중복과 잘못된 식별자 확인
        { // 검증 시작
            var type = RuleType("ObservationJournal"); // 회차 관찰 기록 형식
            var journal = System.Activator.CreateInstance(type); // 빈 관찰 기록 생성
            var record = type.GetMethod("TryRecord"); // 관찰 기록 추가 조회
            var contains = type.GetMethod("Contains"); // 관찰 기록 확인 조회
            Assert.That(record, Is.Not.Null); // 관찰 기록 추가 제공 확인
            Assert.That(contains, Is.Not.Null); // 관찰 기록 조회 제공 확인
            var arguments = new object[] // 대상 식별자 인수
            { // 인수 목록 시작
                "trial-elite" // 시험 대상 식별자
            }; // 인수 목록 끝
            Assert.That(contains.Invoke(journal, arguments), Is.False); // 처음에는 관찰 기록 없음
            Assert.That(record.Invoke(journal, arguments), Is.True); // 첫 관찰 기록 추가
            Assert.That(contains.Invoke(journal, arguments), Is.True); // 관찰 기록 보존
            Assert.That(record.Invoke(journal, arguments), Is.False); // 중복 관찰 기록 차단
            arguments[0] = " "; // 공백 식별자
            Assert.That(record.Invoke(journal, arguments), Is.False); // 잘못된 대상 기록 차단
            var fresh = System.Activator.CreateInstance(type); // 새 회차 기록 생성
            arguments[0] = "trial-elite"; // 기존 시험 대상 식별자
            Assert.That(contains.Invoke(fresh, arguments), Is.False); // 새 회차의 독립된 빈 기록
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
