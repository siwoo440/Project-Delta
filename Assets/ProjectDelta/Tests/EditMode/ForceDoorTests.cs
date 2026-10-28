using System; // 형식 조회
using System.Linq; // 시험 구조 선택
using System.Reflection; // 구현 전 규칙 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 던전과 문 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ForceDoorTests // 이벤트 판정과 강제 개방 검증
    { // 클래스 시작
        private static MethodInfo EventRule(string name) // 일반 이벤트 규칙 조회
        { // 조회 시작
            var type = typeof(DoorLockState).Assembly.GetType("ProjectDelta.Core.EventCheck"); // 순수 이벤트 판정 형식
            Assert.That(type, Is.Not.Null); // 이벤트 판정 규칙 확인
            var method = type.GetMethod(name); // 지정 규칙 조회
            Assert.That(method, Is.Not.Null); // 지정 규칙 확인
            return method; // 확인한 규칙 전달
        } // 조회 끝
        [TestCase(1, 1, true)] // 동점 성공
        [TestCase(1, 20, false)] // 낮은 판정 실패
        [TestCase(20, 1, true)] // 높은 판정 성공
        public void EventCheck_UsesTwoDiceAndTieSucceeds(int playerRoll, int eventRoll, bool success) // 기존 이벤트 주사위와 동점 확인
        { // 검증 시작
            var arguments = new object[] // 이벤트 판정 인수
            { // 인수 시작
                50, 0, 50, playerRoll, eventRoll, false // 공격력과 보정 및 주사위
            }; // 인수 끝
            Assert.That(EventRule("TryResolve").Invoke(null, arguments), Is.True); // 유효한 판정 처리
            Assert.That(arguments[5], Is.EqualTo(success)); // 실제 성공과 실패 확인
        } // 검증 끝
        [TestCase(0, 1)] // 첫 주사위 하한 미만
        [TestCase(1, 21)] // 두 번째 주사위 상한 초과
        public void EventCheck_RejectsInvalidDice(int playerRoll, int eventRoll) // 잘못된 주사위 차단
        { // 검증 시작
            var arguments = new object[] // 이벤트 판정 인수
            { // 인수 시작
                50, 0, 50, playerRoll, eventRoll, false // 공격력과 잘못된 주사위
            }; // 인수 끝
            Assert.That(EventRule("TryResolve").Invoke(null, arguments), Is.False); // 유효하지 않은 판정 거절
        } // 검증 끝
        [Test] // 정확한 확률 계산 검증
        public void SuccessCount_UsesAllFourHundredDicePairs() // 동점과 확률 제한 없음 확인
        { // 검증 시작
            var method = EventRule("SuccessCount"); // 성공 조합 계산 조회
            var arguments = new object[] // 공격력과 이벤트 난이도
            { // 인수 시작
                50, 0, 50 // 동등한 시험 수치
            }; // 인수 끝
            Assert.That(method.Invoke(null, arguments), Is.EqualTo(210)); // 동점 포함 성공 조합
            arguments[0] = int.MaxValue; // 최대 공격력
            arguments[2] = int.MinValue; // 최소 난이도
            Assert.That(method.Invoke(null, arguments), Is.EqualTo(400)); // 정수 범위에서 100% 확인
            arguments[0] = int.MinValue; // 최소 공격력
            arguments[2] = int.MaxValue; // 최대 난이도
            Assert.That(method.Invoke(null, arguments), Is.Zero); // 정수 범위에서 0% 확인
        } // 검증 끝
        [Test] // 강제 개방 실패와 열쇠 검증
        public void Force_FailureIsOneAttemptAndKeyStillWorks() // 실패 이후 재시도 차단과 열쇠 사용
        { // 검증 시작
            var seed = Enumerable.Range(0, 100).First(value => new DoorLockState(DungeonGenerator.Generate(value)).TrialDoor != null); // 말단 시험 방이 있는 시드
            var layout = DungeonGenerator.Generate(seed); // 실제 생성 구조
            var state = new DoorLockState(layout); // 현재 층 잠긴 문
            var door = state.TrialDoor; // 시험 잠긴 문
            var reverse = layout.GetConnection(door.TargetRoomX, door.TargetRoomY, door.RoomX, door.RoomY); // 역방향 연결
            var method = typeof(DoorLockState).GetMethod("TryForce"); // 강제 개방 처리 조회
            Assert.That(method, Is.Not.Null); // 강제 개방 제공 확인
            var arguments = new object[] // 강제 개방 인수
            { // 인수 시작
                door, 50, 0, 50, 0, 20, false // 잘못된 첫 주사위
            }; // 인수 끝
            Assert.That(method.Invoke(state, arguments), Is.False); // 잘못된 난수 거절
            arguments[4] = 1; // 유효한 실패 주사위
            Assert.That(method.Invoke(state, arguments), Is.True); // 첫 강제 개방 처리
            Assert.That(arguments[6], Is.False); // 강제 개방 실패 확인
            Assert.That(state.IsLocked(reverse), Is.True); // 양방향 잠금 유지
            arguments[0] = reverse; // 반대편 문의 동일 시도
            arguments[4] = 20; // 성공 가능한 주사위
            Assert.That(method.Invoke(state, arguments), Is.False); // 역방향 재시도 차단
            var keys = new DoorKeyRing(); // 시험 열쇠 보관소
            Assert.That(keys.Add(1), Is.True); // 시험 열쇠 획득
            Assert.That(state.TryUnlock(door, keys), Is.True); // 실패 후 열쇠 사용 가능
            Assert.That(state.Commit(door, keys), Is.True); // 성공한 열쇠 개방 확정
            Assert.That(state.IsLocked(reverse), Is.False); // 열쇠의 양방향 잠금 해제
        } // 검증 끝
        [Test] // 강제 개방 성공 검증
        public void Force_SuccessUnlocksBothDirectionsWithoutKey() // 동점 성공과 양방향 개방
        { // 검증 시작
            var seed = Enumerable.Range(0, 100).First(value => new DoorLockState(DungeonGenerator.Generate(value)).TrialDoor != null); // 말단 시험 방이 있는 시드
            var layout = DungeonGenerator.Generate(seed); // 실제 생성 구조
            var state = new DoorLockState(layout); // 현재 층 잠긴 문
            var door = state.TrialDoor; // 시험 잠긴 문
            var reverse = layout.GetConnection(door.TargetRoomX, door.TargetRoomY, door.RoomX, door.RoomY); // 역방향 연결
            var method = typeof(DoorLockState).GetMethod("TryForce"); // 강제 개방 처리 조회
            Assert.That(method, Is.Not.Null); // 강제 개방 제공 확인
            var arguments = new object[] // 강제 개방 인수
            { // 인수 시작
                door, 50, 0, 50, 7, 7, false // 동점 성공 판정
            }; // 인수 끝
            Assert.That(method.Invoke(state, arguments), Is.True); // 유효한 강제 개방 처리
            Assert.That(arguments[6], Is.True); // 동점 성공 확인
            Assert.That(state.IsLocked(door) || state.IsLocked(reverse), Is.False); // 양방향 잠금 해제
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
