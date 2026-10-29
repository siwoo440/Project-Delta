using System.Linq; // 시작 방 출구 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 던전과 첫 조우 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TutorialEncounterTests // 첫 슬라임 조우 생성 검증
    { // 클래스 시작
        [Test] // 다양한 시드에서 첫 일반 방 보장
        public void NewGeneration_FirstConnectedRoomSupportsSlimeEncounter() // 첫 연결 방의 슬라임 배치 확인
        { // 검증 시작
            for (var seed = -100; seed <= 100; seed++) // 음수와 양수 시드 순회
            { // 시드 검사 시작
                var old = DungeonGenerator.GenerateVersioned(seed, 2); // 기존 비밀방 생성 버전
                var current = DungeonGenerator.GenerateVersioned(seed, 3); // 첫 조우를 지원하는 새 생성 버전
                Assert.That(DungeonGenerator.IsValidVersioned(current, 3), Is.True, "seed " + seed); // 필수 구조 검사
                var first = current.Doors.First(door => door.RoomX == 0 && door.RoomY == 0 && !door.IsSecret); // 시작 방의 첫 출구
                var room = current.GetRoom(first.TargetRoomX, first.TargetRoomY); // 첫 연결 방
                Assert.That(room.Kind, Is.EqualTo(RoomKind.Normal), "seed " + seed); // 첫 연결 방의 일반 역할
                Assert.That(EncounterPlacement.Create(current, 1, room.X, room.Y, new[] { "MON_SLIME" }, true), Is.Not.Null); // 첫 슬라임 조우 배치 허용
                Assert.That(DungeonLayoutFingerprint.Compute(old), Is.EqualTo(DungeonLayoutFingerprint.Compute(DungeonGenerator.GenerateWithSecret(seed))), "legacy seed " + seed); // 기존 생성 버전 불변 확인
            } // 시드 검사 끝
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
