using System; // 시드 범위와 시험 난수
using System.Collections.Generic; // 방과 출구 목록
using NUnit.Framework; // 층 규칙 검증 도구
using ProjectDelta.Core; // 실제 층 생성 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class FloorLayoutRulesTests // 다섯 층과 마왕 방 검증
    { // 클래스 시작
        [Test] // 기존 시드 호환과 정수 넘침 검증
        public void FloorSeed_PreservesExistingFloorsAndWraps() // 회차와 층별 시드 확인
        { // 검증 시작
            Assert.That(FloorSeedRules.For(123, 1), Is.EqualTo(123)); // 기존 첫 층 시드
            Assert.That(FloorSeedRules.For(123, 2), Is.EqualTo(8042)); // 기존 둘째 층 시드
            Assert.That(FloorSeedRules.For(123, 5), Is.EqualTo(31799)); // 다섯째 층 시드
            Assert.That(FloorSeedRules.For(int.MaxValue, 2), Is.EqualTo(unchecked(int.MaxValue + 7919))); // 정수 넘침 재현
        } // 검증 끝
        [Test] // 기존 층과 최심부 재현 검증
        public void Floors_PreserveOriginalAndConvertOnlyBossRoles() // 세 생성 버전과 다섯 층 확인
        { // 검증 시작
            for (var version = 1; version <= 3; version++) // 기존 생성 버전 순회
            { // 버전 검사 시작
                for (var floor = 1; floor <= 5; floor++) // 실제 다섯 층 순회
                { // 층 검사 시작
                    for (var seed = 0; seed < 20; seed++) // 다양한 회차 씨드 확인
                    { // 씨드 검사 시작
                        var original = DungeonGenerator.GenerateVersioned(FloorSeedRules.For(seed, floor), version); // 기존 결정적 구조
                        var actual = FloorLayoutRules.Generate(seed, floor, version); // 실제 층 구조
                        Assert.That(FloorLayoutRules.IsValid(actual, seed, floor, version), Is.True); // 완전한 구조 검증
                        Assert.That(actual.Rooms.Count, Is.EqualTo(original.Rooms.Count)); // 방 수 유지
                        Assert.That(actual.Doors.Count, Is.EqualTo(original.Doors.Count)); // 출구 수 유지
                        if (floor < 5) // 첫 네 층 검사
                        { // 기존 층 시작
                            Assert.That(DungeonLayoutFingerprint.Compute(actual), Is.EqualTo(DungeonLayoutFingerprint.Compute(original))); // 기존 전체 구조 유지
                        } // 기존 층 끝
                        else // 마지막 층 검사
                        { // 마왕 방 시작
                            var bosses = 0; // 마왕 방 개수
                            foreach (var room in original.Rooms) // 기존 방 순회
                            { // 방 검사 시작
                                var converted = actual.GetRoom(room.X, room.Y); // 같은 좌표의 새 방
                                var expected = room.Kind == RoomKind.Stairs ? RoomKind.Boss : room.Kind == RoomKind.Elite ? RoomKind.Normal : room.Kind; // 실제 변환 역할
                                Assert.That(converted.Kind, Is.EqualTo(expected)); // 방 역할만 변경
                                Assert.That(converted.IsSecret, Is.EqualTo(room.IsSecret)); // 비밀 여부 유지
                                if (converted.Kind == RoomKind.Boss) // 마왕 방 검사
                                { // 집계 시작
                                    bosses++; // 마왕 방 개수 증가
                                } // 집계 끝
                            } // 방 검사 끝
                            Assert.That(bosses, Is.EqualTo(1)); // 정확히 한 마왕 방
                            foreach (var door in original.Doors) // 기존 출구 순회
                            { // 출구 검사 시작
                                var converted = actual.FindDoor(door.RoomX, door.RoomY, door.ApproachX, door.ApproachY, door.Direction); // 같은 실제 출구
                                Assert.That(converted != null && converted.TargetRoomX == door.TargetRoomX && converted.TargetRoomY == door.TargetRoomY && converted.TargetX == door.TargetX && converted.TargetY == door.TargetY && converted.IsSecret == door.IsSecret, Is.True); // 연결과 타일 좌표 보존
                            } // 출구 검사 끝
                        } // 마왕 방 끝
                    } // 씨드 검사 끝
                } // 층 검사 끝
            } // 버전 검사 끝
        } // 검증 끝
        [Test] // 다른 씨드와 변경된 방 거절 검증
        public void Validation_RejectsChangedRoomsAndInvalidInputs() // 완전 재현과 범위 검사
        { // 검증 시작
            var layout = FloorLayoutRules.Generate(123, 5, 3); // 실제 다섯째 층
            var rooms = new List<DungeonRoom>(); // 변조할 방 목록
            foreach (var room in layout.Rooms) // 기존 방 순회
            { // 역할 변경 시작
                rooms.Add(new DungeonRoom(room.X, room.Y, room.IsSecret, room.Kind == RoomKind.Boss ? RoomKind.Stairs : room.Kind)); // 마왕 방의 잘못된 계단 변경
            } // 역할 변경 끝
            var bad = new DungeonLayout(rooms.ToArray(), new List<RoomDoor>(layout.Doors).ToArray()); // 연결은 유지한 변조 구조
            Assert.That(FloorLayoutRules.IsValid(bad, 123, 5, 3), Is.False); // 잘못된 마왕 방 차단
            Assert.That(FloorLayoutRules.IsValid(layout, 123, 0, 3), Is.False); // 층 범위 미만 차단
            Assert.That(FloorLayoutRules.IsValid(layout, 123, 6, 3), Is.False); // 층 범위 초과 차단
            Assert.That(FloorLayoutRules.IsValid(layout, 123, 5, 4), Is.False); // 알 수 없는 생성 버전 차단
        } // 검증 끝
        [Test] // 진행과 상점 및 몬스터 범위 검증
        public void FloorLimits_EnforceLastFloorAndNewCatalogValues() // 다섯 층 진행과 기존 시험 상점
        { // 검증 시작
            var progress = new FloorProgress(); // 첫 층 진행 상태
            Assert.That(progress.TryRestore(5), Is.True); // 다섯째 층 복원
            Assert.That(progress.TryBegin(), Is.False); // 마지막 층의 전환 차단
            Assert.That(progress.TryRestore(6), Is.False); // 여섯째 층 복원 차단
            for (var floor = 1; floor <= 5; floor++) // 기획된 모든 층 검사
            { // 층 검사 시작
                Assert.That(TrialShopCatalog.Create(floor, new Random(5)) != null, Is.True); // 해당 층 시험 상점 생성
                Assert.That(MonsterCatalog.Find("MON_MIMIC").CanAppearOn(floor), Is.True); // 해당 층의 미믹 등장
                Assert.That(ExperienceRules.TryCalculateVictory(floor, 1, 0, out var experience), Is.True); // 일반 전투 경험치 계산
                Assert.That(experience, Is.EqualTo(40 + floor * 20)); // 층별 기획 경험치
            } // 층 검사 끝
            Assert.That(TrialShopCatalog.Create(6, new Random(5)), Is.Null); // 범위 밖 상점 차단
            Assert.That(MonsterCatalog.Find("MON_MIMIC").CanAppearOn(6), Is.False); // 범위 밖 미믹 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
