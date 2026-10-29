using System.Linq; // 생성된 비밀 연결 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 던전 생성과 발견 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class SecretPassageTests // 비밀방 생성과 발견 검증
    { // 클래스 시작
        [Test] // 기존 생성과 새 비밀방 생성 분리
        public void Generate_LeavesLegacyUnchangedAndAddsOneSecretRoom() // 기존 저장 구조 보존
        { // 검증 시작
            var legacy = DungeonGenerator.Generate(123); // 기존 생성 규칙
            var current = DungeonGenerator.GenerateWithSecret(123); // 새 비밀방 생성 규칙
            Assert.That(legacy.Rooms.Count(room => room.IsSecret), Is.Zero); // 기존 저장에 비밀방 없음
            Assert.That(current.Rooms.Count(room => room.IsSecret), Is.EqualTo(1)); // 시험 비밀방 한 곳
            Assert.That(current.Doors.Count(door => door.IsSecret), Is.EqualTo(2)); // 양방향 비밀 연결 확인
            Assert.That(DungeonLayoutFingerprint.Compute(current), Is.EqualTo(DungeonLayoutFingerprint.Compute(DungeonGenerator.GenerateWithSecret(123)))); // 같은 시드의 구조 재현
        } // 검증 끝
        [Test] // 비밀 연결의 발견과 개방 분리
        public void Secret_RemainsHiddenAndBlockedUntilDiscoveredAndOpened() // 발견과 개방 순서 확인
        { // 검증 시작
            var layout = DungeonGenerator.GenerateWithSecret(123); // 시험 비밀방 생성
            var door = layout.Doors.First(item => item.IsSecret && !layout.GetRoom(item.RoomX, item.RoomY).IsSecret); // 일반 방에서 비밀방으로 가는 연결
            var discovery = new DungeonDiscovery(layout, door.RoomX, door.RoomY); // 단서가 있는 현재 방
            Assert.That(discovery.GetVisibility(door.TargetRoomX, door.TargetRoomY), Is.EqualTo(RoomVisibility.Hidden)); // 발견 전 비밀방 숨김
            Assert.That(discovery.TryVisit(door.TargetRoomX, door.TargetRoomY), Is.False); // 발견 전 출입 거절
            Assert.That(discovery.TryDiscoverSecret(door), Is.True); // 비밀벽 발견
            Assert.That(discovery.GetVisibility(door.TargetRoomX, door.TargetRoomY), Is.EqualTo(RoomVisibility.Connected)); // 발견된 미방문 비밀방 표시
            Assert.That(discovery.TryVisit(door.TargetRoomX, door.TargetRoomY), Is.False); // 개방 전 출입 거절
            Assert.That(discovery.TryOpenSecret(door), Is.True); // 발견한 벽 개방
            Assert.That(discovery.TryVisit(door.TargetRoomX, door.TargetRoomY), Is.True); // 열린 비밀방 진입
            Assert.That(discovery.TryVisit(door.RoomX, door.RoomY), Is.True); // 열린 통로의 귀환
        } // 검증 끝
        [Test] // 비밀 연결 발견과 개방 상태의 저장 복원
        public void Restore_PreservesDiscoveredAndOpenedSecret() // 비밀 통로 저장 상태 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0), new DungeonRoom(1, 0, true) }; // 시험 일반 방과 비밀방
            var doors = new[] { new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0, true), new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0, true) }; // 양방향 비밀 연결
            var layout = new DungeonLayout(rooms, doors); // 시험 던전 구조
            var discovery = new DungeonDiscovery(layout, 0, 0); // 시작 방 기록
            Assert.That(discovery.TryDiscoverSecret(doors[0]), Is.True); // 비밀벽 발견
            var found = DungeonDiscovery.FromSaved(layout, discovery.SaveVisited(), 0, 0, discovery.SaveSecrets()); // 발견 상태 복원
            Assert.That(found.IsSecretDiscovered(doors[0]), Is.True); // 발견 기록 보존
            Assert.That(found.IsSecretOpen(doors[0]), Is.False); // 아직 닫힌 상태 유지
            Assert.That(discovery.TryOpenSecret(doors[0]), Is.True); // 비밀벽 개방
            Assert.That(discovery.TryVisit(1, 0), Is.True); // 비밀방 방문
            var opened = DungeonDiscovery.FromSaved(layout, discovery.SaveVisited(), 1, 0, discovery.SaveSecrets()); // 개방과 방문 상태 복원
            Assert.That(opened, Is.Not.Null); // 복원 가능 상태 확인
            Assert.That(opened.IsSecretOpen(doors[1]), Is.True); // 복귀 방향 개방 보존
            Assert.That(opened.GetVisibility(1, 0), Is.EqualTo(RoomVisibility.Visited)); // 비밀방 방문 기록 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
