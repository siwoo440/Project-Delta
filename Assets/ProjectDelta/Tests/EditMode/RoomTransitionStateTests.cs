using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 방 연결 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RoomTransitionStateTests // 방 이동 규칙 검증
    { // 클래스 시작
        private static RoomDoor EastDoor() // 오른쪽 시험 문
        { // 생성 시작
            return new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0); // 첫 방에서 두 번째 방 연결
        } // 생성 끝
        [Test] // 개별 검증 표시
        public void ValidDoor_ChangesRoomAndChecksDestination() // 문 앞의 정상 전환 검증
        { // 검증 시작
            var state = new RoomTransitionState(0, 0); // 첫 번째 방 시작
            var checkedDestination = false; // 도착 검사 기록
            var moved = state.TryTransition(EastDoor(), 5, 0, MoveDirection.Right, (roomX, roomY, x, y) => // 실제 목적 칸 판정
            { // 판정 함수 시작
                checkedDestination = roomX == 1 && roomY == 0 && x == -5 && y == 0; // 방과 내부 좌표 검사
                return checkedDestination; // 유효한 도착 칸 승인
            }); // 판정 함수 끝
            Assert.That(moved, Is.True); // 전환 성공 확인
            Assert.That(checkedDestination, Is.True); // 실제 도착 좌표 검사 확인
            Assert.That(state.RoomX, Is.EqualTo(1)); // 도착 방 좌표 확인
            Assert.That(state.RoomY, Is.EqualTo(0)); // 도착 방 세로 좌표 확인
        } // 검증 끝
        [TestCase(4, 0, MoveDirection.Right)] // 문과 떨어진 칸
        [TestCase(5, 0, MoveDirection.Left)] // 반대 방향 표시
        [TestCase(5, 1, MoveDirection.Right)] // 문 옆 칸
        public void WrongApproachOrDirection_LeavesRoomUnchanged(int x, int y, MoveDirection facing) // 잘못된 문 접근 차단
        { // 검증 시작
            var state = new RoomTransitionState(0, 0); // 첫 번째 방 시작
            Assert.That(state.TryTransition(EastDoor(), x, y, facing, (a, b, c, d) => true), Is.False); // 잘못된 접근 차단
            Assert.That(state.RoomX, Is.EqualTo(0)); // 기존 방 보존
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void InvalidDestination_LeavesRoomUnchanged() // 도착 불가능한 칸 차단
        { // 검증 시작
            var state = new RoomTransitionState(0, 0); // 첫 번째 방 시작
            Assert.That(state.TryTransition(EastDoor(), 5, 0, MoveDirection.Right, (a, b, c, d) => false), Is.False); // 장애물이나 바닥 없음 차단
            Assert.That(state.RoomX, Is.EqualTo(0)); // 기존 방 보존
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void DoorFromAnotherRoom_IsRejected() // 출발 방 불일치 차단
        { // 검증 시작
            var state = new RoomTransitionState(1, 0); // 두 번째 방 시작
            Assert.That(state.TryTransition(EastDoor(), 5, 0, MoveDirection.Right, (a, b, c, d) => true), Is.False); // 반대 방의 문 차단
            Assert.That(state.RoomX, Is.EqualTo(1)); // 기존 방 보존
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void NonAdjacentOrWrongDirection_IsRejected() // 대각선과 잘못된 연결 차단
        { // 검증 시작
            var state = new RoomTransitionState(0, 0); // 첫 번째 방 시작
            var diagonal = new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 1, -5, 0); // 대각 방의 잘못된 연결
            var opposite = new RoomDoor(0, 0, 5, 0, MoveDirection.Right, -1, 0, -5, 0); // 방향이 반대인 연결
            Assert.That(state.TryTransition(diagonal, 5, 0, MoveDirection.Right, (a, b, c, d) => true), Is.False); // 대각선 차단
            Assert.That(state.TryTransition(opposite, 5, 0, MoveDirection.Right, (a, b, c, d) => true), Is.False); // 반대 방향 연결 차단
            Assert.That(state.RoomX, Is.EqualTo(0)); // 기존 방 유지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void OppositeDoor_ReturnsToFirstRoom() // 역방향 문 연결 검증
        { // 검증 시작
            var state = new RoomTransitionState(1, 0); // 두 번째 방 시작
            var west = new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0); // 첫 번째 방으로 돌아가는 문
            Assert.That(state.TryTransition(west, -5, 0, MoveDirection.Left, (a, b, c, d) => true), Is.True); // 역방향 이동 성공
            Assert.That(state.RoomX, Is.EqualTo(0)); // 출발 방 복귀 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
