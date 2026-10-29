using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 회차 복원 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RunRestoreRulesTests // 회차 복원 안전성 검증
    { // 클래스 시작
        private static RunSaveData ValidSnapshot() // 재개 가능한 기본 회차
        { // 생성 시작
            var layout = DungeonGenerator.Generate(123); // 재현 가능한 던전
            return new RunSaveData { version = 1, seed = 123, floor = 1, health = 90, maxHealth = 100, slots = new InventoryState().SaveSlots(), visitedRooms = new[] { new SavedRoom() }, doorLock = new DoorLockState(layout).Save(), history = new string[0] }; // 시작 방의 정상 저장본
        } // 생성 끝
        [Test] // 유효한 회차의 복원 가능 여부
        public void Validate_AcceptsCoherentSnapshot() // 정상 회차 허용
        { // 검증 시작
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 정상 저장본 검증
        } // 검증 끝
        [Test] // 불일치 상태의 복원 거절
        public void Validate_RejectsInvalidLocationAndInventory() // 잘못된 저장 거절
        { // 검증 시작
            var data = ValidSnapshot(); // 정상 회차 준비
            data.roomX = 999; // 없는 방 좌표
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 방 거절
            data = ValidSnapshot(); // 정상 회차 재준비
            data.keys = -1; // 음수 열쇠 수량
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 열쇠 거절
            data = ValidSnapshot(); // 정상 회차 재준비
            data.slots = new SaveItemSlot[0]; // 누락된 인벤토리 칸
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 슬롯 수 오류 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
