using System;
using System.Collections.Generic; // 목록 기능 사용
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 던전 생성 도메인 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class LoopConnectionTests // 34일차 루프·출구 정렬·중복 연결 테스트
    {
        [Test]
        public void Settings_LoopChanceOutsideRange_Throws() // 루프 확률 범위 검증
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DungeonGenerationSettings(8, 4, 4, loopChance: 1.1d)); // 1 초과 확률 차단 확인
        }

        [Test]
        public void TryConnect_WithExactExitPair_StoresBothRoomExits() // 실제 연결에 사용된 출구 쌍을 그래프가 보존하는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트 그래프
            RoomNode left = graph.AddRoom("LEFT", "ROOM_LEFT", new GridPosition(0, 0)); // 왼쪽 방
            RoomNode right = graph.AddRoom("RIGHT", "ROOM_RIGHT", new GridPosition(1, 0)); // 오른쪽 인접 방
            RoomExit east = new RoomExit(new GridPosition(2, 0), CardinalDirection.East); // 왼쪽 방 동쪽 문
            RoomExit west = new RoomExit(new GridPosition(-2, 0), CardinalDirection.West); // 오른쪽 방 서쪽 문

            bool connected = graph.TryConnect(left, east, right, west); // 정확한 출구 쌍 연결

            Assert.IsTrue(connected); // 연결 성공 확인
            Assert.IsTrue(left.TryGetConnection(CardinalDirection.East, out RoomConnectionEdge leftEdge)); // 왼쪽 연결 조회
            Assert.IsTrue(right.TryGetConnection(CardinalDirection.West, out RoomConnectionEdge rightEdge)); // 오른쪽 연결 조회
            Assert.IsTrue(leftEdge.HasExactExitPair); // 정확한 출구 정보 보유 확인
            Assert.AreEqual(east, leftEdge.LocalExit.Value); // 왼쪽 출구 저장 확인
            Assert.AreEqual(west, leftEdge.NeighborExit.Value); // 오른쪽 출구 저장 확인
            Assert.AreEqual(west, rightEdge.LocalExit.Value); // 반대 Edge의 로컬 출구 확인
            Assert.AreEqual(east, rightEdge.NeighborExit.Value); // 반대 Edge의 이웃 출구 확인
        }

        [Test]
        public void TryConnect_MisalignedExitPair_IsRejected() // 방향은 반대지만 문 위치 축이 다른 경우 연결을 막는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트 그래프
            RoomNode left = graph.AddRoom("LEFT", "ROOM_LEFT", new GridPosition(0, 0)); // 왼쪽 방
            RoomNode right = graph.AddRoom("RIGHT", "ROOM_RIGHT", new GridPosition(1, 0)); // 오른쪽 방
            RoomExit east = new RoomExit(new GridPosition(2, 1), CardinalDirection.East); // Z=1 동쪽 문
            RoomExit west = new RoomExit(new GridPosition(-2, 0), CardinalDirection.West); // Z=0 서쪽 문

            bool connected = graph.TryConnect(left, east, right, west); // 어긋난 문 연결 시도

            Assert.IsFalse(connected); // 연결 거부 확인
            Assert.AreEqual(0, left.Connections.Count); // 기존 방 연결 오염 없음 확인
            Assert.AreEqual(0, right.Connections.Count); // 이웃 방 연결 오염 없음 확인
        }

        [Test]
        public void TryConnect_DuplicateConnection_DoesNotOverwriteExistingEdge() // 같은 방향 연결을 다시 시도해도 기존 연결을 덮어쓰지 않는지 확인
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트 그래프
            RoomNode left = graph.AddRoom("LEFT", "ROOM_LEFT", new GridPosition(0, 0)); // 왼쪽 방
            RoomNode right = graph.AddRoom("RIGHT", "ROOM_RIGHT", new GridPosition(1, 0)); // 오른쪽 방
            RoomExit east = new RoomExit(new GridPosition(2, 0), CardinalDirection.East); // 동쪽 문
            RoomExit west = new RoomExit(new GridPosition(-2, 0), CardinalDirection.West); // 서쪽 문

            Assert.IsTrue(graph.TryConnect(left, east, right, west)); // 첫 연결 성공
            Assert.IsFalse(graph.TryConnect(left, east, right, west)); // 같은 연결 재시도 거부
            Assert.IsTrue(left.TryGetConnection(CardinalDirection.East, out RoomConnectionEdge edge)); // 기존 연결 조회
            Assert.AreSame(right, edge.Neighbor); // 기존 대상이 그대로인지 확인
        }

        [Test]
        public void TryConnect_NonAdjacentRooms_IsRejected() // 던전 격자상 붙어 있지 않은 방 연결 방지
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 테스트 그래프
            RoomNode first = graph.AddRoom("A", "ROOM_A", new GridPosition(0, 0)); // 기준 방
            RoomNode far = graph.AddRoom("B", "ROOM_B", new GridPosition(2, 0)); // 두 칸 떨어진 방
            RoomExit east = new RoomExit(new GridPosition(2, 0), CardinalDirection.East); // 동쪽 문
            RoomExit west = new RoomExit(new GridPosition(-2, 0), CardinalDirection.West); // 서쪽 문

            Assert.IsFalse(graph.TryConnect(first, east, far, west)); // 비인접 연결 거부 확인
        }

        [Test]
        public void Generate_LoopChanceZero_ProducesTreeConnectionsOnly() // 루프 확률 0이면 추가 순환 연결이 생기지 않는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(
                12,
                6,
                6,
                branchChance: 1d,
                minBranchLength: 1,
                maxBranchLength: 2,
                specialCandidateChance: 0d,
                loopChance: 0d); // 루프 비활성

            GeneratedDungeon result = new DungeonGenerator(34).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 메인 경로 성공 확인
            int undirectedEdges = CountUndirectedEdges(result.Layout); // 전체 양방향 연결을 한 번씩 계산
            Assert.AreEqual(result.Layout.AllRooms.Count - 1, undirectedEdges); // 트리의 Edge 수 공식 확인
        }

        [Test]
        public void Generate_AllExactEdges_HaveCompatibleStoredExitPairs() // 설정 기반 생성의 모든 연결이 실제 정렬된 출구 정보를 보존하는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(
                10,
                5,
                5,
                branchChance: 1d,
                loopChance: 1d); // 가능한 루프도 모두 시도

            GeneratedDungeon result = new DungeonGenerator(73).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 생성 성공 확인

            foreach (RoomNode room in result.Layout.AllRooms) // 전체 방 순회
            {
                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in room.Connections) // 모든 방향 연결 순회
                {
                    RoomConnectionEdge edge = pair.Value; // 현재 연결
                    Assert.IsTrue(edge.HasExactExitPair); // 실제 출구 쌍 저장 확인
                    Assert.IsTrue(edge.LocalExit.Value.CanConnectTo(edge.NeighborExit.Value)); // 저장된 출구 정렬 규칙 확인
                }
            }
        }

        [Test]
        public void Generate_SameSeed_ReproducesSameConnectionsWithLoops() // 같은 Seed에서 루프까지 포함한 연결 구조가 재현되는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(
                12,
                5,
                6,
                branchChance: 1d,
                minBranchLength: 1,
                maxBranchLength: 3,
                specialCandidateChance: 0.3d,
                loopChance: 0.75d); // 루프 포함 동일 설정

            GeneratedDungeon first = new DungeonGenerator(3400).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 첫 생성
            GeneratedDungeon second = new DungeonGenerator(3400).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 두 번째 생성
            HashSet<string> firstConnections = BuildConnectionSet(first.Layout); // 첫 연결 집합
            HashSet<string> secondConnections = BuildConnectionSet(second.Layout); // 두 번째 연결 집합

            Assert.IsTrue(first.MainPathCompleted, first.FailureReason); // 첫 생성 성공
            Assert.IsTrue(second.MainPathCompleted, second.FailureReason); // 두 번째 생성 성공
            CollectionAssert.AreEquivalent(firstConnections, secondConnections); // 루프 포함 연결 구조 동일 확인
        }

        private static int CountUndirectedEdges(DungeonLayoutGraph graph) // 양방향 저장된 Edge를 한 번씩 계산
        {
            int directedEdges = 0; // 방향별 Edge 수

            foreach (RoomNode room in graph.AllRooms) // 전체 방 순회
            {
                directedEdges += room.Connections.Count; // 각 방 연결 수 누적
            }

            return directedEdges / 2; // 양방향이므로 2로 나눔
        }

        private static HashSet<string> BuildConnectionSet(DungeonLayoutGraph graph) // 비교용 무방향 연결 문자열 집합
        {
            HashSet<string> connections = new HashSet<string>(); // 연결 집합

            foreach (RoomNode room in graph.AllRooms) // 전체 방 순회
            {
                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in room.Connections) // 연결 순회
                {
                    string first = room.RoomId; // 현재 방 ID
                    string second = pair.Value.Neighbor.RoomId; // 이웃 방 ID
                    string key = string.CompareOrdinal(first, second) < 0
                        ? $"{first}<->{second}"
                        : $"{second}<->{first}"; // 방향과 무관한 연결 키 생성
                    connections.Add(key); // 중복 자동 제거
                }
            }

            return connections; // 연결 집합 반환
        }

        private static RoomTemplate CrossTemplate(string id) // 5×5 중앙 4출구 방 템플릿
        {
            return new RoomTemplate(id, new List<RoomExit>
            {
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(2, 0), CardinalDirection.East),
                new RoomExit(new GridPosition(0, -2), CardinalDirection.South),
                new RoomExit(new GridPosition(-2, 0), CardinalDirection.West)
            });
        }

        private static List<RoomTemplate> CrossPool() // 테스트용 다중 출구 방 후보
        {
            return new List<RoomTemplate>
            {
                CrossTemplate("ROOM_CROSS_A"),
                CrossTemplate("ROOM_CROSS_B"),
                CrossTemplate("ROOM_CROSS_C")
            };
        }
    }
}
