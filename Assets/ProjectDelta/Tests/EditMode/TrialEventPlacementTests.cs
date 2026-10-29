using NUnit.Framework; // 이벤트 방 배치 검증 도구
using ProjectDelta.Core; // 던전과 시험 배치 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventPlacementTests // 결정적인 시험 이벤트 방 검증
    { // 검증 클래스 시작
        [Test] // 실제 층별 콘텐츠 제외 검증
        public void Selection_ExcludesPersistedChestsTrapAndEncounter() // 저장 콘텐츠와 공존하는 이벤트 방 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0), new DungeonRoom(5, 0), new DungeonRoom(6, 0), new DungeonRoom(7, 0), new DungeonRoom(8, 0) }; // 충분한 일반 방 구성
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 시험 층 구조
            var encounters = new[] { rooms[1] }; // 기존 조우 방
            Assert.That(FloorRoomContent.TryCreate(1, layout, 42, encounters, out var contents), Is.True); // 실제 상자와 함정 생성
            var saved = contents.Snapshot(); // 저장 가능한 독립 콘텐츠
            Assert.That(TrialEventPlacement.TrySelect(layout, 42, saved, encounters, out var room), Is.True); // 모든 기존 콘텐츠 제외
            Assert.That(TrialEventPlacement.TrySelect(layout, 42, contents.Snapshot(), encounters, out var again), Is.True); // 동일 저장의 반복 선택
            Assert.That(room.X, Is.EqualTo(again.X)); // 이벤트 방의 가로 재현성
            Assert.That(room.Y, Is.EqualTo(again.Y)); // 이벤트 방의 세로 재현성
            Assert.That(room.X, Is.Not.EqualTo(rooms[1].X)); // 기존 조우 방 제외
            Assert.That(room.X, Is.Not.EqualTo(saved.trapX)); // 함정 방 제외
            foreach (var chest in saved.chests) // 저장된 상자 방 순회
            { // 상자 제외 검사 시작
                Assert.That(room.X, Is.Not.EqualTo(chest.x)); // 상자 방과의 겹침 방지
            } // 상자 제외 검사 끝
        } // 검증 끝
        [Test] // 동일 씨드와 제외 방 검증
        public void Selection_ExcludesReservedAndSpecialRoomsDeterministically() // 일반 방과 다른 콘텐츠 분리 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0, false, RoomKind.Stairs), new DungeonRoom(2, 0, false, RoomKind.Elite), new DungeonRoom(3, 0, true), new DungeonRoom(4, 0), new DungeonRoom(5, 0), new DungeonRoom(6, 0), new DungeonRoom(7, 0) }; // 특수 방과 시험 일반 방
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 실제 방 좌표 목록
            var excluded = new[] { rooms[4], rooms[5], rooms[6] }; // 기존 조우와 상자 및 함정 방
            Assert.That(TrialEventPlacement.TrySelect(layout, 123, excluded, out var first), Is.True); // 제외 방을 반영한 선택
            Assert.That(TrialEventPlacement.TrySelect(layout, 123, excluded, out var again), Is.True); // 동일 입력의 반복 선택
            Assert.That(first.X, Is.EqualTo(7)); // 유일하게 남은 일반 방
            Assert.That(first.X, Is.EqualTo(again.X)); // 반복 선택의 동일 가로 좌표
            Assert.That(first.Y, Is.EqualTo(again.Y)); // 반복 선택의 동일 세로 좌표
        } // 검증 끝
        [Test] // 후보 공간 부족 검증
        public void Selection_FailsWithoutFreeRoom() // 기존 콘텐츠를 침범하지 않는 실패 확인
        { // 검증 시작
            var room = new DungeonRoom(1, 0); // 기존 콘텐츠의 일반 방
            var layout = new DungeonLayout(new[] { new DungeonRoom(0, 0, false, RoomKind.Start), room }, new RoomDoor[0]); // 남은 방이 없는 층
            Assert.That(TrialEventPlacement.TrySelect(layout, 0, new[] { room }, out var selected), Is.False); // 예약된 방의 배치 거절
            Assert.That(selected, Is.Null); // 잘못된 이벤트 방 없음
            Assert.That(TrialEventPlacement.TrySelect(null, 0, null, out _), Is.False); // 누락된 층 거절
        } // 검증 끝
        [Test] // 입력 방 순서 독립성 검증
        public void Selection_DoesNotDependOnRoomListOrder() // 같은 좌표의 결정적 선택 확인
        { // 검증 시작
            var first = new DungeonLayout(new[] { new DungeonRoom(1, 2), new DungeonRoom(2, 3), new DungeonRoom(3, 4) }, new RoomDoor[0]); // 최초 입력 순서
            var second = new DungeonLayout(new[] { new DungeonRoom(3, 4), new DungeonRoom(1, 2), new DungeonRoom(2, 3) }, new RoomDoor[0]); // 다른 입력 순서
            Assert.That(TrialEventPlacement.TrySelect(first, -42, null, out var chosen), Is.True); // 최초 이벤트 방 선택
            Assert.That(TrialEventPlacement.TrySelect(second, -42, null, out var again), Is.True); // 순서 변경 후 선택
            Assert.That(again.X, Is.EqualTo(chosen.X)); // 같은 가로 좌표 유지
            Assert.That(again.Y, Is.EqualTo(chosen.Y)); // 같은 세로 좌표 유지
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
