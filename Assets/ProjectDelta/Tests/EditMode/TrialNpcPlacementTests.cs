using NUnit.Framework; // 상인 배치 검증 도구
using ProjectDelta.Core; // 던전과 시험 콘텐츠
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialNpcPlacementTests // 시험 상인의 결정적 배치 검증
    { // 검증 클래스 시작
        [Test] // 실제 방 콘텐츠와 이벤트 제외 검증
        public void Placement_ExcludesExistingContentAndReproducesCoordinates() // 기존 방 보존과 반복 배치 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0), new DungeonRoom(5, 0), new DungeonRoom(6, 0), new DungeonRoom(7, 0), new DungeonRoom(8, 0), new DungeonRoom(9, 0) }; // 기존 콘텐츠와 시험 일반 방
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 실제 방 목록
            var encounters = new[] { rooms[1] }; // 기존 조우 좌표
            Assert.That(FloorRoomContent.TryCreate(1, layout, 42, encounters, out var content), Is.True); // 상자와 함정 시험 배치
            Assert.That(TrialEventPlacement.TrySelect(layout, 42, content.Snapshot(), encounters, out var eventRoom), Is.True); // 실제 이벤트 방 배치
            Assert.That(TrialNpcPlacement.TrySelect(layout, 42, content.Snapshot(), encounters, eventRoom, out var selected), Is.True); // 기존 콘텐츠를 피한 상인 방
            Assert.That(TrialNpcPlacement.TrySelect(layout, 42, content.Snapshot(), encounters, eventRoom, out var repeat), Is.True); // 동일한 입력의 반복 배치
            Assert.That(selected.X, Is.EqualTo(repeat.X)); // 같은 가로 좌표 재현
            Assert.That(selected.Y, Is.EqualTo(repeat.Y)); // 같은 세로 좌표 재현
            Assert.That(selected.X, Is.Not.EqualTo(rooms[1].X)); // 기존 조우와 겹침 차단
            Assert.That(selected.X, Is.Not.EqualTo(eventRoom.X)); // 이벤트 방과 겹침 차단
            Assert.That(selected.X, Is.Not.EqualTo(content.TrapRoom.X)); // 함정 방과 겹침 차단
            foreach (var chest in content.Snapshot().chests) // 실제 상자 방 순회
            { // 상자 제외 검증 시작
                Assert.That(selected.X, Is.Not.EqualTo(chest.x)); // 상자 방과 겹침 차단
            } // 상자 제외 검증 끝
        } // 검증 끝
        [Test] // 후보 방이 없을 때 거절 검증
        public void Placement_RejectsMissingOrOccupiedRooms() // 콘텐츠 손실 없는 배치 실패 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0) }; // 콘텐츠 방뿐인 시험 구조
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 시험 층 구성
            Assert.That(FloorRoomContent.TryCreate(1, layout, 7, null, out var content), Is.True); // 세 상자와 함정 배치
            Assert.That(TrialNpcPlacement.TrySelect(layout, 7, content.Snapshot(), null, null, out var selected), Is.False); // 빈 방이 없는 상인 배치 차단
            Assert.That(selected, Is.Null); // 잘못된 대상 방 없음
            Assert.That(TrialNpcPlacement.TrySelect(null, 0, null, null, null, out _), Is.False); // 누락된 층과 콘텐츠 차단
        } // 검증 끝
        [Test] // 입력 방 순서 독립 검증
        public void Placement_IgnoresRoomArrayOrder() // 같은 씨드의 위치 재현 확인
        { // 검증 시작
            var first = new DungeonLayout(new[] { new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0), new DungeonRoom(5, 0), new DungeonRoom(6, 0) }, new RoomDoor[0]); // 최초 방 순서
            var second = new DungeonLayout(new[] { new DungeonRoom(6, 0), new DungeonRoom(4, 0), new DungeonRoom(2, 0), new DungeonRoom(1, 0), new DungeonRoom(3, 0), new DungeonRoom(5, 0) }, new RoomDoor[0]); // 변경된 방 순서
            Assert.That(FloorRoomContent.TryCreate(1, first, 2, null, out var content), Is.True); // 결정된 층 콘텐츠
            Assert.That(TrialNpcPlacement.TrySelect(first, 2, content.Snapshot(), null, null, out var a), Is.True); // 최초 상인 배치
            Assert.That(TrialNpcPlacement.TrySelect(second, 2, content.Snapshot(), null, null, out var b), Is.True); // 방 순서를 바꾼 상인 배치
            Assert.That(a.X, Is.EqualTo(b.X)); // 상인 가로 좌표 유지
            Assert.That(a.Y, Is.EqualTo(b.Y)); // 상인 세로 좌표 유지
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
