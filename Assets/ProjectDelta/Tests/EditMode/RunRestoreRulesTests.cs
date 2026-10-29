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
        [Test] // 새로운 던전 생성 버전과 비밀 상태 검증
        public void Validate_AcceptsVersionedSecretLayoutAndRejectsMissingState() // 비밀 생성과 저장 상태 일치
        { // 검증 시작
            var data = ValidSnapshot(); // 정상 이전 회차 준비
            var layout = DungeonGenerator.GenerateWithSecret(data.seed); // 비밀방 생성 규칙
            data.version = 3; // 비밀 상태를 포함한 저장 버전
            data.generationVersion = 2; // 비밀방 생성 버전
            data.secretPassages = new SavedSecretPassage[0]; // 아직 미발견된 비밀 연결
            data.doorLock = new DoorLockState(layout).Save(); // 새 구조의 시험 문 상태
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(layout); // 새 구조의 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 새 구조의 재개 허용
            data.secretPassages = null; // 누락된 비밀 상태
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 누락된 상태 거절
        } // 검증 끝
        [Test] // 기존 생성 구조의 새 저장 형식 유지
        public void Validate_LegacyLayoutCanBeResavedWithOriginalGenerator() // 이전 회차 생성 규칙 보존
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 생성 구조의 정상 회차
            data.version = 3; // 새 저장 형식
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 빈 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 구조 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 기존 구조의 새 형식 저장 허용
            data.generationVersion = 2; // 잘못된 새 생성 규칙
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 구조로 재생성 거절
        } // 검증 끝
        [Test] // 첫 조우 상태와 새 저장 버전 검증
        public void Validate_TutorialEncounterStateMatchesGeneratedLayout() // 첫 조우의 저장 상태 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 이전 형식의 정상 데이터
            var layout = DungeonGenerator.GenerateVersioned(data.seed, 3); // 첫 일반 방이 보장된 구조
            data.version = 4; // 첫 조우 저장 버전
            data.generationVersion = 3; // 첫 조우 생성 버전
            data.secretPassages = new SavedSecretPassage[0]; // 비밀 연결의 초기 상태
            data.doorLock = new DoorLockState(layout).Save(); // 현재 구조의 시험 문
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(layout); // 현재 구조 지문
            data.tutorialHealth = 40; // 도망 후 남은 상대 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 미완료 조우의 유효 체력 허용
            data.tutorialCompleted = true; // 첫 조우 완료 상태
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 완료 조우의 남은 체력 거절
            data.tutorialHealth = 0; // 완료 상태의 체력 기록 제거
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 완료한 첫 조우 허용
        } // 검증 끝
        [Test] // 마나를 포함한 저장과 기존 저장 호환 검증
        public void Validate_ManaSnapshotRequiresValidBounds() // 새 회차의 마나 범위 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 회차 저장 준비
            data.version = 5; // 마나를 포함하는 저장 버전
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 구조 지문
            data.mana = 25; // 현재 마나 설정
            data.maxMana = 50; // 기본 최대 마나 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 마나가 유효한 저장 허용
            data.mana = 51; // 최대치를 넘는 마나 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 마나 저장 차단
            data.mana = -1; // 음수 마나 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 음수 마나 저장 차단
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 마나가 없는 기존 저장 허용
        } // 검증 끝
        [Test] // 다수 조우의 저장과 이전 저장 검증
        public void Validate_GroupEncounterRequiresCoherentEnemyHealth() // 적별 체력과 완료 상태 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 회차 저장 준비
            data.version = 6; // 다수 조우 저장 버전
            data.generationVersion = 3; // 시험 조우가 있는 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 새 구조의 비밀 기록
            var layout = DungeonGenerator.GenerateVersioned(data.seed, 3); // 시험 조우 구조 생성
            data.doorLock = new DoorLockState(layout).Save(); // 실제 시험 문의 저장 상태
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(layout); // 실제 구조 지문
            data.mana = 50; // 현재 마나
            data.maxMana = 50; // 기본 최대 마나
            data.groupEncounterEnabled = true; // 시험 다수 조우 활성화
            data.groupEnemyHealth = new[] { 0, 35 }; // 쓰러진 적과 남은 적 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 일부 생존한 조우 허용
            data.groupEnemyHealth = new[] { 0, 0 }; // 모두 쓰러진 미완료 조우
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 미완료 조우의 전멸 거절
            data.groupEnemyHealth = new[] { 70, 71 }; // 잘못된 적 최대 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 범위 밖 체력 거절
            data.groupEnemyHealth = new[] { 70, 20 }; // 정상 상태 재설정
            data.groupEncounterCompleted = true; // 완료 상태와 남은 적 동시 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 완료와 미완료 상태 충돌 거절
            data.groupEnemyHealth = null; // 완료 후 적별 체력 기록 제거
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 완료 조우 저장 허용
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 다수 조우 없는 기존 저장 허용
        } // 검증 끝
        [Test] // 회차별 영구 기록 식별자 검증
        public void Validate_RunIdentifierIsRequiredOnlyForNewVersion() // 새 회차 식별자와 기존 저장 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 회차 저장 준비
            data.version = 7; // 회차 식별자를 포함하는 저장 버전
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 구조 지문
            data.mana = 50; // 현재 마나
            data.maxMana = 50; // 기본 최대 마나
            data.runId = "invalid"; // 잘못된 회차 식별자
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 식별자 차단
            data.runId = System.Guid.NewGuid().ToString("N"); // 유효한 회차 식별자
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 정상 회차 식별자 허용
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 식별자가 없는 기존 저장 허용
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
