using System.Collections.Generic; // 출구 목록 기능 사용
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // RoomExit·RoomTemplate·DungeonGenerator 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class MultiExitRoomTests // 31일차 다중 출구 규격 테스트
    {
        [Test]
        public void RoomTemplate_WithFourPositionedExits_PreservesEveryExit() // 4방향 출구를 모두 보존하는지 확인
        {
            RoomTemplate template = CreateCrossTemplate("ROOM_TEST_CROSS"); // 중앙 4출구 템플릿 생성

            Assert.AreEqual(4, template.Exits.Count); // 출구 네 개 보존 확인
            Assert.AreEqual(new RoomExit(new GridPosition(0, 2), CardinalDirection.North), template.Exits[0]); // 북쪽 출구 확인
            Assert.AreEqual(new RoomExit(new GridPosition(2, 0), CardinalDirection.East), template.Exits[1]); // 동쪽 출구 확인
            Assert.AreEqual(new RoomExit(new GridPosition(0, -2), CardinalDirection.South), template.Exits[2]); // 남쪽 출구 확인
            Assert.AreEqual(new RoomExit(new GridPosition(-2, 0), CardinalDirection.West), template.Exits[3]); // 서쪽 출구 확인
        }

        [Test]
        public void RoomTemplate_SameDirectionDifferentPositions_RemainDistinct() // 같은 방향의 다른 위치 출구를 구분하는지 확인
        {
            RoomTemplate template = new RoomTemplate("ROOM_TEST_OFFSET", new List<RoomExit>
            {
                new RoomExit(new GridPosition(-1, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(1, 2), CardinalDirection.North)
            });

            Assert.AreEqual(2, template.Exits.Count); // 두 출구 모두 유지 확인
            Assert.AreNotEqual(template.Exits[0], template.Exits[1]); // 좌표가 다르면 다른 출구인지 확인
        }

        [Test]
        public void RoomExit_CanConnectTo_ValidatesNorthSouthAndEastWestAxes() // 양 축 정렬 규칙 확인
        {
            RoomExit north = new RoomExit(new GridPosition(0, 2), CardinalDirection.North); // 북쪽 중앙 출구
            RoomExit south = new RoomExit(new GridPosition(0, -2), CardinalDirection.South); // 남쪽 중앙 출구
            RoomExit east = new RoomExit(new GridPosition(2, 0), CardinalDirection.East); // 동쪽 중앙 출구
            RoomExit west = new RoomExit(new GridPosition(-2, 0), CardinalDirection.West); // 서쪽 중앙 출구

            Assert.IsTrue(north.CanConnectTo(south)); // 북-남 중앙 정렬 확인
            Assert.IsTrue(east.CanConnectTo(west)); // 동-서 중앙 정렬 확인
            Assert.IsFalse(north.CanConnectTo(east)); // 직각 방향 연결 차단 확인
        }

        [Test]
        public void DungeonGenerator_WithPositionedCrossRooms_ReachesTargetRoomCount() // 실제 좌표 출구 템플릿으로 생성 가능한지 확인
        {
            DungeonGenerator generator = new DungeonGenerator(seed: 1); // 고정 시드 생성기
            RoomTemplate entry = CreateCrossTemplate("ROOM_ENTRY"); // 시작 4출구 방
            List<RoomTemplate> pool = new List<RoomTemplate> { CreateCrossTemplate("ROOM_CROSS") }; // 4출구 방 후보

            GeneratedDungeon result = generator.Generate(entry, pool, targetRoomCount: 8); // 8개 방 생성 시도

            Assert.AreEqual(8, result.Layout.AllRooms.Count); // 실제 RoomExit 좌표를 가진 템플릿도 목표 개수 생성 확인
        }

        private static RoomTemplate CreateCrossTemplate(string id) // 5x5 중앙 4출구 템플릿 생성
        {
            return new RoomTemplate(id, new List<RoomExit>
            {
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(2, 0), CardinalDirection.East),
                new RoomExit(new GridPosition(0, -2), CardinalDirection.South),
                new RoomExit(new GridPosition(-2, 0), CardinalDirection.West)
            });
        }
    }
}
