using NUnit.Framework; // 휴식 규칙 검증 도구
using ProjectDelta.Core; // 실제 던전 모델
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialRestRulesTests // 휴식 방 규칙 검증
    { // 검증 클래스 시작
        [Test] // 휴식 규칙 존재 검증
        public void RestRules_AreAvailable() // 새 규칙의 실제 존재 확인
        { // 검증 시작
            Assert.That(typeof(DungeonLayout).Assembly.GetType("ProjectDelta.Core.TrialRestRules"), Is.Not.Null); // 휴식 규칙 제공 확인
        } // 검증 끝
        [Test] // 실제 기존 콘텐츠와 위치 복원 검증
        public void Placement_ExcludesOccupiedRoomsAndRestores() // 충돌 없는 재현 배치 확인
        { // 검증 시작
            var rooms = new[] { new DungeonRoom(0, 0, false, RoomKind.Start), new DungeonRoom(1, 0), new DungeonRoom(2, 0), new DungeonRoom(3, 0), new DungeonRoom(4, 0), new DungeonRoom(5, 0), new DungeonRoom(6, 0), new DungeonRoom(7, 0), new DungeonRoom(8, 0), new DungeonRoom(9, 0), new DungeonRoom(10, 0) }; // 충분한 일반 방 목록
            var layout = new DungeonLayout(rooms, new RoomDoor[0]); // 시험 층 구조
            var encounters = new[] { rooms[1] }; // 기존 몬스터 조우 방
            Assert.That(FloorRoomContent.TryCreate(1, layout, 42, encounters, out var content), Is.True); // 기존 상자와 함정 생성
            var floor = content.Snapshot(); // 독립 콘텐츠 저장 정보
            Assert.That(TrialEventPlacement.TrySelect(layout, 42, floor, encounters, out var eventRoom), Is.True); // 실제 시험 이벤트 배치
            Assert.That(TrialNpcPlacement.TrySelect(layout, 42, floor, encounters, eventRoom, out var npcRoom), Is.True); // 실제 상인 배치
            Assert.That(TrialRestRules.TryCreate(1, layout, 42, floor, encounters, eventRoom, npcRoom, out var rest), Is.True); // 빈 휴식 방 배치
            Assert.That(TrialRestRules.TryRestore(rest, layout, 42, floor, encounters, eventRoom, npcRoom), Is.True); // 동일 배치 복원 확인
            Assert.That(rest.x, Is.Not.EqualTo(eventRoom.X)); // 이벤트 방 충돌 방지
            Assert.That(rest.x, Is.Not.EqualTo(npcRoom.X)); // 상인 방 충돌 방지
            Assert.That(rest.x, Is.Not.EqualTo(floor.trapX)); // 함정 방 충돌 방지
            Assert.That(rest.x, Is.Not.EqualTo(rooms[1].X)); // 조우 방 충돌 방지
            foreach (var chest in floor.chests) // 상자 방 순회
            { // 상자 위치 검사 시작
                Assert.That(rest.x, Is.Not.EqualTo(chest.x)); // 상자 방 충돌 방지
            } // 상자 위치 검사 끝
            rest.x = eventRoom.X; // 저장 좌표 변조
            Assert.That(TrialRestRules.TryRestore(rest, layout, 42, floor, encounters, eventRoom, npcRoom), Is.False); // 다른 방으로 변조된 저장 차단
        } // 검증 끝
        [Test] // 새 회차 시작 씨드 재선택 검증
        public void StartSeed_ReplacesUnsuitableSeedAndIsDeterministic() // 접근 가능한 휴식 방과 재현성 확인
        { // 검증 시작
            var rules = typeof(DungeonLayout).Assembly.GetType("ProjectDelta.Core.TrialStartSeedRules"); // 실제 시작 씨드 규칙 조회
            Assert.That(rules, Is.Not.Null); // 시작 씨드 규칙 제공 확인
            var suitable = rules.GetMethod("IsSuitable"); // 첫 층의 휴식 배치 검사
            var select = rules.GetMethod("TrySelect"); // 안전한 시작 씨드 선택
            Assert.That(suitable.Invoke(null, new object[] { 55 }), Is.False); // 휴식이 부족한 씨드 거절
            var arguments = new object[] { 55, 0 }; // 실패 씨드와 결과 자리
            Assert.That(select.Invoke(null, arguments), Is.True); // 접근 가능한 새 씨드 선택
            var chosen = (int)arguments[1]; // 실제 선택된 씨드
            Assert.That(chosen, Is.Not.EqualTo(55)); // 부족한 씨드 교체 확인
            Assert.That(suitable.Invoke(null, new object[] { chosen }), Is.True); // 선택한 씨드의 휴식 보장
            var again = new object[] { 55, 0 }; // 같은 시작 씨드의 재검사
            Assert.That(select.Invoke(null, again), Is.True); // 동일한 재선택 성공
            Assert.That(again[1], Is.EqualTo(chosen)); // 동일 입력의 결정적 결과
        } // 검증 끝
        [Test] // 회복량과 단일 사용 검증
        public void Use_HealsThirtyPercentOnceAndPreservesInput() // 최대치 기준 회복과 사용 기록 확인
        { // 검증 시작
            var rest = new SavedFloorRest { floor = 1, x = 3, y = 2 }; // 미사용 휴식 방
            Assert.That(TrialRestRules.TryUse(rest, 50, 100, 20, 50, out var next, out var health, out var mana), Is.True); // 체력과 마나 회복 요청
            Assert.That(health, Is.EqualTo(80)); // 최대 체력의 삼십 퍼센트 회복
            Assert.That(mana, Is.EqualTo(35)); // 최대 마나의 삼십 퍼센트 회복
            Assert.That(rest.used, Is.False); // 입력 저장의 원본 보존
            Assert.That(next.used, Is.True); // 사용 완료 기록
            Assert.That(TrialRestRules.TryUse(next, health, 100, mana, 50, out _, out _, out _), Is.False); // 사용 완료 후 재사용 차단
        } // 검증 끝
        [Test] // 최대치 제한과 정수 범위 검증
        public void Use_ClampsAndRejectsInvalidValues() // 최대치와 입력 보호 확인
        { // 검증 시작
            var rest = new SavedFloorRest { floor = 1 }; // 미사용 휴식 기록
            Assert.That(TrialRestRules.TryUse(rest, 99, 100, 49, 50, out _, out var health, out var mana), Is.True); // 최대치 근처 회복
            Assert.That(health, Is.EqualTo(100)); // 체력 최대치 제한
            Assert.That(mana, Is.EqualTo(50)); // 마나 최대치 제한
            Assert.That(TrialRestRules.TryUse(rest, int.MaxValue - 1, int.MaxValue, 0, int.MaxValue, out _, out health, out mana), Is.True); // 큰 수치의 안전한 회복
            Assert.That(health, Is.EqualTo(int.MaxValue)); // 정수 넘침 없는 체력 제한
            Assert.That(mana, Is.EqualTo((int)((long)int.MaxValue * 3 / 10))); // 정수 넘침 없는 마나 회복
            Assert.That(TrialRestRules.TryUse(rest, 0, 100, 0, 50, out _, out _, out _), Is.False); // 사망 상태 회복 차단
            Assert.That(TrialRestRules.TryUse(rest, 10, 100, 51, 50, out _, out _, out _), Is.False); // 잘못된 자원 상태 차단
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
