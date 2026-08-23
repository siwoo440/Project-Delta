using System.Collections.Generic; // 목록·집합 기능 사용
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 던전 생성 도메인 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BranchGenerationTests // 33일차 가지 경로 생성 규칙 테스트
    {
        [Test]
        public void Generate_BranchChanceZero_KeepsOnlyMainPath() // 분기 확률 0이면 가지가 생성되지 않는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(10, 5, 5, branchChance: 0d); // 가지 생성 비활성
            GeneratedDungeon result = new DungeonGenerator(1).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 메인 경로 성공 확인
            Assert.AreEqual(result.MainPath.Count, result.Layout.AllRooms.Count); // 전체 방이 메인 경로뿐인지 확인
            Assert.AreEqual(0, result.BranchRooms.Count); // 가지 방 없음 확인
        }

        [Test]
        public void Generate_BranchChanceOne_ReachesTargetRoomCountWhenSpaceExists() // 분기 확률 1이면 가능한 경우 전체 목표 방 수를 채우는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(10, 5, 5, branchChance: 1d, minBranchLength: 1, maxBranchLength: 2); // 가지 생성 강제
            GeneratedDungeon result = new DungeonGenerator(7).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 메인 경로 성공 확인
            Assert.IsTrue(result.RoomCountTargetReached); // 전체 목표 방 수 도달 확인
            Assert.AreEqual(10, result.Layout.AllRooms.Count); // 정확한 전체 방 수 확인
            Assert.Greater(result.BranchRooms.Count, 0); // 실제 가지 방 생성 확인
        }

        [Test]
        public void Generate_Branches_DoNotChangeMainPathOrStairs() // 가지 생성 후에도 메인 경로와 계단 방이 유지되는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(11, 6, 6, branchChance: 1d); // 가지 포함 생성 설정
            GeneratedDungeon result = new DungeonGenerator(14).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 메인 경로 성공 확인
            Assert.AreSame(result.EntryRoom, result.MainPath[0]); // 첫 방 유지 확인
            Assert.AreSame(result.StairsRoom, result.MainPath[result.MainPath.Count - 1]); // 마지막 방이 계단인지 확인
        }

        [Test]
        public void Generate_AllRoomsUseUniqueMacroCoordinates() // 메인·가지 전체에서 좌표 중복이 없는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(12, 6, 6, branchChance: 1d); // 가지 포함 설정
            GeneratedDungeon result = new DungeonGenerator(21).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성
            HashSet<GridPosition> coordinates = new HashSet<GridPosition>(); // 좌표 중복 검사용 집합

            foreach (RoomNode room in result.Layout.AllRooms) // 전체 방 순회
            {
                Assert.IsTrue(coordinates.Add(room.MacroCoordinate), $"중복 좌표: {room.MacroCoordinate}"); // 좌표 고유성 확인
            }
        }

        [Test]
        public void Generate_BranchRooms_AreReachableFromEntry() // 모든 가지 방이 시작 방에서 도달 가능한지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(12, 5, 5, branchChance: 1d); // 가지 포함 설정
            GeneratedDungeon result = new DungeonGenerator(33).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성
            HashSet<string> visited = new HashSet<string> { result.EntryRoom.RoomId }; // 방문 방 ID
            Queue<RoomNode> queue = new Queue<RoomNode>(); // BFS 대기열
            queue.Enqueue(result.EntryRoom); // 시작 방 등록

            while (queue.Count > 0) // 전체 연결 탐색
            {
                RoomNode current = queue.Dequeue(); // 현재 방

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> connection in current.Connections) // 모든 연결 순회
                {
                    RoomNode neighbor = connection.Value.Neighbor; // 이웃 방

                    if (visited.Add(neighbor.RoomId)) // 처음 방문한 방인지 확인
                    {
                        queue.Enqueue(neighbor); // 탐색 대기열 등록
                    }
                }
            }

            Assert.AreEqual(result.Layout.AllRooms.Count, visited.Count); // 모든 방 도달 가능 확인
        }

        [Test]
        public void Generate_BranchEndCandidates_AreDeadEndsAndNotMainPath() // 가지 끝 후보가 실제 막다른 방이며 메인 경로가 아닌지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(11, 5, 5, branchChance: 1d, specialCandidateChance: 0d); // 모든 가지 끝을 일반 막다른 후보로 지정
            GeneratedDungeon result = new DungeonGenerator(45).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성
            HashSet<string> mainIds = new HashSet<string>(); // 메인 경로 ID 집합

            for (int i = 0; i < result.MainPath.Count; i++) // 메인 경로 ID 등록
            {
                mainIds.Add(result.MainPath[i].RoomId); // ID 저장
            }

            Assert.Greater(result.DeadEndCandidates.Count, 0); // 막다른 후보 존재 확인

            for (int i = 0; i < result.DeadEndCandidates.Count; i++) // 막다른 후보 검사
            {
                RoomNode room = result.DeadEndCandidates[i]; // 현재 후보
                Assert.AreEqual(1, room.Connections.Count); // 실제 그래프상 막다른 방 확인
                Assert.IsFalse(mainIds.Contains(room.RoomId)); // 메인 경로가 아닌지 확인
            }
        }

        [Test]
        public void Generate_SpecialChanceOne_ClassifiesBranchEndsAsSpecialCandidates() // 특수 후보 확률 1이면 가지 끝이 특수 후보가 되는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(11, 5, 5, branchChance: 1d, specialCandidateChance: 1d); // 특수 후보 강제
            GeneratedDungeon result = new DungeonGenerator(51).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성

            Assert.Greater(result.SpecialRoomCandidates.Count, 0); // 특수 후보 존재 확인
            Assert.AreEqual(0, result.DeadEndCandidates.Count); // 일반 막다른 후보가 없는지 확인

            for (int i = 0; i < result.SpecialRoomCandidates.Count; i++) // 특수 후보 역할 검사
            {
                Assert.IsTrue(result.TryGetRoomRole(result.SpecialRoomCandidates[i], out DungeonRoomRole role)); // 역할 조회 확인
                Assert.AreEqual(DungeonRoomRole.SpecialCandidate, role); // 특수 후보 역할 확인
            }
        }

        [Test]
        public void Generate_SameSeed_ReproducesSameBranchLayoutAndRoles() // 같은 Seed가 같은 가지 구조와 역할을 재현하는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(12, 5, 6, branchChance: 0.8d, minBranchLength: 1, maxBranchLength: 3, specialCandidateChance: 0.4d); // 동일 설정
            GeneratedDungeon first = new DungeonGenerator(9876).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 첫 생성
            GeneratedDungeon second = new DungeonGenerator(9876).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 두 번째 생성
            Dictionary<GridPosition, DungeonRoomRole> firstRoles = BuildRoleByCoordinate(first); // 첫 결과 좌표별 역할
            Dictionary<GridPosition, DungeonRoomRole> secondRoles = BuildRoleByCoordinate(second); // 두 번째 결과 좌표별 역할

            Assert.AreEqual(first.Layout.AllRooms.Count, second.Layout.AllRooms.Count); // 전체 방 수 동일 확인
            Assert.AreEqual(firstRoles.Count, secondRoles.Count); // 역할 기록 수 동일 확인

            foreach (KeyValuePair<GridPosition, DungeonRoomRole> pair in firstRoles) // 첫 결과 역할 전체 비교
            {
                Assert.IsTrue(secondRoles.TryGetValue(pair.Key, out DungeonRoomRole secondRole)); // 같은 좌표 존재 확인
                Assert.AreEqual(pair.Value, secondRole); // 같은 역할인지 확인
            }
        }

        private static Dictionary<GridPosition, DungeonRoomRole> BuildRoleByCoordinate(GeneratedDungeon dungeon) // 좌표별 생성 역할 정리
        {
            Dictionary<GridPosition, DungeonRoomRole> result = new Dictionary<GridPosition, DungeonRoomRole>(); // 결과 사전

            foreach (RoomNode room in dungeon.Layout.AllRooms) // 전체 방 순회
            {
                if (dungeon.TryGetRoomRole(room, out DungeonRoomRole role)) // 생성 역할이 있는 방만 등록
                {
                    result[room.MacroCoordinate] = role; // 좌표별 역할 저장
                }
            }

            return result; // 역할 사전 반환
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
