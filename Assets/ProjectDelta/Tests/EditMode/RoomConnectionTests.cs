using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 도메인 방 연결 규칙 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class RoomConnectionTests // 테스트 방 연결 규칙 테스트
    {
        private RoomConnection connection; // 공통 양방향 연결 데이터

        [SetUp] // 각 테스트 전 연결 데이터 구성
        public void SetUp() // A 북쪽과 B 남쪽 연결 생성
        {
            RoomConnectionEnd endA = new RoomConnectionEnd("TestRoom_A", new GridPosition(0, 2), CardinalDirection.North); // A방 북쪽 연결 끝 생성
            RoomConnectionEnd endB = new RoomConnectionEnd("TestRoom_B", new GridPosition(0, -2), CardinalDirection.South); // B방 남쪽 연결 끝 생성
            connection = new RoomConnection(endA, endB); // 양방향 연결 생성
        }

        [Test] // A에서 B 이동 테스트
        public void FromA_NorthBoundary_ReturnsBEntry() // A 북쪽 경계에서 B 남쪽 입구를 반환하는지 확인
        {
            bool result = connection.TryGetDestination("TestRoom_A", new GridPosition(0, 2), CardinalDirection.North, out string roomId, out GridPosition entry); // A방 북쪽 이동 대상 조회
            Assert.IsTrue(result); // 연결 성공 확인
            Assert.AreEqual("TestRoom_B", roomId); // B방 식별자 확인
            Assert.AreEqual(0, entry.X); // B방 입구 X 확인
            Assert.AreEqual(-2, entry.Z); // B방 입구 Z 확인
        }

        [Test] // B에서 A 복귀 테스트
        public void FromB_SouthBoundary_ReturnsAEntry() // B 남쪽 경계에서 A 북쪽 입구를 반환하는지 확인
        {
            bool result = connection.TryGetDestination("TestRoom_B", new GridPosition(0, -2), CardinalDirection.South, out string roomId, out GridPosition entry); // B방 남쪽 이동 대상 조회
            Assert.IsTrue(result); // 연결 성공 확인
            Assert.AreEqual("TestRoom_A", roomId); // A방 식별자 확인
            Assert.AreEqual(0, entry.X); // A방 입구 X 확인
            Assert.AreEqual(2, entry.Z); // A방 입구 Z 확인
        }

        [Test] // 잘못된 방향 차단 테스트
        public void WrongDirection_IsRejected() // 경계 칸에서 다른 방향으로 이동하면 연결되지 않는지 확인
        {
            bool result = connection.TryGetDestination("TestRoom_A", new GridPosition(0, 2), CardinalDirection.East, out _, out _); // 잘못된 출구 방향 조회
            Assert.IsFalse(result); // 연결 실패 확인
        }

        [Test] // 잘못된 경계 칸 차단 테스트
        public void WrongBoundaryPosition_IsRejected() // 출구 방향이 같아도 다른 칸이면 연결되지 않는지 확인
        {
            bool result = connection.TryGetDestination("TestRoom_A", new GridPosition(1, 2), CardinalDirection.North, out _, out _); // 잘못된 경계 칸 조회
            Assert.IsFalse(result); // 연결 실패 확인
        }

        [Test] // 연결되지 않은 방 차단 테스트
        public void UnknownRoom_IsRejected() // 등록되지 않은 방에서는 연결이 발생하지 않는지 확인
        {
            bool result = connection.TryGetDestination("UnknownRoom", new GridPosition(0, 2), CardinalDirection.North, out _, out _); // 미등록 방 연결 조회
            Assert.IsFalse(result); // 연결 실패 확인
        }
    }
}
