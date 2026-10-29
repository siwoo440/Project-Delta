using System; // 정수 범위와 예외
using System.Collections.Generic; // 구조 복사 목록
using System.Linq; // 비밀 방과 연결 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 던전 생성 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class VersionedDungeonValidationTests // 버전별 던전 생성 검증
    { // 클래스 시작
        [Test] // 다양한 시드의 연결과 중복 검증
        public void Generate_ManySeedsPreservesBaseAndOneSecretRoom() // 기존 구조와 비밀방 보존
        { // 검증 시작
            var seeds = Enumerable.Range(-500, 1000).Concat(new[] { int.MinValue, int.MaxValue }).ToArray(); // 다양한 값과 정수 경계
            foreach (var seed in seeds) // 모든 시드 순회
            { // 시드 검사 시작
                var legacy = DungeonGenerator.Generate(seed); // 기존 생성 구조
                var current = DungeonGenerator.GenerateWithSecret(seed); // 비밀방 포함 구조
                Assert.That(DungeonGenerator.IsValidVersioned(legacy, 1), Is.True, "legacy seed " + seed); // 기존 필수 경로 검사
                Assert.That(DungeonGenerator.IsValidVersioned(current, 2), Is.True, "secret seed " + seed); // 비밀 구조 검사
                Assert.That(current.Rooms.Count, Is.EqualTo(legacy.Rooms.Count + 1), "room count seed " + seed); // 시험 비밀방 한 곳
                Assert.That(current.Rooms.Select(room => (room.X, room.Y)).Distinct().Count(), Is.EqualTo(current.Rooms.Count), "room overlap seed " + seed); // 중복 좌표 없음
                Assert.That(current.Doors.Count, Is.EqualTo(legacy.Doors.Count + 2), "door count seed " + seed); // 양방향 비밀 연결 추가
                Assert.That(DungeonLayoutFingerprint.Compute(current), Is.EqualTo(DungeonLayoutFingerprint.Compute(DungeonGenerator.GenerateWithSecret(seed))), "repeat seed " + seed); // 같은 시드의 구조 재현
            } // 시드 검사 끝
        } // 검증 끝
        [Test] // 잘못된 연결의 생성 규칙 거절
        public void Validate_RejectsSecretConnectedToRequiredRoom() // 시작 방의 잘못된 비밀 연결 차단
        { // 검증 시작
            var layout = DungeonGenerator.Generate(0); // 정상 기본 던전
            var start = layout.GetRoom(0, 0); // 필수 시작 방
            var directions = new[] { MoveDirection.Up, MoveDirection.Down, MoveDirection.Left, MoveDirection.Right }; // 네 방향 목록
            var door = directions.Select(direction => Candidate(layout, start, direction)).First(candidate => candidate != null); // 빈 방향의 잘못된 연결
            var rooms = new List<DungeonRoom>(layout.Rooms) { new DungeonRoom(door.TargetRoomX, door.TargetRoomY, true) }; // 시작 방에 붙은 비밀방
            var doors = new List<RoomDoor>(layout.Doors) { door, Reverse(door) }; // 잘못된 양방향 연결
            Assert.That(DungeonGenerator.IsValidVersioned(new DungeonLayout(rooms.ToArray(), doors.ToArray()), 2), Is.False); // 필수 방의 비밀 연결 거절
            Assert.That(DungeonGenerator.IsValidVersioned(layout, 2), Is.False); // 비밀방 누락 거절
            Assert.That(DungeonGenerator.IsValidVersioned(layout, 99), Is.False); // 알 수 없는 생성 버전 거절
        } // 검증 끝
        private static RoomDoor Candidate(DungeonLayout layout, DungeonRoom start, MoveDirection direction) // 시작 방의 빈 방향 후보
        { // 후보 시작
            var dx = direction == MoveDirection.Right ? 1 : direction == MoveDirection.Left ? -1 : 0; // 방향의 가로 변화
            var dy = direction == MoveDirection.Up ? 1 : direction == MoveDirection.Down ? -1 : 0; // 방향의 세로 변화
            if (layout.GetRoom(start.X + dx, start.Y + dy) != null) // 기존 방 존재 검사
            { // 중복 조건 시작
                return null; // 빈 위치가 아닌 방향 제외
            } // 중복 조건 끝
            return new RoomDoor(start.X, start.Y, dx * 5, dy * 3, direction, start.X + dx, start.Y + dy, -dx * 5, -dy * 3, true); // 시험용 비밀 연결
        } // 후보 끝
        private static RoomDoor Reverse(RoomDoor door) // 시험용 반대 방향 연결
        { // 변환 시작
            var direction = door.Direction == MoveDirection.Up ? MoveDirection.Down : door.Direction == MoveDirection.Down ? MoveDirection.Up : door.Direction == MoveDirection.Left ? MoveDirection.Right : MoveDirection.Left; // 반대 방향 선택
            return new RoomDoor(door.TargetRoomX, door.TargetRoomY, door.TargetX, door.TargetY, direction, door.RoomX, door.RoomY, door.ApproachX, door.ApproachY, true); // 양방향 검증에 맞는 반대 연결
        } // 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
