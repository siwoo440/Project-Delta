using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 던전 방문 기록
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DiscoverySaveTests // 방문 기록 복원 검증
    { // 클래스 시작
        [Test] // 방문 방과 현재 방 복원 검증
        public void Snapshot_RestoresVisitedRoomsAndCurrentRoom() // 방문 기록 유지
        { // 검증 시작
            var layout = DungeonGenerator.Generate(123); // 재현 가능한 던전
            var discovery = new DungeonDiscovery(layout, 0, 0); // 시작 방 방문 기록
            var door = layout.Doors[0]; // 시작 방의 첫 연결
            Assert.That(discovery.TryVisit(door.TargetRoomX, door.TargetRoomY), Is.True); // 다음 방 방문
            var snapshot = discovery.SaveVisited(); // 방문 좌표 저장본
            var restored = DungeonDiscovery.FromSaved(layout, snapshot, door.TargetRoomX, door.TargetRoomY); // 방문 기록 복원
            Assert.That(restored, Is.Not.Null); // 유효한 방문 기록 확인
            Assert.That(restored.CurrentX, Is.EqualTo(door.TargetRoomX)); // 현재 방 가로 좌표 확인
            Assert.That(restored.GetVisibility(0, 0), Is.EqualTo(RoomVisibility.Visited)); // 시작 방 방문 상태 확인
            Assert.That(restored.GetVisibility(door.TargetRoomX, door.TargetRoomY), Is.EqualTo(RoomVisibility.Visited)); // 도착 방 방문 상태 확인
        } // 검증 끝
        [Test] // 존재하지 않는 방과 중복 방문 거절
        public void Restore_RejectsInvalidVisitedRooms() // 잘못된 기록 거절
        { // 검증 시작
            var layout = DungeonGenerator.Generate(123); // 시험 던전 생성
            Assert.That(DungeonDiscovery.FromSaved(layout, new[] { new SavedRoom { x = 999, y = 999 } }, 0, 0), Is.Null); // 없는 방 거절
            Assert.That(DungeonDiscovery.FromSaved(layout, new[] { new SavedRoom(), new SavedRoom() }, 0, 0), Is.Null); // 중복 방 거절
            Assert.That(DungeonDiscovery.FromSaved(layout, new[] { new SavedRoom() }, 999, 999), Is.Null); // 방문하지 않은 현재 방 거절
            foreach (var room in layout.Rooms) // 생성된 방 순회
            { // 연결 검사 시작
                if ((room.X != 0 || room.Y != 0) && layout.GetConnection(0, 0, room.X, room.Y) == null) // 시작 방과 직접 연결되지 않은 방 검사
                { // 먼 방 조건 시작
                    Assert.That(DungeonDiscovery.FromSaved(layout, new[] { new SavedRoom(), new SavedRoom { x = room.X, y = room.Y } }, room.X, room.Y), Is.Null); // 연결이 빠진 방문 기록 거절
                    break; // 한 개의 먼 방 검사 완료
                } // 먼 방 조건 끝
            } // 연결 검사 끝
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
