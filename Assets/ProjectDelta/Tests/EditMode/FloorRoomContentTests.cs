using NUnit.Framework; // 층별 콘텐츠 검증 도구
using ProjectDelta.Core; // 층별 방과 상자 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class FloorRoomContentTests // 층별 상자와 함정 저장 검증
    { // 검증 클래스 시작
        [Test] // 층별 상태의 복원과 독립성 검증
        public void FloorContent_RestoresDeterministicPlacementAndCopiesState() // 실제 배치와 저장 복원 확인
        { // 검증 시작
            var layout = new DungeonLayout(new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0), new DungeonRoom(5, 0) }, new RoomDoor[0]); // 결정적 시험 층
            Assert.That(FloorRoomContent.TryCreate(1, layout, 7, null, out var state), Is.True); // 시험 상자와 함정 생성
            var saved = state.Snapshot(); // 독립 저장본 생성
            Assert.That(saved.chests[0].chest.grade, Is.EqualTo((int)ChestGrade.Common)); // 첫 일반 상자 시험 배치
            Assert.That(saved.chests[0].chest.wasLocked, Is.False); // 첫 상자의 미잠금 시험값
            Assert.That(saved.chests[1].chest.grade, Is.EqualTo((int)ChestGrade.Advanced)); // 두 번째 고급 상자 시험 배치
            Assert.That(saved.chests[1].chest.wasLocked, Is.True); // 두 번째 상자의 잠금 시험값
            Assert.That(saved.chests.Length, Is.EqualTo(3)); // 홀수 씨드의 세 상자 시험 배치
            Assert.That(saved.chests[2].chest.grade, Is.EqualTo((int)ChestGrade.Rare)); // 세 번째 희귀 상자 시험 배치
            Assert.That(saved.chests[2].chest.wasLocked, Is.False); // 세 번째 상자의 미잠금 시험값
            Assert.That(FloorRoomContent.TryRestore(saved, layout, 7, null, out var restored), Is.True); // 정확한 층별 배치 복원
            saved.chests[0].chest.chestId = "changed"; // 외부 저장본 변경
            Assert.That(restored.Snapshot().chests[0].chest.chestId, Is.Not.EqualTo("changed")); // 복원본의 심층 독립성
            Assert.That(state.TryCompleteTrap(out var done), Is.True); // 첫 함정 완료 전이
            Assert.That(done.Snapshot().trapCompleted, Is.True); // 전이 결과의 함정 완료
            Assert.That(state.Snapshot().trapCompleted, Is.False); // 기존 상태 변경 방지
            Assert.That(done.TryCompleteTrap(out _), Is.False); // 함정 재완료 차단
        } // 검증 끝
        [Test] // 중복과 불일치 저장 거절 검증
        public void FloorContent_RejectsDuplicateAndExcludedRooms() // 잘못된 콘텐츠 저장 차단
        { // 검증 시작
            var layout = new DungeonLayout(new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0), new DungeonRoom(5, 0) }, new RoomDoor[0]); // 시험 층 준비
            Assert.That(FloorRoomContent.TryCreate(1, layout, 7, null, out var state), Is.True); // 정상 층 콘텐츠 생성
            var saved = state.Snapshot(); // 독립 저장 데이터
            saved.chests[1].chest.chestId = saved.chests[0].chest.chestId; // 중복 상자 식별자 삽입
            Assert.That(FloorRoomContent.TryRestore(saved, layout, 7, null, out _), Is.False); // 중복 상자 저장 거절
            saved = state.Snapshot(); // 정상 저장 복구
            saved.trapX = saved.chests[0].x; // 상자 방과 함정 좌표 일치
            saved.trapY = saved.chests[0].y; // 동일 방의 세로 좌표 일치
            Assert.That(FloorRoomContent.TryRestore(saved, layout, 7, null, out _), Is.False); // 상자와 함정 중첩 거절
            Assert.That(FloorRoomContent.TryRestore(state.Snapshot(), layout, 7, new[] { state.TrapRoom }, out _), Is.False); // 기존 조우 제외 방 불일치 거절
            Assert.That(FloorRoomContent.HasUniqueFloors(new[] { state.Snapshot(), state.Snapshot() }), Is.False); // 동일 층 중복 저장 거절
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
