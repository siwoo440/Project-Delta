using System;
using System.Collections.Generic; // 목록·집합 기능 사용
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 던전 생성 도메인 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class MainPathGenerationTests // 32일차 메인 경로 생성 규칙 테스트
    {
        [Test]
        public void Settings_MaxMainPathCannotExceedTargetRoomCount() // 메인 경로가 전체 목표 방 수를 넘지 못하는지 확인
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonGenerationSettings(6, 4, 7)); // 잘못된 설정 차단 확인
        }

        [Test]
        public void Generate_ControlledMainPath_UsesLengthInsideConfiguredRange() // 설정 범위 안에서 메인 경로 길이를 선택하는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 10); // 고정 Seed 생성기
            DungeonGenerationSettings settings = new DungeonGenerationSettings(12, 5, 8); // 메인 경로 5~8방 설정
            GeneratedDungeon result = generator.Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 설정 기반 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 목표 메인 경로 완성 확인
            Assert.GreaterOrEqual(result.TargetMainPathLength, 5); // 최소 길이 확인
            Assert.LessOrEqual(result.TargetMainPathLength, 8); // 최대 길이 확인
            Assert.AreEqual(result.TargetMainPathLength, result.MainPath.Count); // 실제 경로가 선택 목표와 같은지 확인
        }

        [Test]
        public void Generate_FixedMainPathLength_EndsAtStairsRoom() // 메인 경로 마지막 방이 계단 방인지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 22); // 고정 Seed 생성기
            DungeonGenerationSettings settings = new DungeonGenerationSettings(10, 6, 6); // 정확히 6방 메인 경로
            GeneratedDungeon result = generator.Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 설정 기반 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 경로 완성 확인
            Assert.AreEqual(6, result.MainPath.Count); // 정확한 길이 확인
            Assert.AreSame(result.EntryRoom, result.MainPath[0]); // 첫 방이 시작 방인지 확인
            Assert.AreSame(result.StairsRoom, result.MainPath[result.MainPath.Count - 1]); // 마지막 방이 계단 방인지 확인
        }

        [Test]
        public void Generate_MainPath_AllAdjacentRoomsAreConnected() // 메인 경로 앞뒤 방이 실제 그래프에서도 연결되는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 31); // 고정 Seed 생성기
            DungeonGenerationSettings settings = new DungeonGenerationSettings(10, 7, 7); // 정확히 7방 메인 경로
            GeneratedDungeon result = generator.Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 설정 기반 생성

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 경로 완성 확인

            for (int i = 1; i < result.MainPath.Count; i++) // 두 번째 방부터 앞 방과 연결 확인
            {
                RoomNode previous = result.MainPath[i - 1]; // 앞 방 조회
                RoomNode current = result.MainPath[i]; // 현재 방 조회
                bool connected = false; // 연결 발견 여부

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> connection in previous.Connections) // 앞 방 연결 전체 순회
                {
                    if (ReferenceEquals(connection.Value.Neighbor, current)) // 현재 방으로 연결되는지 확인
                    {
                        connected = true; // 연결 확인
                        break; // 추가 탐색 중단
                    }
                }

                Assert.IsTrue(connected, $"{previous.RoomId}에서 {current.RoomId}로 이어지는 메인 경로 연결이 없습니다."); // 연속 경로 보장
            }
        }

        [Test]
        public void Generate_MainPath_DoesNotReuseMacroCoordinate() // 메인 경로에서 같은 방 좌표를 두 번 사용하지 않는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 44); // 고정 Seed 생성기
            DungeonGenerationSettings settings = new DungeonGenerationSettings(12, 8, 8); // 정확히 8방 메인 경로
            GeneratedDungeon result = generator.Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 설정 기반 생성
            HashSet<GridPosition> coordinates = new HashSet<GridPosition>(); // 좌표 중복 확인 집합

            Assert.IsTrue(result.MainPathCompleted, result.FailureReason); // 경로 완성 확인

            for (int i = 0; i < result.MainPath.Count; i++) // 메인 경로 전체 순회
            {
                Assert.IsTrue(coordinates.Add(result.MainPath[i].MacroCoordinate), $"중복 좌표: {result.MainPath[i].MacroCoordinate}"); // 새 좌표만 허용
            }
        }

        [Test]
        public void Generate_SameSeed_ReproducesSameMainPath() // 같은 Seed가 같은 메인 경로를 재현하는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(12, 5, 8); // 동일 설정 준비
            GeneratedDungeon first = new DungeonGenerator(seed: 1234).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 첫 번째 생성
            GeneratedDungeon second = new DungeonGenerator(seed: 1234).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 두 번째 생성

            Assert.IsTrue(first.MainPathCompleted, first.FailureReason); // 첫 생성 성공 확인
            Assert.IsTrue(second.MainPathCompleted, second.FailureReason); // 두 번째 생성 성공 확인
            Assert.AreEqual(first.TargetMainPathLength, second.TargetMainPathLength); // 선택된 목표 길이 동일 확인
            Assert.AreEqual(first.MainPath.Count, second.MainPath.Count); // 실제 경로 길이 동일 확인

            for (int i = 0; i < first.MainPath.Count; i++) // 경로 노드 순서 비교
            {
                Assert.AreEqual(first.MainPath[i].DefinitionId, second.MainPath[i].DefinitionId); // 방 종류 동일 확인
                Assert.AreEqual(first.MainPath[i].MacroCoordinate, second.MainPath[i].MacroCoordinate); // 좌표 동일 확인
            }
        }

        [Test]
        public void Generate_WhenPathCannotReachMinimum_ReturnsExplicitFailure() // 콘텐츠 부족으로 목표 길이에 못 미칠 때 실패 상태를 구분하는지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 5); // 고정 Seed 생성기
            RoomTemplate entry = new RoomTemplate("ROOM_ENTRY", new List<RoomExit>
            {
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North)
            });
            List<RoomTemplate> pool = new List<RoomTemplate>
            {
                new RoomTemplate("ROOM_DEAD_END", new List<RoomExit>
                {
                    new RoomExit(new GridPosition(0, -2), CardinalDirection.South)
                })
            };
            DungeonGenerationSettings settings = new DungeonGenerationSettings(4, 4, 4); // 정확히 4방 경로 요구

            GeneratedDungeon result = generator.Generate(entry, pool, settings); // 연결 불가능한 생성 시도

            Assert.IsFalse(result.MainPathCompleted); // 실패 상태 확인
            Assert.IsNotEmpty(result.FailureReason); // 실패 원인 기록 확인
            Assert.AreEqual(1, result.Layout.AllRooms.Count); // 실패한 임시 경로가 그래프에 남지 않는지 확인
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

        private static List<RoomTemplate> CrossPool() // 메인 경로 테스트용 다중 출구 방 목록
        {
            return new List<RoomTemplate>
            {
                CrossTemplate("ROOM_CROSS_A"),
                CrossTemplate("ROOM_CROSS_B")
            };
        }
    }
}
