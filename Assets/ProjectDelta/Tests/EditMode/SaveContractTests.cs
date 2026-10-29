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
            var run = new RunSaveData { version = 21, floor = 1, health = 1, maxHealth = 1, slots = new SaveItemSlot[0] }; // 알 수 없는 버전
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
        [Test] // 장비 저장의 부위와 용량 검증
        public void Run_ValidatesEquipmentAndBagCapacity() // 장비와 확장 인벤토리의 일관성 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 기본 인벤토리
            inventory.TryResize(12); // 낡은 주머니의 두 칸 확장
            var run = new RunSaveData { version = 11, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, statPoints = 5, slots = inventory.SaveSlots(), bagId = EquipmentCatalog.OldBagId }; // 가방이 장착된 회차
            Assert.That(run.IsValid(), Is.True); // 장비와 용량이 일치하는 저장 허용
            run.bagId = EquipmentCatalog.BasicSwordId; // 가방 부위의 잘못된 무기
            Assert.That(run.IsValid(), Is.False); // 다른 부위의 장비 거절
            run.bagId = null; // 확장 없는 빈 가방
            Assert.That(run.IsValid(), Is.False); // 장비와 불일치한 확장 슬롯 거절
            run.version = 10; // 이전 장비 없는 저장
            run.slots = new InventoryState().SaveSlots(); // 기존 기본 슬롯
            Assert.That(run.IsValid(), Is.True); // 이전 버전 호환
        } // 검증 끝
        [Test] // 개별 장비의 회차 전체 중복 검증
        public void Run_RejectsDuplicateVariantAndRetainsLegacyEquipment() // 같은 인스턴스 중복과 이전 저장 확인
        { // 검증 시작
            var item = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 유효한 개별 검
            var inventory = new InventoryState(); // 기본 인벤토리
            Assert.That(inventory.TryAddEquipment(item), Is.True); // 개별 장비 보유
            var run = new RunSaveData { version = 12, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, statPoints = 5, slots = inventory.SaveSlots(), weaponId = EquipmentCatalog.BasicSwordId, weaponVariant = item }; // 같은 검의 보유와 중복 장착
            Assert.That(run.IsValid(), Is.False); // 같은 인스턴스의 중복 보유 차단
            run.hasPendingReward = true; // 누락된 보상 후보가 있다고 표시
            Assert.DoesNotThrow(() => run.IsValid()); // 손상된 후보를 예외 없이 검사
            Assert.That(run.IsValid(), Is.False); // 누락된 보상 후보 거절
            run.hasPendingReward = false; // 정상 보상 상태 복원
            run.weaponVariant = null; // 장착 장비의 개별 상태 제거
            Assert.That(run.IsValid(), Is.True); // 이전 고정 장비와 새로운 보유 장비 허용
            run.slots[0].equipment.options[0].optionId = "unknown"; // 알 수 없는 추가 옵션
            Assert.That(run.IsValid(), Is.False); // 손상된 옵션 저장 거절
        } // 검증 끝
        [Test] // 유물 저장의 중복과 이전 회차 호환 검증
        public void Run_RejectsDuplicateRelicsAndPreservesLegacy() // 유물 저장 범위 확인
        { // 검증 시작
            var run = new RunSaveData { version = 13, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, statPoints = 5, slots = new InventoryState().SaveSlots(), relicIds = new[] { TrialRelicCatalog.AttackId, TrialRelicCatalog.AttackId }, shopStocks = new ShopStockData[0] }; // 중복된 유물을 보유한 저장
            Assert.That(run.IsValid(), Is.False); // 같은 유물 중복 차단
            run.relicIds = new[] { TrialRelicCatalog.AttackId }; // 유효한 단일 유물
            Assert.That(run.IsValid(), Is.True); // 정상 유물 저장 허용
            run.relicIds = null; // 누락된 새 유물 목록
            Assert.That(run.IsValid(), Is.False); // 새 저장의 누락 목록 거절
            run.version = 12; // 유물 저장이 없는 이전 버전
            Assert.That(run.IsValid(), Is.True); // 이전 저장의 빈 유물 호환
        } // 검증 끝
        [Test] // 상점과 소지품 사이의 장비 중복 저장 검증
        public void Run_RejectsStockedEquipmentAlreadyOwned() // 품절 전 장비의 중복 보유 차단
        { // 검증 시작
            var item = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 고유 시험 검
            var inventory = new InventoryState(); // 빈 인벤토리
            Assert.That(inventory.TryAddEquipment(item), Is.True); // 개별 장비 보관
            var offer = new ShopOfferData { offerId = "sword", itemId = item.definitionId, price = 100, initialStock = 1, remaining = 1, equipment = item }; // 같은 장비가 남은 상점
            var run = new RunSaveData { version = 13, floor = 1, health = 1, maxHealth = 1, maxMana = 50, mana = 50, level = 1, statPoints = 5, slots = inventory.SaveSlots(), relicIds = new string[0], shopStocks = new[] { new ShopStockData { floor = 1, offers = new[] { offer } } } }; // 장비 중복 회차
            Assert.That(run.IsValid(), Is.False); // 재고와 소지품의 같은 인스턴스 차단
            offer.remaining = 0; // 이미 구매된 상점 상품
            Assert.That(run.IsValid(), Is.True); // 품절된 가격 기록과 보유 장비 허용
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
