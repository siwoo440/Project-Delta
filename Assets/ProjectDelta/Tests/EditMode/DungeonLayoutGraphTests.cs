using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 도메인 던전 레이아웃 그래프 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class DungeonLayoutGraphTests // 던전 레이아웃 그래프 테스트 모음
    {
        [Test] // 방 추가 및 좌표 조회 테스트
        public void AddRoom_CanBeFoundByIdAndCoordinate() // 식별자·좌표 양쪽으로 조회되는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트용 빈 그래프 생성
            RoomNode node = graph.AddRoom("Room_01", "ROOM_DEF_01", new GridPosition(0, 0)); // 원점에 방 추가

            Assert.IsTrue(graph.TryGetRoom("Room_01", out RoomNode byId)); // 식별자 조회 성공 확인
            Assert.AreSame(node, byId); // 같은 노드인지 확인

            Assert.IsTrue(graph.TryGetRoomAt(new GridPosition(0, 0), out RoomNode byCoordinate)); // 좌표 조회 성공 확인
            Assert.AreSame(node, byCoordinate); // 같은 노드인지 확인
        }

        [Test] // 중복 좌표 차단 테스트
        public void AddRoom_DuplicateCoordinate_Throws() // 같은 좌표에 두 번째 방을 추가하면 예외가 발생하는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트용 빈 그래프 생성
            graph.AddRoom("Room_01", "ROOM_DEF_01", new GridPosition(0, 0)); // 첫 번째 방 추가

            Assert.Throws<System.InvalidOperationException>(() => // 같은 좌표 재사용 시도
                graph.AddRoom("Room_02", "ROOM_DEF_01", new GridPosition(0, 0))); // 좌표 중복 예외 확인
        }

        [Test] // 양방향 연결 테스트
        public void Connect_CreatesBidirectionalEdge() // 한쪽에서 연결하면 반대쪽에도 자동으로 연결되는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트용 빈 그래프 생성
            RoomNode roomA = graph.AddRoom("Room_A", "ROOM_DEF_01", new GridPosition(0, 0)); // A방 추가
            RoomNode roomB = graph.AddRoom("Room_B", "ROOM_DEF_01", new GridPosition(0, 1)); // A방 북쪽에 B방 추가

            graph.Connect(roomA, CardinalDirection.North, roomB); // A -> 북쪽 -> B 연결

            Assert.IsTrue(roomA.TryGetConnection(CardinalDirection.North, out RoomConnectionEdge fromA)); // A방 북쪽 연결 확인
            Assert.AreSame(roomB, fromA.Neighbor); // A방 북쪽 이웃이 B방인지 확인

            Assert.IsTrue(roomB.TryGetConnection(CardinalDirection.South, out RoomConnectionEdge fromB)); // B방 남쪽 연결 확인 (자동 반대 방향)
            Assert.AreSame(roomA, fromB.Neighbor); // B방 남쪽 이웃이 A방인지 확인
        }

        [Test] // 잠긴 연결 정보 전달 테스트
        public void Connect_LockedFlag_PropagatesToBothEnds() // 잠금 여부가 양쪽 연결 정보에 동일하게 반영되는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트용 빈 그래프 생성
            RoomNode roomA = graph.AddRoom("Room_A", "ROOM_DEF_01", new GridPosition(0, 0)); // A방 추가
            RoomNode roomB = graph.AddRoom("Room_B", "ROOM_DEF_01", new GridPosition(1, 0)); // A방 동쪽에 B방 추가

            graph.Connect(roomA, CardinalDirection.East, roomB, isLocked: true); // 잠긴 문으로 연결

            roomA.TryGetConnection(CardinalDirection.East, out RoomConnectionEdge fromA); // A방 동쪽 연결 조회
            roomB.TryGetConnection(CardinalDirection.West, out RoomConnectionEdge fromB); // B방 서쪽 연결 조회

            Assert.IsTrue(fromA.IsLocked); // A방 쪽 잠김 여부 확인
            Assert.IsTrue(fromB.IsLocked); // B방 쪽 잠김 여부 확인
        }

        [Test] // 연결되지 않은 방향 조회 테스트
        public void TryGetConnection_UnconnectedDirection_ReturnsFalse() // 연결이 없는 방향을 조회하면 실패하는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트용 빈 그래프 생성
            RoomNode roomA = graph.AddRoom("Room_A", "ROOM_DEF_01", new GridPosition(0, 0)); // A방만 추가 (연결 없음)

            Assert.IsFalse(roomA.TryGetConnection(CardinalDirection.North, out _)); // 연결 없음 확인
        }
    }
}
