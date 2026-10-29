using NUnit.Framework; // 방 콘텐츠 검증 도구
using ProjectDelta.Core; // 방과 함정 시험 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialRoomContentRulesTests // 결정적 배치와 함정 검증
    { // 검증 클래스 시작
        [Test] // 기존 조우 방의 결정적 제외 검증
        public void Excluded_ReproducesFirstEncounterAndOptionalGroup() // 런타임 조우 방과 같은 좌표 확인
        { // 검증 시작
            var layout = DungeonGenerator.GenerateVersioned(42, 3); // 실제 세 번째 생성 규칙의 첫 층
            DungeonRoom first = null; // 첫 일반 연결의 목적지
            foreach (var door in layout.Doors) // 실제 시작 방의 출구 순회
            { // 첫 조우 탐색 시작
                if (door.RoomX == 0 && door.RoomY == 0 && !door.IsSecret) // 첫 비밀 아닌 출구 검사
                { // 출구 발견 시작
                    first = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 실제 첫 조우 방
                    break; // 이후 연결 제외
                } // 출구 발견 끝
            } // 첫 조우 탐색 끝
            var onlyFirst = TrialRoomContentPlacement.Excluded(layout, 1, 3, false); // 다수 조우 없는 제외 방
            var withGroup = TrialRoomContentPlacement.Excluded(layout, 1, 3, true); // 다수 조우를 포함한 제외 방
            var group = TrialGroupRoomSelector.Select(layout, first.X, first.Y); // 기존 런타임의 다수 조우 선택
            Assert.That(onlyFirst.Length, Is.EqualTo(1)); // 첫 조우만 제외
            Assert.That(onlyFirst[0].X, Is.EqualTo(first.X)); // 첫 조우 가로 좌표
            Assert.That(onlyFirst[0].Y, Is.EqualTo(first.Y)); // 첫 조우 세로 좌표
            Assert.That(withGroup.Length, Is.EqualTo(2)); // 첫 조우와 다수 조우 제외
            Assert.That(withGroup[1].X, Is.EqualTo(group.X)); // 다수 조우 가로 좌표
            Assert.That(withGroup[1].Y, Is.EqualTo(group.Y)); // 다수 조우 세로 좌표
            Assert.That(TrialRoomContentPlacement.Excluded(layout, 2, 3, true), Is.Empty); // 다른 층의 제외 없음
            Assert.That(TrialRoomContentPlacement.Excluded(layout, 1, 2, true), Is.Empty); // 이전 생성 규칙의 제외 없음
            Assert.That(FloorRoomContent.TryCreate(1, layout, 42, withGroup, out var created), Is.True); // 실제 제외 방 적용 배치
            Assert.That(FloorRoomContent.TryRestore(created.Snapshot(), layout, 42, TrialRoomContentPlacement.Excluded(layout, 1, 3, true), out _), Is.True); // 공통 제외 규칙의 저장 복원
        } // 검증 끝
        [Test] // 동일 시드 배치 검증
        public void Placement_ExcludesSpecialRoomsAndIsDeterministic() // 상자와 함정의 독립 위치 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0, false, RoomKind.Stairs), new DungeonRoom(2, 0, false, RoomKind.Elite), new DungeonRoom(3, 0, true), new DungeonRoom(4, 0), new DungeonRoom(5, 0), new DungeonRoom(6, 0), new DungeonRoom(7, 0), new DungeonRoom(8, 0), new DungeonRoom(9, 0) }; // 필수 방과 일반 방 구성
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 좌표별 시험 방 구성
            Assert.That(TrialRoomContentPlacement.TryCreate(layout, 42, new[] { rooms[4] }, out var first), Is.True); // 제외 방을 반영한 배치
            Assert.That(TrialRoomContentPlacement.TryCreate(layout, 42, new[] { rooms[4] }, out var second), Is.True); // 같은 입력의 반복 배치
            Assert.That(first.ChestRooms.Length, Is.InRange(2, 3)); // 층당 상자 수 확인
            Assert.That(second.ChestRooms.Length, Is.EqualTo(first.ChestRooms.Length)); // 동일 시드 상자 수 보존
            for (var index = 0; index < first.ChestRooms.Length; index++) // 상자 방 순회
            { // 상자 위치 검사 시작
                Assert.That(first.ChestRooms[index].X, Is.EqualTo(second.ChestRooms[index].X)); // 결정적인 상자 위치
                Assert.That(first.ChestRooms[index].X, Is.GreaterThanOrEqualTo(5)); // 필수와 제외 방 차단
                Assert.That(first.ChestRooms[index].X, Is.Not.EqualTo(first.TrapRoom.X)); // 상자와 함정 중복 차단
            } // 상자 위치 검사 끝
            Assert.That(first.TrapRoom.X, Is.EqualTo(second.TrapRoom.X)); // 결정적인 시험 함정 위치
        } // 검증 끝
        [Test] // 방 부족 거절 검증
        public void Placement_RejectsNullAndInsufficientRooms() // 안전한 배치 실패 확인
        { // 검증 시작
            Assert.That(TrialRoomContentPlacement.TryCreate(null, 1, null, out _), Is.False); // 생성되지 않은 층 거절
            var layout = new DungeonLayout(new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0) }, new RoomDoor[0]); // 상자와 함정 배치 부족
            Assert.That(TrialRoomContentPlacement.TryCreate(layout, 1, null, out _), Is.False); // 필수 콘텐츠 공간 부족 거절
        } // 검증 끝
        [TestCase(0, 14, true)] // 기본 회피 성공 경계
        [TestCase(0, 15, false)] // 기본 회피 실패 경계
        [TestCase(100, 74, true)] // 최대 회피 성공 경계
        [TestCase(100, 75, false)] // 최대 회피 실패 경계
        public void Trap_TriggersOnceAndRestoresCompletion(int evasion, int roll, bool avoided) // 회피와 저장 복원 확인
        { // 검증 시작
            var state = TrialTrapState.Untriggered; // 아직 작동하지 않은 함정
            Assert.That(state.TryTrigger(evasion, roll, 10, out var next, out var result), Is.True); // 첫 진입 판정
            Assert.That(result.Avoided, Is.EqualTo(avoided)); // 회피 확률 경계 확인
            Assert.That(result.Damage, Is.EqualTo(avoided ? 0 : 10)); // 회피 여부에 따른 피해 확인
            Assert.That(TrialTrapState.TryRestore(next.Completed, out var restored), Is.True); // 완료 상태 저장 복원
            Assert.That(restored.TryTrigger(evasion, roll, 10, out _, out _), Is.False); // 재진입의 중복 발동 차단
        } // 검증 끝
        [Test] // 잘못된 판정 입력 검증
        public void Trap_RejectsInvalidInputsWithoutCompletion() // 판정 입력 범위 확인
        { // 검증 시작
            var state = TrialTrapState.Untriggered; // 미완료 함정 준비
            Assert.That(state.TryTrigger(-1, 0, 10, out _, out _), Is.False); // 음수 회피 거절
            Assert.That(state.TryTrigger(0, 100, 10, out _, out _), Is.False); // 주사위 상한 거절
            Assert.That(state.TryTrigger(0, -1, 10, out _, out _), Is.False); // 음수 주사위 거절
            Assert.That(state.TryTrigger(0, 0, 9, out _, out _), Is.False); // 시험 피해 하한 거절
            Assert.That(state.TryTrigger(0, 0, 19, out _, out _), Is.False); // 시험 피해 상한 거절
            Assert.That(state.Completed, Is.False); // 원본 완료 상태 보존
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
