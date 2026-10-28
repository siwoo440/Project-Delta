using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 문과 열쇠 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DoorLockStateTests // 잠긴 문과 열쇠 검증
    { // 클래스 시작
        [Test] // 열쇠 수 검증
        public void KeyRing_RejectsInvalidAmountsAndConsumesOne() // 열쇠 증감과 정수 범위 확인
        { // 검증 시작
            var keys = new DoorKeyRing(); // 빈 열쇠 보관
            Assert.That(keys.TryConsume(), Is.False); // 없는 열쇠 소모 차단
            Assert.That(keys.Add(0), Is.False); // 0개 추가 차단
            Assert.That(keys.Add(-1), Is.False); // 음수 추가 차단
            Assert.That(keys.Add(2), Is.True); // 열쇠 두 개 추가
            Assert.That(keys.TryConsume(), Is.True); // 열쇠 한 개 소모
            Assert.That(keys.Count, Is.EqualTo(1)); // 남은 열쇠 확인
        } // 검증 끝
        [Test] // 양방향 잠금과 복구 검증
        public void Lock_UsesOneKeyAndSharesBothDirections() // 열쇠 소모와 역방향 잠금 확인
        { // 검증 시작
            var layout = CreateLayout(); // 필수 방을 막지 않는 시험 구조
            var state = new DoorLockState(layout); // 시험 잠긴 문 선택
            var door = state.TrialDoor; // 선택된 시험 문
            Assert.That(door, Is.Not.Null); // 일반 말단 방 문 확인
            Assert.That(door.TargetRoomX, Is.EqualTo(1)); // 시험 방 가로 좌표
            Assert.That(door.TargetRoomY, Is.EqualTo(1)); // 시험 방 세로 좌표
            var reverse = layout.GetConnection(1, 1, 1, 0); // 반대 방향의 문
            var keys = new DoorKeyRing(); // 빈 열쇠 보관
            Assert.That(state.IsLocked(door) && state.IsLocked(reverse), Is.True); // 양방향 잠금 확인
            Assert.That(state.TryUnlock(door, keys), Is.False); // 열쇠 없는 개방 차단
            Assert.That(state.IsLocked(door), Is.True); // 열쇠 부족 시 잠금 유지
            Assert.That(keys.Add(1), Is.True); // 시험 열쇠 획득
            Assert.That(state.TryUnlock(reverse, keys), Is.True); // 반대 방향에서 잠금 해제
            Assert.That(keys.Count, Is.Zero); // 열쇠 한 개 소모 확인
            Assert.That(state.IsLocked(door) || state.IsLocked(reverse), Is.False); // 양방향 잠금 해제 확인
            Assert.That(state.TryUnlock(door, keys), Is.False); // 이미 열린 문 중복 소모 차단
            Assert.That(state.Rollback(door, new DoorKeyRing()), Is.False); // 다른 열쇠 보관소로 복구 차단
            Assert.That(state.Rollback(door, keys), Is.True); // 실패한 이동 복구
            Assert.That(keys.Count, Is.EqualTo(1)); // 소모된 열쇠 복구
            Assert.That(state.IsLocked(reverse), Is.True); // 양방향 잠금 복구
            Assert.That(state.Rollback(door, keys), Is.False); // 중복 복구 차단
            Assert.That(keys.Count, Is.EqualTo(1)); // 중복 열쇠 지급 방지
            Assert.That(state.TryUnlock(door, keys), Is.True); // 잠금 해제 재시도
            Assert.That(state.Commit(door, keys), Is.True); // 성공한 이동의 해제 확정
            Assert.That(state.Rollback(door, keys), Is.False); // 성공한 이동의 열쇠 중복 복구 차단
            Assert.That(keys.Count, Is.Zero); // 성공한 이동의 열쇠 소모 유지
        } // 검증 끝
        [Test] // 열쇠 한도에서 실패 복구 검증
        public void Reservation_ProtectsRefundAtMaximumCapacity() // 예약 중 열쇠 한도 보존
        { // 검증 시작
            var state = new DoorLockState(CreateLayout()); // 시험 잠긴 문 선택
            var keys = new DoorKeyRing(); // 열쇠 보관소
            Assert.That(keys.Add(int.MaxValue), Is.True); // 최대 열쇠 수 설정
            Assert.That(state.TryUnlock(state.TrialDoor, keys), Is.True); // 열쇠 한 개 예약
            Assert.That(keys.Add(1), Is.False); // 예약된 열쇠의 복구 공간 보존
            Assert.That(state.Rollback(state.TrialDoor, keys), Is.True); // 실패한 이동의 예약 복구
            Assert.That(keys.Count, Is.EqualTo(int.MaxValue)); // 원래 열쇠 수 복구
        } // 검증 끝
        [Test] // 시험 문 후보 누락 검증
        public void Lock_NoNormalLeaf_DoesNotBlockRequiredRooms() // 필수 진행 방의 임의 잠금 방지
        { // 검증 시작
            var full = CreateLayout(); // 시험 구조 생성
            var rooms = new[] // 말단 일반 방을 뺀 구조
            { // 방 목록 시작
                full.GetRoom(0, 0), // 시작 방
                full.GetRoom(1, 0), // 중간 일반 방
                full.GetRoom(2, 0) // 계단 방
            }; // 방 목록 끝
            var doors = new[] // 필수 진행 경로의 양방향 문
            { // 문 목록 시작
                full.GetConnection(0, 0, 1, 0), // 시작 방 출구
                full.GetConnection(1, 0, 0, 0), // 시작 방 복귀
                full.GetConnection(1, 0, 2, 0), // 계단 방 출구
                full.GetConnection(2, 0, 1, 0) // 계단 방 복귀
            }; // 문 목록 끝
            var state = new DoorLockState(new DungeonLayout(rooms, doors)); // 필수 경로 전용 구조
            Assert.That(state.TrialDoor, Is.Null); // 임의의 필수 경로 잠금 방지
        } // 검증 끝
        private static DungeonLayout CreateLayout() // 시험용 방과 양방향 출구 생성
        { // 생성 시작
            var rooms = new[] // 시험 방 목록
            { // 방 목록 시작
                new DungeonRoom(0, 0, false, RoomKind.Start), // 시작 방
                new DungeonRoom(1, 0), // 중간 일반 방
                new DungeonRoom(2, 0, false, RoomKind.Stairs), // 계단 방
                new DungeonRoom(1, 1) // 말단 일반 방
            }; // 방 목록 끝
            var doors = new[] // 모든 양방향 출구
            { // 문 목록 시작
                new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 시작에서 중간 방
                new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0), // 중간에서 시작 방
                new RoomDoor(1, 0, 5, 0, MoveDirection.Right, 2, 0, -5, 0), // 중간에서 계단 방
                new RoomDoor(2, 0, -5, 0, MoveDirection.Left, 1, 0, 5, 0), // 계단에서 중간 방
                new RoomDoor(1, 0, 2, 3, MoveDirection.Up, 1, 1, 2, -3), // 중간에서 말단 방
                new RoomDoor(1, 1, 2, -3, MoveDirection.Down, 1, 0, 2, 3) // 말단에서 중간 방
            }; // 문 목록 끝
            return new DungeonLayout(rooms, doors); // 양방향 연결 구조
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
