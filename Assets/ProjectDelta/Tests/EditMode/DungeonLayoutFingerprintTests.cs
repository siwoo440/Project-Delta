using System.Collections.Generic; // 방과 문 목록 복사
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 던전 구조와 지문
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonLayoutFingerprintTests // 던전 구조 지문 검증
    { // 클래스 시작
        [Test] // 같은 구조의 순서 독립 검증
        public void Compute_IgnoresRoomAndDoorListOrder() // 방과 문 순서에 무관한 지문
        { // 검증 시작
            var layout = DungeonGenerator.Generate(123); // 재현 가능한 던전
            var rooms = new List<DungeonRoom>(layout.Rooms); // 방 목록 복사
            var doors = new List<RoomDoor>(layout.Doors); // 문 목록 복사
            rooms.Reverse(); // 방 목록 순서 변경
            doors.Reverse(); // 문 목록 순서 변경
            var reordered = new DungeonLayout(rooms.ToArray(), doors.ToArray()); // 순서만 다른 같은 구조
            Assert.That(DungeonLayoutFingerprint.Compute(reordered), Is.EqualTo(DungeonLayoutFingerprint.Compute(layout))); // 같은 구조 지문 확인
        } // 검증 끝
        [Test] // 방 역할 변경 검증
        public void Compute_ChangesWhenRoomKindChanges() // 방 역할 차이 구분
        { // 검증 시작
            var layout = DungeonGenerator.Generate(123); // 재현 가능한 던전
            var rooms = new List<DungeonRoom>(layout.Rooms); // 방 목록 복사
            var room = rooms[0]; // 첫 방 조회
            rooms[0] = new DungeonRoom(room.X, room.Y, room.IsSecret, RoomKind.Elite); // 첫 방 역할 변경
            var changed = new DungeonLayout(rooms.ToArray(), new List<RoomDoor>(layout.Doors).ToArray()); // 역할이 다른 구조
            Assert.That(DungeonLayoutFingerprint.Compute(changed), Is.Not.EqualTo(DungeonLayoutFingerprint.Compute(layout))); // 변경된 구조 지문 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
