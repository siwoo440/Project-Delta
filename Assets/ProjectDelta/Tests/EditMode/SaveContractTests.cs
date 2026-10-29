using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 저장 계약
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class SaveContractTests // 저장 계약 검증
    { // 클래스 시작
        [Test] // 정상 회차 검증
        public void Run_AcceptsValidSnapshot() // 정상 회차 허용
        { // 검증 시작
            var run = new RunSaveData { version = 1, floor = 2, health = 40, maxHealth = 100, slots = new[] { new SaveItemSlot { itemId = "potion", quantity = 2, stackLimit = 5 } } }; // 정상 회차 데이터
            Assert.That(run.IsValid(), Is.True); // 정상 데이터 허용 확인
        } // 검증 끝
        [Test] // 회차 버전과 체력 검증
        public void Run_RejectsUnknownVersionAndInvalidHealth() // 버전과 체력 오류 거절
        { // 검증 시작
            var run = new RunSaveData { version = 11, floor = 1, health = 1, maxHealth = 1, slots = new SaveItemSlot[0] }; // 알 수 없는 버전
            Assert.That(run.IsValid(), Is.False); // 새 버전 거절 확인
            run.version = 1; // 지원 버전 설정
            run.health = 2; // 최대치 초과 체력
            Assert.That(run.IsValid(), Is.False); // 잘못된 체력 거절 확인
            run.health = 1; // 정상 체력 설정
            run.floor = 0; // 잘못된 층 번호
            Assert.That(run.IsValid(), Is.False); // 잘못된 층 거절 확인
        } // 검증 끝
        [Test] // 성장과 보상 후보 저장 검증
        public void Run_RejectsInvalidGrowthAndRewardCandidate() // 보상 복원의 잘못된 상태 거절
        { // 검증 시작
            var run = new RunSaveData { version = 8, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, experience = 0, statPoints = 5, gold = 0, slots = new SaveItemSlot[0] }; // 정상 성장 저장
            Assert.That(run.IsValid(), Is.True); // 정상 성장 허용
            run.experience = 100; // 이미 레벨이 올라야 하는 경험치
            Assert.That(run.IsValid(), Is.False); // 정규화되지 않은 성장 차단
            run.experience = 0; // 정상 경험치 복구
            run.gold = -1; // 음수 골드
            Assert.That(run.IsValid(), Is.False); // 잘못된 골드 차단
            run.gold = 0; // 정상 골드 복구
            run.pendingReward = new SavedBattleReward { transactionId = "invalid", experience = 60, gold = 10, potionCount = 1 }; // 잘못된 거래 식별자
            run.hasPendingReward = true; // 실제 미완료 후보 표시
            Assert.That(run.IsValid(), Is.False); // 복구 불가능한 후보 차단
        } // 검증 끝
        [Test] // 보상 거래 중복 저장 검증
        public void Run_RejectsCompletedAndPendingSameReward() // 완료 거래의 재지급 차단
        { // 검증 시작
            var id = System.Guid.NewGuid().ToString("N"); // 고유 전투 거래 식별자
            var run = new RunSaveData { version = 8, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, statPoints = 5, slots = new SaveItemSlot[0], hasPendingReward = true, pendingReward = new SavedBattleReward { transactionId = id, normalEnemies = 1, experience = 60, gold = 10 }, rewardReceipts = new[] { new SavedRewardReceipt { transactionId = id, tookPotion = false } } }; // 같은 거래가 완료와 대기에 동시에 있는 저장
            Assert.That(run.IsValid(), Is.False); // 완료된 거래의 재지급 거절
            run.hasPendingReward = false; // 미완료 후보 해제
            Assert.That(run.IsValid(), Is.True); // 단일 완료 거래 허용
            run.rewardReceipts = new[] { new SavedRewardReceipt { transactionId = id }, new SavedRewardReceipt { transactionId = id } }; // 중복 완료 거래
            Assert.That(run.IsValid(), Is.False); // 중복 보상 거래 거절
        } // 검증 끝
        [Test] // 새 저장의 보상 아이템 종류 검증
        public void Run_RequiresKnownRewardItemForNewVersion() // 미지원 보상과 이전 저장 확인
        { // 검증 시작
            var run = new RunSaveData { version = 10, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, statPoints = 5, slots = new SaveItemSlot[0], hasPendingReward = true, pendingReward = new SavedBattleReward { transactionId = System.Guid.NewGuid().ToString("N"), normalEnemies = 1, experience = 60, potionCount = 1, itemId = "unknown" } }; // 알 수 없는 보상 후보
            Assert.That(run.IsValid(), Is.False); // 새 저장의 미등록 아이템 차단
            run.pendingReward.itemId = "trial-mana-potion"; // 실제 시험 마나 회복약
            Assert.That(run.IsValid(), Is.True); // 등록된 보상 후보 허용
            run.version = 9; // 종류가 저장되지 않았던 이전 버전
            run.pendingReward.itemId = null; // 이전 저장의 누락된 아이템 종류
            Assert.That(run.IsValid(), Is.True); // 이전 소형 회복약 후보 호환
        } // 검증 끝
        [Test] // 아이템 수량과 빈 칸 검증
        public void Run_RejectsInvalidSlots() // 잘못된 아이템 거절
        { // 검증 시작
            var run = new RunSaveData { version = 1, floor = 1, health = 1, maxHealth = 1, slots = new[] { new SaveItemSlot { itemId = "potion", quantity = 6, stackLimit = 5 } } }; // 중첩 한도 초과
            Assert.That(run.IsValid(), Is.False); // 수량 오류 거절 확인
            run.slots[0] = new SaveItemSlot { itemId = "", quantity = 0, stackLimit = 1 }; // 정상 빈 슬롯
            Assert.That(run.IsValid(), Is.True); // 빈 슬롯 허용 확인
            run.slots = null; // 누락된 슬롯 목록
            Assert.That(run.IsValid(), Is.False); // 누락 거절 확인
        } // 검증 끝
        [Test] // 프로필 관찰 기록 검증
        public void Profile_RejectsDuplicateOrEmptyObservationIds() // 중복과 빈 식별자 거절
        { // 검증 시작
            var profile = new ProfileSaveData { version = 1, observationIds = new[] { "queen", "slime" } }; // 정상 영구 기록
            Assert.That(profile.IsValid(), Is.True); // 정상 기록 허용 확인
            profile.observationIds = new[] { "queen", "queen" }; // 중복 관찰 기록
            Assert.That(profile.IsValid(), Is.False); // 중복 기록 거절 확인
            profile.observationIds = new[] { " " }; // 공백 관찰 기록
            Assert.That(profile.IsValid(), Is.False); // 빈 기록 거절 확인
            profile.observationIds = new string[0]; // 빈 관찰 기록
            profile.version = 2; // 알 수 없는 버전
            Assert.That(profile.IsValid(), Is.False); // 새 버전 거절 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
