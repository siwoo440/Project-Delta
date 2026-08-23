using System.Collections.Generic; // 목록 기능 사용
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 도메인 던전 생성기 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class DungeonGeneratorTests // 던전 생성기 테스트 모음
    {
        // 사방으로 출구가 있는 "십자형" 방 종류 - 갈림길이 생기는 상황을 검증하기 위한 테스트 전용 템플릿.
        private static RoomTemplate CrossTemplate(string id) // 4방향 출구 템플릿 생성
        {
            return new RoomTemplate(id, new List<CardinalDirection>
            {
                CardinalDirection.North, CardinalDirection.East, CardinalDirection.South, CardinalDirection.West
            });
        }

        // 출구가 하나뿐인 방 종류 - 지금 실제로 있는 미로 방 10종과 같은 조건을 재현한다.
        private static RoomTemplate SingleExitTemplate(string id, CardinalDirection exit) // 출구 1개 템플릿 생성
        {
            return new RoomTemplate(id, new List<CardinalDirection> { exit });
        }

        [Test] // 갈림길 있는 던전 생성 테스트
        public void Generate_WithMultiExitRooms_ReachesTargetRoomCount() // 출구 여러 개짜리 방으로 목표 개수만큼 생성되는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 1); // 고정 시드 생성기
            RoomTemplate entry = CrossTemplate("ROOM_ENTRY"); // 시작 방 (십자형)
            List<RoomTemplate> pool = new List<RoomTemplate> { CrossTemplate("ROOM_CROSS") }; // 배치 후보 (십자형 하나뿐)

            GeneratedDungeon result = generator.Generate(entry, pool, targetRoomCount: 8); // 8개 방 목표로 생성

            Assert.AreEqual(8, result.Layout.AllRooms.Count); // 목표 개수만큼 만들어졌는지 확인
        }

        [Test] // 전체 방 도달 가능성 테스트
        public void Generate_AllRooms_AreReachableFromEntry() // 생성된 모든 방이 시작 방에서 도달 가능한지 확인 (너비 우선 탐색)
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 42); // 고정 시드 생성기
            RoomTemplate entry = CrossTemplate("ROOM_ENTRY"); // 시작 방
            List<RoomTemplate> pool = new List<RoomTemplate> { CrossTemplate("ROOM_CROSS") }; // 배치 후보

            GeneratedDungeon result = generator.Generate(entry, pool, targetRoomCount: 12); // 12개 방 목표로 생성

            HashSet<string> visited = new HashSet<string> { result.EntryRoom.RoomId }; // 방문한 방 식별자 목록
            Queue<RoomNode> queue = new Queue<RoomNode>(); // 너비 우선 탐색 대기열
            queue.Enqueue(result.EntryRoom); // 시작 방 등록

            while (queue.Count > 0) // 대기열이 빌 때까지 반복
            {
                RoomNode current = queue.Dequeue(); // 대기열에서 하나 꺼냄

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> connection in current.Connections) // 현재 방의 모든 연결 반복
                {
                    RoomNode neighbor = connection.Value.Neighbor; // 이웃 방 조회

                    if (visited.Add(neighbor.RoomId)) // 처음 방문하는 방인지 확인하며 등록
                    {
                        queue.Enqueue(neighbor); // 대기열에 등록
                    }
                }
            }

            Assert.AreEqual(result.Layout.AllRooms.Count, visited.Count); // 전체 방 개수와 방문한 방 개수가 같은지 확인
        }

        [Test] // 계단 방이 막다른 방인지 테스트
        public void Generate_StairsRoom_IsDeadEnd() // 계단으로 선정된 방의 연결이 1개(막다른 방)인지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 7); // 고정 시드 생성기
            RoomTemplate entry = CrossTemplate("ROOM_ENTRY"); // 시작 방
            List<RoomTemplate> pool = new List<RoomTemplate> { CrossTemplate("ROOM_CROSS") }; // 배치 후보

            GeneratedDungeon result = generator.Generate(entry, pool, targetRoomCount: 10); // 10개 방 목표로 생성

            Assert.AreEqual(1, result.StairsRoom.Connections.Count); // 계단 방의 연결이 1개인지 확인
        }

        [Test] // 출구 하나뿐인 콘텐츠 테스트 (지금 실제 미로 방과 같은 조건)
        public void Generate_WithSingleExitRoomsOnly_ProducesSmallChainWithoutHanging() // 출구 1개짜리 방만 있을 때도 무한 루프 없이 안전하게 끝나는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 3); // 고정 시드 생성기
            RoomTemplate entry = SingleExitTemplate("ROOM_ENTRY", CardinalDirection.North); // 시작 방 (북쪽 출구 하나)
            List<RoomTemplate> pool = new List<RoomTemplate> { SingleExitTemplate("ROOM_MAZE", CardinalDirection.South) }; // 배치 후보 (남쪽 출구 하나 - 시작 방과 맞물림)

            GeneratedDungeon result = generator.Generate(entry, pool, targetRoomCount: 8); // 8개를 목표로 하지만 콘텐츠 부족으로 다 못 채움

            Assert.AreEqual(2, result.Layout.AllRooms.Count); // 시작 방 + 막다른 방 1개, 총 2개에서 멈추는지 확인
            Assert.AreEqual(1, result.StairsRoom.Connections.Count); // 계단 방은 막다른 방이어야 함
        }

        [Test] // 방 하나뿐인 극단적인 경우 테스트
        public void Generate_EntryWithNoExits_ProducesSingleRoomDungeon() // 시작 방에 출구가 없으면 방 하나짜리로 끝나는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 99); // 고정 시드 생성기
            RoomTemplate entry = new RoomTemplate("ROOM_ISOLATED", new List<CardinalDirection>()); // 출구 없는 시작 방
            List<RoomTemplate> pool = new List<RoomTemplate> { CrossTemplate("ROOM_CROSS") }; // 배치 후보는 있지만 쓸 수 없음

            GeneratedDungeon result = generator.Generate(entry, pool, targetRoomCount: 5); // 5개 목표

            Assert.AreEqual(1, result.Layout.AllRooms.Count); // 방 하나에서 멈추는지 확인
            Assert.AreSame(result.EntryRoom, result.StairsRoom); // 계단도 시작 방 자신이 되는지 확인 (막다른 방이 따로 없으므로)
        }
    }
}
