using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 조우 배치와 던전 구조
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterPlacementTests // 고정 조우 배치 검증
    { // 클래스 시작
        [Test] // 층별 몬스터 정의 확인
        public void Catalog_UsesPlannedFloorsAndStableIds() // 층과 종족의 실제 정의 확인
        { // 검증 시작
            Assert.That(MonsterCatalog.Find("MON_SLIME").CanAppearOn(1), Is.True); // 슬라임 첫 층 허용
            Assert.That(MonsterCatalog.Find("MON_SLIME").CanAppearOn(2), Is.False); // 슬라임 추가 층 미지정
            Assert.That(MonsterCatalog.Find("MON_GOBLIN").CanAppearOn(1), Is.True); // 고블린 첫 층 허용
            Assert.That(MonsterCatalog.Find("MON_GOBLIN").CanAppearOn(2), Is.True); // 고블린 두 번째 층 허용
            Assert.That(MonsterCatalog.Find("unknown"), Is.Null); // 없는 종족 거절
        } // 검증 끝
        [Test] // 일반 조우의 중복 종족과 수량 검증
        public void Placement_AllowsDuplicateSpeciesUpToThree() // 같은 종족의 세 개체 허용
        { // 검증 시작
            var layout = DungeonGenerator.GenerateWithSecret(123); // 시험 던전 생성
            var room = System.Linq.Enumerable.First(layout.Rooms, item => item.Kind == RoomKind.Normal && !item.IsSecret); // 일반 조우 후보 방
            var group = EncounterPlacement.Create(layout, 1, room.X, room.Y, new[] { "MON_GOBLIN", "MON_GOBLIN", "MON_SLIME" }, false); // 중복 종족의 세 개체
            Assert.That(group, Is.Not.Null); // 유효한 조우 허용
            Assert.That(group.Monsters.Count, Is.EqualTo(3)); // 개체 수 보존
            Assert.That(EncounterPlacement.Create(layout, 1, room.X, room.Y, new[] { "MON_SLIME", "MON_SLIME", "MON_SLIME", "MON_SLIME" }, false), Is.Null); // 네 개체 거절
            Assert.That(EncounterPlacement.Create(layout, 1, room.X, room.Y, new string[0], false), Is.Null); // 빈 조우 거절
        } // 검증 끝
        [Test] // 첫 조우와 특수 방 차단 검증
        public void Placement_RequiresFirstSlimeAndCorrectRoomType() // 튜토리얼과 방 종류 검사
        { // 검증 시작
            var layout = DungeonGenerator.GenerateWithSecret(123); // 시험 던전 생성
            var room = System.Linq.Enumerable.First(layout.Rooms, item => item.Kind == RoomKind.Normal && !item.IsSecret); // 일반 조우 후보 방
            var elite = System.Linq.Enumerable.First(layout.Rooms, item => item.Kind == RoomKind.Elite); // 상위 개체 방
            var secret = System.Linq.Enumerable.First(layout.Rooms, item => item.IsSecret); // 비밀방
            Assert.That(EncounterPlacement.Create(layout, 1, room.X, room.Y, new[] { "MON_SLIME" }, true), Is.Not.Null); // 첫 슬라임 조우 허용
            Assert.That(EncounterPlacement.Create(layout, 1, room.X, room.Y, new[] { "MON_GOBLIN" }, true), Is.Null); // 첫 고블린 조우 거절
            Assert.That(EncounterPlacement.Create(layout, 1, secret.X, secret.Y, new[] { "MON_SLIME" }, false), Is.Null); // 비밀방 일반 조우 거절
            Assert.That(EncounterPlacement.Create(layout, 1, elite.X, elite.Y, new[] { "MON_SLIME_QUEEN" }, false), Is.Not.Null); // 첫 층 상위 개체 허용
            Assert.That(EncounterPlacement.Create(layout, 1, elite.X, elite.Y, new[] { "MON_SLIME_QUEEN", "MON_SLIME" }, false), Is.Not.Null); // 상위 개체와 일반 개체 동반 허용
            Assert.That(EncounterPlacement.Create(layout, 1, elite.X, elite.Y, new[] { "MON_GOBLIN_QUEEN" }, false), Is.Null); // 다른 층 상위 개체 거절
            Assert.That(EncounterPlacement.Create(layout, 2, room.X, room.Y, new[] { "MON_SLIME" }, false), Is.Null); // 등장하지 않는 층의 종족 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
