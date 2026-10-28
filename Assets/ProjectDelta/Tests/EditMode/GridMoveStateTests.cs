using System; // 좌표 범위 계산
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 격자 이동 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class GridMoveStateTests // 논리 이동 규칙 검증
    { // 클래스 시작
        [TestCase(MoveDirection.Up, 0, 1)] // 위쪽 이동 기준
        [TestCase(MoveDirection.Down, 0, -1)] // 아래쪽 이동 기준
        [TestCase(MoveDirection.Left, -1, 0)] // 왼쪽 이동 기준
        [TestCase(MoveDirection.Right, 1, 0)] // 오른쪽 이동 기준
        public void CardinalInput_CommitsExactlyOneCell(MoveDirection direction, int expectedX, int expectedY) // 네 방향 한 칸 이동 검증
        { // 검증 시작
            var move = new GridMoveState(0, 0, IsOpen); // 시작 칸과 시험 영역
            Assert.That(move.RequestMove(direction), Is.EqualTo(MoveRequestResult.Started)); // 이동 시작 확인
            Assert.That(move.X, Is.EqualTo(expectedX)); // 가로 칸 확정 확인
            Assert.That(move.Y, Is.EqualTo(expectedY)); // 세로 칸 확정 확인
            Assert.That(move.Facing, Is.EqualTo(direction)); // 방향 표시 기준 확인
            Assert.That(move.IsMoving, Is.True); // 화면 이동 중 상태 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void BoundaryAndObstacle_KeepPositionAndFacing() // 벽과 장애물 차단 검증
        { // 검증 시작
            var move = new GridMoveState(1, 0, IsOpen); // 경계 옆 시작 칸
            Assert.That(move.RequestMove(MoveDirection.Right), Is.EqualTo(MoveRequestResult.Rejected)); // 방 밖 이동 차단 확인
            Assert.That(move.RequestMove(MoveDirection.Up), Is.EqualTo(MoveRequestResult.Rejected)); // 장애물 이동 차단 확인
            Assert.That(move.X, Is.EqualTo(1)); // 가로 위치 보존 확인
            Assert.That(move.Y, Is.EqualTo(0)); // 세로 위치 보존 확인
            Assert.That(move.Facing, Is.EqualTo(MoveDirection.Down)); // 기존 방향 보존 확인
            Assert.That(move.IsMoving, Is.False); // 이동 미시작 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Moving_AcceptsOnlyOneBufferedCommand() // 한 칸 입력 대기 검증
        { // 검증 시작
            var move = new GridMoveState(0, -1, IsOpen); // 두 칸 이동 가능한 시작점
            Assert.That(move.RequestMove(MoveDirection.Up), Is.EqualTo(MoveRequestResult.Started)); // 첫 이동 시작 확인
            Assert.That(move.RequestMove(MoveDirection.Right), Is.EqualTo(MoveRequestResult.Buffered)); // 첫 대기 입력 확인
            Assert.That(move.RequestMove(MoveDirection.Left), Is.EqualTo(MoveRequestResult.Rejected)); // 추가 대기 입력 거절 확인
            Assert.That(move.X, Is.EqualTo(0)); // 중간 가로 위치 중복 갱신 방지
            Assert.That(move.Y, Is.EqualTo(0)); // 중간 세로 위치 중복 갱신 방지
            Assert.That(move.FinishMove(), Is.True); // 첫 대기 입력 실행 확인
            Assert.That(move.X, Is.EqualTo(1)); // 대기 이동의 가로 위치 확인
            Assert.That(move.Y, Is.EqualTo(0)); // 대기 이동의 세로 위치 확인
            Assert.That(move.Facing, Is.EqualTo(MoveDirection.Right)); // 대기 이동 방향 확인
            Assert.That(move.FinishMove(), Is.False); // 추가 대기 입력 없음 확인
            Assert.That(move.IsMoving, Is.False); // 최종 이동 완료 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void BlockedBufferedCommand_DoesNotStartSecondMove() // 막힌 대기 이동 검증
        { // 검증 시작
            var move = new GridMoveState(0, 0, IsOpen); // 시작 칸 준비
            Assert.That(move.RequestMove(MoveDirection.Up), Is.EqualTo(MoveRequestResult.Started)); // 첫 이동 확인
            Assert.That(move.RequestMove(MoveDirection.Right), Is.EqualTo(MoveRequestResult.Buffered)); // 장애물 방향 대기
            Assert.That(move.FinishMove(), Is.False); // 막힌 대기 이동 거절 확인
            Assert.That(move.X, Is.EqualTo(0)); // 막힌 가로 이동 방지
            Assert.That(move.Y, Is.EqualTo(1)); // 이전 세로 위치 보존
            Assert.That(move.IsMoving, Is.False); // 이동 종료 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void InputLock_DiscardsPendingCommand() // 입력 잠금의 대기 취소 검증
        { // 검증 시작
            var move = new GridMoveState(0, -1, IsOpen); // 시작 칸 준비
            Assert.That(move.RequestMove(MoveDirection.Up), Is.EqualTo(MoveRequestResult.Started)); // 첫 이동 확인
            Assert.That(move.RequestMove(MoveDirection.Right), Is.EqualTo(MoveRequestResult.Buffered)); // 대기 입력 확인
            move.DiscardBufferedMove(); // 입력 잠금 반영
            Assert.That(move.FinishMove(), Is.False); // 취소된 입력 미실행 확인
            Assert.That(move.X, Is.EqualTo(0)); // 취소된 가로 이동 방지
            Assert.That(move.Y, Is.EqualTo(0)); // 첫 이동 결과 보존
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void InvalidDirection_AndCompletionWhileIdle_DoNothing() // 잘못된 명령 차단 검증
        { // 검증 시작
            var move = new GridMoveState(0, 0, IsOpen); // 시작 칸 준비
            Assert.That(move.RequestMove(MoveDirection.None), Is.EqualTo(MoveRequestResult.Rejected)); // 입력 없음 차단
            Assert.That(move.RequestMove((MoveDirection)99), Is.EqualTo(MoveRequestResult.Rejected)); // 알 수 없는 방향 차단
            Assert.That(move.FinishMove(), Is.False); // 대기 상태 완료 호출 차단
            Assert.That(move.X, Is.EqualTo(0)); // 가로 위치 보존 확인
            Assert.That(move.Y, Is.EqualTo(0)); // 세로 위치 보존 확인
        } // 검증 끝
        private static bool IsOpen(int x, int y) // 시험 방과 장애물 규칙
        { // 판정 시작
            return Math.Abs(x) <= 1 && Math.Abs(y) <= 1 && !(x == 1 && y == 1); // 세 칸 범위와 장애물 판정
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
