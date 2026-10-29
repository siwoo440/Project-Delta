using System.Linq; // 최초 일반 연결 조회
using NUnit.Framework; // 시험 조우 배치 검증 도구
using ProjectDelta.Core; // 던전과 시험 방 선택 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialGroupRoomSelectorTests // 시험 다수 슬라임 방 선택 검증
    { // 클래스 시작
        [Test] // 실제 생성된 여러 던전의 후보 검증
        public void Select_GeneratedDungeonsHaveDistinctStableNormalRoom() // 생성 시드별 시험 후보 확인
        { // 검증 시작
            for (var seed = -100; seed <= 100; seed++) // 음수와 양수 시드 순회
            { // 생성 검사 시작
                var layout = DungeonGenerator.GenerateVersioned(seed, 3); // 첫 조우가 있는 실제 구조
                var first = layout.Doors.First(door => door.RoomX == 0 && door.RoomY == 0 && !door.IsSecret); // 현재 게임의 최초 연결
                var chosen = TrialGroupRoomSelector.Select(layout, first.TargetRoomX, first.TargetRoomY); // 첫 조우를 제외한 시험 후보
                Assert.That(chosen, Is.Not.Null, "seed " + seed); // 실제 생성 방의 후보 존재
                Assert.That(chosen.Kind, Is.EqualTo(RoomKind.Normal), "seed " + seed); // 일반 방 역할 확인
                Assert.That(chosen.IsSecret, Is.False, "seed " + seed); // 비밀 방 제외 확인
                Assert.That(chosen.X != first.TargetRoomX || chosen.Y != first.TargetRoomY, Is.True, "seed " + seed); // 최초 조우 방 제외 확인
                Assert.That(TrialGroupRoomSelector.Select(layout, first.TargetRoomX, first.TargetRoomY), Is.SameAs(chosen), "seed " + seed); // 반복 선택 결과 확인
            } // 생성 검사 끝
        } // 검증 끝
        [Test] // 튜토리얼과 특수 방 제외 검증
        public void Select_ExcludesTutorialStartSpecialAndSecretRooms() // 실제 일반 방 후보 선택 확인
        { // 검증 시작
            var tutorial = new DungeonRoom(-9, 1); // 최초 튜토리얼 일반 방
            var secret = new DungeonRoom(-8, 1, true); // 비밀 방
            var stairs = new DungeonRoom(-7, 1, false, RoomKind.Stairs); // 계단 방
            var elite = new DungeonRoom(-6, 1, false, RoomKind.Elite); // 상위 개체 방
            var start = new DungeonRoom(-5, 1, false, RoomKind.Start); // 시작 역할 방
            var candidate = new DungeonRoom(-4, 1); // 시험 다수 조우 후보
            var layout = new DungeonLayout(new[] { tutorial, secret, stairs, elite, start, candidate }, new RoomDoor[0]); // 역할별 방 구성
            var chosen = TrialGroupRoomSelector.Select(layout, tutorial.X, tutorial.Y); // 실제 시험 후보 선택
            Assert.That(chosen, Is.SameAs(candidate)); // 튜토리얼과 특수 방 제외 확인
        } // 검증 끝
        [Test] // 방 배열 순서와 선택 재현성 검증
        public void Select_UsesStableCoordinatesAcrossRoomOrder() // 같은 좌표 구조의 재현 확인
        { // 검증 시작
            var tutorial = new DungeonRoom(1, 0); // 최초 튜토리얼 방
            var first = new DungeonRoom(-2, 3); // 정렬상 선택될 일반 방
            var second = new DungeonRoom(3, -5); // 다른 일반 방
            var sameX = new DungeonRoom(-2, 1); // 같은 가로 좌표의 더 작은 세로 방
            var layout = new DungeonLayout(new[] { tutorial, first, second, sameX }, new RoomDoor[0]); // 첫 방 순서
            var reversed = new DungeonLayout(new[] { sameX, second, first, tutorial }, new RoomDoor[0]); // 반대 방 순서
            Assert.That(TrialGroupRoomSelector.Select(layout, 1, 0), Is.SameAs(sameX)); // 좌표순 후보 선택 확인
            Assert.That(TrialGroupRoomSelector.Select(reversed, 1, 0), Is.SameAs(sameX)); // 방 배열 순서에 독립적인 선택
            Assert.That(TrialGroupRoomSelector.Select(layout, 1, 0), Is.SameAs(sameX)); // 반복 선택의 재현 확인
        } // 검증 끝
        [Test] // 생성 누락과 후보 부재 검증
        public void Select_ReturnsNullForMissingOrUnavailableRoom() // 생성 실패와 후보 없음 확인
        { // 검증 시작
            Assert.That(TrialGroupRoomSelector.Select(null, 1, 0), Is.Null); // 생성 실패 시 빈 결과
            var rooms = new[] // 제외되는 방만 있는 구조
            { // 목록 시작
                new DungeonRoom(0, 0, false, RoomKind.Start), // 시작 방
                new DungeonRoom(1, 0), // 최초 튜토리얼 일반 방
                new DungeonRoom(2, 0, true) // 비밀 방
            }; // 목록 끝
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 후보 없는 시험 구조
            Assert.That(TrialGroupRoomSelector.Select(layout, 1, 0), Is.Null); // 적합한 일반 방 부재 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
