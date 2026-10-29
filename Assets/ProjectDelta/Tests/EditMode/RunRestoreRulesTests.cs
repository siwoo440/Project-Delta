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
        [Test] // 새 저장의 던전 구조 일치 검증
        public void Validate_RequiresMatchingLayoutFingerprintForNewVersion() // 던전 구조 변경 거절
        { // 검증 시작
            var data = ValidSnapshot(); // 정상 이전 저장 준비
            data.version = 2; // 새 회차 저장 버전
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 실제 생성 구조의 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 같은 던전의 재개 허용
            data.layoutFingerprint = "incorrect"; // 변조된 던전 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 구조 거절
            data.layoutFingerprint = null; // 지문 누락
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 누락된 지문 거절
        } // 검증 끝
        [Test] // 이전 회차 저장 호환성 검증
        public void Validate_AcceptsLegacyVersionWithoutFingerprint() // 이전 저장 읽기 유지
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 버전 회차
            Assert.That(data.version, Is.EqualTo(1)); // 기존 버전 확인
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 지문이 없는 기존 회차 허용
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
